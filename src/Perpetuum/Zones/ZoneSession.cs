using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Builders;
using Perpetuum.Common.Loggers;
using Perpetuum.Data;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.IDGenerators;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Log;
using Perpetuum.Modules;
using Perpetuum.Network;
using Perpetuum.Players;
using Perpetuum.Reactive;
using Perpetuum.Robots;
using Perpetuum.Services.Looting;
using Perpetuum.Services.Sessions;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Terraforming;

namespace Perpetuum.Zones
{
    public class ZoneSession : IZoneSession
    {
        public static readonly IZoneSession None = new NullZoneSession();
        private static readonly IIDGenerator<int> _idGenerator = IDGenerator.CreateIntIDGenerator();

        private readonly IZone _zone;
        private readonly ISessionManager _sessionManager;
        private readonly EncryptedTcpConnection _connection;

        public Character Character { get; private set; } = Character.None;
        private Player _player;
        public DateTime DisconnectTime { get; private set; }
        private DateTime _lastReceivedPacketTime;

        public int Id { get; set; }

        private TerrainUpdateNotifier _terrainUpdateNotifier;

        public delegate ZoneSession Factory(IZone zone,Socket socket);

        public ZoneSession(IZone zone,Socket socket,ISessionManager sessionManager)
        {
            Id = _idGenerator.GetNextID();
            _zone = zone;
            _connection = new EncryptedTcpConnection(socket);
            _connection.Received += OnReceived;
            _connection.Disconnected += OnDisconnected;
            _sessionManager = sessionManager;
        }

        public void Start()
        {
            _connection.Receive();
        }

        public void Stop()
        {
            OnStopped();
        }

        public event Action<ZoneSession> Stopped;

        private void OnStopped()
        {
            if (_beamsMonitor != null)
            {
                _beamsMonitor.Dispose();
                _beamsMonitor = null;
            }

            if (_weatherMonitor != null)
            {
                _weatherMonitor.Dispose();
                _weatherMonitor = null;
            }

            Stopped?.Invoke(this);
        }

        public void SendPacket(Packet packet)
        {
            _connection.Send(packet.ToArray());
        }

        public void SendPacket(IBuilder<Packet> packetBuilder)
        {
            if (packetBuilder == null)
                return;

            var packet = packetBuilder.Build();
            SendPacket(packet);
        }

        public AccessLevel AccessLevel { get; private set; }

        private void OnDisconnected(ITcpConnection connection)
        {
            var player = _player;
            if (player == null)
            {
                OnStopped();
            }
            else
            {
                DisconnectTime = DateTime.Now; //for the logs
                LogoutRequest(false);
            }

            WriteFQLog($"Player disconnected. characterId:{(Character != Character.None ? Character.Id : 0)}");
        }

