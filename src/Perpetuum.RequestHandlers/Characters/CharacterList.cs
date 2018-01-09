using System;
using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterList : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly DockingBaseHelper _dockingBaseHelper;

        public CharacterList(IAccountManager accountManager,DockingBaseHelper dockingBaseHelper)
        {
            _accountManager = accountManager;
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var result = new Dictionary<string, object>();
            var charactersDict = new Dictionary<string, object>();

            var count = 0;

            var records = Db.Query().CommandText(@"select
c.characterid as characterid,
c.rootEID as rooteid,
c.moodMessage as moodmessage,
c.lastUsed as lastused,
c.creation as creation,
c.credit as credit,
c.nick as nick,
c.inUse as inuse,
c.avatar as avatar,
c.docked as docked,
c.activechassis as activechassis,
c.zoneID as zoneid,
c.baseEID as baseeid,
c.homebaseEID as homebaseeid,
c.offensivenick as offensivenick,
e.ename as currentbasename,
h.ename as homebasename
from characters c JOIN entities e on e.eid=c.baseEID 
LEFT JOIN entities h ON c.homebaseEID=h.eid 
where accountID = @accountID and active = 1").SetParameter("@accountID", request.Session.AccountId)
                .Execute();

            foreach (var record in records)
            {
                var characterID = record.GetValue<int>("characterid");
                var isDocked = record.GetValue<bool>("docked");
                var zoneId = record.GetValue<int?>("zoneid");
                var currentBaseEID = record.GetValue<long>("baseeid");
                var homeBaseEID = record.GetValue<long?>("homebaseeid") ?? 0L;
                var offensiveNick = record.GetValue<bool>("offensivenick");
                var currentBaseName = record.GetValue<string>("currentbasename");
                var homeBaseName = record.GetValue<string>("homebasename");
                var moodMessage = record.GetValue<string>("moodmessage");
                var lastUsed = record.GetValue<DateTime?>("lastused");
                var creation = record.GetValue<DateTime>("creation");
                var credit = record.GetValue<double>("credit");
                var nick = record.GetValue<string>("nick");
                var inUse = record.GetValue<bool>("inuse");
                var avatar = record.GetValue<string>("avatar");
                var rootEid = record.GetValue<long>("rooteid");

                var currentDockingBase = _dockingBaseHelper.GetDockingBase(currentBaseEID);
                var homeDockingBase = _dockingBaseHelper.GetDockingBase(homeBaseEID);

                var dict = new Dictionary<string, object>
                {
                    {k.characterID, characterID},
                    {k.rootEID, rootEid},
                    {k.moodMessage, moodMessage},
                    {k.lastUsed, lastUsed},
                    {k.creation, creation},
                    {k.credit, (long) credit},
                    {k.nick, nick},
                    {k.inUse, inUse ? 1 : 0},
                    {k.avatar, (GenxyString) avatar},
                    {k.docked, isDocked},
                    {k.zoneID, zoneId},
                    {k.baseEID, currentBaseEID},
                    {k.homeBaseEID, homeBaseEID},
                    {k.baseName, currentBaseName},
                    {k.homeBaseName, homeBaseName},
                    {k.offensiveNick, offensiveNick},
                    {k.currentBaseZone, currentDockingBase?.Zone.Id},
                    {k.homeBaseZone, homeDockingBase?.Zone.Id},
                    {k.baseDefinition, currentDockingBase?.Definition},
                    {k.homeBaseDefinition, homeDockingBase?.Definition},
                    {k.dockingBaseInfo, currentDockingBase?.GetDockingBaseDetails()}
                };

                charactersDict.Add("c" + count++, dict);
            }

            result.Add("characters", charactersDict);

            var account = _accountManager.Repository.Get(request.Session.AccountId);
            var ep = _accountManager.CalculateCurrentEp(account);
            result.Add("extensionPoints", ep);
            
            Message.Builder.FromRequest(request).WithData(result).WrapToResult().WithEmpty().Send();
        }
    }
}