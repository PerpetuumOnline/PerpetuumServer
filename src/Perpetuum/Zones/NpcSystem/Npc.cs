using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Collections;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Modules.Weapons;
using Perpetuum.PathFinders;
using Perpetuum.Players;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Movements;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem
{

    public enum NpcBehaviorType
    {
        Passive,
        Neutral,
        Aggressive
    }

    public enum NpcSpecialType
    {
        Normal,
        Boss
    }

    public abstract class NpcAI : IState
    {
        protected readonly Npc npc;

        protected NpcAI(Npc npc)
        {
            this.npc = npc;
        }

        public virtual void Enter()
        {
            WriteLog("enter state = " + GetType().Name);
        }

        public virtual void Exit()
        {
            npc.StopMoving();
            WriteLog("exit state = " + GetType().Name);
        }

        public abstract void Update(TimeSpan time);

        protected virtual void ToHomeAI()
        {
            npc.AI.Push(new HomingAI(npc));
        }

        protected virtual void ToAggressorAI()
        {
            if (npc.Behavior.Type == NpcBehaviorType.Passive)
                return;

            npc.AI.Push(new AggressorAI(npc));
        }

        [Conditional("DEBUG")]
        protected void WriteLog(string message)
        {
//            Logger.InfoFormat("NpcAI: {0}",message);
        }
    }

    public class IdleAI : NpcAI
    {
        private RandomMovement _movement;

        public IdleAI(Npc npc) : base(npc)
        {
        }

        public override void Enter()
        {
            npc.StopAllModules();
            npc.ResetLocks();
            _movement = new RandomMovement(npc.HomePosition, npc.HomeRange);
            _movement.Start(npc);
            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (!npc.IsInHomeRange)
            {
                ToHomeAI();
                return;
            }

            if (!npc.ThreatManager.Hostiles.IsEmpty)
            {
                ToAggressorAI();
                return;
            }

            _movement?.Update(npc, time);
        }
    }

    public class CombatAI : NpcAI
    {
        private List<ModuleActivator> _moduleActivators;
        private readonly IntervalTimer _processHostilesTimer = new IntervalTimer(1500);

        protected CombatAI(Npc npc) : base(npc)
        {
        }

        public override void Enter()
        {
            _moduleActivators = npc.ActiveModules.Select(m => new ModuleActivator(m)).ToList();
            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            _processHostilesTimer.Update(time);
            if (_processHostilesTimer.Passed)
            {
                _processHostilesTimer.Reset();
                ProcessHostiles();
            }
            
            foreach (var activator in _moduleActivators)
            {
                activator.Update(time);
            }
        }

        private bool IsAttackable(Hostile hostile)
        {
            if (!hostile.unit.InZone)
                return false;

            if (hostile.unit.States.Dead)
                return false;

            if (!hostile.unit.IsLockable)
                return false;

            if (hostile.unit.IsAttackable != ErrorCodes.NoError)
                return false;

            if (hostile.unit.IsInvulnerable)
                return false;

            if (npc.Behavior.Type == NpcBehaviorType.Neutral)
            {
                if (hostile.IsExpired)
                    return false;
            }

            var isVisible = npc.IsVisible(hostile.unit);
            if (!isVisible)
                return false;

            return true;
        }

        private void ProcessHostiles()
        {
            var hostileEnumerator = npc.ThreatManager.Hostiles.GetEnumerator();
            while (hostileEnumerator.MoveNext())
            {
                var hostile = hostileEnumerator.Current;
                if (!IsAttackable(hostile))
                {
                    npc.ThreatManager.Remove(hostile);
                    npc.AddPseudoThreat(hostile.unit);
                    continue;
                }

                if (!npc.IsInLockingRange(hostile.unit))
                    continue;

                var mostHated = npc.ThreatManager.GetMostHatedHostile() == hostile;

                var l = npc.GetLockByUnit(hostile.unit);
                if (l == null)
                {
                    if (TryMakeFreeLockSlotFor(hostile))
                        npc.AddLock(hostile.unit, mostHated);
                }
                else
                {
                    if (mostHated && !l.Primary)
                        npc.SetPrimaryLock(l.Id);
                }
            }
        }

        private bool TryMakeFreeLockSlotFor(Hostile hostile)
        {
            if (npc.HasFreeLockSlot)
                return true;

            var weakestLock = npc.ThreatManager.Hostiles.SkipWhile(h => h != hostile).Skip(1).Select(h => npc.GetLockByUnit(h.unit)).LastOrDefault();
            if (weakestLock == null)
                return false;

            weakestLock.Cancel();
            return true;
        }
    }

    public class HomingAI : CombatAI
    {
        private PathMovement _movement;

        public HomingAI(Npc npc) : base(npc)
        {
        }

        public override void Enter()
        {
            var pathFinder = new AStarFinder(Heuristic.Manhattan, npc.IsWalkable);
            var randomHome = npc.HomePosition.GetRandomPositionInRange2D(1, npc.HomeRange * 0.4);
            pathFinder.FindPathAsync(npc.CurrentPosition, randomHome).ContinueWith(t =>
            {
                var path = t.Result;
                if (path == null)
                {
                    WriteLog("Path not found! (" + npc.CurrentPosition + " => " + npc.HomePosition + ")");

                    var f = new AStarFinder(Heuristic.Manhattan,(x,y) => true);
                    path = f.FindPath(npc.CurrentPosition, npc.HomePosition);

                    if (path == null)
                    {
                        WriteLog("Safe path not found! (" + npc.CurrentPosition + " => " + npc.HomePosition + ")");
                    }
                }

                _movement = new PathMovement(path);
                _movement.Start(npc);
            });

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (_movement != null)
            {
                _movement.Update(npc, time);

                if (_movement.Arrived)
                {
                    npc.AI.Pop();
                    return;
                }
            }

            base.Update(time);
        }

        protected override void ToHomeAI()
        {
            // mar haza megy
        }

        protected override void ToAggressorAI()
        {

        }
    }


    public class AggressorAI : CombatAI
    {
        public AggressorAI(Npc npc) : base(npc)
        {
        }

        protected override void ToAggressorAI()
        {
            // mar combatban van
        }

        public override void Exit()
        {
            _source?.Cancel();

            base.Exit();
        }

        public override void Update(TimeSpan time)
        {
            if (!npc.IsInHomeRange)
            {
                npc.AI.Push(new HomingAI(npc));
                return;
            }

            if (npc.ThreatManager.Hostiles.IsEmpty)
            {
                EnterEvadeMode();
                return;
            }

            UpdateHostile(time);

            base.Update(time);
        }

        private void EnterEvadeMode()
        {
            npc.AI.Pop();
            npc.AI.Push(new HomingAI(npc));
            WriteLog("Enter evade mode.");
        }

        private Position _lastTargetPosition;
        private PathMovement _movement;
        private PathMovement _nextMovement;

        private void UpdateHostile(TimeSpan time)
        {
            var mostHated = npc.ThreatManager.GetMostHatedHostile();
            if (mostHated == null)
                return;

            if (!mostHated.unit.CurrentPosition.IsEqual2D(_lastTargetPosition))
            {
                _lastTargetPosition = mostHated.unit.CurrentPosition;

                var findNewTargetPosition = false;

                if (!npc.IsInRangeOf3D(mostHated.unit, npc.BestCombatRange))
                {
                    findNewTargetPosition = true;
                }
                else
                {
                    var visibility = npc.GetVisibility(mostHated.unit);
                    if (visibility != null)
                    {
                        var r = visibility.GetLineOfSight(false);
                        if (r.hit)
                            findNewTargetPosition = true;
                    }
                }

                if (findNewTargetPosition)
                {
                    FindNewAttackPositionAsync(mostHated.unit).ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                            return;

                        var path = t.Result;
                        if (path == null)
                        {
                            npc.ThreatManager.Remove(mostHated);
                            npc.AddPseudoThreat(mostHated.unit);
                            return;
                        }

                        Interlocked.Exchange(ref _nextMovement,new PathMovement(path));
                    });
                }
            }

            if (_nextMovement != null)
            {
                _movement = Interlocked.Exchange(ref _nextMovement, null);
                _movement.Start(npc);
            }

            _movement?.Update(npc, time);
        }

        private CancellationTokenSource _source;

        private const int SQRT2 = 141;
        private const int WEIGHT = 1000;

        private Task<List<Point>> FindNewAttackPositionAsync(Unit hostile)
        {
            _source?.Cancel();

            _source = new CancellationTokenSource();
            return Task.Run(() => FindNewAttackPosition(hostile, _source.Token), _source.Token);
        }

        private List<Point> FindNewAttackPosition(Unit hostile,CancellationToken cancellationToken)
        {
            var end = hostile.CurrentPosition.GetRandomPositionInRange2D(0, npc.BestCombatRange - 1).ToPoint();

            npc.StopMoving();

            var maxNode = Math.Pow(npc.HomeRange, 2) * Math.PI;
            var pq = new PriorityQueue<Node>((int) maxNode);
            var startNode = new Node(npc.CurrentPosition);

            pq.Enqueue(startNode);

            var closed = new HashSet<Point>();
            closed.Add(startNode.position);

            Node current;
            while (pq.TryDequeue(out current))
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                if (IsValidAttackPosition(hostile, current.position))
                    return BuildPath(current);

                foreach (var n in current.position.GetNeighbours())
                {
                    if (closed.Contains(n))
                        continue;

                    closed.Add(n);

                    if (!npc.IsWalkable(n.X, n.Y))
                        continue;

                    if (!n.IsInRange(npc.HomePosition, npc.HomeRange))
                        continue;

                    var newG = current.g + (n.X - current.position.X == 0 || n.Y - current.position.Y == 0 ? 100 : SQRT2);
                    var newH = Heuristic.Manhattan.Calculate(n.X, n.Y, end.X, end.Y) * WEIGHT;

                    var newNode = new Node(n)
                    {
                        g = newG,
                        f = newG + newH,
                        parent = current
                    };

                    pq.Enqueue(newNode);
                }
            }

            return null;
        }

        private bool IsValidAttackPosition(Unit hostile,Point position)
        {
            var position3 = npc.Zone.FixZ(position.ToPosition()).AddToZ(npc.Height);

            if (!hostile.CurrentPosition.IsInRangeOf3D(position3, npc.BestCombatRange))
                return false;

            var r = npc.Zone.IsInLineOfSight(position3, hostile, false);
            return !r.hit;
        }

        private static List<Point> BuildPath(Node current)
        {
            var stack = new Stack<Point>();

            var node = current;
            while (node != null)
            {
                stack.Push(node.position);
                node = node.parent;
            }

            return stack.ToList();
        }

        private class Node : IComparable<Node>
        {
            public readonly Point position;
            public Node parent;
            public int g;
            public int f;

            public Node(Point position)
            {
                this.position = position;
            }

            public int CompareTo(Node other)
            {
                return f - other.f;
            }

            public override int GetHashCode()
            {
                return position.GetHashCode();
            }
        }
    }

    public class NpcBehavior
    {
        public NpcBehaviorType Type { get; private set; }

        protected NpcBehavior(NpcBehaviorType type)
        {
            Type = type;
        }

        public void Update(TimeSpan time)
        {
            
        }

        public static NpcBehavior Create(NpcBehaviorType type)
        {
            switch (type)
            {
                case NpcBehaviorType.Neutral:
                    return new NeutralBehavior();
                case NpcBehaviorType.Aggressive:
                    return new AggressiveBehavior();
               
                case NpcBehaviorType.Passive:
                    return new PassiveBehavior();
                default:
                    return new PassiveBehavior();
            }
        }
    }

    public class PassiveBehavior : NpcBehavior
    {
        public PassiveBehavior() : base(NpcBehaviorType.Passive)
        {
        }
    }

    public class NeutralBehavior : NpcBehavior
    {
        public NeutralBehavior() : base(NpcBehaviorType.Neutral)
        {
        }
    }

    public class AggressiveBehavior : NpcBehavior
    {
        public AggressiveBehavior() : base(NpcBehaviorType.Aggressive)
        {
        }
    }

    public class PsuedoThreat
    {
        private TimeSpan _lastUpdated = TimeSpan.Zero;
        private TimeSpan Expiration = TimeSpan.FromMinutes(1);

        public PsuedoThreat(Unit unit)
        {
            Unit = unit;
        }

        public Unit Unit { get; }

        public bool IsExpired
        {
            get { return _lastUpdated > Expiration; }
        }

        public void RefreshThreat()
        {
            _lastUpdated = TimeSpan.Zero;
        }

        public void Update(TimeSpan time)
        {
            _lastUpdated += time;
        }
    }

    public class Npc : Creature,ITaggable
    {
        private readonly TagHelper _tagHelper;
        private const double CALL_FOR_HELP_ARMOR_THRESHOLD = 0.2;
        private readonly ThreatManager _threatManager;
        private object _bestCombatRange;
        private TimeSpan _lastHelpCalled;
        private readonly EventListenerService _eventChannel;
        private List<PsuedoThreat> PseudoThreats;
        private object PseudoLock = new Object();

        public Npc(TagHelper tagHelper, EventListenerService eventChannel)
        {
            _eventChannel = eventChannel;
            _tagHelper = tagHelper;
            _threatManager = new ThreatManager();
            AI = new StackFSM();
            PseudoThreats = new List<PsuedoThreat>();
        }

        public NpcBehavior Behavior { get; set; }
        public NpcSpecialType SpecialType { get; set; }
        public NpcBossInfo BossInfo { get; set; }

        public bool IsBoss()
        {
            return SpecialType == NpcSpecialType.Boss && BossInfo != null;
        }

        [CanBeNull]
        private INpcGroup _group;

        public void SetGroup(INpcGroup group)
        {
            _group = group;
        }

        public INpcGroup Group
        {
            get { return _group; }
        }

        public StackFSM AI { get; private set; }

        public IThreatManager ThreatManager
        {
            get { return _threatManager; }
        }

        public ILootGenerator LootGenerator { get; set; }

        public double HomeRange { get; set; }
        public Position HomePosition { get; set; }

        public bool IsInHomeRange
        {
            get { return CurrentPosition.IsInRangeOf2D(HomePosition, HomeRange); }
        }

        public int BestCombatRange
        {
            get { return (int) (_bestCombatRange ?? (_bestCombatRange = CalculateCombatRange())); }
        }

        public void Tag(Player tagger,TimeSpan duration)
        {
            _tagHelper.DoTagging(this,tagger,duration);
        }

        [CanBeNull]
        public Player GetTagger()
        {
            return TagHelper.GetTagger(this);
        }

        public void AddThreat(Unit hostile, Threat threat,bool spreadToGroup)
        {
            _threatManager.GetOrAddHostile(hostile).AddThreat(threat);

            var PseudoThreat = PseudoThreats.Where(x => x.Unit == hostile).FirstOrDefault();
            if (PseudoThreat != null)
            {
                lock (PseudoLock)
                {
                    PseudoThreats.Remove(PseudoThreat);
                    Logger.DebugInfo(" Removed " + PseudoThreat.ToString());
                }
            }

            if (!spreadToGroup)
                return;

            var group = _group;
            if (@group == null)
                return;

            var t = Threat.Multiply(threat, 0.5);

            foreach (var member in @group.Members)
            {
                if (member == this)
                    continue;
                member.AddThreat(hostile,t,false);
            }
        }

        public void AddPseudoThreat(Unit hostile)
        {
            lock (PseudoLock)
            {
                PseudoThreats.Add(new PsuedoThreat(hostile));
            }
        }

        private void UpdatePseudoThreats(TimeSpan time)
        {
            lock (PseudoLock)
            {
                PseudoThreats.ForEach(threat => threat.Update(time));
                PseudoThreats.RemoveAll(threat => threat.IsExpired);
            }
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnPropertyChanged(ItemProperty property)
        {
            base.OnPropertyChanged(property);

            switch (property.Field)
            {
                case AggregateField.locking_range:
                {
                    _bestCombatRange = null;
                    break;
                }
                case AggregateField.armor_current:
                {
                    var percentage = Armor.Ratio(ArmorMax);
                    if (percentage <= CALL_FOR_HELP_ARMOR_THRESHOLD)
                    {
                        CallingForHelp();
                    }

                    break;
                }
            }
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();
            var homeDistance = HomePosition.TotalDistance2D(CurrentPosition);

            info.Add("homePositionX", HomePosition.intX);
            info.Add("homePositionY", HomePosition.intY);
            info.Add("homeRange", HomeRange);
            info.Add("homeDistance", homeDistance);
            info.Add("coreMax", CoreMax);
            info.Add("coreCurrent", Core);
            info.Add("bestCombatRange", BestCombatRange);

            var currentAI = AI.Current;
            if (currentAI != null)
                info.Add("fsm", currentAI.GetType().Name);

            info.Add("threat", _threatManager.ToDebugString());

            _group?.AddDebugInfoToDictionary(info);

            info.Add("ismission",GetMissionGuid() != Guid.Empty);

            return info;
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            base.OnDamageTaken(source, e);

            var player = Zone.ToPlayerOrGetOwnerPlayer(source);
            if (player == null)
                return;

            AddThreat(player,new Threat(ThreatType.Damage, e.TotalDamage * 0.9),true);

            if (IsBoss())
            {
                BossInfo.OnAggro(player, _eventChannel);
            }
        }

        protected override void OnDead(Unit killer)
        {
            var zone = Zone;
            var tagger = GetTagger();
            Debug.Assert(zone != null, "zone != null");
           
            HandleNpcDeadAsync(zone, killer, tagger).ContinueWith((t) => base.OnDead(killer)).LogExceptions();

            if (IsBoss())
            {
                BossInfo.OnDeath(this, killer, _eventChannel);
            }
        }

        private Task HandleNpcDeadAsync(IZone zone, Unit killer, Player tagger)
        {
            return Task.Run(() => HandleNpcDead(zone, killer, tagger));
        }

        private void HandleNpcDead([NotNull] IZone zone, Unit killer, Player tagger)
        {
            Logger.DebugInfo($"   >>>> NPC died.  Killer unitName:{killer.Name} o:{killer.Owner}   Tagger botname:{tagger?.Name} o:{killer.Owner} characterId:{tagger?.Character.Id}");

            using (var scope = Db.CreateTransaction())
            {

                if (IsBoss() && BossInfo.IsLootSplit())
                {
                    //Boss - Split loot equally to all participants
                    List<Player> participants = new List<Player>();
                    participants = ThreatManager.Hostiles.Select(x => zone.ToPlayerOrGetOwnerPlayer(x.unit)).ToList();
                    if (participants.Count > 0)
                    {
                        ISplittableLootGenerator splitLooter = new SplittableLootGenerator(LootGenerator);
                        List<ILootGenerator> lootGenerators = splitLooter.GetGenerators(participants.Count);
                        for (var i = 0; i < participants.Count; i++)
                        {
                            LootContainer.Create().SetOwner(participants[i]).AddLoot(lootGenerators[i]).BuildAndAddToZone(zone, participants[i].CurrentPosition);
                        }
                    }
                }
                else
                {
                    //Normal case: loot can awarded in full to tagger
                    LootContainer.Create().SetOwner(tagger).AddLoot(LootGenerator).BuildAndAddToZone(zone, CurrentPosition);
                }


                var killerPlayer = zone.ToPlayerOrGetOwnerPlayer(killer);

                if (GetMissionGuid() != Guid.Empty)
                {
                    Logger.DebugInfo("   >>>> NPC is mission related.");

                    SearchForMissionOwnerAndSubmitKill(zone, killer);
                }
                else
                {
                    Logger.DebugInfo("   >>>> independent NPC.");

                    if (killerPlayer != null)
                        EnqueueKill(killerPlayer, killer);
                }

                var ep = Db.Query().CommandText("GetNpcKillEp").SetParameter("@definition", Definition).ExecuteScalar<int>();

                //Logger.Warning($"Ep4Npc:{ep} def:{Definition} {ED.Name}");

                if (zone.Configuration.IsBeta)
                    ep *= 2;

                if (zone.Configuration.Type == ZoneType.Training) ep = 0;

                if (ep > 0)
                {
                    foreach (var hostile in ThreatManager.Hostiles)
                    {
                        var hostilePlayer = zone.ToPlayerOrGetOwnerPlayer(hostile.unit);
                        hostilePlayer?.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Npc, ep);
                    }

                    // same as above, but add half EP for players that were out of range of the NPC.
                    var psuedoHostiles = PseudoThreats.Where(p => !ThreatManager.Contains(p.Unit));
                    foreach (var hostile in psuedoHostiles)
                    {
                        var hostilePlayer = zone.ToPlayerOrGetOwnerPlayer(hostile.Unit);
                        hostilePlayer?.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Npc, ep / 2);
                    }
                }

                scope.Complete();
            }
        }


        /// <summary>
        /// This occurs when aoe kills the npc. 
        /// Background task that searches for the related missionguid and sumbits the kill for that specific player
        /// </summary>
        private void SearchForMissionOwnerAndSubmitKill(IZone zone, Unit killerUnit)
        {
            var missionGuid = GetMissionGuid();
            var missionOwner = MissionHelper.FindMissionOwnerByGuid(missionGuid);

            var missionOwnerPlayer = zone.GetPlayer(missionOwner);
            if (missionOwnerPlayer == null)
            {
                //the owner is not this zone
                //address the mission plugin directly

                var info = new Dictionary<string, object>
                {
                    {k.characterID, missionOwner.Id},
                    {k.guid, missionGuid.ToString()},
                    {k.type, MissionTargetType.kill_definition},
                    {k.definition, ED.Definition},
                    {k.increase ,1},
                    {k.zoneID, zone.Id},
                    {k.position, killerUnit.CurrentPosition}
                };

                if (killerUnit is Player killerPlayer && killerPlayer.Character.Id != missionOwner.Id)
                {
                    info[k.assistingCharacterID] = killerPlayer.Character.Id;
                }

                Task.Run(() =>
                {
                    MissionHelper.MissionProcessor.NpcGotKilledInAway(missionOwner,missionGuid,info);
                });
                return;
            }
                
            //local enqueue, this is the proper player, we can skip gang
            EnqueueKill(missionOwnerPlayer, killerUnit);
        }

        private void EnqueueKill(Player missionOwnerPlayer, Unit killerUnit)
        {
            var eventSourcePlayer = missionOwnerPlayer;
            var killerPlayer = killerUnit as Player;
            if (killerPlayer != null && !killerPlayer.Equals(missionOwnerPlayer))
            {
                eventSourcePlayer = killerPlayer;
            }

            Logger.DebugInfo($"   >>>> EventSource: botName:{eventSourcePlayer.Name} o:{eventSourcePlayer.Owner} characterId:{eventSourcePlayer.Character.Id} MissionOwner: botName:{missionOwnerPlayer.Name} o:{missionOwnerPlayer.Owner} characterId:{missionOwnerPlayer.Character.Id}");

            //local enqueue, this is the proper player, we can skip gang
            missionOwnerPlayer.MissionHandler.EnqueueMissionEventInfoLocally(new KillEventInfo(eventSourcePlayer, this, CurrentPosition));
        }

        protected override void OnTileChanged()
        {
            base.OnTileChanged();
            LookingForHostiles();
        }

        private void LookingForHostiles()
        {
            foreach (var visibility in GetVisibleUnits())
            {
                AddBodyPullThreat(visibility.Target);
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            Behavior.Update(time);

            AI.Update(time);

            UpdatePseudoThreats(time);
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            States.Aggressive = Behavior.Type == NpcBehaviorType.Aggressive;

            base.OnEnterZone(zone, enterType);

            AI.Push(new IdleAI(this));
        }

        public override string InfoString
        {
            get
            {
                var s = $"Npc:{ED.Name}:{Eid}";

                var zone = Zone;
                if (zone != null)
                {
                    s += " z:" + zone.Id;
                }

                if (_group != null)
                    s += " g:" + _group.Name;

                return s;
            }
        }

        private const double AGGRO_RANGE = 30;

        private bool IsInAggroRange(Unit target)
        {
            return IsInRangeOf3D(target, AGGRO_RANGE);
        }

        protected override void OnUnitLockStateChanged(Lock @lock)
        {
            var unitLock = @lock as UnitLock;
            if ( unitLock == null )
                return;

            if (unitLock.Target != this)
                return;

            if (unitLock.State != LockState.Locked) 
                return;

            var threatValue = unitLock.Primary ? Threat.LOCK_PRIMARY : Threat.LOCK_SECONDARY;
            AddThreat(unitLock.Owner,new Threat(ThreatType.Lock, threatValue),true);
        }

        protected override void OnUnitTileChanged(Unit target)
        {
            AddBodyPullThreat(target);                
        }

        internal override bool IsHostile(Player player)
        {
            return true;
        }

        internal override bool IsHostile(AreaBomb bomb)
        {
            return true;
        }

        public bool CanAddThreatTo(Unit target,Threat threat)
        {
            var hasThreat = _threatManager.Contains(target);
            if (hasThreat)
                return true;

            var check = false;

            switch (Behavior.Type)
            {
                case NpcBehaviorType.Passive:
                    return false;
                case NpcBehaviorType.Neutral:
                {
                    switch (threat.type)
                    {
                        case ThreatType.Direct:
                        {
                            // itt nincs aggrorange,mehet mindig
                            return true;
                        }

                        case ThreatType.Debuff:
                        case ThreatType.Support:
                        case ThreatType.Lock:
                        {
                            check = true;
                            break;
                        }
                    }
                    break;
                }
                case NpcBehaviorType.Aggressive:
                {
                    check = true;
                    break;
                }
            }

            return check && IsInAggroRange(target);
        }

        private void AddBodyPullThreat(Unit enemy)
        {
            if ( !IsHostile(enemy))
                return;

            var helper = new BodyPullThreatHelper(this);
            enemy.AcceptVisitor(helper);
        }

        public bool CallForHelp { private get; set; }

        private void CallingForHelp()
        {
            if (!CallForHelp)
                return;

            if (!GlobalTimer.IsPassed(ref _lastHelpCalled, TimeSpan.FromSeconds(5)))
                return;

            var group = _group;
            if (group == null)
                return;

            foreach (var member in group.Members.Where(flockMember => flockMember != this))
            {
                member.HelpingFor(this);
            }
        }

        private void HelpingFor(Npc caller)
        {
            if (Armor.Ratio(ArmorMax) < CALL_FOR_HELP_ARMOR_THRESHOLD)
                return;
            
            _threatManager.Clear();
            foreach (var hostile in caller.ThreatManager.Hostiles)
            {
                AddThreat(hostile.unit,new Threat(ThreatType.Undefined,hostile.Threat),true);
            }
        }

        public void AddAssistThreat(Unit assistant, Unit target, Threat threat)
        {
            if ( !_threatManager.Contains(target) )
                return;

            if ( !CanAddThreatTo(assistant,threat))
                return;

            AddThreat(assistant,threat,true);
        }

        private int CalculateCombatRange()
        {
            double range = (int)ActiveModules.Where(m => m.IsRanged)
                         .Select(module => module.OptimalRange)
                         .Concat(new[] { MaxTargetingRange })
                         .Min();

            range *= BEST_COMBAT_RANGE_MODIFIER;
            range = Math.Max(3, range);
            return (int) range;
        }

        private const double BEST_COMBAT_RANGE_MODIFIER = 0.9;

        private class BodyPullThreatHelper : IEntityVisitor<Player>,IEntityVisitor<AreaBomb>
        {
            private readonly Npc _npc;

            public BodyPullThreatHelper(Npc npc)
            {
                _npc = npc;
            }

            public void Visit(Player player)
            {
                if (_npc.Behavior.Type != NpcBehaviorType.Aggressive)
                    return;

                if (player.HasTeleportSicknessEffect)
                    return;

                if (!_npc.ThreatManager.Hostiles.IsEmpty)
                    return;

                if (!_npc.IsInAggroRange(player))
                    return;

                var threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);
                _npc.AddThreat(player, new Threat(ThreatType.Bodypull, threat));
            }

            public void Visit(AreaBomb bomb)
            {
                if (!_npc.IsInAggroRange(bomb))
                    return;

                // csak akkor ha van is mivel tamadni
                if (!_npc.ActiveModules.Any(m => m is WeaponModule))
                    return;

                // ha valaki mar foglalkozik a bombaval akkor ne csinaljon semmit

                var g = _npc._group;
                if (g != null && g.Members.Any(m => m.ThreatManager.Contains(bomb)))
                    return;

                var threat = Threat.BODY_PULL;
                if (!_npc.ThreatManager.Hostiles.IsEmpty)
                {
                    var h = _npc.ThreatManager.GetMostHatedHostile();
                    if (h != null)
                        threat = h.Threat*100;
                }

                _npc.AddThreat(bomb, new Threat(ThreatType.Bodypull, threat + FastRandom.NextDouble(0, 5)));
            }
        }

        public override bool IsWalkable(Vector2 position)
        {
            return Zone.IsWalkableForNpc((int)position.X, (int)position.Y, Slope);
        }

        public override bool IsWalkable(Position position)
        {
            return Zone.IsWalkableForNpc((int)position.X, (int)position.Y, Slope);
        }

        public override bool IsWalkable(int x, int y)
        {
            return Zone.IsWalkableForNpc(x, y, Slope);
        }

    }
}