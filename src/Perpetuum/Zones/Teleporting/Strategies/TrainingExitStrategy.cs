using System;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Players;
using Perpetuum.Services.Channels;
using Perpetuum.Services.Sparks;
using Perpetuum.Zones.Training.Reward;

namespace Perpetuum.Zones.Teleporting.Strategies
{
    public class TrainingExitStrategy : ITeleportStrategy
    {
        private const double CHARACTER_START_CREDIT = 500000; //TODO: move to DB
        private const int MAX_REWARD_LEVEL = 4;

        private readonly TeleportDescription _description;
        private readonly ITrainingRewardRepository _trainingRewardRepository;
        private readonly IChannelManager _channelManager;
        private readonly CharacterCleaner _characterCleaner;
        private readonly SparkHelper _sparkHelper;
        private int _trainingRewardLevel;

        public delegate TrainingExitStrategy Factory(TeleportDescription description);

        public TrainingExitStrategy(TeleportDescription description, ITrainingRewardRepository trainingRewardRepository, IChannelManager channelManager, CharacterCleaner characterCleaner, SparkHelper sparkHelper)
        {
            _description = description;
            _trainingRewardRepository = trainingRewardRepository;
            _channelManager = channelManager;
            _characterCleaner = characterCleaner;
            _sparkHelper = sparkHelper;
        }

        public int TrainingRewardLevel
        {
            get => _trainingRewardLevel;
            set => _trainingRewardLevel = value.Clamp(0, MAX_REWARD_LEVEL);
        }

        public void DoTeleport(Player player)
        {
            //Throw if email not confirmed
            player.Character.GetAccount().EmailConfirmed.ThrowIfFalse(ErrorCodes.EmailNotConfirmed);

            player.States.Dock = true;

            var character = player.Character;

            var oldCorporation = character.GetCorporation();

            _characterCleaner.CleanUp(character);

            var info = GetCharacterWizardInfo();

            var newCorporation = DefaultCorporation.GetBySchool(info.raceId, info.schoolId);
            oldCorporation.RemoveMember(character);
            newCorporation.AddMember(character, CorporationRole.NotDefined, oldCorporation);

            character.MajorId = info.majorId;
            character.RaceId = info.raceId;
            character.SchoolId = info.schoolId;
            character.SparkId = info.sparkId;
            character.Credit = CHARACTER_START_CREDIT;
            character.DefaultCorporationEid = newCorporation.Eid;

            //add default extensions
            var extensions = character.GetDefaultExtensions();
            character.SetExtensions(extensions);

            var sparkToActivate = _sparkHelper.ConvertCharacterWizardSparkIdToSpark(info.sparkId);
            _sparkHelper.ActivateSpark(character, sparkToActivate);

            //Grab TM-UAS docking base for New Virginia
            var hardCodeTMACorp = DefaultCorporation.GetBySchool(1, 1);
            var dockingBase = hardCodeTMACorp.GetDockingBase();
            dockingBase.CreateStarterRobotForCharacter(character, true);
            dockingBase.DockIn(character, TimeSpan.Zero, ZoneExitType.TrainingExitTeleport);

            var publicContainer = dockingBase.GetPublicContainerWithItems(character);
            CreateRewardItems(character, publicContainer);

            publicContainer.Save();

            Transaction.Current.OnCommited(() =>
            {
                _channelManager.LeaveChannel(oldCorporation.ChannelName, character);
                _channelManager.JoinChannel(newCorporation.ChannelName, character);
                player.RemoveFromZone();
            });
        }

        private void CreateRewardItems(Character character, Container container)
        {
            var builder = new TrainingRewardBuilder(character);

            var raceId = character.RaceId;

            //industrial exception
            if (character.SparkId == 5)
            {
                raceId = 5;
            }

            var rewards = _trainingRewardRepository.GetAllRewards()
                .Where(r => r.Level <= _trainingRewardLevel && r.RaceId == raceId)
                .Select(builder.Build)
                .SelectMany(items => items);

            foreach (var item in rewards)
            {
                container.AddItem(item, false);
            }
        }

        private struct CharacterWizardInfo
        {
            public int majorId;
            public int raceId;
            public int schoolId;
            public int sparkId;

            public CharacterWizardInfo(int raceId, int schoolId, int majorId, int sparkId) : this()
            {
                this.majorId = majorId;
                this.raceId = raceId;
                this.schoolId = schoolId;
                this.sparkId = sparkId;
            }
        }

        private CharacterWizardInfo GetCharacterWizardInfo()
        {
            return HardcodedLookUp(_description);
        }


        private CharacterWizardInfo HardcodedLookUp(TeleportDescription description)
        {
            var raceIndex = 0;

            if (description.description.Contains("ics"))
            {
                raceIndex = 1;
            }

            if (description.description.Contains("asi"))
            {
                raceIndex = 2;
            }

            var isIndustrial = description.description.Contains("industrial");
            if (!isIndustrial)
            {
                switch (raceIndex)
                {
                    case 0: // tm
                        return new CharacterWizardInfo(1, 1, 1, 1);

                    case 1: // ics
                        return new CharacterWizardInfo(2, 4, 10, 1);

                    case 2: // asi
                        return new CharacterWizardInfo(3, 7, 19, 1);
                }
            }
            else
            {
                switch (raceIndex)
                {
                    case 0: // tm
                        return new CharacterWizardInfo(1, 2, 5, 5);
                    case 1: // ics
                        return new CharacterWizardInfo(2, 5, 14, 5);
                    case 2: // asi
                        return new CharacterWizardInfo(3, 8, 23, 5);
                }
            }

            return new CharacterWizardInfo(1, 1, 1, 1);
        }

    }
}