        private void OnReceived(ITcpConnection connection, byte[] packetData)
        {
            _lastReceivedPacketTime = DateTime.Now;
            var executeTime = GlobalTimer.Elapsed;
            var cancelLogout = true;

            var packet = new Packet(packetData);

            try
            {
                switch (packet.Command)
                {
                    case ZoneCommand.AuthUnit:
                    {
                        HandleAuth(packet);
                        break;
                    }
                    case ZoneCommand.ClientUpdate: { HandleClientUpdate(packet); break; }
                    case ZoneCommand.MoveForward: { HandleMoveForward(packet); break; }
                    case ZoneCommand.Ping: { HandlePing(packet); break; }
                    case ZoneCommand.ClosingSocket:
                    {
                        cancelLogout = false;
                        HandleClosingSocket(packet);
                        break;
                    }
                    case ZoneCommand.ControlCommand: { HandleControlCommand(packet); break; }
                    case ZoneCommand.DeployItem: { HandleDeployItem(packet); break; }
                    case ZoneCommand.EnablePVP: { HandleEnablePvp(packet); break; }
                    case ZoneCommand.GangDoodle: { HandleGangDoodle(packet); break; }
                    case ZoneCommand.GetLayer: { HandleGetLayer(packet); break; }
                    case ZoneCommand.SetLayer: { HandleSetLayer(packet); break; } 
                    case ZoneCommand.GetLootList: { HandleGetLootList(packet); break; }
                    case ZoneCommand.TakeLoot: { HandleTakeLoot(packet); break; }
                    case ZoneCommand.PutLoot: { HandlePutLoot(packet); break; }
                    case ZoneCommand.ReleaseLoot: { HandleReleaseLoot(packet); break; }
                    case ZoneCommand.LoadAmmo: { HandleLoadAmmo(packet); break; }
                    case ZoneCommand.UnloadAmmo: { HandleUnloadAmmo(packet); break; }
                    case ZoneCommand.LocalChat: { HandleLocalChat(packet); break; }
                    case ZoneCommand.LockTerrain: { HandleLockTerrain(packet); break; }
                    case ZoneCommand.LockUnit: { HandleLockUnit(packet); break; }
                    case ZoneCommand.SetPrimaryLock: { HandleSetPrimaryLock(packet); break; }
                    case ZoneCommand.RemoveLock: { HandleRemoveLock(packet); break; }
                    case ZoneCommand.GetTerrainLockParameters: { HandleGetTerrainLockParameters(packet); break; }
                    case ZoneCommand.SetTerrainLockParameters: { HandleSetTerrainLockParameters(packet); break; }


                    case ZoneCommand.Logout:
                    {
                        cancelLogout = false;
                        HandleLogout(packet);
                        break;
                    }
                    case ZoneCommand.GetModuleInfo: { HandleGetModuleInfo(packet); break; }
                    case ZoneCommand.ModuleUse: { HandleModuleUse(packet); break; }
                    case ZoneCommand.ModuleUseByCategoryFlag: { HandleModuleUseByCategoryFlags(packet); break; }
                    case ZoneCommand.UseItem: { HandleUseItem(packet); break; }
                    case ZoneCommand.GetMyRobotInfo: { HandleGetMyRobotInfo(packet); break; }

                }
            }
            catch (Exception ex)
            {
                if (ex is PerpetuumException gex)
                {
                    packet.Error = gex.error;
                    LogGenxyException(packet,gex);
                }
                else
                {
                    packet.Error = ErrorCodes.ServerError;
                }
            }

            var workTime = GlobalTimer.Elapsed - executeTime;
            packet.WorkTime = (int)workTime.TotalMilliseconds;
            SendPacket(packet);

            if (cancelLogout)
                CancelLogout(true);
        }

        private void WriteFQLog(string message)
        {
            var info = string.Empty;

            var player = _player;
            if (player != null)
                info = player.InfoString;

            var e = new LogEvent
            {
                LogType = LogType.Info,
                Tag = "FQ",
                Message = $"{info} - {message}"
            };

            Logger.Log(e);
        }

        private void WritePacketLog(Packet packet, string message = null)
        {
            var player = _player;
            player?.WriteFQLog($"({packet.Command}) {message}");
        }

        [Conditional("DEBUG")]
        private void LogGenxyException(Packet packet, PerpetuumException gex)
        {
            var e = new LogEvent
            {
                LogType = LogType.Error,
                Tag = "ZPACKET",
                Message = $"command:{packet.Command} zone:{_zone.Id} player:{_player.InfoString} ex:{gex}"
            };

            Logger.Log(e);
        }

        private BeamsMonitor _beamsMonitor;
        private Observer<Packet> _weatherMonitor;

        private void HandleAuth(Packet packet)
        {
            packet.ReadInt(); // mar nem kell
            var count = (int)(packet.Length - packet.Position) - sizeof(long);
            var encrypted = packet.ReadBytes(count);

            var character = ZoneTicket.GetCharacterFromEncryptedTicket(encrypted);
            character.ThrowIfEqual(null, ErrorCodes.WTFErrorMedicalAttentionSuggested);
            Logger.Info($"Socket authentication successful. zone: {_zone.Id} character: {character.Id}");
            Character = character;
            AccessLevel = character.AccessLevel;

            if (!_zone.TryGetPlayer(character, out Player player))
            {
                // nincs kint a terepen ezert betoltjuk
                player = Player.LoadPlayerAndAddToZone(_zone, character);
            }

            var session = player.Session as ZoneSession;
            session?.OnStopped();

            _beamsMonitor = new BeamsMonitor(this);
            _beamsMonitor.Subscribe(_zone.Beams);

            _weatherMonitor = Observer<Packet>.Create(OnWeatherUpdated);
            _zone.Weather.Subscribe(_weatherMonitor);
            
            _terrainUpdateNotifier = CreateTerrainNotifier(player);

            player.SetSession(this);
            player.SendInitSelf();
            player.ApplyTeleportSicknessEffect();
            player.ApplyInvulnerableEffect();

            _player = player;
        }

