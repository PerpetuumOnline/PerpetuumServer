using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Transactions;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Newtonsoft.Json;
using Open.Nat;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;
using Perpetuum.Groups.Alliances;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Corporations.Loggers;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host;
using Perpetuum.Host.Requests;
using Perpetuum.IDGenerators;
using Perpetuum.IO;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Log.Formatters;
using Perpetuum.Log.Loggers;
using Perpetuum.Modules;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.Terraforming;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.RequestHandlers;
using Perpetuum.RequestHandlers.AdminTools;
using Perpetuum.RequestHandlers.Channels;
using Perpetuum.RequestHandlers.Characters;
using Perpetuum.RequestHandlers.Corporations;
using Perpetuum.RequestHandlers.Corporations.YellowPages;
using Perpetuum.RequestHandlers.Extensions;
using Perpetuum.RequestHandlers.FittingPreset;
using Perpetuum.RequestHandlers.Gangs;
using Perpetuum.RequestHandlers.Intrusion;
using Perpetuum.RequestHandlers.Mails;
using Perpetuum.RequestHandlers.Markets;
using Perpetuum.RequestHandlers.Missions;
using Perpetuum.RequestHandlers.Production;
using Perpetuum.RequestHandlers.RobotTemplates;
using Perpetuum.RequestHandlers.Socials;
using Perpetuum.RequestHandlers.Sparks;
using Perpetuum.RequestHandlers.Standings;
using Perpetuum.RequestHandlers.TechTree;
using Perpetuum.RequestHandlers.Trades;
using Perpetuum.RequestHandlers.TransportAssignments;
using Perpetuum.RequestHandlers.Zone;
using Perpetuum.RequestHandlers.Zone.Containers;
using Perpetuum.RequestHandlers.Zone.MissionRequests;
using Perpetuum.RequestHandlers.Zone.NpcSafeSpawnPoints;
using Perpetuum.RequestHandlers.Zone.PBS;
using Perpetuum.RequestHandlers.Zone.StatsMapDrawing;
using Perpetuum.Robots;
using Perpetuum.Services;
using Perpetuum.Services.Channels;
using Perpetuum.Services.Channels.ChatCommands;
using Perpetuum.Services.Daytime;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.EventServices.EventProcessors;
using Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.HighScores;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.ItemShop;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.MissionBonusObjects;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Services.ProductionEngine.ResearchKits;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Relics;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Services.RiftSystem.StrongholdRifts;
using Perpetuum.Services.Sessions;
using Perpetuum.Services.Social;
using Perpetuum.Services.Sparks;
using Perpetuum.Services.Sparks.Teleports;
using Perpetuum.Services.Standing;
using Perpetuum.Services.Steam;
using Perpetuum.Services.TechTree;
using Perpetuum.Services.Trading;
using Perpetuum.Services.Weather;
using Perpetuum.Threading.Process;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units.FieldTerminals;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.CombatLogs;
using Perpetuum.Zones.Decors;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Effects.ZoneEffects;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Environments;
using Perpetuum.Zones.Gates;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Presences.InterzonePresences;
using Perpetuum.Zones.NpcSystem.Presences.PathFinders;
using Perpetuum.Zones.NpcSystem.SafeSpawnPoints;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.ArmorRepairers;
using Perpetuum.Zones.PBS.ControlTower;
using Perpetuum.Zones.PBS.CoreTransmitters;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.EffectNodes;
using Perpetuum.Zones.PBS.EnergyWell;
using Perpetuum.Zones.PBS.HighwayNode;
using Perpetuum.Zones.PBS.ProductionNodes;
using Perpetuum.Zones.PBS.Reactors;
using Perpetuum.Zones.PBS.Turrets;
using Perpetuum.Zones.PlantTools;
using Perpetuum.Zones.ProximityProbes;
using Perpetuum.Zones.PunchBags;
using Perpetuum.Zones.Scanning.Ammos;
using Perpetuum.Zones.Scanning.Modules;
using Perpetuum.Zones.Scanning.Results;
using Perpetuum.Zones.Scanning.Scanners;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Teleporting.Strategies;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;
using Perpetuum.Zones.Terrains.Terraforming;
using Perpetuum.Zones.Training;
using Perpetuum.Zones.Training.Reward;
using Perpetuum.Zones.ZoneEntityRepositories;
using ChangeAmmo = Perpetuum.RequestHandlers.ChangeAmmo;
using CorporationDocumentConfig = Perpetuum.RequestHandlers.Corporations.CorporationDocumentConfig;
using EquipAmmo = Perpetuum.RequestHandlers.EquipAmmo;
using EquipModule = Perpetuum.RequestHandlers.EquipModule;
using ListContainer = Perpetuum.RequestHandlers.ListContainer;
using LogEvent = Perpetuum.Log.LogEvent;
using Module = Perpetuum.Modules.Module;
using PackItems = Perpetuum.RequestHandlers.PackItems;
using RelocateItems = Perpetuum.RequestHandlers.RelocateItems;
using RemoveModule = Perpetuum.RequestHandlers.RemoveModule;
using SetItemName = Perpetuum.RequestHandlers.SetItemName;
using TrashItems = Perpetuum.RequestHandlers.TrashItems;
using UnpackItems = Perpetuum.RequestHandlers.UnpackItems;
using UnstackAmount = Perpetuum.RequestHandlers.UnstackAmount;

namespace Perpetuum.Bootstrapper
{
    class EntityAggregateServices : IEntityServices
    {
        public IEntityFactory Factory { get; set; }
        public IEntityDefaultReader Defaults { get; set; }
        public IEntityRepository Repository { get; set; }
    }

    class RobotTemplateServicesImpl : IRobotTemplateServices
    {
        public IRobotTemplateReader Reader { get; set; }
        public IRobotTemplateRelations Relations { get; set; }
    }

    class TeleportStrategyFactoriesImpl : ITeleportStrategyFactories
    {
        public TeleportWithinZone.Factory TeleportWithinZoneFactory { get; set; }
        public TeleportToAnotherZone.Factory TeleportToAnotherZoneFactory { get; set; }
        public TrainingExitStrategy.Factory TrainingExitStrategyFactory { get; set; }
    }

    public delegate ITerrain TerrainFactory(IZone zone);

    public class PerpetuumBootstrapper
    {
        private ContainerBuilder _builder;
        private IContainer _container;

        public void Start()
        {
            var s = _container.Resolve<IHostStateService>();
            s.State = HostState.Starting;
        }

        public void Stop()
        {
            var s = _container.Resolve<IHostStateService>();
            s.State = HostState.Stopping;
        }

        public void Stop(TimeSpan delay)
        {
            var m = _container.Resolve<HostShutDownManager>();
            m.Shutdown(delay);
        }

        public IContainer GetContainer()
        {
            return _container;
        }

        public void WaitForStop()
        {
            var are = new AutoResetEvent(false);

            var s = _container.Resolve<IHostStateService>();
            s.StateChanged += (sender, state) =>
            {
                if (state == HostState.Off)
                    are.Set();
            };

            are.WaitOne();
        }

        public void WriteCommandsToFile(string path)
        {
            var sb = new StringBuilder();

            foreach (var command in GetCommands().OrderBy(c => c.Text))
            {
                sb.AppendLine($"{command.Text},{command.AccessLevel}");
            }
           
            File.WriteAllText(path,sb.ToString());
        }

        public IEnumerable<Command> GetCommands()
        {
            return  typeof(Commands).GetFields(BindingFlags.Static | BindingFlags.Public).Select(info => (Command)info.GetValue(null));
        }

        public void Init(string gameRoot)
        {
            _builder = new ContainerBuilder();
            InitContainer(gameRoot);
            _container = _builder.Build();
            Logger.Current = _container.Resolve<ILogger<LogEvent>>();

            var config = _container.Resolve<GlobalConfiguration>();
            _container.Resolve<IHostStateService>().State = HostState.Init;


            Logger.Info($"Game root: {config.GameRoot}");
            Logger.Info($"GC isServerGC: {GCSettings.IsServerGC}");
            Logger.Info($"GC Latency mode: {GCSettings.LatencyMode}");
            Logger.Info($"Vector is hardware accelerated: {Vector.IsHardwareAccelerated}");

            Db.DbQueryFactory = _container.Resolve<Func<DbQuery>>();

            using (var connection = _container.Resolve<DbConnectionFactory>()()) { Logger.Info($"Database: {connection.Database}"); }

            InitGame(_container);

            EntityDefault.Reader = _container.Resolve<IEntityDefaultReader>();
            Entity.Services = _container.Resolve<IEntityServices>();

            GenxyConverter.RegisterConverter<Character>((writer, character) =>
            {
                GenxyConverter.ConvertInt(writer, character.Id);
            });

            CorporationData.CorporationManager = _container.Resolve<ICorporationManager>();

            Character.CharacterFactory = _container.Resolve<CharacterFactory>();
            Character.CharacterCache = _container.Resolve<Func<string, ObjectCache>>().Invoke("CharacterCache");            

            MissionHelper.Init(_container.Resolve<MissionDataCache>(), _container.Resolve<IStandingHandler>());
            MissionHelper.MissionProcessor = _container.Resolve<MissionProcessor>();
            MissionHelper.EntityServices = _container.Resolve<IEntityServices>();

            Mission.Init(_container.Resolve<MissionDataCache>());
            MissionInProgress.Init(_container.Resolve<MissionDataCache>());
            MissionAgent.Init(_container.Resolve<MissionDataCache>());
            MissionStandingChangeCalculator.Init(_container.Resolve<MissionDataCache>());
            ZoneMissionInProgress.Init(_container.Resolve<MissionDataCache>());
            MissionSpot.Init(_container.Resolve<MissionDataCache>());
            MissionSpot.ZoneManager = _container.Resolve<IZoneManager>();
            MissionLocation.Init(_container.Resolve<MissionDataCache>());
            MissionLocation.ZoneManager = _container.Resolve<IZoneManager>();
            MissionTarget.missionDataCache = _container.Resolve<MissionDataCache>();
            MissionTarget.ProductionDataAccess = _container.Resolve<IProductionDataAccess>();
            MissionTarget.RobotTemplateRelations = _container.Resolve<IRobotTemplateRelations>();
            MissionTarget.MissionTargetInProgressFactory = _container.Resolve<MissionTargetInProgress.Factory>();

            MissionTargetRewardCalculator.Init(_container.Resolve<MissionDataCache>());
            MissionTargetSuccessInfoGenerator.Init(_container.Resolve<MissionDataCache>());
            MissionBonus.Init(_container.Resolve<MissionDataCache>());
            ZoneMissionTarget.MissionProcessor = _container.Resolve<MissionProcessor>();
            ZoneMissionTarget.PresenceFactory = _container.Resolve<PresenceFactory>();

            MissionResolveTester.Init(_container.Resolve<MissionDataCache>());
            TransportAssignment.EntityServices = _container.Resolve<IEntityServices>();
            ProductionLine.ProductionLineFactory = _container.Resolve<ProductionLine.Factory>();
            MissionInProgress.MissionInProgressFactory = _container.Resolve<MissionInProgress.Factory>();
            MissionInProgress.MissionProcessor = _container.Resolve<MissionProcessor>();
            PriceCalculator.PriceCalculatorFactory = _container.Resolve<PriceCalculator.Factory>();

            Message.MessageBuilderFactory = _container.Resolve<MessageBuilder.Factory>();

            PBSHelper.ProductionDataAccess = _container.Resolve<IProductionDataAccess>();
            PBSHelper.ProductionManager = _container.Resolve<ProductionManager>();
            PBSHelper.ItemDeployerHelper = _container.Resolve<ItemDeployerHelper>();

            ProductionComponentCollector.ProductionComponentCollectorFactory = _container.Resolve<ProductionComponentCollector.Factory>();

            CorporationData.InfoCache = _container.Resolve<Func<string, ObjectCache>>().Invoke("CorporationInfoCache");

            _container.Resolve<IHostStateService>().StateChanged += (sender, state) =>
            {
                switch (state)
                {
                    case HostState.Stopping:
                    {
                        _container.Resolve<IProcessManager>().Stop();
                        NatDiscoverer.ReleaseAll();
                        sender.State = HostState.Off;
                        break;
                    }
                    case HostState.Starting:
                    {
                        _container.Resolve<IProcessManager>().Start();
                        sender.State = HostState.Online;
                        break;
                    }
                }
            };

            DefaultCorporationDataCache.LoadAll();
            _container.Resolve<MissionDataCache>().CacheMissionData();
            // initialize our markets.
            // this is dependent on all zones being loaded.
            _container.Resolve<MarketHelper>().Init();
            _container.Resolve<MarketHandler>().Init();


        }

        public bool TryInitUpnp(out bool success)
        {
            success = false;
            var config = _container.Resolve<GlobalConfiguration>();
            if (!config.EnableUpnp)
                return false;

            try
            {
                var discoverer = new NatDiscoverer();
                NatDiscoverer.ReleaseAll();

                var natDevice = discoverer.DiscoverDeviceAsync().Result;
                if (natDevice == null)
                {
                    Logger.Error("[UPNP] NAT device not found!");
                    return false;
                }

                void Map(int port)
                {
                    var task = natDevice.CreatePortMapAsync(new Mapping(Protocol.Tcp,port,port)).ContinueWith(t =>
                    {
                        Logger.Info($"[UPNP] Port mapped: {port}");
                    });
                    task.Wait();
                }

                Map(config.ListenerPort);

                foreach (var zone in _container.Resolve<IZoneManager>().Zones)
                {
                    Map(zone.Configuration.ListenerPort);
                }

                success = true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }

            return true;
        }

