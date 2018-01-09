using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.RequestHandlers.Intrusion
{
    public class SetIntrusionSiteMessage : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public SetIntrusionSiteMessage(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var siteEid = request.Data.GetOrDefault<long>(k.eid);
                var message = request.Data.GetOrDefault<string>(k.message);
                var character = request.Session.Character;

                var eventType = IntrusionEvents.messageSet;
                var clearMessage = false;
                if (string.IsNullOrEmpty(message))
                {
                    clearMessage = true;
                    eventType = IntrusionEvents.messageClear;
                }

                var outpost = _dockingBaseHelper.GetDockingBase(siteEid).ThrowIfNotType<Outpost>(ErrorCodes.ItemNotFound);
                var siteInfo = outpost.GetIntrusionSiteInfo();

                var owner = siteInfo.Owner.ThrowIfNull(ErrorCodes.AccessDenied);
                siteInfo.Owner.ThrowIfNotEqual(character.CorporationEid, ErrorCodes.AccessDenied);

                var role = Corporation.GetRoleFromSql(character);
                role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.PRManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                if (!message.IsNullOrEmpty() && message.Length > 256)
                {
                    message = message.Substring(0, 256);
                }

                if (clearMessage)
                    outpost.ClearSiteMessage();
                else
                    outpost.SetSiteMessage(message);

                outpost.InsertIntrusionSiteMessageLog(character, message, owner, eventType);

                Message.Builder.FromRequest(request).WithOk().Send();

                Transaction.Current.OnCommited(() => outpost.SendSiteInfoToOnlineCharacters());
                
                scope.Complete();
            }
        }
    }
}