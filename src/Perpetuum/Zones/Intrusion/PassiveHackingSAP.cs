using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;

namespace Perpetuum.Zones.Intrusion
{
    /// <summary>
    /// Intrusion target which can be completed by standing in the vicinity of the SAP
    /// </summary>
    public class PassiveHackingSAP : SAP
    {
        private static readonly TimeSpan _updateScoreInterval = TimeSpan.FromSeconds(2);
        private static TimeSpan _takeoverTime = TimeSpan.FromMinutes(8);
        private static readonly int _maxScore = (int) _takeoverTime.Divide(_updateScoreInterval).Ticks;
        private const int RANGE = 5;
        private readonly IntervalTimer _updateScoreTimer = new IntervalTimer(_updateScoreInterval);

        public PassiveHackingSAP() : base(BeamType.attackpoint_presence_enter, BeamType.attackpoint_presence_out)
        {
        }

        protected override void OnUpdate(TimeSpan time)
        {
            UpdatePlayerScores(time);
            base.OnUpdate(time);
        }

        protected override int MaxScore => _maxScore;

        private void UpdatePlayerScores(TimeSpan time)
        {
            _updateScoreTimer.Update(time);

            if (!_updateScoreTimer.Passed)
                return;

            _updateScoreTimer.Reset();

            var playersInRange = GetPlayersInSAPRange();

            CheckPlayersInRange(playersInRange);
            CheckInactivePlayers(playersInRange);
        }

        private IList<Player> GetPlayersInSAPRange()
        {
            var playersInSAPRange = Zone.Players.WithinRange(CurrentPosition,RANGE).ToArray();
            return playersInSAPRange;
        }

        private void CheckPlayersInRange(IEnumerable<Player> playersInRange)
        {
            var builder = Beam.NewBuilder()
                              .WithType(BeamType.loot_bolt)
                              .WithSource(this)
                              .WithState(BeamState.Hit)
                              .WithDuration(3000);

            foreach (var player in playersInRange)
            {
                builder.WithTarget(player);
                Zone.CreateBeam(builder);
                IncrementPlayerScore(player, 1);
            }
        }

        private void CheckInactivePlayers(IList<Player> playersInRange)
        {
            var playerInfos = PlayerInfos;

            foreach (var playerInfo in playerInfos)
            {
                if ( playersInRange.Any(p => p.Character == playerInfo.character))
                    continue;

                RemovePlayerInfo(playerInfo.character);
            }
        }

        protected override void AppendTopScoresToPacket(Packet packet,int count)
        {
            AppendPlayerTopScoresToPacket(this, packet,count);
        }
    }
}