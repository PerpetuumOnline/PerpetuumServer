using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public static class ProductionFacilityEx
    {
        public static void OnPBSStartFacility(this ProductionFacility facility)
        {
            if (facility.IsOpen) 
                return;
            
            Logger.Info("    production facility received a START. " + facility );
            SetPauseInFacility(facility, false);
        }

        public static void OnPBSStopFacility(this ProductionFacility facility)
        {
            if (!facility.IsOpen) 
                return;

            Logger.Info("    production facility received a STOP. " + facility);
            SetPauseInFacility(facility, true);
        }

        private static void SetPauseInFacility(ProductionFacility facility, bool isPaused)
        {
            var productionEvent = isPaused ? ProductionEvent.GotPaused : ProductionEvent.GotResumed;

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var productions = facility.ProductionProcessor.RunningProductions.Where(p => p.facilityEID == facility.Eid).ToArray();

                    foreach (var productionInProgressGroup in productions.GroupBy(p=>p.character))
                    {
                        var currentCharacter = productionInProgressGroup.Key;

                        foreach (var productionInProgress in productionInProgressGroup)
                        {
                            productionInProgress.SetPause(isPaused);    
                        }

                        NotifyClient(currentCharacter, productionInProgressGroup.ToList(), productionEvent);
                    }

                    Logger.Info(productions.Length + " productions set to isPaused:" + isPaused + " in facility: " + facility);
                    scope.Complete();
                }
                catch(Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        private static void NotifyClient(Character character, IEnumerable<ProductionInProgress> affectedProductions, ProductionEvent productionEvent )
        {
            Transaction.Current.OnCommited(() =>
            {
                var info = new Dictionary<string, object>
                {
                    {k.eventType, (int) productionEvent},
                };

                var counter = 0;
                var productions = new Dictionary<string, object>();
                foreach (var productionInProgress in affectedProductions)
                {
                    productions.Add("p"+counter++, productionInProgress.ToDictionary());
                }

                info.Add(k.production, productions);

                Message.Builder.SetCommand(Commands.ProductionUpdate)
                    .WithData(info)
                    .ToCharacter(character)
                    .Send();
            });
        }
    }

}
