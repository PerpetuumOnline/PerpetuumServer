using Perpetuum.Accounting.Characters;
using Perpetuum.GenXY;
using Perpetuum.Groups.Corporations;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Teleporting.Strategies;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.Channels.ChatCommands
{
    public class ChatCommand: Attribute
    {
        public string Command { get; private set; }
        public ChatCommand(string command)
        {
            Command = command.ToLower();
        }
    }


    public static class AdminCommandHandlers
    {
        #region helpers
        private static bool IsDevModeEnabled(AdminCommandData data)
        {
            return data.IsDevMode;
        }
        private static void SendMessageToAll(AdminCommandData data, string message)
        {
            data.Channel.SendMessageToAll(data.SessionManager, data.Sender, message);
        }
        private static void HandleLocalRequest(AdminCommandData data, string cmd)
        {
            data.Request.Session.HandleLocalRequest(data.Request.Session.CreateLocalRequest(cmd));
        }
        private static void ZoneSetTilesControl(AdminCommandData data, TerrainControlFlags flag)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool.TryParse(data.Command.Args[0], out bool adddelete);
            bool.TryParse(data.Command.Args[1], out bool keeplock);

            var character = data.Request.Session.Character;
            var zone = data.Request.Session.ZoneMgr.GetZone((int)character.ZoneId);
            var player = zone.GetPlayer(character.ActiveRobotEid);

            var lockedtiles = player.GetLocks();

            using (new TerrainUpdateMonitor(zone))
            {
                foreach (Lock item in lockedtiles)
                {
                    Position pos = (item as TerrainLock).Location;
                    TerrainControlInfo ti = zone.Terrain.Controls.GetValue(pos);
                    ti.SetFlags(flag, adddelete);
                    zone.Terrain.Controls.SetValue(pos, ti);
                    if (!keeplock)
                    {
                        item.Cancel(); // cancel this lock. we processed it.
                    }
                }
            }
            SendMessageToAll(data, string.Format("Altered state of control layer on {0} Tiles ({1}: set to {2})", lockedtiles.Count, flag, adddelete));
        }
        public static void Unknown(AdminCommandData data)
        {
            SendMessageToAll(data, $"Unknown command: {data.Command.Name}");
        }
        #endregion
        #region AdminCommands
        [ChatCommand("Secure")]
        public static void Secure(AdminCommandData data)
        {
            data.Channel.SetAdmin(true);
            data.Channel.SendMessageToAll(data.SessionManager, data.Sender, "Channel Secured.");
        }
        [ChatCommand("UnSecure")]
        public static void UnSecure(AdminCommandData data)
        {
            data.Channel.SetAdmin(false);
            data.Channel.SendMessageToAll(data.SessionManager, data.Sender, "Channel is now public.");
        }
        [ChatCommand("Shutdown")]
        public static void Shutdown(AdminCommandData data)
        {
            DateTime shutdownin = DateTime.Now;
            int minutes = 1;
            if (!int.TryParse(data.Command.Args[1], out minutes))
            {
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }
            shutdownin = shutdownin.AddMinutes(minutes);

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                { "message", data.Command.Args[0] },
                { "date", shutdownin }
                };

            string cmd = string.Format("serverShutDown:relay:{0}", GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ShutdownCancel")]
        public static void ShutdownCancel(AdminCommandData data)
        {
            string cmd = string.Format("serverShutDownCancel:relay:null");
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("JumpTo")]
        public static void JumpTo(AdminCommandData data)
        {
            bool err = false; //TODO this only throws on bad last-arg!
            err = !int.TryParse(data.Command.Args[0], out int zone);
            err = !int.TryParse(data.Command.Args[1], out int x);
            err = !int.TryParse(data.Command.Args[2], out int y);
            if (err)
            {
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "zoneID" , zone },
                    { "x" , x },
                    { "y" , y }
                };

            string cmd = string.Format("jumpAnywhere:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("MovePlayer")]
        public static void MovePlayer(AdminCommandData data)
        {
            bool err = false; //TODO this parse checking is broken
            err = !int.TryParse(data.Command.Args[0], out int characterID);
            err = !int.TryParse(data.Command.Args[1], out int zoneID);
            err = !int.TryParse(data.Command.Args[2], out int x);
            err = !int.TryParse(data.Command.Args[3], out int y);
            if (err)
            {
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            // get the target character's session.
            var charactersession = data.SessionManager.GetByCharacter(characterID);

            if (charactersession.Character.ZoneId == null)
            {
                SendMessageToAll(data, string.Format("ERR: Character with ID {0} does not have a zone. Are they docked?", characterID));
                return;
            }

            // get destination zone.
            var zone = data.Request.Session.ZoneMgr.GetZone(zoneID);

            if (charactersession.Character.ZoneId == null)
            {
                SendMessageToAll(data, string.Format("ERR: Invalid Zone ID {0}", zoneID));
                return;
            }

            // get a teleporter object to teleport the player.
            TeleportToAnotherZone tp = new TeleportToAnotherZone(zone);

            // we need the player (robot, etc) to teleport on the origin zone
            var player = data.Request.Session.ZoneMgr.GetZone((int)charactersession.Character.ZoneId).GetPlayer(charactersession.Character.ActiveRobotEid);
            //var player = zone.GetPlayer(charactersession.Character.Eid);

            // set the position.
            tp.TargetPosition = new Position(x, y);

            // do it.
            tp.DoTeleportAsync(player);
            tp = null;

            SendMessageToAll(data, string.Format("Moved Character {0}-{1} to Zone {2} @ {3},{4}", characterID, charactersession.Character.Nick, zone.Id, x, y));
        }
        [ChatCommand("GiveItem")]
        public static void GiveItem(AdminCommandData data)
        {
            int.TryParse(data.Command.Args[0], out int definition);
            int.TryParse(data.Command.Args[1], out int qty);

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", definition },
                    { "quantity", qty }
                };

            string cmd = string.Format("createItem:relay:{0}", GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
            SendMessageToAll(data, string.Format("Gave Item {0} ", definition));
        }
        [ChatCommand("GetLockedTileProperties")]
        public static void GetLockedTileProperties(AdminCommandData data)
        {
            var character = data.Request.Session.Character;
            var zone = data.Request.Session.ZoneMgr.GetZone((int)character.ZoneId);
            var player = zone.GetPlayer(character.ActiveRobotEid);

            var lockedtile = player.GetPrimaryLock();

            TerrainControlInfo ti = zone.Terrain.Controls.GetValue((lockedtile as TerrainLock).Location);

            SendMessageToAll(data, string.Format("Tile at {0},{1} has the following flags..", (lockedtile as TerrainLock).Location.X, (lockedtile as TerrainLock).Location.Y));
            SendMessageToAll(data, "TerrainControlFlags:");
            foreach (TerrainControlFlags f in Enum.GetValues(typeof(TerrainControlFlags)))
            {
                if (ti.Flags.HasFlag(f) && f != TerrainControlFlags.Undefined)
                {
                    SendMessageToAll(data, string.Format("{0}", f.ToString()));
                }
            }

            BlockingInfo bi = zone.Terrain.Blocks.GetValue((lockedtile as TerrainLock).Location);

            SendMessageToAll(data, "BlockingFlags:");
            foreach (BlockingFlags f in Enum.GetValues(typeof(BlockingFlags)))
            {
                if (bi.Flags.HasFlag(f) && f != BlockingFlags.Undefined)
                {
                    SendMessageToAll(data, string.Format("{0}", f.ToString()));
                }
            }

            PlantInfo pi = zone.Terrain.Plants.GetValue((lockedtile as TerrainLock).Location);

            SendMessageToAll(data, "PlantType:");
            foreach (PlantType f in Enum.GetValues(typeof(PlantType)))
            {
                if (pi.type.HasFlag(f) && f != PlantType.NotDefined)
                {
                    SendMessageToAll(data, string.Format("{0}", f.ToString()));
                }
            }

            SendMessageToAll(data, "GroundType:");
            foreach (GroundType f in Enum.GetValues(typeof(GroundType)))
            {
                if (pi.groundType.HasFlag(f))
                {
                    SendMessageToAll(data, string.Format("{0}", f.ToString()));
                }
            }
        }
        [ChatCommand("SetVisibility")]
        public static void SetVisibility(AdminCommandData data)
        {
            bool.TryParse(data.Command.Args[0], out bool visiblestate);

            var character = data.Request.Session.Character;
            var zone = data.Request.Session.ZoneMgr.GetZone((int)character.ZoneId);
            var player = zone.GetPlayer(character.ActiveRobotEid);

            player.HasGMStealth = !visiblestate;

            SendMessageToAll(data, string.Format("Player {0} visibility is {1}", player.Character.Nick, visiblestate));
        }
        [ChatCommand("ZoneDrawStatMap")]
        public static void ZoneDrawStatMap(AdminCommandData data)
        {
            //TODO check args, maybe allow zone id, fallback to sender zone
            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "type", data.Command.Args[0] }
                };

            string cmd = string.Format("zoneDrawStatMap:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ListAllPlayersInZone")]
        public static void ListAllPlayersInZone(AdminCommandData data)
        {
            int.TryParse(data.Command.Args[0], out int zoneid);

            SendMessageToAll(data, string.Format("Players On Zone {0}", zoneid));
            SendMessageToAll(data, string.Format("  AccountId    CharacterId    Nick    Access Level    Docked?    DockedAt    Position"));
            foreach (Character c in data.SessionManager.SelectedCharacters.Where(x => x.ZoneId == zoneid))
            {
                SendMessageToAll(data, string.Format("   {0}       {1}        {2}        {3}       {4}       {5}      {6}",
                    c.AccountId, c.Id, c.Nick, c.AccessLevel, c.IsDocked, c.GetCurrentDockingBase().Eid, c.GetPlayerRobotFromZone().CurrentPosition));
            }
        }
        [ChatCommand("CountOfPlayers")]
        public static void CountOfPlayers(AdminCommandData data)
        {
            foreach (IZone z in data.Request.Session.ZoneMgr.Zones)
            {
                SendMessageToAll(data, string.Format("Players On Zone {0}: {1}", z.Id, z.Players.ToList().Count));
            }
        }
        [ChatCommand("AddToChannel")]
        public static void AddToChannel(AdminCommandData data)
        {
            int.TryParse(data.Command.Args[0], out int characterid);

            var c = data.SessionManager.GetByCharacter(characterid);

            data.ChannelManager.JoinChannel(data.Channel.Name, c.Character, ChannelMemberRole.Operator, string.Empty);

            SendMessageToAll(data, string.Format("Added character {0} to channel ", c.Character.Nick));
        }
        [ChatCommand("RemoveFromChannel")]
        public static void RemoveFromChannel(AdminCommandData data)
        {
            int.TryParse(data.Command.Args[0], out int characterid);

            var c = data.SessionManager.GetByCharacter(characterid);

            data.ChannelManager.LeaveChannel(data.Channel.Name, c.Character);

            SendMessageToAll(data, string.Format("Removed character {0} from channel ", c.Character.Nick));
        }
        [ChatCommand("ListRifts")]
        public static void ListRifts(AdminCommandData data)
        {
            foreach (IZone z in data.Request.Session.ZoneMgr.Zones)
            {
                var rift = z.Units.OfType<Rift>();
                foreach (Rift r in rift)
                {
                    SendMessageToAll(data, string.Format("Rift - Zone: {0}, Position: ({1})", r.Zone, r.CurrentPosition));
                }
            }
        }
        [ChatCommand("FlagPlayerNameOffensive")]
        public static void FlagPlayerNameOffensive(AdminCommandData data)
        {
            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int characterID);
            err = !bool.TryParse(data.Command.Args[1], out bool isoffensive);

            //TODO does this work if the character doesnt have a session?
            var charactersession = data.SessionManager.GetByCharacter(characterID);
            charactersession.Character.IsOffensiveNick = isoffensive;

            SendMessageToAll(data, string.Format("Player with nick {0} is offensive:{1}", charactersession.Character.Nick, charactersession.Character.IsOffensiveNick));
        }
        [ChatCommand("RenameCorp")]
        public static void RenameCorp(AdminCommandData data)
        {
            string currentCorpName = data.Command.Args[0];
            string desiredCorpName = data.Command.Args[1];
            string desiredCorpNick = data.Command.Args[2];

            Corporation.IsNameOrNickTaken(desiredCorpName, desiredCorpNick).ThrowIfTrue(ErrorCodes.NameTaken);
            var corp = Corporation.GetByName(currentCorpName);
            corp.SetName(desiredCorpName, desiredCorpNick);

            SendMessageToAll(data, string.Format("Corp with nick {0} changed to: {1} [{2}]", currentCorpName, desiredCorpName, desiredCorpNick));
        }
        [ChatCommand("UnlockAllEP")]
        public static void UnlockAllEP(AdminCommandData data)
        {
            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int accountID);
            if (err)
            {
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { k.accountID, accountID }
                };

            string cmd = string.Format("{0}:relay:{1}", Commands.ExtensionFreeAllLockedEpCommand.Text, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
            SendMessageToAll(data, "unlockallep: " + dictionary.ToDebugString());
        }
        [ChatCommand("EPBonusSet")]
        public static void EPBonusSet(AdminCommandData data)
        {
            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int bonusBoost);
            err = !int.TryParse(data.Command.Args[1], out int hours);
            if (err)
            {
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { k.bonus, bonusBoost },
                    { k.duration, hours }
                };

            string cmd = string.Format("{0}:relay:{1}", Commands.EPBonusSet.Text, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
            SendMessageToAll(data, "EP Bonus Set with command: " + dictionary.ToDebugString());
        }
        [ChatCommand("ListRelics")]
        public static void ListRelics(AdminCommandData data)
        {
            bool err = false;

            var character = data.Request.Session.Character;
            IZone zone = null;

            if (data.Command.Args.Length == 1)
            {
                err = !int.TryParse(data.Command.Args[0], out int zoneCommand);
                if (err)
                {
                    SendMessageToAll(data, "Bad args");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }
                var zoneid = zoneCommand;
                zone = data.Request.Session.ZoneMgr.GetZone((int)zoneid);
            }
            else if (character.ZoneId != null)
            {
                zone = data.Request.Session.ZoneMgr.GetZone((int)character.ZoneId);
            }

            if (zone == null)
            {
                SendMessageToAll(data, "Zone not provided or not found");
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    {"zoneid", zone.Id }
                };
            if (zone.RelicManager != null)
            {
                var relicDictList = zone.RelicManager.GetRelicListDictionary();
                foreach (var dict in relicDictList)
                {
                    SendMessageToAll(data, dict.ToDebugString());
                }
            }
            else
            {
                SendMessageToAll(data, "This zone does NOT support relics!");
            }
        }
        [ChatCommand("SetWeather")]
        public static void SetWeather(AdminCommandData data)
        {
            if (data.Command.Args.Length < 2)
            {
                SendMessageToAll(data, $"bad or missing args");
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }
            var zoneId = data.Sender.ZoneId ?? -1;
            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int weatherInt);
            err = !int.TryParse(data.Command.Args[1], out int seconds);
            if (data.Command.Args.Length > 2)
            {
                err = !int.TryParse(data.Command.Args[2], out zoneId);
            }
            if (!data.Request.Session.ZoneMgr.ContainsZone(zoneId))
            {
                SendMessageToAll(data, $"Bad or missing zone id");
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }
            var zone = data.Request.Session.ZoneMgr.GetZone(zoneId);
            var current = zone.Weather.GetCurrentWeather();
            var weather = new WeatherInfo(current.Next, weatherInt.Min(255), TimeSpan.FromSeconds(seconds));
            zone.Weather.SetCurrentWeather(weather);
            SendMessageToAll(data, $"Weather set {zone.Weather.GetCurrentWeather().ToString()}");
        }
        [ChatCommand("GetWeather")]
        public static void GetWeather(AdminCommandData data)
        {
            var zoneId = data.Sender.ZoneId ?? -1;
            bool err = false;
            if (!data.Command.Args.IsNullOrEmpty())
            {
                err = !int.TryParse(data.Command.Args[0], out zoneId);
            }
            if (!data.Request.Session.ZoneMgr.ContainsZone(zoneId) || err)
            {
                SendMessageToAll(data, $"Bad or missing zone id");
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }
            var zone = data.Request.Session.ZoneMgr.GetZone(zoneId);
            SendMessageToAll(data, $"Weather set {zone.Weather.GetCurrentWeather().ToString()}");
        }
        #endregion
        #region DevCommands
        [ChatCommand("ZoneCleanObstacleBlocking")]
        public static void ZoneCleanObstacleBlocking(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            string cmd = string.Format("zoneCleanObstacleBlocking:zone_{0}:null", data.Sender.ZoneId);
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneDrawBlockingByEid")]
        public static void ZoneDrawBlockingByEid(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            err = !Int64.TryParse(data.Command.Args[0], out Int64 eid);

            if (err)
            {
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "eid", eid }
                };

            string cmd = string.Format("zoneDrawBlockingByEid:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneRemoveObjectByEid")]
        public static void ZoneRemoveObjectByEid(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            err = !Int64.TryParse(data.Command.Args[0], out Int64 eid);

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "target", eid }
                };

            string cmd = string.Format("zoneRemoveObject:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneCreateIsland")]
        public static void ZoneCreateIsland(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int lvl);

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "low", lvl }
                };

            string cmd = string.Format("zoneCreateIsland:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZonePlaceWall")]
        public static void ZonePlaceWall(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            string cmd = string.Format("zonePlaceWall:zone_{0}:null", data.Sender.ZoneId);
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneClearWalls")]
        public static void ZoneClearWalls(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            string cmd = string.Format("zoneClearWalls:zone_{0}:null", data.Sender.ZoneId);
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneAddDecor")]
        public static void ZoneAddDecor(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int definition);
            err = !int.TryParse(data.Command.Args[1], out int x);
            err = !int.TryParse(data.Command.Args[2], out int y);
            err = !int.TryParse(data.Command.Args[3], out int z);
            err = !double.TryParse(data.Command.Args[4], out double qx);
            err = !double.TryParse(data.Command.Args[5], out double qy);
            err = !double.TryParse(data.Command.Args[6], out double qz);
            err = !double.TryParse(data.Command.Args[7], out double qw);
            err = !double.TryParse(data.Command.Args[8], out double scale);
            err = !int.TryParse(data.Command.Args[9], out int cat);

            if (err)
            {
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", definition },
                    { "x", x*256 },
                    { "y", y*256 },
                    { "z", z*256 },
                    { "quaternionX", qx },
                    { "quaternionY", qy },
                    { "quaternionZ", qz },
                    { "quaternionW", qw },
                    { "scale", scale },
                    { "category", cat }
                };

            string cmd = string.Format("zoneDecorAdd:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneAddDecorToLockedTile")]
        public static void ZoneAddDecorToLockedTile(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            var character = data.Request.Session.Character;
            var zone = data.Request.Session.ZoneMgr.GetZone((int)character.ZoneId);
            var player = zone.GetPlayer(character.ActiveRobotEid);

            var terrainLock = player.GetPrimaryLock() as TerrainLock;
            if (terrainLock == null)
            {
                return;
            }

            double x = terrainLock.Location.X;
            double y = terrainLock.Location.Y;
            double z = terrainLock.Location.Z;

            bool err = !double.TryParse(data.Command.Args[1], out double scale);
            err = !int.TryParse(data.Command.Args[0], out int definition);

            if (err)
            {
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", definition },
                    { "x", (int)x*256 },
                    { "y", (int)y*256 },
                    { "z", (int)z*256 },
                    { "quaternionX", (double)0 },
                    { "quaternionY", (double)0 },
                    { "quaternionZ", (double)0 },
                    { "quaternionW", (double)0 },
                    { "scale", scale },
                    { "category", 1 }
                };

            string cmd = string.Format("zoneDecorAdd:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneDeleteDecor")]
        public static void ZoneDeleteDecor(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int idno);

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "ID", idno }
                };

            string cmd = string.Format("zoneDecorDelete:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneClearLayer")]
        public static void ZoneClearLayer(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "layerName", data.Command.Args[0] }
                };

            string cmd = string.Format("zoneClearLayer:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneSetPlantSpeed")]
        public static void ZoneSetPlantSpeed(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int speed);

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "speed", speed }
                };

            string cmd = string.Format("zoneSetPlantsSpeed:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneSetPlantMode")]
        public static void ZoneSetPlantMode(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "mode", data.Command.Args[0] }
                };

            string cmd = string.Format("zoneSetPlantsMode:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneRestoreOriginalGamma")]
        public static void ZoneRestoreOriginalGamma(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "mode", data.Command.Args[0] }
                };

            string cmd = string.Format("zoneRestoreOriginalGamma:zone_{0}:null", data.Sender.ZoneId);
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneDrawBlockingByDefinition")]
        public static void ZoneDrawBlockingByDefinition(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int def);
            int[] defs = new int[1];
            defs[0] = def;

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "definition", defs }
                };

            string cmd = string.Format("zoneDrawBlockingByDefinition:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneAddBlockingToLockedTiles")]
        public static void ZoneAddBlockingToLockedTiles(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            var character = data.Request.Session.Character;
            var zone = data.Request.Session.ZoneMgr.GetZone((int)character.ZoneId);
            var player = zone.GetPlayer(character.ActiveRobotEid);

            var lockedtiles = player.GetLocks();

            using (new TerrainUpdateMonitor(zone))
            {
                foreach (Lock item in lockedtiles)
                {
                    Position pos = (item as TerrainLock).Location;
                    zone.Terrain.Blocks.SetValue(pos, new BlockingInfo() { Obstacle = true });
                    item.Cancel(); // cancel this lock. we processed it.
                }
            }

            SendMessageToAll(data, string.Format("Added Blocking To {0} Tiles.", lockedtiles.Count));
        }
        [ChatCommand("ZoneRemoveBlockingToLockedTiles")]
        public static void ZoneRemoveBlockingToLockedTiles(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            var character = data.Request.Session.Character;
            var zone = data.Request.Session.ZoneMgr.GetZone((int)character.ZoneId);
            var player = zone.GetPlayer(character.ActiveRobotEid);

            var lockedtiles = player.GetLocks();

            using (new TerrainUpdateMonitor(zone))
            {
                foreach (Lock item in lockedtiles)
                {
                    Position pos = (item as TerrainLock).Location;
                    zone.Terrain.Blocks.SetValue(pos, new BlockingInfo() { Obstacle = false });
                    item.Cancel(); // cancel this lock. we processed it.
                }
            }

            SendMessageToAll(data, string.Format("Removed Blocking From {0} Tiles.", lockedtiles.Count));
        }
        [ChatCommand("ZoneLockDecor")]
        public static void ZoneLockDecor(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            err = !int.TryParse(data.Command.Args[0], out int id);
            err = !int.TryParse(data.Command.Args[1], out int locked);

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "ID", id },
                    { "locked", locked }
                };

            string cmd = string.Format("zoneDecorLock:zone_{0}:{1}", data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
        }
        [ChatCommand("ZoneSetTilesHighway")]
        public static void ZoneSetTilesHighway(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.Highway);
        }
        [ChatCommand("ZoneSetTilesConcreteA")]
        public static void ZoneSetTilesConcreteA(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.ConcreteA);
        }
        [ChatCommand("ZoneSetTilesConcreteB")]
        public static void ZoneSetTilesConcreteB(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.ConcreteB);
        }
        [ChatCommand("ZoneSetTilesAntiPlant")]
        public static void ZoneSetTilesAntiPlant(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.AntiPlant);
        }
        [ChatCommand("ZoneSetTilesNPCRestricted")]
        public static void ZoneSetTilesNPCRestricted(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.NpcRestricted);
        }
        [ChatCommand("ZoneSetTilesHighwayPBS")]
        public static void ZoneSetTilesHighwayPBS(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.PBSHighway);
        }
        [ChatCommand("ZoneSetTilesHighwayCombo")]
        public static void ZoneSetTilesHighwayCombo(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.HighWayCombo);
        }
        [ChatCommand("ZoneSetTilesNPCRoaming")]
        public static void ZoneSetTilesNPCRoaming(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.Roaming);
        }
        [ChatCommand("ZoneSetTilesSyndicate")]
        public static void ZoneSetTilesSyndicate(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.SyndicateArea);
        }
        [ChatCommand("ZoneSetTilesTerraformProtect")]
        public static void ZoneSetTilesTerraformProtect(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.TerraformProtected);
        }
        [ChatCommand("ZoneSetTilesTerraformProtectCombo")]
        public static void ZoneSetTilesTerraformProtectCombo(AdminCommandData data)
        {
            ZoneSetTilesControl(data, TerrainControlFlags.TerraformProtectedCombo);
        }
        [ChatCommand("SaveLayers")]
        public static void SaveLayers(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            var dictionary = new Dictionary<string, object>();
            if (data.Command.Args.Length == 1)
            {
                err = !int.TryParse(data.Command.Args[0], out int zoneId);
                if (err)
                {
                    SendMessageToAll(data, "Bad args");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }
                dictionary.Add(k.zoneID, zoneId);
            }
            var cmd = string.Format("{0}:relay:{1}", Commands.ZoneSaveLayer.Text, GenxyConverter.Serialize(dictionary));
            SendMessageToAll(data, $"SaveLayers command accepted: {dictionary.ToDebugString()} \r\nSaving... ");
            HandleLocalRequest(data, cmd);
            SendMessageToAll(data, $"Layer(s) Saved! ");
        }
        [ChatCommand("ZoneIslandBlock")]
        public static void ZoneIslandBlock(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            var dictionary = new Dictionary<string, object>();
            if (data.Command.Args.Length != 1)
            {
                SendMessageToAll(data, "Missing or too many args");
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            err = !int.TryParse(data.Command.Args[0], out int low);
            if (err)
            {
                SendMessageToAll(data, "Bad args");
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }
            dictionary.Add(k.low, low);
            var cmd = string.Format("{0}:zone_{1}:{2}", Commands.ZoneCreateIsland.Text, data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            SendMessageToAll(data, $"Islandblocking command accepted: {dictionary.ToDebugString()} \r\nBlocking... ");
            HandleLocalRequest(data, cmd);
            SendMessageToAll(data, $"Zone water level blocked! ");
        }
        [ChatCommand("ZoneCreateGarden")]
        public static void ZoneCreateGarden(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            var dictionary = new Dictionary<string, object>();
            if (data.Command.Args.Length != 2)
            {
                SendMessageToAll(data, "Missing or too many args");
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }

            err = !int.TryParse(data.Command.Args[0], out int x);
            err = !int.TryParse(data.Command.Args[1], out int y);
            if (err)
            {
                SendMessageToAll(data, "Bad args");
                throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
            }
            dictionary.Add(k.x, x);
            dictionary.Add(k.y, y);
            var cmd = string.Format("{0}:zone_{1}:{2}", Commands.ZoneCreateGarden.Text, data.Sender.ZoneId, GenxyConverter.Serialize(dictionary));
            SendMessageToAll(data, $"Garden command accepted: {dictionary.ToDebugString()} \r\nPlanting... ");
            HandleLocalRequest(data, cmd);
            SendMessageToAll(data, $"Garden Created! ");
        }
        [ChatCommand("TestMissions")]
        public static void TestMissions(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            int.TryParse(data.Command.Args[0], out int charID);
            int.TryParse(data.Command.Args[1], out int zoneID);
            int.TryParse(data.Command.Args[2], out int level);
            int.TryParse(data.Command.Args[3], out int numAttempts);
            int.TryParse(data.Command.Args[4], out int displayFlag);
            int.TryParse(data.Command.Args[5], out int singleFlag);
            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { k.characterID, charID },
                    { k.zone, zoneID },
                    { k.level, level },
                    { "display", displayFlag },
                    { "attempts", numAttempts },
                    { "single", singleFlag },
                };

            string cmd = string.Format("{0}:relay:{1}", Commands.MissionResolveTest.Text, GenxyConverter.Serialize(dictionary));
            HandleLocalRequest(data, cmd);
            SendMessageToAll(data, string.Format("Running missionresolve test {0}", dictionary.ToDebugString()));
        }
        [ChatCommand("SpawnRelic")]
        public static void SpawnRelic(AdminCommandData data)
        {
            if (!IsDevModeEnabled(data))
                return;

            bool err = false;
            var character = data.Request.Session.Character;
            var zone = data.Request.Session.ZoneMgr.GetZone((int)character.ZoneId);
            var player = zone.GetPlayer(character.ActiveRobotEid);

            var terrainLock = player.GetPrimaryLock() as TerrainLock;

            int x, y, zoneid;

            if (terrainLock == null)
            {
                if (data.Command.Args.Length != 3)
                {
                    SendMessageToAll(data, "Bad args");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }
                err = !int.TryParse(data.Command.Args[0], out int xCommand);
                err = !int.TryParse(data.Command.Args[1], out int yCommand);
                err = !int.TryParse(data.Command.Args[2], out int zoneCommand);
                if (err)
                {
                    SendMessageToAll(data, "Bad args");
                    throw PerpetuumException.Create(ErrorCodes.RequiredArgumentIsNotSpecified);
                }
                x = xCommand;
                y = yCommand;
                zoneid = zoneCommand;
                zone = data.Request.Session.ZoneMgr.GetZone((int)zoneid);
                if (zone == null)
                {
                    SendMessageToAll(data, "Bad zone id!");
                }
            }
            else
            {
                x = terrainLock.Location.intX;
                y = terrainLock.Location.intY;
                zoneid = zone.Id;
            }

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
                {
                    { "x", x },
                    { "y", y },
                    {"zoneid", zoneid }
                };
            if (zone.RelicManager != null)
            {
                bool success = zone.RelicManager.ForceSpawnRelicAt(x, y);
                if (success)
                {
                    SendMessageToAll(data, "Spawned relic at: " + dictionary.ToDebugString());
                }
                else
                {
                    SendMessageToAll(data, "FAILED to spawn relic at: " + dictionary.ToDebugString());
                }
            }
            else
            {
                SendMessageToAll(data, "This zone does NOT support relics!");
            }
        }
        #endregion
    }
}
