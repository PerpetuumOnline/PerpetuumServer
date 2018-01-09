using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Groups.Corporations
{
    public class DefaultCorporation : Corporation
    {
        private readonly DockingBaseHelper _dockingBaseHelper;
        private const string SYNDICATE_FREELANCER = "syndicatefreelancer";

        private static readonly Lazy<long> _freelancerCorporationEid;

        static DefaultCorporation()
        {
            DockingBaseEids = Database.CreateCache<long, long>("cw_corporation","corporationeid", "baseeid");
            _freelancerCorporationEid = new Lazy<long>(() => GetEidByName(SYNDICATE_FREELANCER));
        }

        public DefaultCorporation(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public static bool IsFreelancerCorporation(long corporationEid)
        {
            return _freelancerCorporationEid.Value == corporationEid;
        }

        public static DefaultCorporation GetFreelancerCorporation()
        {
            return (DefaultCorporation) GetOrThrow(_freelancerCorporationEid.Value);
        }

        private static IDictionary<long, long> DockingBaseEids { get; }

        public override void AddMember(Character member, CorporationRole role, Corporation oldCorporation)
        {
            member.AllianceEid = DefaultCorporationDataCache.GetAllianceEidByCorporationEid(Eid);
            base.AddMember(member, role, oldCorporation);
        }

        public void AddNewCharacter(Character character)
        {
            Db.Query().CommandText("insert into corporationmembers (memberid,corporationEID,role) values (@memberid,@corporationEID,0)")
                .SetParameter("@memberid", character.Id)
                .SetParameter("@corporationEID", Eid)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            Db.Query().CommandText("insert corporationHistory (characterID, corporationEID, corporationJoined) values (@characterID, @corporationEID, @now) ")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@corporationEID", Eid)
                .SetParameter("@now", DateTime.Now)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            character.CorporationEid = Eid;
        }

        public DockingBase GetDockingBase()
        {
            var eid = DockingBaseEids[Eid];
            return _dockingBaseHelper.GetDockingBase(eid);
        }

        public static long GetDockingBaseEid(Character character)
        {
            var defaultCorporationEid = character.DefaultCorporationEid;
            return DockingBaseEids[defaultCorporationEid];
        }

        [CanBeNull]
        public static DefaultCorporation GetBySchool(int raceID,int schoolId)
        {
            if (schoolId == 0)
                return null;

            var corporationName = "";

            switch (raceID)
            {
                case 1:
                    corporationName = "usa_corp_";
                    break;

                case 2:
                    corporationName = "eu_corp_";
                    break;

                case 3:
                    corporationName = "asia_corp_";
                    break;
            }

            switch (schoolId)
            {
                case 1:
                case 4:
                case 7:
                    corporationName += "ww";
                    break;

                case 2:
                case 5:
                case 8:
                    corporationName += "ii";
                    break;

                case 3:
                case 6:
                case 9:
                    corporationName += "ss";
                    break;
            }

            return (DefaultCorporation)GetByName(corporationName);
        }
    }
}