        private void OnWeatherUpdated(Packet weatherUpdatePacket)
        {
            SendPacket(weatherUpdatePacket);
        }

        private TerrainUpdateNotifier CreateTerrainNotifier(Player player)
        {
            var layerTypes = _zone.Configuration.Terraformable ? new[] { LayerType.Altitude, LayerType.Blocks, LayerType.Control, LayerType.Plants } : 
                                                                 new[] { LayerType.Blocks, LayerType.Plants, LayerType.Control };

            return new TerrainUpdateNotifier(_zone,player,layerTypes);
        }

        private void HandleClientUpdate(Packet packet)
        {
            var player = _player;
            if (player == null)
                return;

            player.States.InMoveable.ThrowIfTrue(ErrorCodes.InvalidMovement);
            var position = packet.ReadPosition();
            var speed = (float)packet.ReadByte() / 255;
            var direction = (float)packet.ReadByte() / 255;

            if ( !player.IsWalkable(position) )
                throw new PerpetuumException(ErrorCodes.InvalidMovement);

            player.CurrentPosition = position;
            player.CurrentSpeed = speed;
            player.Direction = direction;
        }

        private void HandleMoveForward(Packet packet)
        {
            var direction = (double)packet.ReadUShort() / ushort.MaxValue;
            var speed = (double)packet.ReadUShort() / ushort.MaxValue;

            _player.Direction = direction;
            _player.CurrentSpeed = speed;
        }

        private static void HandlePing(Packet packet)
        {
            packet.PeekLong(5);
            packet.PutLong(13, (long) GlobalTimer.Elapsed.TotalMilliseconds);
        }

        private void HandleClosingSocket(Packet packet)
        {
            WritePacketLog(packet);
            Disconnect();
        }

        private void HandleControlCommand(Packet packet)
        {
        }

        private void HandleLockUnit(Packet packet)
        {
            var targetEid = packet.ReadLong();
            var isPrimary = packet.ReadByte() != 0;

            WritePacketLog(packet, $"target = {targetEid} primary = {isPrimary}");
            _player.AddLock(targetEid, isPrimary);
        }

        private void HandleLockTerrain(Packet packet)
        {
            var x = packet.ReadInt();
            var y = packet.ReadInt();
            packet.ReadInt(); // z
            var z = _zone.GetZ(x, y);
            var location = new Position(x + 0.5, y + 0.5, z);

            var isPrimary = packet.ReadByte() != 0;

            WritePacketLog(packet, $"target = {location} primary = {isPrimary}");
            var terrainLock = new TerrainLock(_player, location) { Primary = isPrimary };

            _player.AddLock(terrainLock);
        }

        private void HandleGetTerrainLockParameters(Packet packet)
        {
            var id = packet.ReadLong();

            var terrainLock = _player.GetLock(id).ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLock);
            var builder = new TerrainLockParametersPacketBuilder(terrainLock);
            _player.Session.SendPacket(builder);
        }

        private void HandleSetTerrainLockParameters(Packet packet)
        {
            var id = packet.ReadLong();
            var terraformType = (TerraformType)packet.ReadByte();
            var terraformDirection = (TerraformDirection)packet.ReadByte();
            var radius = packet.ReadByte();
            var falloff = packet.ReadByte();

            var terrainLock = _player.GetLock(id).ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLock);

            terrainLock.TerraformType = terraformType;
            terrainLock.TerraformDirection = terraformDirection;
            terrainLock.Radius = radius;
            terrainLock.Falloff = falloff;

