using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.Sparks;
using Perpetuum.Zones.Scanning.Results;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterCleaner
    {
        private readonly MarketHelper _marketHelper;
        private readonly SparkHelper _sparkHelper;
        private readonly ProductionManager _productionManager;
        private readonly MissionProcessor _missionProcessor;

        public CharacterCleaner(MarketHelper marketHelper,SparkHelper sparkHelper,ProductionManager productionManager,MissionProcessor missionProcessor)
        {
            _marketHelper = marketHelper;
            _sparkHelper = sparkHelper;
            _productionManager = productionManager;
            _missionProcessor = missionProcessor;
        }

        public void CleanUp(Character character)
        {
            //reset extensions
            character.ResetAllExtensions();

            //reset credit
            character.Credit = 0;

            //reset sparks
            _sparkHelper.ResetSparks(character);

            //remove scanresults
            var repo = new MineralScanResultRepository(character);
            repo.DeleteAll();

            //remove insurance
            InsuranceHelper.RemoveAll(character);

            //remove market orders
            _marketHelper.RemoveAll(character);

            Db.Query().CommandText("delete charactertransactions where characterid=@characterID").SetParameter("@characterID", character.Id).ExecuteNonQuery();

            Db.Query().CommandText("delete productionlog where characterid=@characterID").SetParameter("@characterID", character.Id).ExecuteNonQuery();

            Db.Query().CommandText("delete techtreeunlockednodes where owner=@eid").SetParameter("@eid", character.Eid).ExecuteNonQuery();

            Db.Query().CommandText("delete techtreelog where character=@characterID").SetParameter("@characterID", character.Id).ExecuteNonQuery();

            Db.Query().CommandText("delete techtreepoints where owner=@eid").SetParameter("@eid", character.Eid).ExecuteNonQuery();

            character.HomeBaseEid = null;

            //delete all items
            Db.Query().CommandText("delete entities where owner=@rootEid")
                .SetParameter("@rootEid",character.Eid)
                .ExecuteNonQuery();

            Transaction.Current.OnCommited(() =>
            {
                //stop productions
                ProductionAbort(character);
                //stop all missions
                MissionForceAbort(character);
            });

            //do/finish character wizard
        }

        private void ProductionAbort(Character character)
        {
            _productionManager.ProductionProcessor.AbortProductionsForOneCharacter(character);
        }

        private void MissionForceAbort(Character character)
        {
            if (_missionProcessor.MissionAdministrator.GetMissionInProgressCollector(character,out MissionInProgressCollector collector))
            {
                collector.Reset();
            }

            Logger.Info("all missions aborted for " + this);
        }
    }
}