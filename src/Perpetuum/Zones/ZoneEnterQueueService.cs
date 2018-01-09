using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Perpetuum.Accounting.Characters;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Zones
{
    public class ZoneEnterQueueService : Process,IZoneEnterQueueService
    {
        public delegate IZoneEnterQueueService Factory(IZone zone);

        private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(2));
        private readonly IZone _zone;
        private bool _processing;
        private Queue<QueueItem> _queue = new Queue<QueueItem>();

        public ZoneEnterQueueService(IZone zone)
        {
            _zone = zone;
            MaxPlayersOnZone = zone.Configuration.MaxPlayers;
        }

        private bool HasFreeSlot
        {
            get
            {
                var playersCount = _zone.Players.Count();
                return playersCount < MaxPlayersOnZone;
            }
        }

        private void OnQueueChanged()
        {
            QueueItem[] items;
            lock (_queue)
            {
                if ( _queue.Count == 0 )
                    return;
                
                items = _queue.ToArray();
            }

            var messageBuilder = Message.Builder.SetCommand(new Command("zoneEnterQueueInfo")).SetData("length", items.Length);

            foreach (var queueInfo in items.Select((info, currentPosition) => new { info, currentPosition }))
            {
                messageBuilder.SetData("current", queueInfo.currentPosition).ToCharacter(queueInfo.info.character).Send();
            }
        }

        public override void Update(TimeSpan time)
        {
            _timer.Update(time).IsPassed(ProcessQueueAsync);
        }

        private void ProcessQueueAsync()
        {
            if ( _processing || _queue.Count == 0 )
                return;

            Logger.Info("[Zone EQ] start processing queue. zone:" + _zone.Id + " count:" + _queue.Count);

            _processing = true;
            ThreadPool.UnsafeQueueUserWorkItem(_ => ProcessQueue(), null);
        }


        private void ProcessQueue()
        {
            try
            {
                while (true)
                {
                    if ( !HasFreeSlot )
                        return;

                    QueueItem item;
                    lock (_queue)
                    {
                        if (_queue.Count == 0)
                            return;

                        item = _queue.Dequeue();
                    }

                    Logger.Info("[Zone EQ] start processing character. zone:" + _zone.Id + " character:" + item.character + " command:" + item.replyCommand);

                    var character = item.character;
                    var replyCommand = item.replyCommand;

                    try
                    {
                        LoadPlayerAndSendReply(character,replyCommand);
                    }
                    catch (Exception ex)
                    {
                        var err = ErrorCodes.ServerError;

                        var gex = ex as PerpetuumException;
                        if (gex != null)
                        {
                            err = gex.error;
                            var e = new LogEvent
                            {
                                LogType = LogType.Error,
                                Tag = "UREQ",
                                Message = $"[UREQ] {err} Req: {replyCommand}"
                            };

                            Logger.Log(e);
                        }
                        else
                        {
                            Logger.Exception(ex);
                        }

                        character.CreateErrorMessage(replyCommand,err).Send();
                    }

                    Logger.Info("[Zone EQ] end processing character. zone:" + _zone.Id + " character:" + item.character + " command:" + item.replyCommand);

                    OnQueueChanged();
                }
            }
            finally
            {
                _processing = false;
                Logger.Info("[Zone EQ] end processing queue. zone:" + _zone.Id + " count:" + _queue.Count);
            }
        }

        public void EnqueuePlayer(Character character, Command replyCommand)
        {
            Logger.Info("[Zone EQ] start enqueue player. zone:" + _zone.Id + " character:" + character + " command:" + replyCommand);
            lock (_queue)
            {
                _queue.Enqueue(new QueueItem { character = character, replyCommand = replyCommand });
            }

            OnQueueChanged();
            Logger.Info("[Zone EQ] end enqueue player. zone:" + _zone.Id + " character:" + character + " command:" + replyCommand);
        }

        public void RemovePlayer(Character character)
        {
            var changed = false;

            try
            {
                lock (_queue)
                {
                    if ( _queue.Count == 0 )
                        return;

                    var newQ = new Queue<QueueItem>();

                    QueueItem item;
                    while (_queue.TryDequeue(out item))
                    {
                        if (item.character == character)
                        {
                            changed = true;
                            continue;
                        }

                        newQ.Enqueue(item);
                    }

                    _queue = newQ;
                }
            }
            finally
            {
                if ( changed )
                    OnQueueChanged();
            }
        }

        public int MaxPlayersOnZone { get; set; }

        public void LoadPlayerAndSendReply(Character character, Command replyCommand)
        {
            if (!_zone.TryGetPlayer(character,out Player player))
            {
                Logger.Info("[Zone EQ] start loading player. zone:" + _zone.Id + " character:" + character.Id);
                player = Player.LoadPlayerAndAddToZone(_zone,character);
                Logger.Info("[Zone EQ] end loading player. zone:" + _zone.Id + " character:" + character.Id);
            }

            SendReplyCommand(character,player, replyCommand);
        }

        public void SendReplyCommand(Character character, Player player, Command replyCommand)
        {
            Logger.Info("[Zone EQ] start sending reply command. player: " + player.Eid + " character:" + character.Id + " reply:" + replyCommand);

            var result = new Dictionary<string, object>
            {
                {k.characterID, character.Id},
                {k.rootEID, character.Eid},
                {k.corporationEID, character.CorporationEid},
                {k.allianceEID, character.AllianceEid},
                {
                    k.zone, new Dictionary<string, object>
                    {
                        {k.robot, player.ToDictionary()},
                        {k.guid, ZoneTicket.CreateAndEncryptFor(character)},
                        {k.plugin, _zone.Configuration.PluginName},
                        {k.decor, _zone.DecorHandler.DecorObjectsToDictionary()},
                        {k.plants, _zone.Configuration.PlantRules.GetPlantInfoForClient()},
                        {k.buildings, _zone.GetBuildingsDictionaryForCharacter(player.Character)}
                    }
                }
            };

            Message.Builder.SetCommand(replyCommand)
                           .WithData(result)
                           .WrapToResult()
                           .ToCharacter(character)
                           .Send();

            Logger.Info("[Zone EQ] end sending reply command. player: " + player.Eid + " character:" + character.Id + " reply:" + replyCommand);
        }

        public Dictionary<string, object> GetQueueInfoDictionary()
        {
            var result = new Dictionary<string, object>
            {
                {k.zoneID, _zone.Id}, 
                {k.length, MaxPlayersOnZone}, 
                {k.count,_queue.Count}
            };

            return result;
        }

        private struct QueueItem
        {
            public Character character;
            public Command replyCommand;
        }
    }
}