            var builder = new TerrainLockParametersPacketBuilder(terrainLock);
            _player.Session.SendPacket(builder);
        }

        private void HandleDeployItem(Packet packet)
        {
            var itemEid = packet.ReadLong();
            var argsCount = packet.ReadInt();
            var binaryStream = new BinaryStream(packet.ReadBytes(argsCount));

            _player.HasTeleportSicknessEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInTeleportSickness);

            using (var scope = Db.CreateTransaction())
            {
                var container = _player.GetContainer();
                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();

                var item = container.GetItemOrThrow(itemEid);

                var itemDeployer = item.ThrowIfNotType<ItemDeployerBase>(ErrorCodes.DefinitionNotSupported);
                if (itemDeployer is FieldContainerCapsule capsule)
                    capsule.PinCode = binaryStream.ReadInt();

                itemDeployer.Deploy(_zone, _player);

                if (item.ED.AttributeFlags.Consumable)
                {
                    var tmpItem = container.RemoveItem(item, 1).ThrowIfNull(ErrorCodes.ItemNotFound);
                    Entity.Repository.Delete(tmpItem);
                    container.Save();
                }

                Transaction.Current.OnCommited(() => container.SendUpdateToOwner());
                scope.Complete();
            }
        }

        private void HandleEnablePvp(Packet packet)
        {
            _zone.Configuration.Type.ThrowIfEqual(ZoneType.Training, ErrorCodes.NoPvpInTraining);
            Logger.Info($"Pvp enabled. zone:{_zone.Id} player:{_player.InfoString}");

            WritePacketLog(packet);
            _player.ApplyPvPEffect();
        }

        private void HandleGangDoodle(Packet packet)
        {
            var gang = _player.Gang;
            if (gang == null)
                return;

            packet.ReadLong();
            var doodleData = packet.ReadBytes(8);

            using (var doodlePacket = new Packet(ZoneCommand.GangDoodle))
            {
                doodlePacket.AppendLong(_player.Eid);
                doodlePacket.AppendByteArray(doodleData);
                _zone.SendPacketToGang(gang, doodlePacket, _player.Eid);
            }
        }

        private void HandleGetLayer(Packet packet)
        {
           
            _player.Session.AccessLevel.IsAdminOrGm().ThrowIfFalse(ErrorCodes.AccessDenied);

            var layerType = (LayerType)packet.ReadByte();
            var materialType = (MaterialType)packet.ReadByte();
            var x1 = packet.ReadInt();
            var y1 = packet.ReadInt();
            var x2 = packet.ReadInt();
            var y2 = packet.ReadInt();
            var area = new Area(x1, y1, x2, y2);

            WritePacketLog(packet, $"type = {layerType} mtype = {materialType} area = {area}");

            var p = _zone.Terrain.BuildLayerUpdatePacket(layerType, area);
            if (p != null)
            {
                _player.Session.SendPacket(p);
            }
        }

        private void HandleGetLootList(Packet packet)
        {
            var pinCode = packet.ReadInt();
            var containerEid = packet.ReadLong();

            WritePacketLog(packet, $"pin = {LootHelper.PinToString(pinCode)} guid = {containerEid}");

            var container = _zone.GetUnit(containerEid) as LootContainer;
            container?.SendLootListToPlayer(_player, pinCode);
        }

        private void HandleGetModuleInfo(Packet packet)
        {
            var robotComponentType = (RobotComponentType)packet.ReadByte();
            var slot = packet.ReadByte();

            WritePacketLog(packet, $"rc = {robotComponentType} s = {slot}");

            var robotComponent = _player.GetRobotComponent(robotComponentType).ThrowIfNull(ErrorCodes.RobotComponentNotSupplied);
            var module = robotComponent.GetModule(slot);

            using (var infoPacket = module.BuildModuleInfoPacket())
            {
                _player.Session.SendPacket(infoPacket);
            }
        }

        private void HandleLoadAmmo(Packet packet)
        {
            var ammoDefinition = packet.ReadInt();
            var robotComponentType = (RobotComponentType)packet.ReadByte();
            var slot = packet.ReadByte();

            WritePacketLog(packet, $"d = {ammoDefinition} rc = {robotComponentType} s = {slot}");

            var component = _player.GetRobotComponent(robotComponentType).ThrowIfNull(ErrorCodes.RobotComponentNotSupplied);
            var module = component.GetModule(slot).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);

            if (!module.IsAmmoable)
                return;

            var ammo = module.GetAmmo();

            if (ammoDefinition == 0)
            {
                if (ammo != null)
                    module.State.UnloadAmmo();
            }
            else
            {
                if (ammo?.Definition == ammoDefinition && ammo.Quantity == module.AmmoCapacity)
                    return;

                module.CheckLoadableAmmo(ammoDefinition).ThrowIfFalse(ErrorCodes.InvalidAmmoDefinition);

                if (module.ParentRobot is Player player)
                {
                    var tmpAmmo = (Ammo)Entity.Factory.CreateWithRandomEID(ammoDefinition);
                    tmpAmmo.CheckEnablerExtensionsAndThrowIfFailed(player.Character, ErrorCodes.ExtensionLevelMismatchTerrain);
                }

                module.State.LoadAmmo(ammoDefinition);
            }
        }

        private const int MAX_MESSAGE_LENGTH = 200;

        private void HandleLocalChat(Packet packet)
        {
            _player.Character.GlobalMuted.ThrowIfTrue(ErrorCodes.CharacterIsMuted);

            packet.Skip(4);

            var message = packet.ReadUtf8String();

            WriteLocalChatLog(_player, message);

            if (message.Length > MAX_MESSAGE_LENGTH)
            {
                message = message.Substring(0, MAX_MESSAGE_LENGTH);
            }

            using (var chatPacket = new Packet(ZoneCommand.LocalChat))
            {
                chatPacket.AppendInt(_player.Character.Id);
                chatPacket.AppendUtf8String(message);

                _player.SendPacketToWitnessPlayers(chatPacket, true);
            }
        }

        private void WriteLocalChatLog(Player sender, string message)
        {
            var cell = sender.CurrentPosition.ToCellCoord();
            _zone.ChatLogger.LogMessage(sender.Character, $"[{cell}] {message}");
        }

        private void HandleLogout(Packet packet)
        {
            WritePacketLog(packet);

            _player.States.Combat.ThrowIfTrue(ErrorCodes.RobotInCombat);
            _player.HasPvpEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInPvp);

            LogoutRequest(true);
        }

        private void HandleModuleUse(Packet packet)
        {
            var lockId = packet.ReadLong();
            var robotComponentType = (RobotComponentType)packet.ReadByte();
            var slot = packet.ReadByte();
            var moduleState = (ModuleStateType)packet.ReadByte();

            WritePacketLog(packet, $"lockId = {lockId} rc = {robotComponentType} s = {slot} state = {moduleState}");

            var component = _player.GetRobotComponent(robotComponentType).ThrowIfNull(ErrorCodes.RobotComponentNotSupplied);
            var module = component.GetModule(slot).ThrowIfNotType<ActiveModule>(ErrorCodes.ModuleNotFound);

            if (module.IsAmmoable)
            {
                var ammo = module.GetAmmo();
                if (ammo == null || ammo.Definition == 0)
                {
                    _player.SendModuleProcessError(module, ErrorCodes.AmmoNotFound);
                    return;
                }
            }

            module.Lock = _player.GetLock(lockId);
            module.State.SwitchTo(moduleState);
        }

        private void HandleModuleUseByCategoryFlags(Packet packet)
        {
            var lockId = packet.ReadLong();
            var cf = (CategoryFlags)packet.ReadLong();
            var moduleState = (ModuleStateType)packet.ReadByte();

            WritePacketLog(packet, $"lockId = {lockId} cf = {cf} state = {moduleState}");

            foreach (var module in _player.ActiveModules)
            {
                if (!module.IsCategory(cf))
                    continue;

                if (module.IsAmmoable)
                {
                    var ammo = module.GetAmmo();
                    if (ammo == null || ammo.Quantity == 0)
                        continue;
                }

                var lockTarget = module.ED.AttributeFlags.PrimaryLockedTarget ? _player.GetPrimaryLock().ThrowIfNull(ErrorCodes.PrimaryLockTargetNotFound) :
                    _player.GetLock(lockId).ThrowIfNull(ErrorCodes.LockTargetNotFound);

                module.Lock = lockTarget;

                try
                {
                    module.State.SwitchTo(moduleState);
                }
                catch (PerpetuumException gex)
                {
                    _player.SendModuleProcessError(module, gex.error);
                }
            }
        }

        private void HandlePutLoot(Packet packet)
        {
            var pinCode = packet.ReadInt();
            var containerEid = packet.ReadLong();
            var count = packet.ReadInt();

            WritePacketLog(packet, $"pin = {LootHelper.PinToString(pinCode)} guid = {containerEid} count = {count}");

            var items = new List<KeyValuePair<long, int>>();

            for (var i = 0; i < count; i++)
            {
                var itemEid = packet.ReadLong();
                var qty = packet.ReadInt();
                items.Add(new KeyValuePair<long, int>(itemEid, qty));
            }

            var container = _zone.GetUnit(containerEid) as FieldContainer;
            container?.PutLoots(_player, pinCode, items);
        }

        private void HandleReleaseLoot(Packet packet)
        {
            var pinCode = packet.ReadInt();
            var containerEid = packet.ReadLong();
            WritePacketLog(packet, $"pin = {LootHelper.PinToString(pinCode)} guid = {containerEid}");

            var container = _zone.GetUnit(containerEid) as LootContainer;
            container?.ReleaseLootContainer(_player);
        }

        private void HandleRemoveLock(Packet packet)
        {
            var lockId = packet.ReadLong();
            WritePacketLog(packet, $"lockId = {lockId}");
            _player.CancelLock(lockId);
        }

        private void HandleSetLayer(Packet packet)
        {
            _player.Session.AccessLevel.IsAdminOrGm().ThrowIfFalse(ErrorCodes.AccessDenied);

            using (new TerrainUpdateMonitor(_zone))
            {
                _zone.Terrain.UpdateAreaFromPacket(packet);
            }
        }

        private void HandleSetPrimaryLock(Packet packet)
        {
            var lockId = packet.ReadLong();
            WritePacketLog(packet, $"lockId = {lockId}");
            _player.SetPrimaryLock(lockId);
        }

        private void HandleTakeLoot(Packet packet)
        {
            var pinCode = packet.ReadInt();
            var containerEid = packet.ReadLong();

            WritePacketLog(packet, $"pin = {LootHelper.PinToString(pinCode)} guid = {containerEid}");

            var items = new List<KeyValuePair<Guid, int>>();

            while (!packet.AtEnd())
            {
                var lootId = packet.ReadGuid();
                var count = packet.ReadInt();
                items.Add(new KeyValuePair<Guid, int>(lootId, count));
            }

            var container = _zone.GetUnit(containerEid) as LootContainer;
            container?.TakeLoots(_player, pinCode, items);
        }

        private void HandleUnloadAmmo(Packet packet)
        {
            var robotComponent = (RobotComponentType)packet.ReadByte();
            var slot = packet.ReadByte();

            WritePacketLog(packet, $"rc = {robotComponent} s = {slot}");

            var component = _player.GetRobotComponent(robotComponent);
            var module = component?.GetModule(slot) as ActiveModule;
            if (module == null)
                return;

            using (var scope = Db.CreateTransaction())
            {
                var container = _player.GetContainer();
                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();
                module.UnequipAmmoToContainer(container);

                module.Save();
                container.Save();

                Transaction.Current.OnCompleted(c =>
                {
                    container.SendUpdateToOwner();
                });

                scope.Complete();
            }
        }

        private void HandleUseItem(Packet packet)
        {
            var itemEid = packet.ReadLong();

            var usableItem = _zone.GetUnit(itemEid) as IUsableItem;
            usableItem?.UseItem(_player);
        }

        private void HandleGetMyRobotInfo(Packet packet)
        {
            var builder = new RobotInfoPacketBuilder(_player);
            _player.Session.SendPacket(builder);
        }

        [CanBeNull]
        private IntervalTimer _logoutTimer;
        private bool _safeLogout;
        private readonly object _logoutSync = new object();

        private static readonly TimeSpan _pveLogoutTime = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan _pvpLogoutTime = TimeSpan.FromMinutes(2);

        private void LogoutRequest(bool safeLogout)
        {
            var player = _player;
            if (player == null)
                return;

            lock (_logoutSync)
            {
                if (_logoutTimer != null)
                    return;

                _safeLogout = safeLogout;

                if (player.HasPvpEffect)
                    player.StopAllModules();

                var logoutTime = player.IsInSafeArea ? _pveLogoutTime : _pvpLogoutTime;

                var pvpEffect = player.EffectHandler.GetEffectsByType(EffectType.effect_pvp).FirstOrDefault();
                var effectTimer = pvpEffect?.Timer;
                if (effectTimer != null)
                    logoutTime = logoutTime.Max(effectTimer.Remaining);

                _logoutTimer = new IntervalTimer(logoutTime);

                // mennyi ido mulva
                SendStartLogoutPacket(_logoutTimer);
            }
        }

        private void SendStartLogoutPacket([NotNull]IntervalTimer logoutTimer)
        {
            var packet = new Packet(ZoneCommand.StartLogout);
            packet.AppendInt((int)logoutTimer.Interval.TotalMilliseconds);
            SendPacket(packet);
        }

        private void SendCancelLogoutPacket()
        {
            var packet = new Packet(ZoneCommand.CancelLogout);
            SendPacket(packet);
        }

        public void CancelLogout()
        {
            CancelLogout(false);
        }

        private void CancelLogout(bool force, bool sendPacket = true)
        {
            if (_logoutTimer == null || (!force && !_safeLogout))
                return;

            _safeLogout = false;
            _logoutTimer = null;

            if (!sendPacket)
                return;
            // itt is kuldunk packetet,h megszakadt
            SendCancelLogoutPacket();
        }

        public void ResetLogoutTimer()
        {
            if (_logoutTimer == null)
                return;

            _logoutTimer.Reset();
            SendStartLogoutPacket(_logoutTimer);
        }

        public void SendTerrainData()
        {
            _terrainUpdateNotifier.ForceUpdateGrids();
        }

        public void SendBeam(IBuilder<Beam> builder)
        {
            SendBeam(builder.Build());
        }

        public void SendBeam(Beam beam)
        {
            if (beam.Type == BeamType.undefined)
                return;

            SendPacket(new BeamPacketBuilder(beam));
        }

        public void EnqueueLayerUpdates(IReadOnlyCollection<TerrainUpdateInfo> infos)
        {
            _terrainUpdateNotifier.EnqueueNewUpdates(infos);
        }

        private bool _isInLogout;

        private void UpdateLogout(TimeSpan time)
        {
            if (_logoutTimer == null)
                return;

            _logoutTimer.Update(time);

            if (!_logoutTimer.Passed)
                return;

            _logoutTimer = null;

            if (_isInLogout)
                return;

            _isInLogout = true;

            Task.Run(() => LogoutPlayer());
        }

        private void LogoutPlayer()
        {
            var character = Character;

            using (var scope = Db.CreateTransaction())
            {
                _player.Save();
                character.ZoneId = _zone.Id;
                character.ZonePosition = _player.CurrentPosition;

                _player.RemoveFromZone();
                _player.SetSession(None);

                _sessionManager.DeselectCharacter(character);
                scope.Complete();
            }

            Disconnect();
            OnStopped();
        }

        public void Disconnect()
        {
            LogoutRequest(false);
            _connection?.Disconnect();
        }

        public TimeSpan InactiveTime
        {
            get { return DateTime.Now.Subtract(_lastReceivedPacketTime); }
        }

        public void Update(TimeSpan time)
        {
            _terrainUpdateNotifier?.Update();
            _beamsMonitor?.Update();

            UpdateLogout(time);
        }

        private class BeamsMonitor : Observer<Beam>
        {
            private readonly ZoneSession _session;
            private readonly ConcurrentQueue<Beam> _beams = new ConcurrentQueue<Beam>();

            public BeamsMonitor(ZoneSession session)
            {
                _session = session;
            }

            public override void OnNext(Beam beam)
            {
                _beams.Enqueue(beam);
            }

            public void Update()
            {
                var player = _session._player;
                if (player == null)
                    return;

                while (_beams.TryDequeue(out Beam beam))
                {
                    _session.SendBeamIfVisible(beam);
                }
            }

            protected override void OnDispose()
            {
                _beams.Clear();
                base.OnDispose();
            }
        }

        public void SendBeamIfVisible(Beam beam)
        {
            var player = _player;
            if (player == null)
                return;

            if (player.IsInRangeOf3D(beam.SourcePosition, beam.Visibility) || player.IsInRangeOf3D(beam.TargetPosition, beam.Visibility))
            {
                SendBeam(beam);
            }
        }
    }
}