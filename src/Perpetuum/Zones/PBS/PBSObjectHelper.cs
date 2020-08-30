using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Services.Looting;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.PBS.Connections;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Zones.PBS
{
    public sealed class PBSObjectHelper<T> where T : Unit, IPBSObject
    {
        private readonly T _pbsUnit;
        private readonly UnitOptionalProperty<int> _constructionLevelCurrent;
        private readonly PBSObjectSaver<T> _saver;

        private readonly object _lockObject = new object();
        private bool _contructionLootDropped;

        public IPBSConnectionHandler ConnectionHandler { get; private set; }

        public delegate PBSObjectHelper<T> Factory(T pbsUnit);

        public PBSObjectHelper(T pbsUnit)
        {
            _pbsUnit = pbsUnit;
            _pbsUnit.DamageTaken += OnUnitDamageTaken;
            _pbsUnit.Dead += _pbsUnit_Dead;

            ConnectionHandler = new PBSConnectionHandler<T>(pbsUnit);
            _constructionLevelCurrent = new UnitOptionalProperty<int>(_pbsUnit,UnitDataType.ConstructionLevelCurrent,k.constructionLevelCurrent,() => 1);
            _pbsUnit.OptionalProperties.Add(_constructionLevelCurrent);

            _saver = new PBSObjectSaver<T>(Entity.Repository, TimeSpan.FromMinutes(15));
        }

        private void _pbsUnit_Dead(Unit arg1, Unit arg2)
        {
            _pbsUnit.DamageTaken -= OnUnitDamageTaken;
            _pbsUnit.Dead -= _pbsUnit_Dead;
        }

        private int _damageTaken;

        private void OnUnitDamageTaken(Unit unit, Unit attacker, DamageTakenEventArgs e)
        {
            ConnectionHandler.SendEventToNetwork(new NodeAttackedEventArgs(attacker));

            if (Interlocked.CompareExchange(ref _damageTaken, 1, 0) == 1)
                return;

            Logger.DebugInfo($"PBS node attacked ({_pbsUnit.Eid}) attacker: {attacker.InfoString}");

            if (_pbsUnit.States.Dead)
                return;

            Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(t =>
            {
                try
                {
                    if (!_pbsUnit.States.Dead)
                    {
                        _pbsUnit.SendNodeUpdate(PBSEventType.nodeAttacked);
                    }
                }
                finally
                {
                    _damageTaken = 0;
                }
            });
        }

        public void Init()
        {
            if (_pbsUnit.DynamicProperties.Contains(k.isOnline))
            {
                OnlineStatus = _pbsUnit.DynamicProperties.GetOrAdd<int>(k.isOnline) == 1;
            }
            else
            {
                _pbsUnit.DynamicProperties.Update(k.isOnline,OnlineStatus ? 1 : 0);
            }

            if (_pbsUnit.DynamicProperties.Contains(k.constructionDirection))
            {
                _constructionDirection = _pbsUnit.DynamicProperties.GetOrAdd<int>(k.constructionDirection);
            }
            else
            {
                _pbsUnit.DynamicProperties.Update(k.constructionDirection,0);
            }
        }

        public void OnSave()
        {
            _pbsUnit.DynamicProperties.Update(k.armor,_pbsUnit.Armor.Ratio(_pbsUnit.ArmorMax));
            _pbsUnit.DynamicProperties.Update(k.currentCore,_pbsUnit.Core);
            _pbsUnit.DynamicProperties.Update(k.isOnline,OnlineStatus ? 1 : 0);
            _pbsUnit.DynamicProperties.Update(k.constructionLevelCurrent,ConstructionLevelCurrent);
            _pbsUnit.DynamicProperties.Update(k.constructionDirection,_constructionDirection);
        }

        public void RemoveFromZone(IZone zone)
        {
            Logger.DebugInfo("objecthelper removefromzone");
            Logger.DebugInfo("async helper cucc start");

            Db.CreateTransactionAsync(scope =>
            {
                ConnectionHandler.RemoveAllConnections();
                _pbsUnit.OnPBSObjectRemovedFromZone(zone);
                Logger.DebugInfo("connections remove");
            });
        }

        public void AddToDictionary(IDictionary<string, object> info)
        {
            info.Add(k.constructionLevelCurrent, ConstructionLevelCurrent);
            info.Add(k.isFullyConstructed, IsFullyConstructed);
            info.Add(k.isOnline, OnlineStatus);
            info.Add(k.connections, ConnectionHandler.ToDictionary());
            info.Add(k.armorCurrent, _pbsUnit.Armor.Ratio(_pbsUnit.ArmorMax));
            info.Add(k.constructionDirection, _constructionDirection);

            if (_pbsUnit.DynamicProperties.Contains(k.allowDeconstruction))
            {
                info.Add(k.allowDeconstruction, 1);
            }

            info.Add(k.orphan, IsOrphaned ? 1 : 0);

        }

        private int _bandwidthUsage = -1;

        public int BandwidthUsage
        {
            get { return PBSHelper.LazyInitBandwidthUsage(_pbsUnit, ref _bandwidthUsage); }
        }

        private void ForceDeconstruct()
        {
            _constructionLevelCurrent.Value = 0;
            SetToDeconstruct();
            OnConstructionLevelChanged();
        }

        private ErrorCodes ConstructionLevelChange(int amount, bool force = false)
        {
            if (force)
            {
                ForceDeconstruct();
                return ErrorCodes.NoError;
            }
          
            if (IsConstructionUp() && amount < 0)
            {
                return ErrorCodes.OnlyConstructionPossible;
            }

            var constructionLevelMax = ConstructionLevelMax;

            if (amount > 0)
            {

                if (ConstructionLevelCurrent >= constructionLevelMax)
                {
                    return ErrorCodes.ConstructionLevelMaxReached; //hogy ne hasznaljon ammot
                }
            }

            if (amount < 0)
            {
                if (ConstructionLevelCurrent <= 0)
                {
                    return ErrorCodes.ConstructionLevelMinReached; //hogy ne hasznaljon ammot
                }
            }

            var newLevel = ConstructionLevelCurrent;
            if (ConstructionLevelCurrent < constructionLevelMax && amount > 0 || ConstructionLevelCurrent > 0 && amount < 0)
            {
                newLevel = (ConstructionLevelCurrent + amount).Clamp(0, constructionLevelMax);
            }

            if (newLevel != ConstructionLevelCurrent)
            {
                _constructionLevelCurrent.Value = newLevel;

                lock (_lockObject)
                {
                    OnConstructionLevelChanged();    
                }
            }

            return ErrorCodes.NoError;
        }



        public ErrorCodes ModifyConstructionLevel(int amount, bool force = false)
        {
            var ec = ConstructionLevelChange(amount, force);

            ec.ThrowIfError();

            // %%% ezt itt a pbs saverre lehetne bizni
            /*
            if (_pbsUnit.InZone)
            {
                //if it is still in the zone we schedule a save
                
                Task.Run(() =>
                {
                    Entity.Repository.Update(_pbsUnit);
                });
            }
            */
            return ec;

        }

        public int ConstructionLevelCurrent
        {
            get { return _constructionLevelCurrent.Value; }
        }

        private int _constructionLevelMax;

        public int ConstructionLevelMax
        {
            get { return PBSHelper.LazyInitConstrustionLevelMax(_pbsUnit, ref _constructionLevelMax); }
        }

        private void OnConstructionLevelChanged()
        {
            var zone = _pbsUnit.Zone;
            if (zone == null)
                return;

            var currentLevel = ConstructionLevelCurrent;

            if (currentLevel <= 0)
            {
                //object got deconstructed, remove from zone, add capsule to loot

                if (_contructionLootDropped) 
                    return;

                _contructionLootDropped = true;

                using (var scope = Db.CreateTransaction())
                {
                    LootContainer.Create()
                                 .AddLoot(PBSHelper.GetCapsuleDefinitionByPBSObject(_pbsUnit),1)
                                 .AddLoot(PBSHelper.GetConstructionAmmoLootOnDeconstruct(_pbsUnit))
                                 .BuildAndAddToZone(zone, _pbsUnit.CurrentPosition);

                    var dockingBase = _pbsUnit as PBSDockingBase;
                    if (dockingBase != null)
                    {

                        Logger.DebugInfo("dropping loot from base");
                        PBSHelper.DropLootToZoneFromBaseItems(zone, dockingBase, false);

                    }
                    _pbsUnit.RemoveFromZone();
                   
                    Logger.Info("pbs node got deconstructed. " + _pbsUnit.Eid + " " + _pbsUnit.ED.Name + " owner:" + _pbsUnit.Owner);
                    PBSHelper.WritePBSLog(PBSLogType.deconstructed, _pbsUnit.Eid, _pbsUnit.Definition,_pbsUnit.Owner, zoneId: zone.Id);	

                    scope.Complete();
                }

                return;
            }

            if (!IsFullyConstructed) 
                return;

            SetToDeconstruct(); //felepult, mostmar lehet lebontani is vagy barmi

            PBSHelper.WritePBSLog(PBSLogType.constructed, _pbsUnit.Eid, _pbsUnit.Definition, _pbsUnit.Owner, zoneId: zone.Id);
            SendNodeUpdate();
        }

        private int _constructionDirection;

        private bool IsConstructionUp()
        {
            return _constructionDirection == 0;
        }

        private void SetToDeconstruct()
        {
            _constructionDirection = 1;
        }

        public bool IsFullyConstructed
        {
            get { return ConstructionLevelCurrent >= ConstructionLevelMax; }
        }

        //ellenorizz per request %%% nem kellenek ezek a boolok 
        public void SetOnlineStatus(bool state, bool checkNofBase, bool forcedByServer = false)
        {
            if (OnlineStatus == state)
                return;

            if (!forcedByServer)
            {
                if (!state)
                {

                    if (PBSHelper.IsOfflineOnReinforce(_pbsUnit))
                    {
                        _pbsUnit.IsReinforced().ThrowIfTrue(ErrorCodes.NotPossibleDuringReinforce);
                    }
                }
            }

            if (checkNofBase)
                _pbsUnit.ConnectionHandler.NetworkNodes.Any(n => n is PBSDockingBase).ThrowIfFalse(ErrorCodes.NoBaseInNetwork);

            OnlineStatus = state;
        }

        private bool _onlineStatus;

        public event Action<bool> OnlineStatusChanged;

        private void OnOnlineStatusChanged(bool state)
        {
            _pbsUnit.States.Online = state;

            OnlineStatusChanged?.Invoke(state);
        }

        public bool OnlineStatus
        {
            get => _onlineStatus;
            private set
            {
                if (_onlineStatus == value)
                    return;

                _onlineStatus = value;

                OnOnlineStatusChanged(value);
            }
        }
        
        public void TakeOver(long newOwner)
        {
            var zone = _pbsUnit.Zone;
            if (zone == null)
                return;

            if (newOwner != _pbsUnit.Owner)
            {
                PBSHelper.WritePBSLog(PBSLogType.takeOver, _pbsUnit.Eid, _pbsUnit.Definition, _pbsUnit.Owner, takeOverCorporationEid: newOwner, zoneId:zone.Id);
                _pbsUnit.Owner = newOwner;
            }

            _pbsUnit.IsOrphaned = false;
        }

        public event Action<Unit,bool> OrphanedStateChanged;

        private void OnOrphanedStateChanged(bool orphanState)
        {
            OrphanedStateChanged?.Invoke(_pbsUnit, orphanState);

            _pbsUnit.States.IsOrphaned = orphanState;

            if (orphanState)
            {
                //entering orphan state

                //got orphaned -> gets offline
                SetOnlineStatus(false, false, true);
            }

            var zone = _pbsUnit.Zone;
            if (zone == null)
                return;

            PBSHelper.WritePBSLog(orphanState ? PBSLogType.gotOrphaned : PBSLogType.gotConnected, _pbsUnit.Eid, _pbsUnit.Definition, _pbsUnit.Owner, zoneId: zone.Id);
            SendNodeUpdate();
        }

        private bool _wasFirstUpdate;
        private bool _isOrphaned;

        public bool IsOrphaned
        {
            get => _isOrphaned;
            set
            {
                if (_isOrphaned == value)
                    return;

                _isOrphaned = value;
                OnOrphanedStateChanged(value);
            }
        }
        
        private bool IsNodeOrphaned()
        {
            return !_pbsUnit.ConnectionHandler.NetworkNodes.Any(n => n is PBSDockingBase);
        }

        public void DropLootToZone(IZone zone, T pbsNode, Unit killer)
        {
            PBSHelper.DropLootToZone(zone, pbsNode, killer);
        }

        public void DropLootToZoneFromBase(IZone zone,PBSDockingBase pbsDockingBase, Unit killer)
        {
            PBSHelper.DropLootToZoneFromBase(zone, pbsDockingBase, killer);
        }

        public void OnUpdate(TimeSpan time)
        {
            if (!_wasFirstUpdate)
            {
                //every node is on zone, staring update

                //itt kell mindent initelni ami akkor tud helyesen futni, ha mindenki lent van a zonan. 
                _wasFirstUpdate = true;

                //ezek miatt raktam be a delayedstate-et, csak az az orphanednek nincs

                //init orphan status
                //itt csak siman a valtozot atirjuk, h ne logoljon
                _isOrphaned = IsNodeOrphaned();
            }

            _saver.Update(_pbsUnit,time);
        }

        private const CorporationRole PBS_UPDATE_ROLE_MASK = CorporationRole.CEO | CorporationRole.DeputyCEO | CorporationRole.viewPBS;

        public void SendNodeUpdate(PBSEventType eventType = PBSEventType.nodeUpdate)
        {
            Message.Builder.SetCommand(Commands.PbsEvent)
                .WithData(GetUpdateDictionary(eventType))
                .ToCorporation(_pbsUnit.Owner,PBS_UPDATE_ROLE_MASK)
                .Send();
        }

        public Dictionary<string,object> GetUpdateDictionary(PBSEventType pbsEventType,Dictionary<string,object> data = null)
        {
            var sourceDict = _pbsUnit.ToDictionary();

            if (data == null)
                data = new Dictionary<string,object>();
#if DEBUG
            data.Add(k.reason,pbsEventType.ToString()); //ez csak debug !!! hogy olvashato legyen
#endif
            data.Add(k.message,(int)pbsEventType);
            data.Add(k.source,sourceDict);
            data[k.zoneID] = _pbsUnit.ZoneIdCached; //OPP: use cached value to avoid accessing null zone on death
            return data;
        }
    }
}
