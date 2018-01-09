using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations.Applications;
using Perpetuum.Log;
using Perpetuum.Services.Channels;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.TechTree;
using Perpetuum.Zones;

namespace Perpetuum.Groups.Corporations
{
    public class PrivateCorporation : Corporation
    {
        private readonly ITechTreeService _techTreeService;
        private readonly IChannelManager _channelManager;
        private readonly IReadOnlyRepository<int,CharacterProfile> _characterProfiles;
        private readonly IVoteHandler _voteHandler;
        private readonly CharacterWalletFactory _characterWalletFactory;
        private readonly IMarketOrderRepository _marketOrderRepository;

        public PrivateCorporation(ITechTreeService techTreeService,IChannelManager channelManager,IReadOnlyRepository<int,CharacterProfile> characterProfiles,IVoteHandler voteHandler,CharacterWalletFactory characterWalletFactory,IMarketOrderRepository marketOrderRepository)
        {
            _techTreeService = techTreeService;
            _channelManager = channelManager;
            _characterProfiles = characterProfiles;
            _voteHandler = voteHandler;
            _characterWalletFactory = characterWalletFactory;
            _marketOrderRepository = marketOrderRepository;
        }

        public override IDictionary<string, object> GetInfoDictionaryForMember(Character member)
        {
            var result = base.GetInfoDictionaryForMember(member);

            //add members if it's a private corporation and it's active
            if (IsActive)
            {
                result.Add(k.members, Members.ToDictionary());
                result.Add(k.maxMemberCount, MaxMemberCount);
            }

            if (member != CEO)
            {
                var leaveEnd = CorporationManager.GetLeaveTime(member, out bool isLeaveActive);
                if (isLeaveActive)
                {
                    result.Add(k.leaveEnd, leaveEnd);
                }

                var joinEnd = CorporationManager.GetJoinEnd(member, Eid, out bool isJoinActive);
                if (isJoinActive)
                {
                    result.Add(k.joinEnd, joinEnd);
                }
            }

            if (HasRole(member, PresetCorporationRoles.CAN_LIST_TECHTREE))
            {
                _techTreeService.AddInfoToDictionary(Eid, result);
            }

            return result;
        }


        public override void AddMember(Character member, CorporationRole role, Corporation oldCorporation)
        {
            member.AllianceEid = 0L;
            base.AddMember(member, role, oldCorporation);
        }

        public void AddRecruitedMember(Character newMember, Character recruiterMember)
        {
            IsActive.ThrowIfFalse(ErrorCodes.AccessDenied);
            IsAvailableFreeSlot.ThrowIfFalse(ErrorCodes.CorporationMaxMembersReached);

            var oldCorporation = newMember.GetCorporation();

            //this protects the alliance board member complication and default corp situation
            var role = GetRoleFromSql(newMember);
            role.ThrowIfNotEqual(CorporationRole.NotDefined, ErrorCodes.MemberHasRolesError);

            CorporationManager.IsJoinAllowed(newMember).ThrowIfFalse(ErrorCodes.CorporationChangeTooOften);

            AddMember(newMember, CorporationRole.NotDefined, oldCorporation);
            oldCorporation.RemoveMember(newMember);

            newMember.GetCorporationApplications().DeleteAll();

            _channelManager.LeaveChannel(oldCorporation.ChannelName, newMember);
            _channelManager.JoinChannel(ChannelName, newMember, GetMemberRole(newMember));

            Transaction.Current.OnCommited(() =>
            {
                CorporationManager.InformCorporationMemberTransferred(oldCorporation, this, newMember);

                newMember.GetPlayerRobotFromZone()?.UpdateCorporationOnZone(Eid);
                var info = new Dictionary<string, object>
                {
                    {k.@from, oldCorporation.Eid},
                    {k.to, Eid},
                    {k.characterID, newMember.Id},
                };

                ZoneManager.Value.Zones.ForEach(z => z.UpdateCorporation(CorporationCommand.TransferMember,info));

                CorporationData.RemoveFromCache(oldCorporation.Eid);
                CorporationData.RemoveFromCache(Eid);

                if (_characterProfiles is CachedReadOnlyRepository<int,CharacterProfile> c)
                    c.Remove(newMember.Id);

            });
        }

