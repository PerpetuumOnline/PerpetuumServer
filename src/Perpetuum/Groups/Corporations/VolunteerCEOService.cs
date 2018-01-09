using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Services.Channels;

namespace Perpetuum.Groups.Corporations
{
    public class VolunteerCEOService : IVolunteerCEOService
    {
        private readonly IVolunteerCEORepository _volunteerCEORepository;
        private readonly IChannelManager _channelManager;

        public DateTime Expiry { get; set; }

        public VolunteerCEOService(IVolunteerCEORepository volunteerCEORepository,IChannelManager channelManager)
        {
            _volunteerCEORepository = volunteerCEORepository;
            _channelManager = channelManager;

#if DEBUG
            Expiry = DateTime.Now.AddMinutes(3);
#else
            Expiry = DateTime.Now.AddDays(2);
#endif
        }

        public VolunteerCEO AddVolunteer(Corporation corporation,Character character)
        {
            var volunteerCEO = new VolunteerCEO
            {
                character = character,
                expiry = Expiry,
                corporation = corporation
            };

            _volunteerCEORepository.Insert(volunteerCEO);
            return volunteerCEO;
        }

        public void TakeOverCeoRole(VolunteerCEO volunteerCEO)
        {
            Logger.Info($"takeover for {volunteerCEO}");

            var corporation = volunteerCEO.character.GetPrivateCorporationOrThrow();

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    if (corporation.Eid != volunteerCEO.corporation.Eid)
                        return;

                    var volunteerCharacter = volunteerCEO.character;

                    var role = corporation.GetMemberRole(volunteerCharacter);
                    var oldRole = role;

                    var currentCEO = corporation.CEO;
                    if (currentCEO == volunteerCEO.character || !role.HasFlag(CorporationRole.DeputyCEO) || role.HasFlag(CorporationRole.CEO))
                        return;

                    //check member count again, possible downgrade could've happened
                    var desiredCEOMaxMembers = corporation.GetMaxmemberByCharacter(volunteerCharacter);
                    if (corporation.Members.Length > desiredCEOMaxMembers)
                        return;

                    role = role.SetRole(CorporationRole.CEO);
                    role = role.ClearRole(CorporationRole.DeputyCEO);

                    corporation.SetMemberRole(volunteerCharacter,role);
                    corporation.WriteRoleHistory(volunteerCEO.character,volunteerCEO.character,role,oldRole);

                    _channelManager.SetMemberRole(corporation.ChannelName,volunteerCEO.character,role);

                    //old ceo

                    var currentCeoRole = corporation.GetMemberRole(currentCEO);
                    var oldCurrentCeoRole = currentCeoRole;

                    currentCeoRole = currentCeoRole.ClearRole(CorporationRole.CEO);
                    currentCeoRole = currentCeoRole.SetRole(CorporationRole.DeputyCEO);

                    corporation.SetMemberRole(currentCEO,currentCeoRole);
                    corporation.WriteRoleHistory(currentCEO,currentCEO,currentCeoRole,oldCurrentCeoRole);

                    _channelManager.SetMemberRole(corporation.ChannelName,currentCEO,currentCeoRole);
                    CorporationData.RemoveFromCache(corporation.Eid);

                    Logger.Info($"{volunteerCharacter} took over CEO for {currentCEO} at corporationeid:{corporation.Eid}");

                    _volunteerCEORepository.Delete(volunteerCEO);

                    //ok inform
                    Transaction.Current.OnCompleted(c =>
                    {
                        SendVolunteerStatusToMembers(volunteerCEO);
                    });

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        public void SendVolunteerStatusToMembers(VolunteerCEO volunteerCEO)
        {
            Message.Builder.SetCommand(Commands.CorporationCeoTakeOverStatus).WithData(volunteerCEO.ToDictionary()).ToCharacters(volunteerCEO.corporation.GetCharacterMembers()).Send();
        }

        public VolunteerCEO GetVolunteer(long corporationEid)
        {
            return _volunteerCEORepository.Get(corporationEid);
        }

        public void ClearVolunteer(VolunteerCEO volunteerCEO)
        {
            _volunteerCEORepository.Delete(volunteerCEO);
        }

        public IEnumerable<VolunteerCEO> GetExpiredVolunteers()
        {
            return _volunteerCEORepository.GetAll().Where(v => v.expiry < DateTime.Now).ToArray();
        }
    }
}