        /// <summary>
        /// this method cleans up every runtime table
        /// </summary>
        private static void InitGame(IComponentContext container)
        {
            //the current host has to clean up things in the onlinehost table, and other runtime tables
            Db.Query().CommandText("initServer").ExecuteNonQuery();

            var globalConfiguration = container.Resolve<GlobalConfiguration>();
            if (!string.IsNullOrEmpty(globalConfiguration.PersonalConfig))
            {
                Db.Query().CommandText(globalConfiguration.PersonalConfig).ExecuteNonQuery();
                Logger.Info("Personal sp executed:" + globalConfiguration.PersonalConfig);
            }

            Logger.Info("DB init done.");
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterAutoActivate<T>(TimeSpan interval) where T : IProcess
        {
            return _builder.RegisterType<T>().SingleInstance().AutoActivate().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(interval));
            });
        }

        private void RegisterAutoActivatedTypes()
        {
            RegisterAutoActivate<HostOnlineStateWriter>(TimeSpan.FromSeconds(7));
            RegisterAutoActivate<ServerInfoService>(TimeSpan.FromMinutes(5));
            //RegisterAutoActivate<CleanUpPayingCustomersService>(TimeSpan.FromHours(10));
            RegisterAutoActivate<MarketCleanUpService>(TimeSpan.FromHours(1));
//            RegisterAutoActivate<AccountCreditHandler>(TimeSpan.FromSeconds(10));
            RegisterAutoActivate<SessionCountWriter>(TimeSpan.FromMinutes(5));
            RegisterAutoActivate<VolunteerCEOProcessor>(TimeSpan.FromMinutes(10));
            RegisterAutoActivate<GiveExtensionPointsService>(TimeSpan.FromMinutes(10));
            RegisterAutoActivate<ArtifactRefresher>(TimeSpan.FromHours(7));
        }

        private void RegisterCommands()
        {
            foreach (var command in GetCommands())
            {
                _builder.RegisterInstance(command).As<Command>().Keyed<Command>(command.Text.ToUpper());
            }

            _builder.Register<Func<string, Command>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return (commandText =>
                {
                    commandText = commandText.ToUpper();
                    return ctx.IsRegisteredWithKey<Command>(commandText) ? ctx.ResolveKeyed<Command>(commandText) : null;
                });
            });
        }

        private void InitContainer(string gameRoot)
        {
            RegisterCommands();
            RegisterRequestHandlers();
            RegisterAutoActivatedTypes();
            RegisterLoggers();
            RegisterEntities();
            RegisterRobotTemplates();
            RegisterMissions();
            RegisterTerrains();
            RegisterNpcs();
            RegisterChannelTypes();
            RegisterMtProducts();
            RegisterRifts();
            RegisterRelics();
            RegisterEffects();
            RegisterIntrusions();
            RegisterZones();
            RegisterPBS();

            _builder.Register<Func<string, ObjectCache>>(x =>
            {
                return name => new MemoryCache(name);
            });

            _builder.RegisterType<CharacterProfileRepository>().AsSelf().As<ICharacterProfileRepository>();
            _builder.Register(c =>
            {
                var cache = new MemoryCache("CharacterProfiles");
                return new CachedReadOnlyRepository<int, CharacterProfile>(cache, c.Resolve<CharacterProfileRepository>());
            }).AsSelf().As<IReadOnlyRepository<int,CharacterProfile>>().SingleInstance();

            _builder.RegisterType<CachedCharacterProfileRepository>().As<ICharacterProfileRepository>();

            _builder.RegisterType<StandingRepository>().As<IStandingRepository>();
            _builder.RegisterType<StandingHandler>().OnActivated(e =>
            {
                e.Instance.Init();
            }).As<IStandingHandler>().SingleInstance();

            _builder.RegisterType<CentralBank>().As<ICentralBank>().AutoActivate().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromHours(1)));
            }).SingleInstance();

            _builder.RegisterType<TechTreeInfoService>().As<ITechTreeInfoService>();
            _builder.RegisterType<TechTreeService>().As<ITechTreeService>();
            _builder.RegisterType<TeleportDescriptionRepository>().As<ITeleportDescriptionRepository>();
            _builder.RegisterType<CustomDictionary>().As<ICustomDictionary>().SingleInstance().AutoActivate();

            _builder.RegisterType<Session>().AsSelf().As<ISession>();

            _builder.RegisterType<SessionManager>().As<ISessionManager>().SingleInstance();

            InitRelayManager();

            _builder.Register(c => new FileSystem(gameRoot)).As<IFileSystem>();
            _builder.Register(c =>
            {
                var fileManager = c.Resolve<IFileSystem>();
                var settingsFile = fileManager.ReadAllText("perpetuum.ini");
                var configuration = JsonConvert.DeserializeObject<GlobalConfiguration>(settingsFile);
                configuration.GameRoot = gameRoot;
                return configuration;
            }).SingleInstance();

            _builder.RegisterType<AdminCommandRouter>().SingleInstance();

            _builder.RegisterType<Gang>();
            _builder.RegisterType<GangRepository>().As<IGangRepository>();
            _builder.RegisterType<GangManager>().As<IGangManager>().SingleInstance();

            _builder.Register(c =>
            {
                var config = c.Resolve<GlobalConfiguration>();
                return config.Corporation;
            }).As<CorporationConfiguration>();

            _builder.RegisterType<HostStateService>().As<IHostStateService>().SingleInstance();
            _builder.Register(c => new ProcessManager(TimeSpan.FromMilliseconds(50))).As<IProcessManager>().SingleInstance();

            _builder.Register<DbConnectionFactory>(x =>
            {
                var connectionString = x.Resolve<GlobalConfiguration>().ConnectionString;
                return (() => new SqlConnection(connectionString));
            });

            _builder.RegisterType<DbQuery>();

            
            _builder.RegisterType<SparkTeleport>();

            _builder.RegisterType<ExtensionReader>().As<IExtensionReader>().SingleInstance();
            _builder.RegisterType<ExtensionPoints>();


            _builder.RegisterType<LootService>().As<ILootService>().SingleInstance().OnActivated(e => e.Instance.Init());
            _builder.RegisterType<ItemPriceHelper>().SingleInstance();
            _builder.RegisterType<PriceCalculator>(); // this doesn't appear to be something that should be a singleton.


            _builder.RegisterType<CharacterExtensions>().As<ICharacterExtensions>().SingleInstance();
            _builder.RegisterType<AccountRepository>().As<IAccountRepository>();

            _builder.RegisterType<SocialService>().As<ISocialService>().SingleInstance();

            _builder.RegisterType<CharacterTransactionLogger>().As<ICharacterTransactionLogger>();

            _builder.RegisterType<CharacterCreditService>().As<ICharacterCreditService>();
            _builder.RegisterType<CharacterWallet>().AsSelf().As<ICharacterWallet>();
            _builder.RegisterType<CharacterWalletHelper>();
            _builder.Register<CharacterWalletFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return ((character, type) =>
                {
                    return ctx.Resolve<CharacterWallet>(new TypedParameter(typeof(Character), character),
                                                        new TypedParameter(typeof(TransactionType), type));
                });
            });

            _builder.RegisterType<Character>().AsSelf();
            _builder.Register(x => x.Resolve<Character>(TypedParameter.From(0))).Named<Character>("nullcharacter").SingleInstance();

            _builder.Register<CharacterFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return (id =>
                {
                    if (id == 0)
                        return ctx.ResolveNamed<Character>("nullcharacter");

                    return ctx.Resolve<Character>(TypedParameter.From(id));
                });
            });

            _builder.RegisterType<MessageBuilder>();
            _builder.RegisterType<MessageSender>().As<IMessageSender>();
            _builder.RegisterType<CorporationMessageSender>().As<ICorporationMessageSender>().SingleInstance();

            _builder.RegisterType<ServerInfo>();
            _builder.RegisterType<ServerInfoManager>().As<IServerInfoManager>();


            _builder.Register(x =>
            {
                var cfg = x.Resolve<GlobalConfiguration>();
                return new SteamManager(cfg.SteamAppID, cfg.SteamKey);
            }).As<ISteamManager>();
        }

        private void RegisterChannelTypes()
        {
            _builder.RegisterType<ChannelRepository>().As<IChannelRepository>();
            _builder.RegisterType<ChannelMemberRepository>().As<IChannelMemberRepository>();
            _builder.RegisterType<ChannelBanRepository>().As<IChannelBanRepository>();
            _builder.RegisterType<ChannelManager>().As<IChannelManager>().SingleInstance();

            RegisterRequestHandler<ChannelCreate>(Commands.ChannelCreate);
            RegisterRequestHandler<ChannelList>(Commands.ChannelList);
            RegisterRequestHandler<ChannelListAll>(Commands.ChannelListAll);
            RegisterRequestHandler<ChannelMyList>(Commands.ChannelMyList);
            RegisterRequestHandler<ChannelJoin>(Commands.ChannelJoin);
            RegisterRequestHandler<ChannelLeave>(Commands.ChannelLeave);
            RegisterRequestHandler<ChannelKick>(Commands.ChannelKick);
            RegisterRequestHandler<ChannelTalk>(Commands.ChannelTalk);
            RegisterRequestHandler<ChannelSetMemberRole>(Commands.ChannelSetMemberRole);
            RegisterRequestHandler<ChannelSetPassword>(Commands.ChannelSetPassword);
            RegisterRequestHandler<ChannelSetTopic>(Commands.ChannelSetTopic);
            RegisterRequestHandler<ChannelBan>(Commands.ChannelBan);
            RegisterRequestHandler<ChannelRemoveBan>(Commands.ChannelRemoveBan);
            RegisterRequestHandler<ChannelGetBannedMembers>(Commands.ChannelGetBannedMembers);
            RegisterRequestHandler<ChannelGlobalMute>(Commands.ChannelGlobalMute);
            RegisterRequestHandler<ChannelGetMutedCharacters>(Commands.ChannelGetMutedCharacters);
            RegisterRequestHandler<ChannelCreateForTerminals>(Commands.ChannelCreateForTerminals);
        }

        private void RegisterEffects()
        {
            _builder.RegisterType<EffectBuilder>();

            _builder.RegisterType<ZoneEffectHandler>().As<IZoneEffectHandler>();

            _builder.Register<Func<IZone, IZoneEffectHandler>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone => new ZoneEffectHandler(zone);
            });

            _builder.RegisterType<InvulnerableEffect>().Keyed<Effect>(EffectType.effect_invulnerable);
            _builder.RegisterType<CoTEffect>().Keyed<Effect>(EffectType.effect_eccm);
            _builder.RegisterType<CoTEffect>().Keyed<Effect>(EffectType.effect_stealth);

            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_core_recharge_time);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_critical_hit_chance);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_locking_time);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_signature_radius);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_fast_extraction);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_core_usage_gathering);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_siege);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_speed);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_repaired_amount);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_locking_range);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_ewar_optimal);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_armor_max);
            _builder.RegisterType<GangEffect>().Keyed<Effect>(EffectType.effect_aura_gang_shield_absorbtion_ratio);

            // intrusion effects

            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl1);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl2);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_geoscan_lvl3);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl1);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl2);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_mining_lvl3);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl1);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl2);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_harvester_lvl3);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl1);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl2);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_detection_lvl3);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl1);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl2);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_masking_lvl3);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl1);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl2);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_repair_lvl3);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl1);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl2);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_core_lvl3);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_signals_lvl4_combined);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_industrial_lvl4_combined);
            _builder.RegisterType<CorporationEffect>().Keyed<Effect>(EffectType.effect_intrusion_engineering_lvl4_combined);

            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl1);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl2);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_mining_tower_gammaterial_lvl3);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl1);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl2);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_gap_generator_masking_lvl3);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl1);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl2);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_engineering_lvl3);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl1);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl2);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_industry_lvl3);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl1);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl2);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_sensors_lvl3);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl1);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl2);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_cycle_time_lvl3);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl1);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl2);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_resist_lvl3);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl1);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl2);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_pbs_booster_sensor_lvl3);

            // New Bonuses - OPP
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_beta_bonus);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_beta2_bonus);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_alpha_bonus);
            _builder.RegisterType<AuraEffect>().Keyed<Effect>(EffectType.effect_alpha2_bonus);

            _builder.Register<EffectFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return effectType =>
                {
                    if (!ctx.IsRegisteredWithKey<Effect>(effectType))
                        return new Effect();

                    return ctx.ResolveKeyed<Effect>(effectType);
                };
            });
        }

        public void InitItems()
        {
            _builder.RegisterType<ItemDeployerHelper>();
            _builder.RegisterType<DefaultPropertyModifierReader>().AsSelf().OnActivated(e => e.Instance.Init()).SingleInstance();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterEntity<T>() where T : Entity
        {
            return _builder.RegisterType<T>().OnActivated(e =>
            {
                e.Instance.EntityServices = e.Context.Resolve<IEntityServices>();
            });
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterUnit<T>() where T : Unit
        {
            return RegisterEntity<T>().PropertiesAutowired();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterPBSObject<T>() where T : PBSObject
        {
            return RegisterUnit<T>().OnActivated(e =>
            {
                e.Instance.SetReinforceHandler(e.Context.Resolve<PBSReinforceHandler<PBSObject>>(new TypedParameter(typeof(PBSObject),e.Instance)));
                e.Instance.SetPBSObjectHelper(e.Context.Resolve<PBSObjectHelper<PBSObject>>(new TypedParameter(typeof(PBSObject),e.Instance)));
            });
        }

        private IRegistrationBuilder<T,ConcreteReflectionActivatorData,SingleRegistrationStyle> RegisterPBSProductionFacilityNode<T>() where T : PBSProductionFacilityNode
        {
            return RegisterPBSObject<T>().OnActivated(e =>
            {
                e.Instance.ProductionManager = e.Context.Resolve<ProductionManager>();
                e.Instance.SetProductionFacilityNodeHelper(e.Context.Resolve<PBSProductionFacilityNodeHelper>(new TypedParameter(typeof(PBSProductionFacilityNode), e.Instance)));
            });
        }

        protected void RegisterCorporation<T>() where T : Corporation
        {
            _builder.RegisterType<CorporationTransactionLogger>();
            RegisterEntity<T>().PropertiesAutowired();
        }

        protected void RegisterProximityProbe<T>() where T : ProximityProbeBase
        {
            RegisterUnit<T>();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterModule<T>() where T : Module
        {
            return RegisterEntity<T>();
        }

        private void RegisterEffectModule<T>() where T : EffectModule
        {
            RegisterModule<T>();
        }

        private void RegisterProductionFacility<T>() where T : ProductionFacility
        {
            RegisterEntity<T>().PropertiesAutowired();
        }

        public IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRobot<T>() where T : Robot
        {
            return RegisterUnit<T>();
        }

        private void RegisterEntities()
        {
            _builder.RegisterType<ItemHelper>();
            _builder.RegisterType<ContainerHelper>();

            _builder.RegisterType<EntityDefaultReader>().As<IEntityDefaultReader>().SingleInstance().OnActivated(e => e.Instance.Init());
            _builder.RegisterType<EntityRepository>().As<IEntityRepository>();

            _builder.RegisterType<ModulePropertyModifiersReader>().OnActivated(e => e.Instance.Init()).SingleInstance();

            _builder.RegisterType<LootItemRepository>().As<ILootItemRepository>();
            _builder.RegisterType<CoreRecharger>().As<ICoreRecharger>();
            _builder.RegisterType<UnitHelper>();
            _builder.RegisterType<DockingBaseHelper>();

            _builder.RegisterType<EntityFactory>().AsSelf().As<IEntityFactory>();

            InitItems();

            RegisterRobot<Npc>().OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<Player>().OnActivated(e => e.Instance.SetCoreRecharger(e.Context.Resolve<ICoreRecharger>()));
            RegisterRobot<PBSTurret>();
            RegisterRobot<PunchBag>();
            
            _builder.RegisterType<EntityAggregateServices>().As<IEntityServices>().PropertiesAutowired().SingleInstance();

            
            RegisterEntity<Entity>();
            RegisterCorporation<DefaultCorporation>();
            RegisterCorporation<PrivateCorporation>();
            RegisterEntity<PrivateAlliance>();
            RegisterEntity<DefaultAlliance>();

            RegisterEntity<RobotHead>();
            RegisterEntity<RobotChassis>();
            RegisterEntity<RobotLeg>();
            RegisterUnit<DockingBase>();
            RegisterUnit<PBSDockingBase>();
            RegisterUnit<Outpost>().OnActivated(e =>
            {
                var listener = new AffectOutpostStability(e.Instance);
                e.Context.Resolve<EventListenerService>().AttachListener(listener);


#if (DEBUG)
                var intrusionWaitTime = TimeRange.FromLength(TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(15));
#else
                var intrusionWaitTime = TimeRange.FromLength(TimeSpan.FromHours(8), TimeSpan.FromHours(8));
#endif
                e.Instance.IntrusionWaitTime = intrusionWaitTime;
            });
            RegisterUnit<TrainingDockingBase>();
            RegisterUnit<ItemShop>();

            RegisterEntity<PublicCorporationHangarStorage>();
            RegisterEntity<CalibrationProgram>();
            RegisterEntity<DynamicCalibrationProgram>();
            RegisterEntity<RandomCalibrationProgram>();
            RegisterEntity<CalibrationProgramCapsule>(); // OPP: new CT Capsule item

            RegisterProductionFacility<Mill>();
            RegisterProductionFacility<Prototyper>();
            RegisterProductionFacility<OutpostMill>();
            RegisterProductionFacility<OutpostPrototyper>();
            RegisterProductionFacility<OutpostRefinery>();
            RegisterProductionFacility<OutpostRepair>();
            RegisterProductionFacility<OutpostReprocessor>();
            RegisterProductionFacility<PBSMillFacility>();
            RegisterProductionFacility<PBSPrototyperFacility>();
            RegisterProductionFacility<ResearchLab>();
            RegisterProductionFacility<OutpostResearchLab>();
            RegisterProductionFacility<PBSResearchLabFacility>();
            RegisterProductionFacility<Refinery>();
            RegisterProductionFacility<Reprocessor>();
            RegisterProductionFacility<Repair>();
            RegisterProductionFacility<InsuraceFacility>();
            RegisterProductionFacility<PBSResearchKitForgeFacility>();
            RegisterProductionFacility<PBSCalibrationProgramForgeFacility>();
            RegisterProductionFacility<PBSRefineryFacility>();
            RegisterProductionFacility<PBSRepairFacility>();
            RegisterProductionFacility<PBSReprocessorFacility>();

            RegisterEntity<ResearchKit>();
            RegisterEntity<RandomResearchKit>();
            RegisterEntity<Market>();
            RegisterEntity<LotteryItem>();
            RegisterProximityProbe<VisibilityBasedProximityProbe>();
            RegisterUnit<TeleportColumn>();
            RegisterUnit<LootContainer>().OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromMinutes(15)));
            RegisterUnit<FieldContainer>().OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromHours(1)));
            RegisterUnit<MissionContainer>().OnActivated(e => e.Instance.SetDespawnTime(TimeSpan.FromMinutes(15)));
            RegisterUnit<ActiveHackingSAP>();
            RegisterUnit<PassiveHackingSAP>();
            RegisterUnit<DestructionSAP>();
            RegisterUnit<SpecimenProcessingSAP>();
            RegisterUnit<MobileTeleport>();
            RegisterUnit<NpcEgg>();

            RegisterEntity<FieldContainerCapsule>();
            RegisterEntity<Ice>();
            RegisterEntity<Ammo>();
            RegisterEntity<WeaponAmmo>();
            RegisterEntity<MiningAmmo>();
            RegisterEntity<TileScannerAmmo>();
            RegisterEntity<OneTileScannerAmmo>();
            RegisterEntity<ArtifactScannerAmmo>();
            RegisterEntity<IntrusionScannerAmmo>();
            RegisterEntity<DirectionalScannerAmmo>();
            RegisterEntity<DefaultSystemContainer>();
            RegisterEntity<PublicContainer>();
            RegisterEntity<RobotInventory>();
            RegisterEntity<InfiniteBoxContainer>();
            RegisterEntity<LimitedBoxContainer>();
            RegisterEntity<CorporateHangar>();
            RegisterEntity<CorporateHangarFolder>();
            RegisterEntity<Item>();
            RegisterEntity<MobileTeleportDeployer>();
            RegisterEntity<PlantSeedDeployer>();
            RegisterEntity<PlantSeedDeployer>();
            RegisterEntity<RiftActivator>();
            RegisterEntity<MineralScanResultItem>();

            RegisterModule<DrillerModule>();
            RegisterModule<HarvesterModule>();
            RegisterModule<Module>();
            RegisterModule<WeaponModule>();
            RegisterModule<FirearmWeaponModule>(); // OPP: new subclass for firearms
            RegisterModule<MissileWeaponModule>();
            RegisterModule<ArmorRepairModule>();
            RegisterModule<RemoteArmorRepairModule>();
            RegisterModule<CoreBoosterModule>();
            RegisterModule<SensorJammerModule>();
            RegisterModule<EnergyNeutralizerModule>();
            RegisterModule<EnergyTransfererModule>();
            RegisterModule<EnergyVampireModule>();
            RegisterModule<GeoScannerModule>();
            RegisterModule<UnitScannerModule>();
            RegisterModule<ContainerScannerModule>();
            RegisterModule<SiegeHackModule>();
            RegisterModule<NeuralyzerModule>();
            RegisterModule<BlobEmissionModulatorModule>();
            RegisterModule<TerraformMultiModule>();
            RegisterModule<WallBuilderModule>();
            RegisterModule<ConstructionModule>();
            RegisterEffectModule<WebberModule>();
            RegisterEffectModule<SensorDampenerModule>();
            RegisterEffectModule<RemoteSensorBoosterModule>();
            RegisterEffectModule<TargetPainterModule>();
            RegisterEffectModule<TargetBlinderModule>(); //OPP: NPC-only module for detection debuff
            RegisterEffectModule<SensorBoosterModule>();
            RegisterEffectModule<ArmorHardenerModule>();
            RegisterEffectModule<StealthModule>();
            RegisterEffectModule<DetectionModule>();
            RegisterEffectModule<GangModule>();
            RegisterEffectModule<ShieldGeneratorModule>();

            RegisterEntity<SystemContainer>();
            RegisterEntity<Item>();
            RegisterEntity<Item>();
            RegisterEntity<PunchBagDeployer>();

            RegisterUnit<BlobEmitterUnit>();
            RegisterUnit<Kiosk>();
            RegisterUnit<AlarmSwitch>();
            RegisterUnit<SimpleSwitch>();
            RegisterUnit<ItemSupply>();
            RegisterUnit<MobileWorldTeleport>();
            RegisterUnit<MobileStrongholdTeleport>(); // OPP: New mobile tele for entry to Strongholds
            RegisterUnit<AreaBomb>();
            RegisterUnit<PBSEgg>();
            RegisterPBSObject<PBSReactor>();
            RegisterPBSObject<PBSCoreTransmitter>();
            RegisterUnit<WallHealer>();
            RegisterPBSProductionFacilityNode<PBSResearchLabEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSRepairEnablerNode>();
            RegisterPBSObject<PBSFacilityUpgradeNode>();
            RegisterPBSProductionFacilityNode<PBSReprocessEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSMillEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSRefineryEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSPrototyperEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSCalibrationProgramForgeEnablerNode>();
            RegisterPBSProductionFacilityNode<PBSResearchKitForgeEnablerNode>();
            RegisterPBSObject<PBSEffectSupplier>();
            RegisterPBSObject<PBSEffectEmitter>();
            RegisterPBSObject<PBSMiningTower>();
            RegisterPBSObject<PBSArmorRepairerNode>();
            RegisterPBSObject<PBSControlTower>();
            RegisterPBSObject<PBSEnergyWell>();
            RegisterPBSObject<PBSHighwayNode>();
            RegisterUnit<FieldTerminal>();
            RegisterUnit<Rift>();
            RegisterUnit<TrainingKillSwitch>();
            RegisterUnit<Gate>();
            RegisterUnit<RandomRiftPortal>();
            RegisterUnit<StrongholdExitRift>(); // OPP: Special rift for exiting strongholds

            RegisterEntity<Item>();
            RegisterEntity<Item>();
            RegisterEntity<AreaBombDeployer>();
            RegisterEntity<VisibilityBasedProbeDeployer>();
            RegisterEntity<PBSDeployer>();
            RegisterEntity<WallHealerDeployer>();
            RegisterEntity<Item>();
            RegisterEntity<VolumeWrapperContainer>();
            RegisterEntity<Kernel>();
            RegisterEntity<RandomMissionItem>();
            RegisterEntity<Trashcan>();
            RegisterEntity<ZoneStorage>();
            RegisterEntity<PunchBagDeployer>();
            RegisterEntity<PlantSeedDeployer>();
            RegisterEntity<GateDeployer>();
            RegisterEntity<ExtensionPointActivator>();
            RegisterEntity<CreditActivator>();
            RegisterEntity<SparkActivator>();
            RegisterEntity<Gift>();
            RegisterEntity<Paint>(); // OPP: Robot paint item
            RegisterEntity<EPBoost>();
            RegisterEntity<Relic>();
            RegisterEntity<SAPRelic>();


            _builder.Register<Func<EntityDefault,Entity>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();

                var b = new ContainerBuilder();

                void ByDefinition<T>(int definition,params Parameter[] parameters) where T:Entity
                {
                    b.Register(_ => ctx.Resolve<T>(parameters)).Keyed<Entity>(definition);
                }

                void ByCategoryFlags<T>(CategoryFlags cf,params Parameter[] parameters) where T : Entity
                {
                    foreach (var entityDefault in ctx.Resolve<IEntityDefaultReader>().GetAll().GetByCategoryFlags(cf))
                    {
                        ByDefinition<T>(entityDefault.Definition,parameters);
                    }
                }

                void ByName<T>(string name,params Parameter[] parameters) where T : Entity
                {
                    var ed = ctx.Resolve<IEntityDefaultReader>().GetByName(name);
                    ByDefinition<T>(ed.Definition, parameters);
                }

                //TODO: bit of a hack for using the same category for many items grouped by definitionname prefixes
                //TODO: make separate category for new item groups!
                void ByNamePatternAndFlag<T>(string substr, CategoryFlags cf, params Parameter[] parameters) where T : Entity
                {
                    //TODO: this might be expensive -- string matching all defaults
                    var matches = ctx.Resolve<IEntityDefaultReader>().GetAll()
                    .Where(i => i.CategoryFlags == cf)
                    .Where(i => i.Name.Contains(substr));
                    foreach (var ed in matches)
                    {
                        ByDefinition<T>(ed.Definition, parameters);
                    }
                }

                ByName<LootContainer>(DefinitionNames.LOOT_CONTAINER_OBJECT);
                ByName<FieldContainer>(DefinitionNames.FIELD_CONTAINER);
                ByName<MissionContainer>(DefinitionNames.MISSION_CONTAINER);
                ByName<Ice>(DefinitionNames.ICE);

                ByCategoryFlags<FieldContainerCapsule>(CategoryFlags.cf_container_capsule);
                ByCategoryFlags<Npc>(CategoryFlags.cf_npc);
                ByCategoryFlags<DefaultCorporation>(CategoryFlags.cf_default_corporation);
                ByCategoryFlags<PrivateCorporation>(CategoryFlags.cf_private_corporation);
                ByCategoryFlags<PrivateAlliance>(CategoryFlags.cf_private_alliance);
                ByCategoryFlags<DefaultAlliance>(CategoryFlags.cf_default_alliance);
                ByCategoryFlags<Player>(CategoryFlags.cf_robots);
                ByCategoryFlags<Npc>(CategoryFlags.cf_npc);
                ByCategoryFlags<PunchBag>(CategoryFlags.cf_test_robot_punchbags);
                ByCategoryFlags<PunchBag>(CategoryFlags.cf_tutorial_punchbag);

                ByCategoryFlags<RobotHead>(CategoryFlags.cf_robot_head);
                ByCategoryFlags<RobotChassis>(CategoryFlags.cf_robot_chassis);
                ByCategoryFlags<RobotLeg>(CategoryFlags.cf_robot_leg);
                ByCategoryFlags<Ammo>(CategoryFlags.cf_ammo);
                ByCategoryFlags<WeaponAmmo>(CategoryFlags.cf_railgun_ammo);
                ByCategoryFlags<WeaponAmmo>(CategoryFlags.cf_laser_ammo);
                ByCategoryFlags<WeaponAmmo>(CategoryFlags.cf_projectile_ammo);
                ByCategoryFlags<WeaponAmmo>(CategoryFlags.cf_missile_ammo);
                ByCategoryFlags<MiningAmmo>(CategoryFlags.cf_mining_ammo);
                ByCategoryFlags<TileScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_tile);
                ByCategoryFlags<OneTileScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_one_tile);
                ByCategoryFlags<ArtifactScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_artifact);
                ByCategoryFlags<IntrusionScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_intrusion);
                ByCategoryFlags<DirectionalScannerAmmo>(CategoryFlags.cf_mining_probe_ammo_direction);

                ByCategoryFlags<DefaultSystemContainer>(CategoryFlags.cf_system_container);
                ByCategoryFlags<PublicContainer>(CategoryFlags.cf_public_container);
                ByCategoryFlags<RobotInventory>(CategoryFlags.cf_robot_inventory);
                ByCategoryFlags<InfiniteBoxContainer>(CategoryFlags.cf_infinite_capacity_box);
                ByCategoryFlags<LimitedBoxContainer>(CategoryFlags.cf_limited_capacity_box);
                ByCategoryFlags<CorporateHangar>(CategoryFlags.cf_corporate_hangar);
                ByCategoryFlags<CorporateHangarFolder>(CategoryFlags.cf_corporate_hangar_folder);
                ByCategoryFlags<PublicCorporationHangarStorage>(CategoryFlags.cf_public_corporation_hangar_storage);
                ByCategoryFlags<DockingBase>(CategoryFlags.cf_public_docking_base);
                ByCategoryFlags<PBSDockingBase>(CategoryFlags.cf_pbs_docking_base);
                ByCategoryFlags<Outpost>(CategoryFlags.cf_outpost);
                ByCategoryFlags<OutpostMill>(CategoryFlags.cf_outpost_mill);
                ByCategoryFlags<OutpostPrototyper>(CategoryFlags.cf_outpost_prototyper);
                ByCategoryFlags<OutpostRefinery>(CategoryFlags.cf_outpost_refinery);
                ByCategoryFlags<OutpostRepair>(CategoryFlags.cf_outpost_repair);
                ByCategoryFlags<OutpostReprocessor>(CategoryFlags.cf_outpost_reprocessor);
                ByCategoryFlags<OutpostResearchLab>(CategoryFlags.cf_outpost_research_lab);


                ByCategoryFlags<TrainingDockingBase>(CategoryFlags.cf_training_docking_base);
                ByCategoryFlags<Item>(CategoryFlags.cf_material);
                ByCategoryFlags<Item>(CategoryFlags.cf_dogtags);
                ByCategoryFlags<Market>(CategoryFlags.cf_public_market);
                ByCategoryFlags<Refinery>(CategoryFlags.cf_refinery_facility);
                ByCategoryFlags<Reprocessor>(CategoryFlags.cf_reprocessor_facility);
                ByCategoryFlags<Repair>(CategoryFlags.cf_repair_facility);
                ByCategoryFlags<InsuraceFacility>(CategoryFlags.cf_insurance_facility);
                ByCategoryFlags<ResearchKit>(CategoryFlags.cf_research_kits);
                ByCategoryFlags<ResearchLab>(CategoryFlags.cf_research_lab);
                ByCategoryFlags<Mill>(CategoryFlags.cf_mill);
                ByCategoryFlags<Prototyper>(CategoryFlags.cf_prototyper);

                ByCategoryFlags<ActiveHackingSAP>(CategoryFlags.cf_active_hacking_sap);
                ByCategoryFlags<PassiveHackingSAP>(CategoryFlags.cf_passive_hacking_sap);
                ByCategoryFlags<DestructionSAP>(CategoryFlags.cf_destrucion_sap);
                ByCategoryFlags<SpecimenProcessingSAP>(CategoryFlags.cf_specimen_processing_sap);
                ByCategoryFlags<MobileTeleportDeployer>(CategoryFlags.cf_mobile_teleport_capsule);
                ByCategoryFlags<PlantSeedDeployer>(CategoryFlags.cf_plant_seed);
                ByCategoryFlags<PlantSeedDeployer>(CategoryFlags.cf_deployable_structure);
                ByCategoryFlags<RiftActivator>(CategoryFlags.cf_npc_egg_deployer);
                ByCategoryFlags<TeleportColumn>(CategoryFlags.cf_public_teleport_column);
                ByCategoryFlags<TeleportColumn>(CategoryFlags.cf_training_exit_teleport);
                ByCategoryFlags<MobileTeleport>(CategoryFlags.cf_mobile_teleport);
                ByCategoryFlags<MineralScanResultItem>(CategoryFlags.cf_material_scan_result);
                ByCategoryFlags<NpcEgg>(CategoryFlags.cf_npc_eggs);
                ByCategoryFlags<CalibrationProgram>(CategoryFlags.cf_calibration_programs);
                ByCategoryFlags<DynamicCalibrationProgram>(CategoryFlags.cf_dynamic_cprg);
                ByCategoryFlags<RandomCalibrationProgram>(CategoryFlags.cf_random_calibration_programs);


                ByCategoryFlags<Module>(CategoryFlags.cf_robot_equipment);
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_small_lasers,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_small_crystals));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_medium_lasers,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_medium_crystals));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_large_lasers,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_large_crystals));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_small_railguns,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_small_railgun_ammo));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_medium_railguns,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_medium_railgun_ammo));
                ByCategoryFlags<WeaponModule>(CategoryFlags.cf_large_railguns,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_large_railgun_ammo));
                ByCategoryFlags<FirearmWeaponModule>(CategoryFlags.cf_small_single_projectile,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_small_projectile_ammo));
                ByCategoryFlags<FirearmWeaponModule>(CategoryFlags.cf_medium_single_projectile,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_medium_projectile_ammo));
                ByCategoryFlags<FirearmWeaponModule>(CategoryFlags.cf_large_single_projectile,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_large_projectile_ammo));
                ByCategoryFlags<MissileWeaponModule>(CategoryFlags.cf_small_missile_launchers,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_small_missile_ammo));
                ByCategoryFlags<MissileWeaponModule>(CategoryFlags.cf_medium_missile_launchers,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_medium_missile_ammo));
                ByCategoryFlags<MissileWeaponModule>(CategoryFlags.cf_large_missile_launchers,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_large_missile_ammo));
                ByCategoryFlags<ShieldGeneratorModule>(CategoryFlags.cf_shield_generators);
                ByCategoryFlags<ArmorRepairModule>(CategoryFlags.cf_armor_repair_systems);
                ByCategoryFlags<RemoteArmorRepairModule>(CategoryFlags.cf_remote_armor_repairers);
                ByCategoryFlags<CoreBoosterModule>(CategoryFlags.cf_core_boosters,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_core_booster_ammo));
                ByCategoryFlags<SensorJammerModule>(CategoryFlags.cf_sensor_jammers);
                ByCategoryFlags<EnergyNeutralizerModule>(CategoryFlags.cf_energy_neutralizers);
                ByCategoryFlags<EnergyTransfererModule>(CategoryFlags.cf_energy_transferers);
                ByCategoryFlags<EnergyVampireModule>(CategoryFlags.cf_energy_vampires);
                ByCategoryFlags<DrillerModule>(CategoryFlags.cf_drillers,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_mining_ammo));
                ByCategoryFlags<HarvesterModule>(CategoryFlags.cf_harvesters,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_harvesting_ammo));
                ByCategoryFlags<GeoScannerModule>(CategoryFlags.cf_mining_probes,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_mining_probe_ammo));
                ByCategoryFlags<UnitScannerModule>(CategoryFlags.cf_chassis_scanner);
                ByCategoryFlags<ContainerScannerModule>(CategoryFlags.cf_cargo_scanner);
                ByCategoryFlags<SiegeHackModule>(CategoryFlags.cf_siege_hack_modules);
                ByCategoryFlags<NeuralyzerModule>(CategoryFlags.cf_neuralyzer);
                ByCategoryFlags<BlobEmissionModulatorModule>(CategoryFlags.cf_blob_emission_modulator,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_blob_emission_modulator_ammo));
                ByCategoryFlags<WebberModule>(CategoryFlags.cf_webber);
                ByCategoryFlags<SensorDampenerModule>(CategoryFlags.cf_sensor_dampeners);
                ByCategoryFlags<RemoteSensorBoosterModule>(CategoryFlags.cf_remote_sensor_boosters);
                ByCategoryFlags<TargetPainterModule>(CategoryFlags.cf_target_painter);
                ByCategoryFlags<SensorBoosterModule>(CategoryFlags.cf_sensor_boosters);
                ByCategoryFlags<ArmorHardenerModule>(CategoryFlags.cf_armor_hardeners);
                ByCategoryFlags<StealthModule>(CategoryFlags.cf_stealth_modules);
                ByCategoryFlags<DetectionModule>(CategoryFlags.cf_detection_modules);
                ByCategoryFlags<Module>(CategoryFlags.cf_armor_plates);
                ByCategoryFlags<Module>(CategoryFlags.cf_core_batteries);
                ByCategoryFlags<Module>(CategoryFlags.cf_core_rechargers);
                ByCategoryFlags<Module>(CategoryFlags.cf_maneuvering_equipment);
                ByCategoryFlags<Module>(CategoryFlags.cf_powergrid_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_cpu_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_mining_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_massmodifiers);
                ByCategoryFlags<Module>(CategoryFlags.cf_weapon_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_tracking_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_armor_repair_upgrades);
                ByCategoryFlags<Module>(CategoryFlags.cf_kers);
                ByCategoryFlags<Module>(CategoryFlags.cf_shield_hardener);
                ByCategoryFlags<Module>(CategoryFlags.cf_eccm);
                ByCategoryFlags<Module>(CategoryFlags.cf_resistance_plating);

                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_speed,new NamedParameter("effectType",EffectType.effect_aura_gang_speed),new NamedParameter("effectModifier",AggregateField.effect_speed_max_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_defense,new NamedParameter("effectType",EffectType.effect_aura_gang_armor_max),new NamedParameter("effectModifier",AggregateField.effect_armor_max_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_information,new NamedParameter("effectType",EffectType.effect_aura_gang_locking_range),new NamedParameter("effectModifier",AggregateField.effect_locking_range_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_industry,new NamedParameter("effectType",EffectType.effect_aura_gang_core_usage_gathering),new NamedParameter("effectModifier",AggregateField.effect_core_usage_gathering_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_shared_dataprocessing,new NamedParameter("effectType",EffectType.effect_aura_gang_locking_time),new NamedParameter("effectModifier",AggregateField.effect_locking_time_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_coordinated_manuevering,new NamedParameter("effectType",EffectType.effect_aura_gang_signature_radius),new NamedParameter("effectModifier",AggregateField.effect_signature_radius_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_maintance,new NamedParameter("effectType",EffectType.effect_aura_gang_repaired_amount),new NamedParameter("effectModifier",AggregateField.effect_repair_amount_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_precision_firing,new NamedParameter("effectType",EffectType.effect_aura_gang_critical_hit_chance),new NamedParameter("effectModifier",AggregateField.effect_critical_hit_chance_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_core_management,new NamedParameter("effectType",EffectType.effect_aura_gang_core_recharge_time),new NamedParameter("effectModifier",AggregateField.effect_core_recharge_time_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_shield_calculations,new NamedParameter("effectType",EffectType.effect_aura_gang_shield_absorbtion_ratio),new NamedParameter("effectModifier",AggregateField.effect_shield_absorbtion_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_siege,new NamedParameter("effectType",EffectType.effect_aura_gang_siege),new NamedParameter("effectModifier",AggregateField.effect_weapon_cycle_time_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_ewar,new NamedParameter("effectType",EffectType.effect_aura_gang_ewar_optimal),new NamedParameter("effectModifier",AggregateField.effect_ew_optimal_range_modifier));
                ByCategoryFlags<GangModule>(CategoryFlags.cf_gang_assist_fast_extracting,new NamedParameter("effectType",EffectType.effect_aura_gang_fast_extraction),new NamedParameter("effectModifier",AggregateField.effect_gathering_cycle_time_modifier));


                ByCategoryFlags<SystemContainer>(CategoryFlags.cf_logical_storage);
                ByCategoryFlags<Item>(CategoryFlags.cf_mission_items);
                ByCategoryFlags<Item>(CategoryFlags.cf_robotshards);
                ByCategoryFlags<PunchBagDeployer>(CategoryFlags.cf_others);
                ByCategoryFlags<BlobEmitterUnit>(CategoryFlags.cf_blob_emitter);
                ByCategoryFlags<Item>(CategoryFlags.cf_reactor_cores);
                ByCategoryFlags<Kiosk>(CategoryFlags.cf_kiosk);
                ByCategoryFlags<AlarmSwitch>(CategoryFlags.cf_alarm_switch);
                ByCategoryFlags<SimpleSwitch>(CategoryFlags.cf_simple_switch);
                ByCategoryFlags<ItemSupply>(CategoryFlags.cf_item_supply);
                ByCategoryFlags<MobileWorldTeleport>(CategoryFlags.cf_mobile_world_teleport);
                ByNamePatternAndFlag<MobileStrongholdTeleport>("def_mobile_teleport_stronghold", CategoryFlags.cf_mobile_world_teleport); // OPP: stronghold tele
                ByCategoryFlags<Item>(CategoryFlags.cf_mission_coin);
                ByCategoryFlags<AreaBomb>(CategoryFlags.cf_area_bomb);
                ByCategoryFlags<AreaBombDeployer>(CategoryFlags.cf_plasma_bomb);
                ByCategoryFlags<VisibilityBasedProximityProbe>(CategoryFlags.cf_visibility_based_probe);
                ByCategoryFlags<RandomResearchKit>(CategoryFlags.cf_random_research_kits);
                ByCategoryFlags<LotteryItem>(CategoryFlags.cf_lottery_items);

                //TODO ORDER MATTERS!  Register Paints AFTER lottery will ensure Paint objects are valid subset of lottery category
                //TODO entitydefaults must contain name "def_paint" and have cf_lottery_items
                //TODO make separate category
                ByNamePatternAndFlag<Paint>("def_paint", CategoryFlags.cf_lottery_items);

                //TODO new CalibrationTemplateItem -- activates like paint! same category!
                ByNamePatternAndFlag<CalibrationProgramCapsule>("_CT_capsule", CategoryFlags.cf_lottery_items);

                // TODO new ep boost item -- activates like paint
                ByNamePatternAndFlag<EPBoost>("def_boost_ep", CategoryFlags.cf_lottery_items);

                // TODO new Blinder module
                ByNamePatternAndFlag<TargetBlinderModule>(DefinitionNames.STANDARD_BLINDER_MODUL, CategoryFlags.cf_target_painter);

                //New Relic Definition for Units
                ByNamePatternAndFlag<Relic>(DefinitionNames.RELIC, CategoryFlags.undefined);
                ByNamePatternAndFlag<SAPRelic>(DefinitionNames.RELIC_SAP, CategoryFlags.undefined);

                ByCategoryFlags<VisibilityBasedProbeDeployer>(CategoryFlags.cf_proximity_probe_deployer);
                ByCategoryFlags<Item>(CategoryFlags.cf_gift_packages);
                ByCategoryFlags<PBSDeployer>(CategoryFlags.cf_pbs_capsules);
                ByCategoryFlags<PBSEgg>(CategoryFlags.cf_pbs_egg);
                ByCategoryFlags<PBSReactor>(CategoryFlags.cf_pbs_reactor);
                ByCategoryFlags<PBSCoreTransmitter>(CategoryFlags.cf_pbs_core_transmitter);
                ByCategoryFlags<WallHealer>(CategoryFlags.cf_wall_healer);
                ByCategoryFlags<WallHealerDeployer>(CategoryFlags.cf_wall_healer_capsule);
                ByCategoryFlags<PBSResearchLabEnablerNode>(CategoryFlags.cf_pbs_reseach_lab_nodes);
                ByCategoryFlags<PBSRepairEnablerNode>(CategoryFlags.cf_pbs_repair_nodes);
                ByCategoryFlags<PBSFacilityUpgradeNode>(CategoryFlags.cf_pbs_production_upgrade_nodes);
                ByCategoryFlags<PBSReprocessEnablerNode>(CategoryFlags.cf_pbs_reprocessor_nodes);
                ByCategoryFlags<PBSMillEnablerNode>(CategoryFlags.cf_pbs_mill_nodes);
                ByCategoryFlags<PBSRefineryEnablerNode>(CategoryFlags.cf_pbs_refinery_nodes);
                ByCategoryFlags<PBSPrototyperEnablerNode>(CategoryFlags.cf_pbs_prototyper_nodes);
                ByCategoryFlags<PBSCalibrationProgramForgeEnablerNode>(CategoryFlags.cf_pbs_calibration_forge_nodes);
                ByCategoryFlags<PBSResearchKitForgeEnablerNode>(CategoryFlags.cf_pbs_research_kit_forge_nodes);
                ByCategoryFlags<PBSEffectSupplier>(CategoryFlags.cf_pbs_effect_supplier);
                ByCategoryFlags<PBSEffectEmitter>(CategoryFlags.cf_pbs_effect_emitter);
                ByCategoryFlags<PBSMiningTower>(CategoryFlags.cf_pbs_mining_towers);
                ByCategoryFlags<PBSTurret>(CategoryFlags.cf_pbs_turret);
                ByCategoryFlags<PBSArmorRepairerNode>(CategoryFlags.cf_pbs_armor_repairer);
                ByCategoryFlags<PBSResearchKitForgeFacility>(CategoryFlags.cf_research_kit_forge);
                ByCategoryFlags<PBSCalibrationProgramForgeFacility>(CategoryFlags.cf_calibration_program_forge);
                ByCategoryFlags<PBSControlTower>(CategoryFlags.cf_pbs_control_tower);
                ByCategoryFlags<Item>(CategoryFlags.cf_pbs_reactor_booster);
                ByCategoryFlags<VolumeWrapperContainer>(CategoryFlags.cf_volume_wrapper_container);
                ByCategoryFlags<Kernel>(CategoryFlags.cf_kernels);
                ByCategoryFlags<PBSEnergyWell>(CategoryFlags.cf_pbs_energy_well);
                ByCategoryFlags<PBSHighwayNode>(CategoryFlags.cf_pbs_highway_node);
                ByCategoryFlags<FieldTerminal>(CategoryFlags.cf_field_terminal);
                ByCategoryFlags<RandomMissionItem>(CategoryFlags.cf_generic_random_items);
                ByCategoryFlags<Rift>(CategoryFlags.cf_rifts);

                ByCategoryFlags<ExtensionPointActivator>(CategoryFlags.cf_package_activator_ep);
                ByCategoryFlags<CreditActivator>(CategoryFlags.cf_package_activator_credit);
                ByCategoryFlags<SparkActivator>(CategoryFlags.cf_package_activator_spark);
                ByCategoryFlags<ItemShop>(CategoryFlags.cf_zone_item_shop);


                ByName<TrainingKillSwitch>(DefinitionNames.TRAINING_KILL_SWITCH);
                ByName<Trashcan>(DefinitionNames.ADMIN_TRASHCAN);
                ByName<ZoneStorage>(DefinitionNames.ZONE_STORAGE);
                ByName<PunchBagDeployer>(DefinitionNames.DEPLOY_PUNCHBAG);
                ByName<TerraformMultiModule>(DefinitionNames.TERRAFORM_MULTI_MODULE,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_ammo_terraforming_multi));
                ByName<WallBuilderModule>(DefinitionNames.STANDARD_WALL_BUILDER,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_wall_builder_ammo));
                ByName<PBSMillFacility>(DefinitionNames.PBS_FACILITY_MILL);
                ByName<PBSPrototyperFacility>(DefinitionNames.PBS_FACILITY_PROTOTYPER);
                ByName<PBSRefineryFacility>(DefinitionNames.PBS_FACILITY_REFINERY);
                ByName<PBSRepairFacility>(DefinitionNames.PBS_FACILITY_REPAIR);
                ByName<PBSReprocessorFacility>(DefinitionNames.PBS_FACILITY_REPROCESSOR);
                ByName<PBSResearchLabFacility>(DefinitionNames.PBS_FACILITY_RESEARCH_LAB);
                ByName<ConstructionModule>(DefinitionNames.PBS_CONSTRUCTION_MODULE,new NamedParameter("ammoCategoryFlags",CategoryFlags.cf_construction_ammo));
                ByName<PlantSeedDeployer>(DefinitionNames.PLANT_SEED_DEVRINOL);
                ByName<Gate>(DefinitionNames.GATE);
                ByName<GateDeployer>(DefinitionNames.GATE_CAPSULE);
                ByName<RandomRiftPortal>(DefinitionNames.RANDOM_RIFT_PORTAL);
                ByName<ItemShop>(DefinitionNames.BASE_ITEM_SHOP);
                ByName<Gift>(DefinitionNames.ANNIVERSARY_PACKAGE);
                ByName<StrongholdExitRift>(DefinitionNames.STRONGHOLD_EXIT_RIFT);

                var c = b.Build();

                return ed =>
                {
                    Entity entity;
                    if (!c.IsRegisteredWithKey<Entity>(ed.Definition))
                    {
                        entity = ctx.Resolve<Entity>();
                    }
                    else
                    {
                        entity = c.ResolveKeyed<Entity>(ed.Definition);
                    }

                    entity.ED = ed;
                    entity.Health = ed.Health;
                    entity.Quantity = ed.Quantity;
                    entity.IsRepackaged = ed.AttributeFlags.Repackable;
                    return entity;
                };
            }).SingleInstance();
        }

        private void RegisterLoggers()
        {
            _builder.Register(x =>
            {
                return new LoggerCache(new MemoryCache("LoggerCache"))
                {
                    Expiration = TimeSpan.FromHours(1)
                };
            }).As<ILoggerCache>().SingleInstance();

            _builder.Register<ChannelLoggerFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return name =>
                {
                    var fileLogger = ctx.Resolve<Func<string, string, FileLogger<ChatLogEvent>>>().Invoke("channels", name);
                    return new ChannelLogger(fileLogger);
                };
            });

            _builder.RegisterGeneric(typeof(FileLogger<>));

            _builder.Register<Func<string, string, FileLogger<ChatLogEvent>>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return ((directory, filename) =>
                {
                    var formatter = new ChatLogFormatter();
                    return ctx.Resolve<FileLogger<ChatLogEvent>.Factory>().Invoke(formatter,() => Path.Combine("chatlogs",directory,filename,DateTime.Now.ToString("yyyy-MM-dd"), $"{filename.RemoveSpecialCharacters()}.txt"));
                });
            });

            _builder.Register<ChatLoggerFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return (directory, filename) =>
                {
                    var fileLogger = ctx.Resolve<Func<string, string, FileLogger<ChatLogEvent>>>().Invoke(directory, filename);
                    return fileLogger;
                };
            });


            _builder.Register(c =>
            {
                var defaultFormater = new DefaultLogEventFormatter();

                var formater = new DelegateLogEventFormatter<LogEvent, string>(e =>
                {
                    var formatedEvent = defaultFormater.Format(e);

                    var gex = e.ThrownException as PerpetuumException;
                    if (gex == null)
                        return formatedEvent;

                    var sb = new StringBuilder(formatedEvent);

                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendFormat("Error = {0}\n", gex.error);

                    if (gex.Data.Count > 0)
                        sb.AppendFormat("Data: {0}", gex.Data.ToDictionary().ToDebugString());

                    return sb.ToString();
                });

                var fileLogger = c.Resolve<FileLogger<LogEvent>.Factory>().Invoke(formater, () => Path.Combine("logs", DateTime.Now.ToString("yyyy-MM-dd"), "hostlog.txt"));
                fileLogger.BufferSize = 100;
                fileLogger.AutoFlushInterval = TimeSpan.FromSeconds(10);

                return new CompositeLogger<LogEvent>(fileLogger, new ColoredConsoleLogger(formater));
            }).As<ILogger<LogEvent>>();

            _builder.RegisterType<CombatLogger>();
            _builder.RegisterType<CombatLogHelper>();
            _builder.RegisterType<CombatSummary>();
            _builder.RegisterType<CombatLogSaver>().As<ICombatLogSaver>();

            _builder.RegisterGeneric(typeof(DbLogger<>));

        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> 
            RegisterPresence<T>(PresenceType presenceType) where T:Presence
        {
            return _builder.RegisterType<T>().Keyed<Presence>(presenceType).PropertiesAutowired();
        }

        private void RegisterFlock<T>(PresenceType presenceType) where T : Flock
        {
            _builder.RegisterType<T>().Keyed<Flock>(presenceType).OnActivated(e =>
            {
                e.Instance.EntityService = e.Context.Resolve<IEntityServices>();
                e.Instance.LootService = e.Context.Resolve<ILootService>();
            });
        }

        public void RegisterNpcs()
        {
            _builder.RegisterType<NpcReinforcementsRepository>().SingleInstance().As<INpcReinforcementsRepository>();

            _builder.RegisterType<FlockConfiguration>().As<IFlockConfiguration>();
            _builder.RegisterType<FlockConfigurationBuilder>();
            _builder.RegisterType<IntIDGenerator>().Named<IIDGenerator<int>>("directFlockIDGenerator").SingleInstance().WithParameter("startID",25000);


            _builder.RegisterType<FlockConfigurationRepository>().OnActivated(e =>
            {
                e.Instance.LoadAllConfig();
            }).As<IFlockConfigurationRepository>().SingleInstance();

            _builder.RegisterType<RandomFlockSelector>().As<IRandomFlockSelector>();

            _builder.RegisterType<RandomFlockReader>()
                .As<IRandomFlockReader>()
                .SingleInstance()
                .OnActivated(e => e.Instance.Init());

            _builder.RegisterType<NpcSafeSpawnPointsRepository>().As<ISafeSpawnPointsRepository>();
            _builder.RegisterType<PresenceConfigurationReader>().As<IPresenceConfigurationReader>();
            _builder.RegisterType<InterzonePresenceConfigReader>().As<IInterzonePresenceConfigurationReader>();
            _builder.RegisterType<InterzoneGroup>().As<IInterzoneGroup>();
            _builder.RegisterType<PresenceManager>().OnActivated(e =>
            {
                var pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.AsTimed(TimeSpan.FromSeconds(2)).ToAsync());

                e.Instance.LoadAll();

            }).As<IPresenceManager>();

            _builder.Register<Func<IZone, IPresenceManager>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    var presenceFactory = ctx.Resolve<PresenceFactory>();
                    var presenceService = ctx.Resolve<PresenceManager.Factory>().Invoke(zone,presenceFactory);
                    return presenceService;
                };
            });

            _builder.Register<FlockFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();

                return ((configuration, presence) =>
                {
                    return ctx.ResolveKeyed<Flock>(presence.Configuration.PresenceType, TypedParameter.From(configuration), TypedParameter.From(presence));
                });
            });

            RegisterFlock<NormalFlock>(PresenceType.Normal);
            RegisterFlock<Flock>(PresenceType.Direct);
            RegisterFlock<NormalFlock>(PresenceType.DynamicPool);
            RegisterFlock<NormalFlock>(PresenceType.Dynamic);
            RegisterFlock<RemoteSpawningFlock>(PresenceType.DynamicExtended);
            RegisterFlock<Flock>(PresenceType.Random);
            RegisterFlock<Flock>(PresenceType.Roaming);
            RegisterFlock<NormalFlock>(PresenceType.FreeRoaming);
            RegisterFlock<NormalFlock>(PresenceType.Interzone);
            RegisterFlock<NormalFlock>(PresenceType.InterzoneRoaming);

            RegisterPresence<Presence>(PresenceType.Normal);
            RegisterPresence<DirectPresence>(PresenceType.Direct).OnActivated(e =>
            {
                e.Instance.FlockIDGenerator = e.Context.ResolveNamed<IIDGenerator<int>>("directFlockIDGenerator");
            });
            RegisterPresence<DynamicPoolPresence>(PresenceType.DynamicPool);
            RegisterPresence<DynamicPresence>(PresenceType.Dynamic);
            RegisterPresence<DynamicPresenceExtended>(PresenceType.DynamicExtended);
            RegisterPresence<RandomPresence>(PresenceType.Random);
            RegisterPresence<RoamingPresence>(PresenceType.Roaming);
            RegisterPresence<RoamingPresence>(PresenceType.FreeRoaming);
            RegisterPresence<InterzonePresence>(PresenceType.Interzone);
            RegisterPresence<InterzoneRoamingPresence>(PresenceType.InterzoneRoaming);

            _builder.Register<PresenceFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return ((zone, configuration) =>
                {
                    if (!ctx.IsRegisteredWithKey<Presence>(configuration.PresenceType))
                        return null;

                    var p = ctx.ResolveKeyed<Presence>(configuration.PresenceType,TypedParameter.From(zone),TypedParameter.From(configuration));

                    if (p is IRoamingPresence roamingPresence)
                    {
                        switch (p.Configuration.PresenceType)
                        {
                            case PresenceType.Roaming:
                            {
                                roamingPresence.PathFinder = new NormalRoamingPathFinder(zone);
                                break;
                            }
                            case PresenceType.FreeRoaming:
                            {
                                roamingPresence.PathFinder = new FreeRoamingPathFinder(zone);
                                break;
                            }
                            case PresenceType.InterzoneRoaming:
                            {
                                roamingPresence.PathFinder = new FreeRoamingPathFinder(zone);
                                break;
                            }
                        }
                    }

                    return p;
                });
            });


        }

        private void RegisterMtProducts()
        {
            _builder.RegisterType<MtProductRepository>().As<IMtProductRepository>();
            _builder.RegisterType<MtProductHelper>();
            RegisterRequestHandler<MtProductPriceList>(Commands.MtProductPriceList);
        }

        private void RegisterMissions()
        {
            _builder.RegisterType<DisplayMissionSpotsProcess>();
            _builder.RegisterType<MissionDataCache>().SingleInstance();
            _builder.RegisterType<MissionHandler>();
            _builder.RegisterType<MissionInProgress>();
            _builder.RegisterType<MissionAdministrator>();
            _builder.RegisterType<MissionProcessor>().OnActivated(e =>
            {
                var pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            }).SingleInstance();

            RegisterRequestHandler<MissionData>(Commands.MissionData);
            RegisterRequestHandler<MissionStart>(Commands.MissionStart);
            RegisterRequestHandler<MissionAbort>(Commands.MissionAbort);
            RegisterRequestHandler<MissionAdminListAll>(Commands.MissionAdminListAll);
            RegisterRequestHandler<MissionAdminTake>(Commands.MissionAdminTake);
            RegisterRequestHandler<MissionLogList>(Commands.MissionLogList);
            RegisterRequestHandler<MissionListRunning>(Commands.MissionListRunning);
            RegisterRequestHandler<MissionReloadCache>(Commands.MissionReloadCache);
            RegisterRequestHandler<MissionGetOptions>(Commands.MissionGetOptions);
            RegisterRequestHandler<MissionResolveTest>(Commands.MissionResolveTest);
            RegisterRequestHandler<MissionDeliver>(Commands.MissionDeliver);
            RegisterRequestHandler<MissionFlush>(Commands.MissionFlush);
            RegisterRequestHandler<MissionReset>(Commands.MissionReset);
            RegisterRequestHandler<MissionListAgents>(Commands.MissionListAgents);

            _builder.RegisterType<DeliveryHelper>();
            _builder.RegisterType<MissionTargetInProgress>();
        }

        private void InitRelayManager()
        {            
            _builder.RegisterType<MarketHelper>().SingleInstance();
            _builder.RegisterType<MarketHandler>().SingleInstance();

            _builder.RegisterType<MarketOrder>();
            _builder.RegisterType<MarketOrderRepository>().As<IMarketOrderRepository>();
            _builder.Register(c => new MarketInfoService(0.3, 10, false)).As<IMarketInfoService>();

            _builder.RegisterType<MarketRobotPriceWriter>().As<IMarketRobotPriceWriter>().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromHours(4)));
            });

            _builder.RegisterType<GangInviteService>().As<IGangInviteService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(2)));
            });

            _builder.RegisterType<VolunteerCEOService>().As<IVolunteerCEOService>();

            _builder.RegisterType<VolunteerCEORepository>().As<IVolunteerCEORepository>();

            _builder.RegisterType<CorporationLogger>();
            _builder.RegisterType<CorporationManager>().As<ICorporationManager>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            });
            _builder.RegisterType<CorporateInvites>();
            _builder.RegisterType<BulletinHandler>().As<IBulletinHandler>();
            _builder.RegisterType<VoteHandler>().As<IVoteHandler>();

            _builder.RegisterType<ReprocessSessionMember>();
            _builder.RegisterType<ReprocessSession>();

            _builder.RegisterType<ProductionDataAccess>().OnActivated(e =>
            {
                e.Instance.Init();
            }).As<IProductionDataAccess>().SingleInstance();
            _builder.RegisterType<ProductionDescription>();
            _builder.RegisterType<ProductionComponentCollector>();
            _builder.RegisterType<ProductionInProgressRepository>().As<IProductionInProgressRepository>();
            _builder.RegisterType<ProductionLine>();
            _builder.RegisterType<ProductionInProgress>();
            _builder.RegisterType<ProductionProcessor>().SingleInstance().OnActivated(e =>
            {
                e.Instance.InitProcessor();
            });
            _builder.RegisterType<ProductionManager>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            });

            _builder.RegisterType<LoginQueueService>().As<ILoginQueueService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(5)));
            });


            _builder.RegisterType<RelayStateService>().As<IRelayStateService>().SingleInstance();
            _builder.RegisterType<RelayInfoBuilder>();

            _builder.RegisterType<TradeService>().SingleInstance().As<ITradeService>();

            _builder.RegisterType<HostShutDownManager>().SingleInstance();

            _builder.RegisterType<HighScoreService>().As<IHighScoreService>();
            _builder.RegisterType<CorporationHandler>();
            _builder.RegisterType<TerraformHandler>().OnActivated(e =>
            {
                var pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.AsTimed(TimeSpan.FromMilliseconds(200)).ToAsync());
            });

            _builder.RegisterType<InsuranceHelper>();
            _builder.RegisterType<InsurancePayOut>();
            _builder.RegisterType<InsuranceDescription>();
            _builder.RegisterType<CharacterCleaner>();

            _builder.RegisterType<SparkTeleportRepository>().As<ISparkTeleportRepository>();
            _builder.RegisterType<SparkTeleportHelper>();
 
            _builder.RegisterType<SparkExtensionsReader>().As<ISparkExtensionsReader>();
            _builder.RegisterType<SparkRepository>().As<ISparkRepository>();
            _builder.RegisterType<SparkHelper>();


            _builder.RegisterType<Trade>();

            _builder.RegisterType<GoodiePackHandler>();

            // OPP: EPBonusEventService singleton
            _builder.RegisterType<EPBonusEventService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromMinutes(1)));
            });

            // OPP: EventListenerService and consumers
            _builder.RegisterType<ChatEcho>();
            _builder.RegisterType<NpcChatEcho>();
            _builder.RegisterType<AffectOutpostStability>();
            _builder.RegisterType<OreNpcSpawner>().As<NpcSpawnEventHandler<OreNpcSpawnMessage>>();
            _builder.RegisterType<NpcReinforcementSpawner>().As<NpcSpawnEventHandler<NpcReinforcementsMessage>>();
            _builder.RegisterType<EventListenerService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(0.75)));
                e.Instance.AttachListener(e.Context.Resolve<ChatEcho>());
                e.Instance.AttachListener(e.Context.Resolve<NpcChatEcho>());
                var obs = new GameTimeObserver(e.Instance);
                obs.Subscribe(e.Context.Resolve<IGameTimeService>());
            });

            _builder.RegisterType<GameTimeService>().As<IGameTimeService>().SingleInstance().OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromMinutes(15)));
            });

            // OPP: InterzoneNPCManager
            RegisterAutoActivate<InterzonePresenceManager>(TimeSpan.FromSeconds(10));

            _builder.RegisterType<AccountManager>().As<IAccountManager>();

            _builder.RegisterType<Account>();
            _builder.RegisterType<AccountWallet>().AsSelf().As<IAccountWallet>();
            _builder.Register<AccountWalletFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return ((account, type) =>
                {
                    return ctx.Resolve<AccountWallet>(new TypedParameter(typeof(Account), account),
                        new TypedParameter(typeof(AccountTransactionType), type));
                });
            });
            _builder.RegisterType<AccountTransactionLogger>();
            _builder.RegisterType<EpForActivityLogger>();
        }

        private IRegistrationBuilder<TRequestHandler, ConcreteReflectionActivatorData, SingleRegistrationStyle> 
            RegisterRequestHandler<TRequestHandler,TRequest>(Command command) where TRequestHandler:IRequestHandler<TRequest> where TRequest : IRequest
        {
            var res = _builder.RegisterType<TRequestHandler>();

            _builder.Register(c =>
            {
                return c.Resolve<RequestHandlerProfiler<TRequest>>(new TypedParameter(typeof(IRequestHandler<TRequest>), c.Resolve<TRequestHandler>()));
            }).Keyed<IRequestHandler<TRequest>>(command);

            return res;
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterRequestHandler<T>(Command command) where T : IRequestHandler<IRequest>
        {
            return RegisterRequestHandler<T, IRequest>(command);
        }

        private void RegisterRequestHandlerFactory<T>() where T : IRequest
        {
            _builder.Register<RequestHandlerFactory<T>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return (command =>
                {
                    return ctx.IsRegisteredWithKey<IRequestHandler<T>>(command) ? ctx.ResolveKeyed<IRequestHandler<T>>(command) : null;
                });
            });
        }

        private void RegisterRequestHandlers()
        {
            _builder.RegisterGeneric(typeof(RequestHandlerProfiler<>));

            RegisterRequestHandlerFactory<IRequest>();
            RegisterRequestHandlerFactory<IZoneRequest>();

            RegisterRequestHandler<GetEnums>(Commands.GetEnums);
            RegisterRequestHandler<GetCommands>(Commands.GetCommands);
            RegisterRequestHandler<GetEntityDefaults>(Commands.GetEntityDefaults).SingleInstance();
            RegisterRequestHandler<GetAggregateFields>(Commands.GetAggregateFields).SingleInstance();
            RegisterRequestHandler<GetDefinitionConfigUnits>(Commands.GetDefinitionConfigUnits).SingleInstance();
            RegisterRequestHandler<GetEffects>(Commands.GetEffects).SingleInstance();
            RegisterRequestHandler<GetDistances>(Commands.GetDistances);
            RegisterRequestHandler<SignIn>(Commands.SignIn);
            RegisterRequestHandler<SignInSteam>(Commands.SignInSteam);
            RegisterRequestHandler<SignOut>(Commands.SignOut);
            RegisterRequestHandler<SteamListAccounts>(Commands.SteamListAccounts);
            RegisterRequestHandler<AccountConfirmEmail>(Commands.AccountConfirmEmail);
            RegisterRequestHandler<CharacterList>(Commands.CharacterList);
            RegisterRequestHandler<CharacterCreate>(Commands.CharacterCreate);
            RegisterRequestHandler<CharacterSelect>(Commands.CharacterSelect);
            RegisterRequestHandler<CharacterDeselect>(Commands.CharacterDeselect);
            RegisterRequestHandler<CharacterForceDeselect>(Commands.CharacterForceDeselect);
            RegisterRequestHandler<CharacterForceDisconnect>(Commands.CharacterForceDisconnect);
            RegisterRequestHandler<CharacterDelete>(Commands.CharacterDelete);
            RegisterRequestHandler<CharacterSetHomeBase>(Commands.CharacterSetHomeBase);
            RegisterRequestHandler<CharacterGetProfiles>(Commands.CharacterGetProfiles);
            RegisterRequestHandler<CharacterRename>(Commands.CharacterRename);
            RegisterRequestHandler<CharacterCheckNick>(Commands.CharacterCheckNick);
            RegisterRequestHandler<CharacterCorrectNick>(Commands.CharacterCorrectNick);
            RegisterRequestHandler<CharacterIsOnline>(Commands.IsOnline);
            RegisterRequestHandler<CharacterSettingsSet>(Commands.CharacterSettingsSet);
            RegisterRequestHandler<CharacterSetMoodMessage>(Commands.CharacterSetMoodMessage);
            RegisterRequestHandler<CharacterTransferCredit>(Commands.CharacterTransferCredit);
            RegisterRequestHandler<CharacterSetAvatar>(Commands.CharacterSetAvatar);
            RegisterRequestHandler<CharacterSetBlockTrades>(Commands.CharacterSetBlockTrades);
            RegisterRequestHandler<CharacterSetCredit>(Commands.CharacterSetCredit);
            RegisterRequestHandler<CharacterClearHomeBase>(Commands.CharacterClearHomeBase);
            RegisterRequestHandler<CharacterSettingsGet>(Commands.CharacterSettingsGet);
            RegisterRequestHandler<CharacterGetMyProfile>(Commands.CharacterGetMyProfile);
            RegisterRequestHandler<CharacterSearch>(Commands.CharacterSearch);
            RegisterRequestHandler<CharacterRemoveFromCache>(Commands.CharacterRemoveFromCache);
            RegisterRequestHandler<CharacterListNpcDeath>(Commands.CharacterListNpcDeath);
            RegisterRequestHandler<CharacterTransactionHistory>(Commands.CharacterTransactionHistory);
            RegisterRequestHandler<CharacterGetZoneInfo>(Commands.CharacterGetZoneInfo);
            RegisterRequestHandler<CharacterNickHistory>(Commands.CharacterNickHistory);
            RegisterRequestHandler<CharacterGetNote>(Commands.CharacterGetNote);
            RegisterRequestHandler<CharacterSetNote>(Commands.CharacterSetNote);
            RegisterRequestHandler<CharacterCorporationHistory>(Commands.CharacterCorporationHistory);
            RegisterRequestHandler<CharacterWizardData>(Commands.CharacterWizardData).SingleInstance();
            RegisterRequestHandler<CharactersOnline>(Commands.GetCharactersOnline);
            RegisterRequestHandler<ReimburseItemRequestHandler>(Commands.ReimburseItem);
            RegisterRequestHandler<Chat>(Commands.Chat);
            RegisterRequestHandler<GoodiePackList>(Commands.GoodiePackList);
            RegisterRequestHandler<GoodiePackRedeem>(Commands.GoodiePackRedeem);
            RegisterRequestHandler<Ping>(Commands.Ping);
            RegisterRequestHandler<Quit>(Commands.Quit);
            RegisterRequestHandler<SetMaxUserCount>(Commands.SetMaxUserCount);
            RegisterRequestHandler<SparkTeleportSet>(Commands.SparkTeleportSet);
            RegisterRequestHandler<SparkTeleportUse>(Commands.SparkTeleportUse);
            RegisterRequestHandler<SparkTeleportDelete>(Commands.SparkTeleportDelete);
            RegisterRequestHandler<SparkTeleportList>(Commands.SparkTeleportList);
            RegisterRequestHandler<SparkChange>(Commands.SparkChange);
            RegisterRequestHandler<SparkRemove>(Commands.SparkRemove);
            RegisterRequestHandler<SparkList>(Commands.SparkList);
            RegisterRequestHandler<SparkSetDefault>(Commands.SparkSetDefault);
            RegisterRequestHandler<SparkUnlock>(Commands.SparkUnlock);
            RegisterRequestHandler<Undock>(Commands.Undock);
            RegisterRequestHandler<ProximityProbeRegisterSet>(Commands.ProximityProbeRegisterSet);
            RegisterRequestHandler<ProximityProbeSetName>(Commands.ProximityProbeSetName);
            RegisterRequestHandler<ProximityProbeList>(Commands.ProximityProbeList);
            RegisterRequestHandler<ProximityProbeGetRegistrationInfo>(Commands.ProximityProbeGetRegistrationInfo);
            RegisterRequestHandler<IntrusionEnabler>(Commands.IntrusionEnabler);
            RegisterRequestHandler<AccountGetTransactionHistory>(Commands.AccountGetTransactionHistory);
            RegisterRequestHandler<AccountList>(Commands.AccountList);
            
            RegisterRequestHandler<AccountEpForActivityHistory>(Commands.AccountEpForActivityHistory);
            RegisterRequestHandler<RedeemableItemList>(Commands.RedeemableItemList);
            RegisterRequestHandler<RedeemableItemRedeem>(Commands.RedeemableItemRedeem);
            RegisterRequestHandler<RedeemableItemActivate>(Commands.RedeemableItemActivate);
            RegisterRequestHandler<CreateItemRequestHandler>(Commands.CreateItem);
            RegisterRequestHandler<TeleportList>(Commands.TeleportList);
            RegisterRequestHandler<TeleportConnectColumns>(Commands.TeleportConnectColumns);
            RegisterRequestHandler<EnableSelfTeleport>(Commands.EnableSelfTeleport);
            RegisterRequestHandler<ItemCount>(Commands.ItemCount);
            RegisterRequestHandler<SystemInfo>(Commands.SystemInfo);
            RegisterRequestHandler<TransferData>(Commands.TransferData);

            RegisterRequestHandler<BaseReown>(Commands.BaseReown);
            RegisterRequestHandler<BaseSetDockingRights>(Commands.BaseSetDockingRights);
            RegisterRequestHandler<BaseSelect>(Commands.BaseSelect);
            RegisterRequestHandler<BaseGetInfo>(Commands.BaseGetInfo);
            RegisterRequestHandler<BaseGetMyItems>(Commands.BaseGetMyItems);
            RegisterRequestHandler<BaseListFacilities>(Commands.BaseListFacilities).SingleInstance();

            RegisterRequestHandler<GetZoneInfo>(Commands.GetZoneInfo);
            RegisterRequestHandler<ItemCountOnZone>(Commands.ItemCountOnZone);


            RegisterRequestHandler<CorporationCreate>(Commands.CorporationCreate);
            RegisterRequestHandler<CorporationRemoveMember>(Commands.CorporationRemoveMember);
            RegisterRequestHandler<CorporationGetMyInfo>(Commands.CorporationGetMyInfo);
            RegisterRequestHandler<CorporationSetMemberRole>(Commands.CorporationSetMemberRole);
            RegisterRequestHandler<CorporationCharacterInvite>(Commands.CorporationCharacterInvite);
            RegisterRequestHandler<CorporationInviteReply>(Commands.CorporationInviteReply);
            RegisterRequestHandler<CorporationInfo>(Commands.CorporationInfo);
            RegisterRequestHandler<CorporationLeave>(Commands.CorporationLeave);
            RegisterRequestHandler<CorporationSearch>(Commands.CorporationSearch);
            RegisterRequestHandler<CorporationSetInfo>(Commands.CorporationSetInfo);
            RegisterRequestHandler<CorporationDropRoles>(Commands.CorporationDropRoles);
            RegisterRequestHandler<CorporationCancelLeave>(Commands.CorporationCancelLeave);
            RegisterRequestHandler<CorporationPayOut>(Commands.CorporationPayOut);
            RegisterRequestHandler<CorporationForceInfo>(Commands.CorporationForceInfo);
            RegisterRequestHandler<CorporationGetDelegates>(Commands.CorporationGetDelegates);
            RegisterRequestHandler<CorporationTransfer>(Commands.CorporationTransfer);
            RegisterRequestHandler<CorporationHangarListAll>(Commands.CorporationHangarListAll);
            RegisterRequestHandler<CorporationHangarListOnBase>(Commands.CorporationHangarListOnBase);
            RegisterRequestHandler<CorporationRentHangar>(Commands.CorporationRentHangar);
            RegisterRequestHandler<CorporationHangarPayRent>(Commands.CorporationHangarPayRent);
            RegisterRequestHandler<CorporationHangarLogSet>(Commands.CorporationHangarLogSet);
            RegisterRequestHandler<CorporationHangarLogClear>(Commands.CorporationHangarLogClear);
            RegisterRequestHandler<CorporationHangarLogList>(Commands.CorporationHangarLogList);
            RegisterRequestHandler<CorporationHangarSetAccess>(Commands.CorporationHangarSetAccess);
            RegisterRequestHandler<CorporationHangarClose>(Commands.CorporationHangarClose);
            RegisterRequestHandler<CorporationHangarSetName>(Commands.CorporationHangarSetName);
            RegisterRequestHandler<CorporationHangarRentPrice>(Commands.CorporationHangarRentPrice);
            RegisterRequestHandler<CorporationHangarFolderSectionCreate>(Commands.CorporationHangarFolderSectionCreate);
            RegisterRequestHandler<CorporationHangarFolderSectionDelete>(Commands.CorporationHangarFolderSectionDelete);
            RegisterRequestHandler<CorporationVoteStart>(Commands.CorporationVoteStart);
            RegisterRequestHandler<CorporationVoteList>(Commands.CorporationVoteList);
            RegisterRequestHandler<CorporationVoteDelete>(Commands.CorporationVoteDelete);
            RegisterRequestHandler<CorporationVoteCast>(Commands.CorporationVoteCast);
            RegisterRequestHandler<CorporationVoteSetTopic>(Commands.CorporationVoteSetTopic);
            RegisterRequestHandler<CorporationBulletinStart>(Commands.CorporationBulletinStart);
            RegisterRequestHandler<CorporationBulletinEntry>(Commands.CorporationBulletinEntry);
            RegisterRequestHandler<CorporationBulletinDelete>(Commands.CorporationBulletinDelete);
            RegisterRequestHandler<CorporationBulletinList>(Commands.CorporationBulletinList);
            RegisterRequestHandler<CorporationBulletinDetails>(Commands.CorporationBulletinDetails);
            RegisterRequestHandler<CorporationBulletinEntryDelete>(Commands.CorporationBulletinEntryDelete);
            RegisterRequestHandler<CorporationBulletinNewEntries>(Commands.CorporationBulletinNewEntries);
            RegisterRequestHandler<CorporationBulletinModerate>(Commands.CorporationBulletinModerate);
            RegisterRequestHandler<CorporationCeoTakeOverStatus>(Commands.CorporationCeoTakeOverStatus);
            RegisterRequestHandler<CorporationVolunteerForCeo>(Commands.CorporationVolunteerForCeo);
            RegisterRequestHandler<CorporationRename>(Commands.CorporationRename);
            RegisterRequestHandler<CorporationDonate>(Commands.CorporationDonate);
            RegisterRequestHandler<CorporationTransactionHistory>(Commands.CorporationTransactionHistory);
            RegisterRequestHandler<CorporationApply>(Commands.CorporationApply);
            RegisterRequestHandler<CorporationDeleteMyApplication>(Commands.CorporationDeleteMyApplication);
            RegisterRequestHandler<CorporationAcceptApplication>(Commands.CorporationAcceptApplication);
            RegisterRequestHandler<CorporationDeleteApplication>(Commands.CorporationDeleteApplication);
            RegisterRequestHandler<CorporationListMyApplications>(Commands.CorporationListMyApplications);
            RegisterRequestHandler<CorporationListApplications>(Commands.CorporationListApplications);
            RegisterRequestHandler<CorporationLogHistory>(Commands.CorporationLogHistory);
            RegisterRequestHandler<CorporationNameHistory>(Commands.CorporationNameHistory);
            RegisterRequestHandler<CorporationSetColor>(Commands.CorporationSetColor);
            RegisterRequestHandler<CorporationDocumentConfig>(Commands.CorporationDocumentConfig).SingleInstance();
            RegisterRequestHandler<CorporationDocumentTransfer>(Commands.CorporationDocumentTransfer);
            RegisterRequestHandler<CorporationDocumentList>(Commands.CorporationDocumentList);
            RegisterRequestHandler<CorporationDocumentCreate>(Commands.CorporationDocumentCreate);
            RegisterRequestHandler<CorporationDocumentDelete>(Commands.CorporationDocumentDelete);
            RegisterRequestHandler<CorporationDocumentOpen>(Commands.CorporationDocumentOpen);
            RegisterRequestHandler<CorporationDocumentUpdateBody>(Commands.CorporationDocumentUpdateBody);
            RegisterRequestHandler<CorporationDocumentMonitor>(Commands.CorporationDocumentMonitor);
            RegisterRequestHandler<CorporationDocumentUnmonitor>(Commands.CorporationDocumentUnmonitor);
            RegisterRequestHandler<CorporationDocumentRent>(Commands.CorporationDocumentRent);
            RegisterRequestHandler<CorporationDocumentRegisterList>(Commands.CorporationDocumentRegisterList);
            RegisterRequestHandler<CorporationDocumentRegisterSet>(Commands.CorporationDocumentRegisterSet);
            RegisterRequestHandler<CorporationInfoFlushCache>(Commands.CorporationInfoFlushCache);
            RegisterRequestHandler<CorporationGetReputation>(Commands.CorporationGetReputation);
            RegisterRequestHandler<CorporationMyStandings>(Commands.CorporationMyStandings);
            RegisterRequestHandler<CorporationSetMembersNeutral>(Commands.CorporationSetMembersNeutral);
            RegisterRequestHandler<CorporationRoleHistory>(Commands.CorporationRoleHistory);
            RegisterRequestHandler<CorporationMemberRoleHistory>(Commands.CorporationMemberRoleHistory);




            RegisterRequestHandler<YellowPagesSearch>(Commands.YellowPagesSearch);
            RegisterRequestHandler<YellowPagesSubmit>(Commands.YellowPagesSubmit);
            RegisterRequestHandler<YellowPagesGet>(Commands.YellowPagesGet);
            RegisterRequestHandler<YellowPagesDelete>(Commands.YellowPagesDelete);


            RegisterRequestHandler<AllianceGetDefaults>(Commands.AllianceGetDefaults).SingleInstance();
            RegisterRequestHandler<AllianceGetMyInfo>(Commands.AllianceGetMyInfo);
            RegisterRequestHandler<AllianceRoleHistory>(Commands.AllianceRoleHistory);

            RegisterRequestHandler<ExtensionTest>(Commands.ExtensionTest);
            RegisterRequestHandler<ExtensionGetAll>(Commands.ExtensionGetAll).SingleInstance();
            RegisterRequestHandler<ExtensionPrerequireList>(Commands.ExtensionPrerequireList).SingleInstance();
            RegisterRequestHandler<ExtensionCategoryList>(Commands.ExtensionCategoryList).SingleInstance();
            RegisterRequestHandler<ExtensionLearntList>(Commands.ExtensionLearntList);
            RegisterRequestHandler<ExtensionGetAvailablePoints>(Commands.ExtensionGetAvailablePoints);
            RegisterRequestHandler<ExtensionGetPointParameters>(Commands.ExtensionGetPointParameters);
            RegisterRequestHandler<ExtensionHistory>(Commands.ExtensionHistory);
            RegisterRequestHandler<ExtensionBuyForPoints>(Commands.ExtensionBuyForPoints);
            RegisterRequestHandler<ExtensionRemoveLevel>(Commands.ExtensionRemoveLevel);
            RegisterRequestHandler<ExtensionBuyEpBoost>(Commands.ExtensionBuyEpBoost);
            RegisterRequestHandler<ExtensionResetCharacter>(Commands.ExtensionResetCharacter);
            RegisterRequestHandler<ExtensionFreeLockedEp>(Commands.ExtensionFreeLockedEp);
            RegisterRequestHandler<ExtensionFreeAllLockedEpByCommand>(Commands.ExtensionFreeAllLockedEpCommand); // For GameAdmin Channel Command
            RegisterRequestHandler<ExtensionGive>(Commands.ExtensionGive);
            RegisterRequestHandler<ExtensionReset>(Commands.ExtensionReset);
            RegisterRequestHandler<ExtensionRevert>(Commands.ExtensionRevert);

            RegisterRequestHandler<ItemShopBuy>(Commands.ItemShopBuy);
            RegisterRequestHandler<ItemShopList>(Commands.ItemShopList);
            RegisterRequestHandler<GiftOpen>(Commands.GiftOpen);
            RegisterRequestHandler<GetHighScores>(Commands.GetHighScores);
            RegisterRequestHandler<GetMyHighScores>(Commands.GetMyHighScores);
            RegisterRequestHandler<ZoneSectorList>(Commands.ZoneSectorList).SingleInstance();

            RegisterRequestHandler<ListContainer>(Commands.ListContainer);

            RegisterRequestHandler<SocialGetMyList>(Commands.SocialGetMyList);
            RegisterRequestHandler<SocialFriendRequest>(Commands.SocialFriendRequest);
            RegisterRequestHandler<SocialConfirmPendingFriendRequest>(Commands.SocialConfirmPendingFriendRequest);
            RegisterRequestHandler<SocialDeleteFriend>(Commands.SocialDeleteFriend);
            RegisterRequestHandler<SocialBlockFriend>(Commands.SocialBlockFriend);

            RegisterRequestHandler<PBSGetReimburseInfo>(Commands.PBSGetReimburseInfo);
            RegisterRequestHandler<PBSSetReimburseInfo>(Commands.PBSSetReimburseInfo);
            RegisterRequestHandler<PBSGetLog>(Commands.PBSGetLog);

            RegisterRequestHandler<MineralScanResultList>(Commands.MineralScanResultList);
            RegisterRequestHandler<MineralScanResultMove>(Commands.MineralScanResultMove);
            RegisterRequestHandler<MineralScanResultDelete>(Commands.MineralScanResultDelete);
            RegisterRequestHandler<MineralScanResultCreateItem>(Commands.MineralScanResultCreateItem);
            RegisterRequestHandler<MineralScanResultUploadFromItem>(Commands.MineralScanResultUploadFromItem);

            RegisterRequestHandler<FreshNewsCount>(Commands.FreshNewsCount);
            RegisterRequestHandler<GetNews>(Commands.GetNews);
            RegisterRequestHandler<AddNews>(Commands.AddNews);
            RegisterRequestHandler<UpdateNews>(Commands.UpdateNews);
            RegisterRequestHandler<NewsCategory>(Commands.NewsCategory).SingleInstance();

            RegisterRequestHandler<EpForActivityDailyLog>(Commands.EpForActivityDailyLog);
            RegisterRequestHandler<GetMyKillReports>(Commands.GetMyKillReports);
            RegisterRequestHandler<UseLotteryItem>(Commands.UseLotteryItem);
            RegisterRequestHandler<ContainerMover>(Commands.ContainerMover);


            RegisterRequestHandler<MarketTaxChange>(Commands.MarketTaxChange);
            RegisterRequestHandler<MarketTaxLogList>(Commands.MarketTaxLogList);
            RegisterRequestHandler<MarketGetInfo>(Commands.MarketGetInfo);
            RegisterRequestHandler<MarketAddCategory>(Commands.MarketAddCategory);
            RegisterRequestHandler<MarketItemList>(Commands.MarketItemList);
            RegisterRequestHandler<MarketGetMyItems>(Commands.MarketGetMyItems);
            RegisterRequestHandler<MarketGetAveragePrices>(Commands.MarketGetAveragePrices);
            RegisterRequestHandler<MarketCreateBuyOrder>(Commands.MarketCreateBuyOrder);
            RegisterRequestHandler<MarketCreateSellOrder>(Commands.MarketCreateSellOrder);
            RegisterRequestHandler<MarketBuyItem>(Commands.MarketBuyItem);
            RegisterRequestHandler<MarketCancelItem>(Commands.MarketCancelItem);
            RegisterRequestHandler<MarketGetState>(Commands.MarketGetState);
            RegisterRequestHandler<MarketSetState>(Commands.MarketSetState);
            RegisterRequestHandler<MarketFlush>(Commands.MarketFlush);
            RegisterRequestHandler<MarketGetDefinitionAveragePrice>(Commands.MarketGetDefinitionAveragePrice);
            RegisterRequestHandler<MarketAvailableItems>(Commands.MarketAvailableItems);
            RegisterRequestHandler<MarketItemsInRange>(Commands.MarketItemsInRange);
            RegisterRequestHandler<MarketInsertStats>(Commands.MarketInsertStats);
            RegisterRequestHandler<MarketListFacilities>(Commands.MarketListFacilities);
            RegisterRequestHandler<MarketInsertAverageForCF>(Commands.MarketInsertAverageForCF);
            RegisterRequestHandler<MarketGlobalAveragePrices>(Commands.MarketGlobalAveragePrices);
            RegisterRequestHandler<MarketModifyOrder>(Commands.MarketModifyOrder);
            RegisterRequestHandler<MarketCreateGammaPlasmaOrders>(Commands.MarketCreateGammaPlasmaOrders);
            RegisterRequestHandler<MarketRemoveItems>(Commands.MarketRemoveItems);
            RegisterRequestHandler<MarketCleanUp>(Commands.MarketCleanUp);



            RegisterRequestHandler<TradeBegin>(Commands.TradeBegin);
            RegisterRequestHandler<TradeCancel>(Commands.TradeCancel);
            RegisterRequestHandler<TradeSetOffer>(Commands.TradeSetOffer);
            RegisterRequestHandler<TradeAccept>(Commands.TradeAccept);
            RegisterRequestHandler<TradeRetractOffer>(Commands.TradeRetractOffer);


            RegisterRequestHandler<GetRobotInfo>(Commands.GetRobotInfo).OnActivated(e => e.Instance.ForFitting = false);
            RegisterRequestHandler<GetRobotInfo>(Commands.GetRobotFittingInfo);
            RegisterRequestHandler<SelectActiveRobot>(Commands.SelectActiveRobot);
            RegisterRequestHandler<RequestStarterRobot>(Commands.RequestStarterRobot);
            RegisterRequestHandler<RobotEmpty>(Commands.RobotEmpty);
            RegisterRequestHandler<SetRobotTint>(Commands.SetRobotTint);

            RegisterRequestHandler<FittingPresetList>(Commands.FittingPresetList);
            RegisterRequestHandler<FittingPresetSave>(Commands.FittingPresetSave);
            RegisterRequestHandler<FittingPresetDelete>(Commands.FittingPresetDelete);
            RegisterRequestHandler<FittingPresetApply>(Commands.FittingPresetApply);

            RegisterRequestHandler<RobotTemplateAdd>(Commands.RobotTemplateAdd);
            RegisterRequestHandler<RobotTemplateUpdate>(Commands.RobotTemplateUpdate);
            RegisterRequestHandler<RobotTemplateDelete>(Commands.RobotTemplateDelete);
            RegisterRequestHandler<RobotTemplateList>(Commands.RobotTemplateList);
            RegisterRequestHandler<RobotTemplateBuild>(Commands.RobotTemplateBuild);

            RegisterRequestHandler<EquipModule>(Commands.EquipModule);
            RegisterRequestHandler<ChangeModule>(Commands.ChangeModule);
            RegisterRequestHandler<RemoveModule>(Commands.RemoveModule);
            RegisterRequestHandler<EquipAmmo>(Commands.EquipAmmo);
            RegisterRequestHandler<ChangeAmmo>(Commands.ChangeAmmo);
            RegisterRequestHandler<RemoveAmmo>(Commands.UnequipAmmo);
            RegisterRequestHandler<PackItems>(Commands.PackItems);
            RegisterRequestHandler<UnpackItems>(Commands.UnpackItems);
            RegisterRequestHandler<TrashItems>(Commands.TrashItems);
            RegisterRequestHandler<RelocateItems>(Commands.RelocateItems);
            RegisterRequestHandler<StackSelection>(Commands.StackSelection);
            RegisterRequestHandler<UnstackAmount>(Commands.UnstackAmount);
            RegisterRequestHandler<SetItemName>(Commands.SetItemName);
            RegisterRequestHandler<StackTo>(Commands.StackTo);
            RegisterRequestHandler<ServerMessage>(Commands.ServerMessage);
            RegisterRequestHandler<RequestInfiniteBox>(Commands.RequestInfiniteBox);
            RegisterRequestHandler<DecorCategoryList>(Commands.DecorCategoryList);
            RegisterRequestHandler<PollGet>(Commands.PollGet);
            RegisterRequestHandler<PollAnswer>(Commands.PollAnswer);
            RegisterRequestHandler<ForceDock>(Commands.ForceDock);
            RegisterRequestHandler<ForceDockAdmin>(Commands.ForceDockAdmin);
            RegisterRequestHandler<GetItemSummary>(Commands.GetItemSummary);

            RegisterRequestHandler<ProductionHistory>(Commands.ProductionHistory);
            RegisterRequestHandler<GetResearchLevels>(Commands.GetResearchLevels).SingleInstance();
            RegisterRequestHandler<ProductionComponentsList>(Commands.ProductionComponentsList);
            RegisterRequestHandler<ProductionRefine>(Commands.ProductionRefine);
            RegisterRequestHandler<ProductionRefineQuery>(Commands.ProductionRefineQuery);
            RegisterRequestHandler<ProductionCPRGInfo>(Commands.ProductionCPRGInfo);
            RegisterRequestHandler<ProductionCPRGForge>(Commands.ProductionCPRGForge);
            RegisterRequestHandler<ProductionCPRGForgeQuery>(Commands.ProductionCPRGForgeQuery);
            RegisterRequestHandler<ProductionGetCPRGFromLine>(Commands.ProductionGetCprgFromLine);
            RegisterRequestHandler<ProductionGetCPRGFromLineQuery>(Commands.ProductionGetCprgFromLineQuery);
            RegisterRequestHandler<ProductionLineSetRounds>(Commands.ProductionLineSetRounds);
            RegisterRequestHandler<ProductionPrototypeStart>(Commands.ProductionPrototypeStart);
            RegisterRequestHandler<ProductionPrototypeQuery>(Commands.ProductionPrototypeQuery);
            RegisterRequestHandler<ProductionInsuranceQuery>(Commands.ProductionInsuranceQuery);
            RegisterRequestHandler<ProductionInsuranceList>(Commands.ProductionInsuranceList);
            RegisterRequestHandler<ProductionInsuranceBuy>(Commands.ProductionInsuranceBuy);
            RegisterRequestHandler<ProductionInsuranceDelete>(Commands.ProductionInsuranceDelete);
            RegisterRequestHandler<ProductionMergeResearchKitsMulti>(Commands.ProductionMergeResearchKitsMulti);
            RegisterRequestHandler<ProductionMergeResearchKitsMultiQuery>(Commands.ProductionMergeResearchKitsMultiQuery);
            RegisterRequestHandler<ProductionQueryLineNextRound>(Commands.ProductionQueryLineNextRound);
            RegisterRequestHandler<ProductionReprocess>(Commands.ProductionReprocess);
            RegisterRequestHandler<ProductionReprocessQuery>(Commands.ProductionReprocessQuery);
            RegisterRequestHandler<ProductionRepair>(Commands.ProductionRepair);
            RegisterRequestHandler<ProductionRepairQuery>(Commands.ProductionRepairQuery);
            RegisterRequestHandler<ProductionResearch>(Commands.ProductionResearch);
            RegisterRequestHandler<ProductionResearchQuery>(Commands.ProductionResearchQuery);
            RegisterRequestHandler<ProductionInProgressHandler>(Commands.ProductionInProgress);
            RegisterRequestHandler<ProductionCancel>(Commands.ProductionCancel);
            RegisterRequestHandler<ProductionFacilityInfo>(Commands.ProductionFacilityInfo);
            RegisterRequestHandler<ProductionLineList>(Commands.ProductionLineList);
            RegisterRequestHandler<ProductionLineCalibrate>(Commands.ProductionLineCalibrate);
            RegisterRequestHandler<ProductionLineDelete>(Commands.ProductionLineDelete);
            RegisterRequestHandler<ProductionLineStart>(Commands.ProductionLineStart);
            RegisterRequestHandler<ProductionFacilityDescription>(Commands.ProductionFacilityDescription);
            RegisterRequestHandler<ProductionInProgressCorporation>(Commands.ProductionInProgressCorporation);
            //admin 
            RegisterRequestHandler<ProductionRemoveFacility>(Commands.ProductionRemoveFacility);
            RegisterRequestHandler<ProductionSpawnComponents>(Commands.ProductionSpawnComponents);
            RegisterRequestHandler<ProductionScaleComponentsAmount>(Commands.ProductionScaleComponentsAmount);
            RegisterRequestHandler<ProductionUnrepairItem>(Commands.ProductionUnrepairItem);
            RegisterRequestHandler<ProductionFacilityOnOff>(Commands.ProductionFacilityOnOff);
            RegisterRequestHandler<ProductionForceEnd>(Commands.ProductionForceEnd);
            RegisterRequestHandler<ProductionServerInfo>(Commands.ProductionServerInfo);
            RegisterRequestHandler<ProductionSpawnCPRG>(Commands.ProductionSpawnCPRG);
            RegisterRequestHandler<ProductionGetInsurance>(Commands.ProductionGetInsurance);
            RegisterRequestHandler<ProductionSetInsurance>(Commands.ProductionSetInsurance);

            RegisterRequestHandler<CreateCorporationHangarStorage>(Commands.CreateCorporationHangarStorage);
            RegisterRequestHandler<DockAll>(Commands.DockAll);
            RegisterRequestHandler<ReturnCorporationOwnderItems>(Commands.ReturnCorporateOwnedItems);

            RegisterRequestHandler<RelayOpen>(Commands.RelayOpen);
            RegisterRequestHandler<RelayClose>(Commands.RelayClose);
            RegisterRequestHandler<ZoneSaveLayer>(Commands.ZoneSaveLayer);
            RegisterRequestHandler<ZoneRemoveObject>(Commands.ZoneRemoveObject);
            RegisterRequestHandler<ZoneDebugLOS>(Commands.ZoneDebugLOS);
            RegisterRequestHandler<ZoneSetBaseDetails>(Commands.ZoneSetBaseDetails);
            RegisterRequestHandler<ZoneSelfDestruct>(Commands.ZoneSelfDestruct);
            RegisterRequestHandler<ZoneSOS>(Commands.ZoneSOS);
            
            RegisterRequestHandler<ZoneGetZoneObjectDebugInfo>(Commands.ZoneGetZoneObjectDebugInfo);
            RegisterRequestHandler<ZoneDrawBlockingByEid>(Commands.ZoneDrawBlockingByEid);


            RegisterRequestHandler<GangCreate>(Commands.GangCreate);
            RegisterRequestHandler<GangDelete>(Commands.GangDelete);
            RegisterRequestHandler<GangLeave>(Commands.GangLeave);
            RegisterRequestHandler<GangKick>(Commands.GangKick);
            RegisterRequestHandler<GangInfo>(Commands.GangInfo);
            RegisterRequestHandler<GangSetLeader>(Commands.GangSetLeader);
            RegisterRequestHandler<GangSetRole>(Commands.GangSetRole);
            RegisterRequestHandler<GangInvite>(Commands.GangInvite);
            RegisterRequestHandler<GangInviteReply>(Commands.GangInviteReply);

            RegisterRequestHandler<TechTreeInfo>(Commands.TechTreeInfo);
            RegisterRequestHandler<TechTreeUnlock>(Commands.TechTreeUnlock);
            RegisterRequestHandler<TechTreeResearch>(Commands.TechTreeResearch);
            RegisterRequestHandler<TechTreeDonate>(Commands.TechTreeDonate);
            RegisterRequestHandler<TechTreeGetLogs>(Commands.TechTreeGetLogs);


            RegisterRequestHandler<TransportAssignmentSubmit>(Commands.TransportAssignmentSubmit);
            RegisterRequestHandler<TransportAssignmentList>(Commands.TransportAssignmentList);
            RegisterRequestHandler<TransportAssignmentCancel>(Commands.TransportAssignmentCancel);
            RegisterRequestHandler<TransportAssignmentTake>(Commands.TransportAssignmentTake);
            RegisterRequestHandler<TransportAssignmentLog>(Commands.TransportAssignmentLog);
            RegisterRequestHandler<TransportAssignmentContainerInfo>(Commands.TransportAssignmentContainerInfo);
            RegisterRequestHandler<TransportAssignmentRunning>(Commands.TransportAssignmentRunning);
            RegisterRequestHandler<TransportAssignmentRetrieve>(Commands.TransportAssignmentRetrieve);
            RegisterRequestHandler<TransportAssignmentListContent>(Commands.TransportAssignmentListContent);
            RegisterRequestHandler<TransportAssignmentGiveUp>(Commands.TransportAssignmentGiveUp);
            RegisterRequestHandler<TransportAssignmentDeliver>(Commands.TransportAssignmentDeliver);


            RegisterRequestHandler<SetStanding>(Commands.SetStanding);
            RegisterRequestHandler<ForceStanding>(Commands.ForceStanding);
            RegisterRequestHandler<ForceFactionStandings>(Commands.ForceFactionStandings);
            RegisterRequestHandler<GetStandingForDefaultCorporations>(Commands.GetStandingForDefaultCorporations);
            RegisterRequestHandler<GetStandingForDefaultAlliances>(Commands.GetStandingForDefaultAlliances);
            RegisterRequestHandler<StandingList>(Commands.StandingList);
            RegisterRequestHandler<StandingHistory>(Commands.StandingHistory);
            RegisterRequestHandler<ReloadStandingForCharacter>(Commands.ReloadStandingForCharacter);

            RegisterRequestHandler<MailList>(Commands.MailList);
            RegisterRequestHandler<MailUsedFolders>(Commands.MailUsedFolders);
            RegisterRequestHandler<MailSend>(Commands.MailSend);
            RegisterRequestHandler<MailDelete>(Commands.MailDelete);
            RegisterRequestHandler<MailOpen>(Commands.MailOpen);
            RegisterRequestHandler<MailMoveToFolder>(Commands.MailMoveToFolder);
            RegisterRequestHandler<MailDeleteFolder>(Commands.MailDeleteFolder);
            RegisterRequestHandler<MailNewCount>(Commands.MailNewCount);
            RegisterRequestHandler<MassMailOpen>(Commands.MassMailOpen);
            RegisterRequestHandler<MassMailDelete>(Commands.MassMailDelete);
            RegisterRequestHandler<MassMailSend>(Commands.MassMailSend);
            RegisterRequestHandler<MassMailList>(Commands.MassMailList);
            RegisterRequestHandler<MassMailNewCount>(Commands.MassMailNewCount);


            RegisterRequestHandler<ServerShutDownState>(Commands.ServerShutDownState);
            RegisterRequestHandler<ServerShutDown>(Commands.ServerShutDown);
            RegisterRequestHandler<ServerShutDownCancel>(Commands.ServerShutDownCancel);

            RegisterZoneRequestHandlers();

            //Admin tool commands
            RegisterRequestHandler<GetAccountsWithCharacters>(Commands.GetAccountsWithCharacters);
            RegisterRequestHandler<AccountGet>(Commands.AccountGet);
            RegisterRequestHandler<AccountUpdate>(Commands.AccountUpdate);
            RegisterRequestHandler<AccountCreate>(Commands.AccountCreate);
            RegisterRequestHandler<ChangeSessionPassword>(Commands.ChangeSessionPassword);
            RegisterRequestHandler<AccountBan>(Commands.AccountBan);
            RegisterRequestHandler<AccountUnban>(Commands.AccountUnban);
            RegisterRequestHandler<AccountDelete>(Commands.AccountDelete);
            RegisterRequestHandler<ServerInfoGet>(Commands.ServerInfoGet);
            RegisterRequestHandler<ServerInfoSet>(Commands.ServerInfoSet);

            // Open account commands
            RegisterRequestHandler<AccountOpenCreate>(Commands.AccountOpenCreate);

            // Event GM Commands
            RegisterRequestHandler<EPBonusEvent>(Commands.EPBonusSet);
        }

        private void RegisterRobotTemplates()
        {
            _builder.Register<RobotTemplateFactory>(x =>
            {
                var relations = x.Resolve<IRobotTemplateRelations>();
                return (definition =>
                {
                    return relations.GetRelatedTemplateOrDefault(definition);
                });
            });

            _builder.RegisterType<RobotTemplateReader>().AsSelf().As<IRobotTemplateReader>();
            _builder.Register(x =>
            {
                return new CachedRobotTemplateReader(x.Resolve<RobotTemplateReader>());
            }).AsSelf().As<IRobotTemplateReader>().SingleInstance().OnActivated(e => e.Instance.Init());

            _builder.RegisterType<RobotTemplateRepository>().As<IRobotTemplateRepository>();
            _builder.RegisterType<RobotTemplateRelations>().As<IRobotTemplateRelations>().SingleInstance().OnActivated(e =>
            {
                e.Instance.Init();
            });

            _builder.RegisterType<RobotTemplateServicesImpl>().As<IRobotTemplateServices>().PropertiesAutowired().SingleInstance();

            _builder.RegisterType<HybridRobotBuilder>();

            _builder.RegisterType<RobotHelper>();
        }

        private void RegisterTerrains()
        {
            _builder.Register<Func<IZone, IEnumerable<IMaterialLayer>>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    var reader = ctx.Resolve<IMineralConfigurationReader>();
                    var listener = new OreNpcSpawner(zone, ctx.Resolve<INpcReinforcementsRepository>(), reader);
                    var eventListenerService = ctx.Resolve<EventListenerService>();
                    eventListenerService.AttachListener(listener);
                    if (zone is TrainingZone)
                    {
                        var repo = ctx.Resolve<GravelRepository>();
                        var config = new GravelConfiguration(zone);
                        var layer = new GravelLayer(zone.Size.Width, zone.Size.Height, config, repo);
                        layer.LoadMineralNodes();
                        return new[] {layer};
                    }

                    var nodeGeneratorFactory = new MineralNodeGeneratorFactory(zone);
                    
                    var materialLayers = new List<IMaterialLayer>();

                    foreach (var configuration in reader.ReadAll().Where(c => c.ZoneId == zone.Id))
                    {
                        var repo = new MineralNodeRepository(zone, configuration.Type);
                        switch (configuration.ExtractionType)
                        {
                            case MineralExtractionType.Solid:
                            {
                                var layer = new OreLayer(zone.Size.Width, zone.Size.Height, configuration, repo, nodeGeneratorFactory, eventListenerService);
                                layer.LoadMineralNodes();
                                materialLayers.Add(layer);
                                break;
                            }
                            case MineralExtractionType.Liquid:
                            {
                                var layer = new LiquidLayer(zone.Size.Width, zone.Size.Height, configuration, repo, nodeGeneratorFactory, eventListenerService);
                                layer.LoadMineralNodes();
                                materialLayers.Add(layer);
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    return materialLayers;
                };
            });

            _builder.RegisterType<Scanner>();
            _builder.RegisterType<MaterialHelper>().SingleInstance();

            _builder.RegisterType<GravelRepository>();
            _builder.RegisterType<LayerFileIO>().As<ILayerFileIO>();
            _builder.RegisterType<Terrain>();
            _builder.RegisterGeneric(typeof(IntervalLayerSaver<>)).InstancePerDependency();

            _builder.Register<TerrainFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    var terrain = ctx.Resolve<Terrain>();

                    var size = zone.Configuration.Size;

                    var loader = ctx.Resolve<ILayerFileIO>();

                    var blocks = loader.Load<BlockingInfo>(zone, LayerType.Blocks);
                    terrain.Blocks = new Layer<BlockingInfo>(LayerType.Blocks, blocks, size.Width, size.Height);

                    var controls = loader.Load<TerrainControlInfo>(zone, LayerType.Control);
                    terrain.Controls = new Layer<TerrainControlInfo>(LayerType.Control, controls, size.Width, size.Height);

                    var plants = loader.Load<PlantInfo>(zone, LayerType.Plants);
                    terrain.Plants = new Layer<PlantInfo>(LayerType.Plants, plants, size.Width, size.Height);

                    var altitude = loader.Load<ushort>(zone, LayerType.Altitude);
                    var altitudeLayer = new AltitudeLayer(altitude, size.Width, size.Height);

                    if (zone.Configuration.Terraformable)
                    {
                        var original = loader.LoadLayerData<ushort>(zone, "altitude_original");
                        var originalAltitude = new Layer<ushort>(LayerType.OriginalAltitude, original, size.Width, size.Height);
                        var blend = loader.LoadLayerData<ushort>(zone, "altitude_blend");
                        var blendLayer = new Layer<ushort>(LayerType.Blend, blend, size.Width, size.Height);
                        altitudeLayer = new TerraformableAltitude(originalAltitude, blendLayer, altitudeLayer.RawData);
                    }

                    terrain.Altitude = altitudeLayer;
                    terrain.Slope = new SlopeLayer(altitudeLayer);

                    if (!zone.Configuration.Terraformable)
                    {
                        var b = new PassableMapBuilder(terrain.Blocks, terrain.Slope, zone.GetPassablePositionFromDb());
                        terrain.Passable = b.Build();
                    }

                    terrain.Materials = ctx.Resolve<Func<IZone, IEnumerable<IMaterialLayer>>>().Invoke(zone).ToDictionary(m => m.Type);

                    var layerSavers = new CompositeProcess();
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<BlockingInfo>.Factory>().Invoke(terrain.Blocks,zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<TerrainControlInfo>.Factory>().Invoke(terrain.Controls,zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<PlantInfo>.Factory>().Invoke(terrain.Plants,zone));
                    layerSavers.AddProcess(ctx.Resolve<IntervalLayerSaver<ushort>.Factory>().Invoke(terrain.Altitude,zone));

                    ctx.Resolve<IProcessManager>().AddProcess(layerSavers.ToAsync().AsTimed(TimeSpan.FromHours(2)));
                    ctx.Resolve<IProcessManager>().AddProcess(terrain.Materials.Values.OfType<IProcess>().ToCompositeProcess().ToAsync().AsTimed(TimeSpan.FromMinutes(2)));
                    return terrain;
                };
            });
        }

        private void RegisterIntrusions()
        {
            RegisterRequestHandler<BaseGetOwnershipInfo>(Commands.BaseGetOwnershipInfo);
            RegisterRequestHandler<IntrusionGetPauseTime>(Commands.IntrusionGetPauseTime);
            RegisterRequestHandler<IntrusionSetPauseTime>(Commands.IntrusionSetPauseTime);
            RegisterRequestHandler<IntrusionUpgradeFacility>(Commands.IntrusionUpgradeFacility);
            RegisterRequestHandler<SetIntrusionSiteMessage>(Commands.SetIntrusionSiteMessage);
            RegisterRequestHandler<GetIntrusionLog>(Commands.GetIntrusionLog);
            RegisterRequestHandler<GetIntrusionStabilityLog>(Commands.GetIntrusionStabilityLog);
            RegisterRequestHandler<GetStabilityBonusThresholds>(Commands.GetStabilityBonusThresholds);
            RegisterRequestHandler<GetIntrusionSiteInfo>(Commands.GetIntrusionSiteInfo);
            RegisterRequestHandler<GetIntrusionPublicLog>(Commands.GetIntrusionPublicLog);
            RegisterRequestHandler<GetIntrusionMySitesLog>(Commands.GetIntrusionMySitesLog);

            RegisterZoneRequestHandler<IntrusionSAPGetItemInfo>(Commands.IntrusionSAPGetItemInfo);
            RegisterZoneRequestHandler<IntrusionSAPSubmitItem>(Commands.IntrusionSAPSubmitItem);
            RegisterZoneRequestHandler<IntrusionSiteSetEffectBonus>(Commands.IntrusionSiteSetEffectBonus);
            RegisterZoneRequestHandler<IntrusionSetDefenseThreshold>(Commands.IntrusionSetDefenseThreshold);

            RegisterZoneRequestHandler<GetRobotInfo>(Commands.GetRobotFittingInfo);
        }

        private void RegisterRifts()
        {
            _builder.Register<Func<IZone, RiftSpawnPositionFinder>>(x =>
            {
                return zone =>
                {
                    if (zone.Configuration.Terraformable)
                    {
                        return new PvpRiftSpawnPositionFinder(zone);
                    }

                    return new PveRiftSpawnPositionFinder(zone);
                };
            });

            _builder.RegisterType<RiftManager>();
            _builder.RegisterType<StrongholdRiftManager>();

            _builder.Register<Func<IZone, IRiftManager>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    if (zone is TrainingZone)
                        return null;

                    if (zone is StrongHoldZone)
                    {
                       return ctx.Resolve<StrongholdRiftManager>(new TypedParameter(typeof(IZone), zone));
                    }

                    var spawnTime = TimeRange.FromLength(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
                    var finder = ctx.Resolve<Func<IZone, RiftSpawnPositionFinder>>().Invoke(zone);
                    return ctx.Resolve<RiftManager>(new TypedParameter(typeof(IZone),zone),new NamedParameter("spawnTime",spawnTime),new NamedParameter("spawnPositionFinder",finder));
                };
            });
        }

        private void RegisterRelics()
        {
            _builder.RegisterType<ZoneRelicManager>().As<IRelicManager>();

            _builder.Register<Func<IZone, IRelicManager>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    var numRelicConfigs = Db.Query().CommandText("SELECT id FROM relicspawninfo WHERE zoneid = @zoneId")
                    .SetParameter("@zoneId", zone.Id)
                    .Execute().Count;
                    if (numRelicConfigs < 1)
                    {
                        return null;
                    }

                    var zoneConfigs = Db.Query().CommandText("SELECT maxspawn FROM reliczoneconfig WHERE zoneid = @zoneId")
                    .SetParameter("@zoneId", zone.Id)
                    .Execute();
                    if (zoneConfigs.Count < 1)
                    {
                        return null;
                    }
                    var record = zoneConfigs[0];
                    var maxspawn = record.GetValue<int>("maxspawn");
                    if (maxspawn < 1)
                    {
                        return null;
                    }
                    //Do not register RelicManagers on zones without the necessary valid entries in reliczoneconfig and relicspawninfo
                    return ctx.Resolve<IRelicManager>(new TypedParameter(typeof(IZone), zone));
                };
            });
        }

        private void RegisterZones()
        {
            _builder.RegisterType<ZoneSession>().AsSelf().As<IZoneSession>();

            _builder.RegisterType<SaveBitmapHelper>();
            _builder.RegisterType<ZoneDrawStatMap>();

            _builder.RegisterType<ZoneConfigurationReader>().As<IZoneConfigurationReader>();

            _builder.Register(c =>
            {
                return new WeatherService(new TimeRange(TimeSpan.FromMinutes(30), TimeSpan.FromHours(1)));
            }).OnActivated(e =>
            {
                e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromMinutes(5)));
            }).As<IWeatherService>();

            _builder.RegisterType<WeatherMonitor>();
            _builder.RegisterType<WeatherEventListener>();
            _builder.Register<Func<IZone, WeatherEventListener>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    return new WeatherEventListener(ctx.Resolve<EventListenerService>(), zone);
                };
            });

            _builder.Register<Func<IZone, EnvironmentalEffectHandler>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    var listener = new EnvironmentalEffectHandler(zone);
                    ctx.Resolve<EventListenerService>().AttachListener(listener);
                    return listener;
                };
            });

            _builder.RegisterType<DefaultZoneUnitRepository>().AsSelf().As<IZoneUnitRepository>();
            _builder.RegisterType<UserZoneUnitRepository>().AsSelf().As<IZoneUnitRepository>();

            _builder.Register<ZoneUnitServiceFactory>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return zone =>
                {
                    return new ZoneUnitService
                    {
                        DefaultRepository = ctx.Resolve<DefaultZoneUnitRepository>(new TypedParameter(typeof(IZone),zone)),
                        UserRepository = ctx.Resolve<UserZoneUnitRepository>(new TypedParameter(typeof(IZone),zone))
                    };
                };
            });

            _builder.RegisterType<BeamService>().As<IBeamService>();
            _builder.RegisterType<MiningLogHandler>();
            _builder.RegisterType<MineralConfigurationReader>().As<IMineralConfigurationReader>().SingleInstance();

            void RegisterZone<T>(ZoneType type) where T:Zone
            {
                _builder.RegisterType<T>().Keyed<Zone>(type).OnActivated(e =>
                {
                    e.Context.Resolve<IProcessManager>().AddProcess(e.Instance.ToAsync());
                });
            }

            RegisterZone<PveZone>(ZoneType.Pve);
            RegisterZone<PvpZone>(ZoneType.Pvp);
            RegisterZone<TrainingZone>(ZoneType.Training);
            RegisterZone<StrongHoldZone>(ZoneType.Stronghold);

            _builder.RegisterType<SettingsLoader>();
            _builder.RegisterType<PlantRuleLoader>();

            _builder.Register<Func<ZoneConfiguration, IZone>>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return configuration =>
                {
                    var zone = ctx.ResolveKeyed<Zone>(configuration.Type);
                    zone.Configuration = configuration;
                    zone.Listener = new TcpListener(new IPEndPoint(IPAddress.Any, configuration.ListenerPort));
                    zone.ZoneEffectHandler = ctx.Resolve<Func<IZone, IZoneEffectHandler>>().Invoke(zone);
                    zone.UnitService = ctx.Resolve<ZoneUnitServiceFactory>().Invoke(zone);
                    zone.Weather = ctx.Resolve<IWeatherService>();
                    zone.Beams = ctx.Resolve<IBeamService>();
                    zone.HighScores = ctx.Resolve<IHighScoreService>();
                    zone.PlantHandler = ctx.Resolve<PlantHandler.Factory>().Invoke(zone);
                    zone.CorporationHandler = ctx.Resolve<CorporationHandler.Factory>().Invoke(zone);
                    zone.MiningLogHandler = ctx.Resolve<MiningLogHandler.Factory>().Invoke(zone);
                    zone.RiftManager = ctx.Resolve<Func<IZone, IRiftManager>>().Invoke(zone);
                    zone.ChatLogger = ctx.Resolve<ChatLoggerFactory>().Invoke("zone", zone.Configuration.Name);
                    zone.EnterQueueService = ctx.Resolve<ZoneEnterQueueService.Factory>().Invoke(zone);
                    zone.Terrain = ctx.Resolve<TerrainFactory>().Invoke(zone);
                    zone.PresenceManager = ctx.Resolve<Func<IZone, IPresenceManager>>().Invoke(zone);
                    zone.DecorHandler = ctx.Resolve<DecorHandler>(new TypedParameter(typeof(IZone),zone));
                    zone.Environment = ctx.Resolve<ZoneEnvironmentHandler>(new TypedParameter(typeof(IZone), zone));
                    zone.SafeSpawnPoints = ctx.Resolve<ISafeSpawnPointsRepository>(new TypedParameter(typeof(IZone), zone));
                    zone.ZoneSessionFactory = ctx.Resolve<ZoneSession.Factory>();
                    zone.RelicManager = ctx.Resolve<Func<IZone, IRelicManager>>().Invoke(zone);

                    if (configuration.Terraformable)
                    {
                        zone.HighwayHandler = ctx.Resolve<PBSHighwayHandler.Factory>().Invoke(zone);
                        zone.TerraformHandler = ctx.Resolve<TerraformHandler.Factory>().Invoke(zone);
                    }

                    ctx.Resolve<EventListenerService>().AttachListener(new NpcReinforcementSpawner(zone, ctx.Resolve<INpcReinforcementsRepository>()));
                    var listener = ctx.Resolve<Func<IZone, WeatherEventListener>>().Invoke(zone);
                    listener.Subscribe(zone.Weather);

                    zone.LoadUnits();
                    return zone;
                };
            });

            _builder.Register(c => c.Resolve<ZoneManager>()).As<IZoneManager>();
            _builder.RegisterType<ZoneManager>().OnActivated(e =>
            {
                foreach (var c in e.Context.Resolve<IZoneConfigurationReader>().GetAll())
                {
                    var zoneFactory = e.Context.Resolve<Func<ZoneConfiguration, IZone>>();
                    var zone = zoneFactory(c);

                    e.Context.Resolve<Func<IZone, EnvironmentalEffectHandler>>().Invoke(zone);

                    Logger.Info("------------------");
                    Logger.Info("--");
                    Logger.Info("--  zone " + zone.Configuration.Id + " loaded.");
                    Logger.Info("--");
                    Logger.Info("------------------");

                    e.Instance.Zones.Add(zone);
                };
            }).SingleInstance();

            _builder.RegisterType<TagHelper>();

            _builder.RegisterType<ZoneEnterQueueService>().OnActivated(e =>
            {
                var pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(1)));
            }).As<IZoneEnterQueueService>().InstancePerDependency();

            _builder.RegisterType<DecorHandler>().OnActivated(e => e.Instance.Initialize()).InstancePerDependency();
            _builder.RegisterType<ZoneEnvironmentHandler>();
            _builder.RegisterType<PlantHandler>().OnActivated(e =>
            {
                var pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.ToAsync().AsTimed(TimeSpan.FromSeconds(5)));
            }).As<IPlantHandler>().InstancePerDependency();

            _builder.RegisterType<TeleportDescriptionBuilder>();
            _builder.RegisterType<TeleportWorldTargetHelper>();
            _builder.RegisterType<MobileTeleportZoneMapCache>().As<IMobileTeleportToZoneMap>().SingleInstance();
            _builder.RegisterType<StrongholdTeleportTargetHelper>();
            _builder.RegisterType<TeleportToAnotherZone>();
            _builder.RegisterType<TeleportWithinZone>();
            _builder.RegisterType<TrainingExitStrategy>();

            _builder.RegisterType<PBSHighwayHandler>().OnActivated(e =>
            {
                var pm = e.Context.Resolve<IProcessManager>();
                pm.AddProcess(e.Instance.AsTimed(TimeSpan.FromMilliseconds(PBSHighwayHandler.DRAW_INTERVAL)).ToAsync());
            });

            _builder.RegisterType<MineralScanResultRepository>();
            _builder.RegisterType<RareMaterialHandler>().SingleInstance();
            _builder.RegisterType<PlantHarvester>().As<IPlantHarvester>();

            _builder.RegisterType<TeleportStrategyFactoriesImpl>()
                .As<ITeleportStrategyFactories>()
                .PropertiesAutowired()
                .SingleInstance();

            _builder.RegisterType<TrainingRewardRepository>().SingleInstance().As<ITrainingRewardRepository>();
        }

        private IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle>
            RegisterZoneRequestHandler<T>(Command command) where T : IRequestHandler<IZoneRequest>
        {
            return RegisterRequestHandler<T, IZoneRequest>(command);
        }

        private void RegisterZoneRequestHandlers()
        {
            RegisterZoneRequestHandler<TeleportGetChannelList>(Commands.TeleportGetChannelList);
            RegisterZoneRequestHandler<TeleportToZoneObject>(Commands.TeleportToZoneObject);
            RegisterZoneRequestHandler<TeleportUse>(Commands.TeleportUse);
            RegisterZoneRequestHandler<TeleportQueryWorldChannels>(Commands.TeleportQueryWorldChannels);
            RegisterZoneRequestHandler<JumpAnywhere>(Commands.JumpAnywhere);
            RegisterZoneRequestHandler<MovePlayer>(Commands.MovePlayer);
            RegisterZoneRequestHandler<ZoneDrawStatMap>(Commands.ZoneDrawStatMap);
            RegisterZoneRequestHandler<MissionStartFromZone>(Commands.MissionStartFromZone);
            RegisterZoneRequestHandler<ZoneItemShopBuy>(Commands.ItemShopBuy);
            RegisterZoneRequestHandler<ZoneItemShopList>(Commands.ItemShopList);
            RegisterZoneRequestHandler<ZoneMoveUnit>(Commands.ZoneMoveUnit);
            RegisterZoneRequestHandler<ZoneGetQueueInfo>(Commands.ZoneGetQueueInfo);
            RegisterZoneRequestHandler<ZoneSetQueueLength>(Commands.ZoneSetQueueLength);
            RegisterZoneRequestHandler<ZoneCancelEnterQueue>(Commands.ZoneCancelEnterQueue);
            RegisterZoneRequestHandler<ZoneGetBuildings>(Commands.ZoneGetBuildings);

            RegisterZoneRequestHandler<Dock>(Commands.Dock);

            RegisterZoneRequestHandler<ZoneDecorAdd>(Commands.ZoneDecorAdd);
            RegisterZoneRequestHandler<ZoneDecorSet>(Commands.ZoneDecorSet);
            RegisterZoneRequestHandler<ZoneDecorDelete>(Commands.ZoneDecorDelete);
            RegisterZoneRequestHandler<ZoneDecorLock>(Commands.ZoneDecorLock);
            RegisterZoneRequestHandler<ZoneDrawDecorEnvironment>(Commands.ZoneDrawDecorEnvironment);
            RegisterZoneRequestHandler<ZoneSampleDecorEnvironment>(Commands.ZoneSampleDecorEnvironment);
            RegisterZoneRequestHandler<ZoneDrawDecorEnvByDef>(Commands.ZoneDrawDecorEnvByDef);
            RegisterZoneRequestHandler<ZoneDrawAllDecors>(Commands.ZoneDrawAllDecors);
            RegisterZoneRequestHandler<ZoneEnvironmentDescriptionList>(Commands.ZoneEnvironmentDescriptionList);
            RegisterZoneRequestHandler<ZoneSampleEnvironment>(Commands.ZoneSampleEnvironment);
            RegisterZoneRequestHandler<ZoneCreateTeleportColumn>(Commands.ZoneCreateTeleportColumn);

            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.PackItems>(Commands.PackItems);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.UnpackItems>(Commands.UnpackItems);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.TrashItems>(Commands.TrashItems);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.RelocateItems>(Commands.RelocateItems);
            RegisterZoneRequestHandler<StackItems>(Commands.StackItems);
            RegisterZoneRequestHandler<StackItems>(Commands.StackSelection);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.UnstackAmount>(Commands.UnstackAmount);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.SetItemName>(Commands.SetItemName);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.ListContainer>(Commands.ListContainer);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.EquipModule>(Commands.EquipModule);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.RemoveModule>(Commands.RemoveModule);
            RegisterZoneRequestHandler<ChangeModule>(Commands.ChangeModule);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.EquipAmmo>(Commands.EquipAmmo);
            RegisterZoneRequestHandler<UnequipAmmo>(Commands.UnequipAmmo);
            RegisterZoneRequestHandler<RequestHandlers.Zone.Containers.ChangeAmmo>(Commands.ChangeAmmo);

            RegisterZoneRequestHandler<MissionGetSupply>(Commands.MissionGetSupply);
            RegisterZoneRequestHandler<MissionSpotPlace>(Commands.MissionSpotPlace);
            RegisterZoneRequestHandler<MissionSpotUpdate>(Commands.MissionSpotUpdate);
            RegisterZoneRequestHandler<ZoneUpdateStructure>(Commands.ZoneUpdateStructure);
            RegisterZoneRequestHandler<RemoveMissionStructure>(Commands.RemoveMissionStructure);
            RegisterZoneRequestHandler<KioskInfo>(Commands.KioskInfo);
            RegisterZoneRequestHandler<KioskSubmitItem>(Commands.KioskSubmitItem);
            RegisterZoneRequestHandler<AlarmStart>(Commands.AlarmStart);
            RegisterZoneRequestHandler<TriggerMissionStructure>(Commands.TriggerMissionStructure);

            RegisterZoneRequestHandler<ZoneUploadScanResult>(Commands.ZoneUploadScanResult);

            //admin
            RegisterZoneRequestHandler<ZoneEntityChangeState>(Commands.ZoneEntityChangeState);
            RegisterZoneRequestHandler<ZoneRemoveByDefinition>(Commands.ZoneRemoveByDefinition);
            RegisterZoneRequestHandler<ZoneMakeGotoXY>(Commands.ZoneMakeGotoXY);
            RegisterZoneRequestHandler<ZoneDrawBeam>(Commands.ZoneDrawBeam);
            RegisterZoneRequestHandler<ZoneSetRuntimeZoneEntityName>(Commands.ZoneSetRuntimeZoneEntityName);
            RegisterZoneRequestHandler<ZoneCheckRoaming>(Commands.ZoneCheckRoaming);
            RegisterZoneRequestHandler<ZonePBSTest>(Commands.ZonePBSTest);
            RegisterZoneRequestHandler<ZonePBSFixOrphaned>(Commands.ZonePBSFixOrphaned);
            RegisterZoneRequestHandler<ZoneFixPBS>(Commands.ZoneFixPBS);
            RegisterZoneRequestHandler<ZoneServerMessage>(Commands.ZoneServerMessage);
            RegisterZoneRequestHandler<ZonePlaceWall>(Commands.ZonePlaceWall);
            RegisterZoneRequestHandler<ZoneClearWalls>(Commands.ZoneClearWalls);
            RegisterZoneRequestHandler<ZoneHealAllWalls>(Commands.ZoneHealAllWalls);
            RegisterZoneRequestHandler<ZoneTerraformTest>(Commands.ZoneTerraformTest);
            RegisterZoneRequestHandler<ZoneForceDeconstruct>(Commands.ZoneForceDeconstruct);
            RegisterZoneRequestHandler<ZoneSetReinforceCounter>(Commands.ZoneSetReinforceCounter);
            RegisterZoneRequestHandler<ZoneRestoreOriginalGamma>(Commands.ZoneRestoreOriginalGamma);
            RegisterZoneRequestHandler<ZoneSwitchDegrade>(Commands.ZoneSwitchDegrade);
            RegisterZoneRequestHandler<ZoneKillNPlants>(Commands.ZoneKillNPlants);
            RegisterZoneRequestHandler<ZoneDisplayMissionRandomPoints>(Commands.ZoneDisplayMissionRandomPoints);
            RegisterZoneRequestHandler<ZoneDisplayMissionSpots>(Commands.ZoneDisplayMissionSpots);
            RegisterZoneRequestHandler<NPCCheckCondition>(Commands.NpcCheckCondition);
            RegisterZoneRequestHandler<ZoneClearLayer>(Commands.ZoneClearLayer);
            RegisterZoneRequestHandler<ZonePutPlant>(Commands.ZonePutPlant);
            RegisterZoneRequestHandler<ZoneSetPlantSpeed>(Commands.ZoneSetPlantsSpeed);
            RegisterZoneRequestHandler<ZoneGetPlantsMode>(Commands.ZoneGetPlantsMode);
            RegisterZoneRequestHandler<ZoneSetPlantsMode>(Commands.ZoneSetPlantsMode);
            RegisterZoneRequestHandler<ZoneCreateGarder>(Commands.ZoneCreateGarden);
            RegisterZoneRequestHandler<ZoneCreateIsland>(Commands.ZoneCreateIsland);
            RegisterZoneRequestHandler<ZoneDrawBlockingByDefinition>(Commands.ZoneDrawBlockingByDefinition);
            RegisterZoneRequestHandler<ZoneCleanBlockingByDefinition>(Commands.ZoneCleanBlockingByDefinition);
            RegisterZoneRequestHandler<ZoneCleanObstacleBlocking>(Commands.ZoneCleanObstacleBlocking);



            RegisterZoneRequestHandler<NpcListSafeSpawnPoint>(Commands.NpcListSafeSpawnPoint);
            RegisterZoneRequestHandler<NpcPlaceSafeSpawnPoint>(Commands.NpcPlaceSafeSpawnPoint);
            RegisterZoneRequestHandler<NpcAddSafeSpawnPoint>(Commands.NpcAddSafeSpawnPoint);
            RegisterZoneRequestHandler<NpcSetSafeSpawnPoint>(Commands.NpcSetSafeSpawnPoint);
            RegisterZoneRequestHandler<NpcDeleteSafeSpawnPoint>(Commands.NpcDeleteSafeSpawnPoint);
            RegisterZoneRequestHandler<ZoneListPresences>(Commands.ZoneListPresences);
            RegisterZoneRequestHandler<ZoneNpcFlockNew>(Commands.ZoneNpcFlockNew);
            RegisterZoneRequestHandler<ZoneNpcFlockSet>(Commands.ZoneNpcFlockSet);
            RegisterZoneRequestHandler<ZoneNpcFlockDelete>(Commands.ZoneNpcFlockDelete);
            RegisterZoneRequestHandler<ZoneNpcFlockKill>(Commands.ZoneNpcFlockKill);
            RegisterZoneRequestHandler<ZoneNpcFlockSetParameter>(Commands.ZoneNpcFlockSetParameter);

            RegisterZoneRequestHandler<GetRifts>(Commands.GetRifts);
            RegisterZoneRequestHandler<UseItem>(Commands.UseItem);
            RegisterZoneRequestHandler<GateSetName>(Commands.GateSetName);
            RegisterZoneRequestHandler<ProximityProbeRemove>(Commands.ProximityProbeRemove);
            RegisterZoneRequestHandler<FieldTerminalInfo>(Commands.FieldTerminalInfo);

            RegisterZoneRequestHandler<PBSFeedableInfo>(Commands.PBSFeedableInfo);
            RegisterZoneRequestHandler<PBSFeedItemsHander>(Commands.PBSFeedItems);
            RegisterZoneRequestHandler<PBSMakeConnection>(Commands.PBSMakeConnection);
            RegisterZoneRequestHandler<PBSBreakConnection>(Commands.PBSBreakConnection);
            RegisterZoneRequestHandler<PBSRenameNode>(Commands.PBSRenameNode);
            RegisterZoneRequestHandler<PBSSetConnectionWeight>(Commands.PBSSetConnectionWeight);
            RegisterZoneRequestHandler<PBSSetOnline>(Commands.PBSSetOnline);
            RegisterZoneRequestHandler<PBSGetNetwork>(Commands.PBSGetNetwork);
            RegisterZoneRequestHandler<PBSCheckDeployment>(Commands.PBSCheckDeployment);
            RegisterZoneRequestHandler<PBSSetStandingLimit>(Commands.PBSSetStandingLimit);
            RegisterZoneRequestHandler<PBSNodeInfo>(Commands.PBSNodeInfo);
            RegisterZoneRequestHandler<PBSGetTerritories>(Commands.PBSGetTerritories);
            RegisterZoneRequestHandler<PBSSetTerritoryVisibility>(Commands.PBSSetTerritoryVisibility);
            RegisterZoneRequestHandler<PBSSetBaseDeconstruct>(Commands.PBSSetBaseDeconstruct);
            RegisterZoneRequestHandler<PBSSetReinforceOffset>(Commands.PBSSetReinforceOffset);
            RegisterZoneRequestHandler<PBSSetEffect>(Commands.PBSSetEffect);
            RegisterZoneRequestHandler<ZoneDrawRamp>(Commands.ZoneDrawRamp);

        }

        private void RegisterPBS()
        {
            _builder.RegisterGeneric(typeof(PBSObjectHelper<>));
            _builder.RegisterGeneric(typeof(PBSReinforceHandler<>));
            _builder.RegisterType<PBSProductionFacilityNodeHelper>();
        }
    }
}