        public void Leave(Character character)
        {
            //is it still valid? (not cancelled)
            if (!CorporationManager.IsInLeavePeriod(character))
            {
                Logger.Info("leaveCorporation: leave cancelled. characterID: " + character.Id);
                return;
            }

            if (character == CEO)
            {
                //ceo is leaving the corp, it's allowed only when he is the last one
                if (GetCharacterMembers().Count() > 1)
                {
                    Logger.Info("leaveCorporation: corporation has members leave cancelled for characterID:" + character.Id);
                    return;
                }

                //he is the last one = corp closing
                Logger.Info("leaveCorporation: corporation is closing: eid:" + Eid);
            }

            

            //freelancer corporation eid
            if (DefaultCorporation.IsFreelancerCorporation(Eid))
            {
                Logger.Info("leaveCorporation: character is in private corporation. characterID:" + character.Id);
                return;
            }

            var newCorporation = DefaultCorporation.GetFreelancerCorporation();

            newCorporation.AddMember(character, CorporationRole.NotDefined, this);

            RemoveMember(character);

            //remove from documents
            CorporationDocumentHelper.OnCorporationLeave(character,Eid);

            _channelManager.LeaveChannel(ChannelName, character);
            _channelManager.JoinChannel(newCorporation.ChannelName, character, CorporationRole.NotDefined);

            Transaction.Current.OnCommited(() =>
            {
                CorporationManager.InformCorporationMemberTransferred(this, newCorporation, character);

                character.GetPlayerRobotFromZone()?.UpdateCorporationOnZone(newCorporation.Eid);
                var info = new Dictionary<string, object>
                {
                    {k.from,Eid},
                    {k.to, newCorporation.Eid},
                    {k.characterID, character.Id},
                };

                ZoneManager.Value.Zones.ForEach(z => z.UpdateCorporation(CorporationCommand.TransferMember,info));
            });

            Logger.Info("characterID: " + character.Id + " left corporation: eid:" + Eid + " default corporation: eid:" + newCorporation.Eid);
        }

        protected override void OnMemberRemoved(Character member)
        {
            base.OnMemberRemoved(member);

            //clean up insurances
            var deletedInsurances = InsuranceHelper.CleanUpOnCorporationLeave(member, Eid);

            InsuranceHelper.SendEmptyCorporationInsuranceList(member);

            //remove market orders
            CancelAllCorporationOrders(member);

            //last member leaving
            if (Members.Length <= 0)
            {
                //set the corp inactive
                IsActive = false;
                CorporationManager.DeleteYellowPages(Eid);

                //... do other corp closing cleanup
                Transaction.Current.OnCommited(() =>
                {
                    ZoneManager.Value.Zones.ForEach(z => z.UpdateCorporation(CorporationCommand.Close,new Dictionary<string,object>
                                {
                                    {k.corporationEID, Eid}
                                }));
                });
            }
            else
            {
                if (deletedInsurances > 0)
                {
                    //refresh insurance list
                    SendInsuranceList();
                }
            }
        }

        private void CancelAllCorporationOrders(Character member)
        {
            var orderz = _marketOrderRepository.GetOrdersOnCorporationLeave(member).ToList();

            Logger.Info($"{orderz.Count} market orders to clean up on character corp leave. {this}");

            foreach (var marketOrder in orderz)
            {
                try
                {
                    marketOrder.Cancel(_marketOrderRepository);
                }
                catch (Exception ex)
                {
                    Logger.Error("trouble with market order: " + marketOrder);
                    Logger.Exception(ex);
                }
            }
        }



        public IEnumerable<Vote> GetVotes()
        {
            return _voteHandler.GetVotesByGroup(Eid);
        }

        public IEnumerable<int> GetOpenVoteIDs(Character member)
        {
            return _voteHandler.GetMyOpenVotes(member, Eid);
        }

        public Vote GetVote(int voteID)
        {
            return _voteHandler.GetVote(voteID);
        }

        public IEnumerable<VoteEntry> GetVoteEntries(Vote vote)
        {
            if (vote.groupEID != Eid)
                return Enumerable.Empty<VoteEntry>();

            return _voteHandler.GetVoteEntries(vote);
        }

        public void StartVote(Character issuer, string name, string topic, int participation, int consensusRate)
        {
            if (!CanStartVote(issuer))
                throw new PerpetuumException(ErrorCodes.InsufficientPrivileges);

            var maxNofVote = CEO.GetExtensionBonusWithPrerequiredExtensions(ExtensionNames.ALLIANCE_VOTING);
            var corpCurrentVotes = _voteHandler.VoteCount(Eid);

            if (maxNofVote < corpCurrentVotes)
                throw new PerpetuumException(ErrorCodes.MaxNumberOfVotesReached);

            var vote = new Vote
            {
                ConsensusRate = consensusRate,
                groupEID = Eid,
                participation = participation,
                startDate = DateTime.Now,
                startedBy = issuer,
                voteName = name,
                voteType = VoteType.Freeform,
                voteTopic = topic,
            };

            _voteHandler.InsertVote(vote);

            Transaction.Current.OnCommited(() => Message.Builder
                .SetCommand(Commands.CorporationVoteStart)
                .WithData(new Dictionary<string, object> { { k.vote, vote.ToDictionary() } })
                .ToCharacters(GetCharacterMembers())
                .Send());
        }

        public void CastVote(Vote vote,Character member,bool voteChoice)
        {
            _voteHandler.CastVote(vote,member,voteChoice);
            _voteHandler.EvaluateVote(vote);
        }

        public void SetVoteTopic(Character issuer, int voteId, string topic)
        {
            if (!CanSetVoteTopic(issuer))
                throw new PerpetuumException(ErrorCodes.InsufficientPrivileges);

            var vote = GetVote(voteId);
            if (vote == null)
                throw new PerpetuumException(ErrorCodes.ItemNotFound);

            vote.voteTopic = topic;
            _voteHandler.UpdateTopic(vote);

            Transaction.Current.OnCommited(() => Message.Builder
                .SetCommand(Commands.CorporationVoteSetTopic)
                .WithData(new Dictionary<string, object> { { k.vote, vote.ToDictionary() } })
                .ToCharacters(GetCharacterMembers())
                .Send());
        }

