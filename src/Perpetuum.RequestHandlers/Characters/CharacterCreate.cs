using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;
using Perpetuum.Services.Mail;
using Perpetuum.Services.Sparks;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Training;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterCreate : IRequestHandler
    {
        private TimeSpan WAIT_TIME_BEFORE_SENDING_MAIL = TimeSpan.FromSeconds(10);
        private TimeSpan WAIT_TIME_BEFORE_SENDING_WELCOME_MESSAGE = TimeSpan.FromSeconds(10);

        private readonly IAccountManager _accountManager;
        private readonly IChannelManager _channelManager;
        private readonly DockingBaseHelper _dockingBaseHelper;
        private readonly CharacterFactory _characterFactory;
        private readonly IEntityServices _entityServices;
        private readonly SparkHelper _sparkHelper;

        public CharacterCreate(IAccountManager accountManager,IChannelManager channelManager,DockingBaseHelper dockingBaseHelper,CharacterFactory characterFactory,IEntityServices entityServices,SparkHelper sparkHelper)
        {
            _accountManager = accountManager;
            _channelManager = channelManager;
            _dockingBaseHelper = dockingBaseHelper;
            _characterFactory = characterFactory;
            _entityServices = entityServices;
            _sparkHelper = sparkHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var nick = request.Data.GetOrDefault<string>(k.nick).Trim();
                var avatar = request.Data.GetOrDefault<Dictionary<string, object>>(k.avatar);
                var raceID = request.Data.GetOrDefault<int>(k.raceID);
                var majorID = request.Data.GetOrDefault<int>(k.majorID);
                var schoolID = request.Data.GetOrDefault<int>(k.schoolID);
                var sparkID = request.Data.GetOrDefault<int>(k.sparkID);

                var account = _accountManager.Repository.Get(request.Session.AccountId);
                if (account == null)
                    throw new PerpetuumException(ErrorCodes.AccountNotFound);

                Character.CheckNickAndThrowIfFailed(nick, request.Session.AccessLevel, account);

                //only 3 characters per account is allowed
                var activeCharactersCount = _accountManager.GetActiveCharactersCount(account);
                if (activeCharactersCount >= 3)
                    throw new PerpetuumException(ErrorCodes.MaximumAmountOfCharactersReached);

                if (account.FirstCharacterDate == null)
                {
                    account.FirstCharacterDate = DateTime.Now;
                    _accountManager.AddExtensionPoints(account, 40000); //TODO: starting EP - store in DB
                    _accountManager.Repository.Update(account);
                }

                var character = CreateNewCharacter();
                character.AccountId = account.Id;
                character.Nick = nick;
                character.Avatar = GenxyString.FromDictionary(avatar);
                character.IsDocked = true;

                DockingBase dockingBase;
                DefaultCorporation corporation;

                if (schoolID > 0)
                {
                    if (majorID == 0 || raceID == 0 || sparkID == 0)
                        throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);

                    corporation = DefaultCorporation.GetBySchool(raceID,schoolID) ?? throw new PerpetuumException(ErrorCodes.CorporationNotExists);
                    dockingBase = corporation.GetDockingBase();
                    dockingBase.CreateStarterRobotForCharacter(character,true);

                    character.MajorId = majorID;
                    character.RaceId = raceID;
                    character.SchoolId = schoolID;
                    character.SparkId = sparkID;
                    character.AddToWallet(TransactionType.CharacterCreate,20000);

                    var extensions = character.GetDefaultExtensions();
                    character.SetExtensions(extensions);

                    var sparkToActivate = _sparkHelper.ConvertCharacterWizardSparkIdToSpark(sparkID);
                    _sparkHelper.ActivateSpark(character,sparkToActivate);
                }
                else
                {
                    // training
                    dockingBase = _dockingBaseHelper.GetTrainingDockingBase();
                    corporation = ((TrainingDockingBase)dockingBase).GetTrainingCorporation();
                    character.SetAllExtensionLevel(6);
                    dockingBase.CreateStarterRobotForCharacter(character);
                    character.AddToWallet(TransactionType.CharacterCreate,10000000);

                    Task.Delay(WAIT_TIME_BEFORE_SENDING_MAIL)
                        .ContinueWith(task => MailHandler.SendWelcomeMailBeginTutorial(character));
                    Task.Delay(WAIT_TIME_BEFORE_SENDING_WELCOME_MESSAGE)
                    .ContinueWith(task =>
                    {
                        ChannelMessageHandler.SendNewPlayerTutorialMessage(_channelManager, character.Nick);
                    });
                }

                character.CurrentDockingBaseEid = dockingBase.Eid;
                character.DefaultCorporationEid = corporation.Eid;

                //add to default corp, only in sql
                corporation.AddNewCharacter(character);

                _accountManager.PackageGenerateAll(account);

                Transaction.Current.OnCommited(() =>
                {
                    _channelManager.JoinChannel(corporation.ChannelName, character);
                    Message.Builder.FromRequest(request).SetData(k.characterID,character.Id).Send();
                });
                
                scope.Complete();
            }
        }

        private Character CreateNewCharacter()
        {
            var characterEntity = _entityServices.Factory.CreateWithRandomEID(DefinitionNames.PLAYER);
            _entityServices.Repository.Insert(characterEntity);

            var id = Db.Query().CommandText("insert into characters (rootEid) values (@rootEid);select cast(scope_identity() as int)")
                .SetParameter("@rootEid", characterEntity.Eid)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            return _characterFactory(id);
        }
    }
}