        private bool CanSetVoteTopic(Character issuer)
        {
            return GetMemberRole(issuer).IsAnyRole(CorporationRole.CEO, CorporationRole.PRManager, CorporationRole.DeputyCEO);
        }

        public void DeleteVote(Character issuer, int voteId)
        {
            if (!CanDeleteVote(issuer))
                throw new PerpetuumException(ErrorCodes.InsufficientPrivileges);

            var vote = GetVote(voteId);
            if (vote == null)
                throw new PerpetuumException(ErrorCodes.ItemNotFound);

            _voteHandler.DeleteVote(vote);

            Transaction.Current.OnCommited(() => Message.Builder
                .SetCommand(Commands.CorporationVoteDelete)
                .WithData(new Dictionary<string, object> { { k.voteID, voteId } })
                .ToCharacters(GetCharacterMembers())
                .Send());
        }

        private bool CanDeleteVote(Character issuer)
        {
            var memberRole = GetMemberRole(issuer);
            var isAnyRole = memberRole.IsAnyRole(CorporationRole.CEO, CorporationRole.PRManager, CorporationRole.DeputyCEO);
            return isAnyRole;
        }

        private bool CanStartVote(Character member)
        {
            return GetMemberRole(member).IsAnyRole(CorporationRole.CEO, CorporationRole.PRManager, CorporationRole.DeputyCEO);
        }

        public void PayOut(Character member, double amount, Character issuer)
        {
            if ( amount <= 0 )
                return;

            CanPayOut(issuer).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
            IsMember(member).ThrowIfFalse(ErrorCodes.NotMemberOfCorporation);

            var ceo = CEO;
            if (ceo != issuer)
            {
                CorporationManager.IsJoinPeriodExpired(member, Eid).ThrowIfFalse(ErrorCodes.corporationTransactionsFrozen);
            }

            var builder = TransactionLogEvent.Builder().SetCorporation(this).SetTransactionType(TransactionType.characterPayOut);

            var corporationWallet = new CorporationWallet(this);
            corporationWallet.Balance -= amount;
            LogTransaction(builder.SetCreditBalance(corporationWallet.Balance)
                                  .SetCreditChange(-amount)
                                  .SetCharacter(issuer)
                                  .SetInvolvedCharacter(member).Build());

            var memberWallet = _characterWalletFactory(member,TransactionType.characterPayOut);
            memberWallet.Balance += amount;
            member.LogTransaction(builder.SetCreditBalance(memberWallet.Balance)
                                            .SetCreditChange(amount)
                                            .SetCharacter(member)
                                            .SetInvolvedCharacter(issuer).Build());
        }

        private bool CanPayOut(Character member)
        {
            var role = GetMemberRole(member);
            return role.IsAnyRole(CorporationRole.CEO, CorporationRole.Accountant, CorporationRole.DeputyCEO);
        }

        public void Transfer(PrivateCorporation target, double amount,Character issuer)
        {
            if ( Eid == target.Eid )
                return;

            CanTransfer(issuer).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
            target.IsActive.ThrowIfFalse(ErrorCodes.CorporationMustBeActive);

            var sourceWallet = new CorporationWallet(this);
            sourceWallet.Balance -= amount;
            LogTransaction(TransactionLogEvent.Builder()
                                              .SetTransactionType(TransactionType.corporationTransfer_to)
                                              .SetCreditBalance(sourceWallet.Balance)
                                              .SetCreditChange(-amount)
                                              .SetCorporation(this)
                                              .SetInvolvedCorporation(target).Build());

            var targetWallet = new CorporationWallet(target);
            targetWallet.Balance += amount;
            target.LogTransaction(TransactionLogEvent.Builder()
                                                     .SetTransactionType(TransactionType.corporationTransfer_from)
                                                     .SetCreditBalance(targetWallet.Balance)
                                                     .SetCreditChange(amount)
                                                     .SetCorporation(target)
                                                     .SetInvolvedCorporation(this).Build());
        }

        private bool CanTransfer(Character character)
        {
            var role = GetMemberRole(character);
            return role.IsAnyRole(CorporationRole.CEO, CorporationRole.Accountant, CorporationRole.DeputyCEO);
        }

        public static PrivateCorporation Create(CorporationDescription corporationDescription)
        {
            var container = SystemContainer.GetByName(k.es_private_corporation);
            return (PrivateCorporation)Create(EntityDefault.GetByName(DefinitionNames.PRIVATE_CORPORATION), container, corporationDescription,EntityIDGenerator.Random);
        }

        [NotNull]
        public new static PrivateCorporation GetOrThrow(long eid)
        {
            return (Get(eid)).ThrowIfNull(ErrorCodes.CorporationMustBePrivate);
        }

        [CanBeNull]
        public new static PrivateCorporation Get(long eid)
        {
            return Corporation.GetOrThrow(eid) as PrivateCorporation;
        }
    }
}