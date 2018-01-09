using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.Sparks;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkSetDefault : IRequestHandler
    {
        private readonly SparkHelper _sparkHelper;

        public SparkSetDefault(SparkHelper sparkHelper)
        {
            _sparkHelper = sparkHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                const string queryStr = "SELECT characterid,sparkID FROM characters WHERE active=1";

                var dataPairs = Db.Query().CommandText(queryStr).Execute().Select(r => new KeyValuePair<int, int>(r.GetValue<int>(0), DataRecordExtensions.GetValue<int>(r, 1))).ToArray();

                var counter = 0;

                foreach (var keyValuePair in dataPairs)
                {
                    if (counter++ % 5 == 0)
                    {
                        Logger.Info(counter / (double)dataPairs.Length * 100.0 + " progress.");
                    }

                    var character = Character.Get(keyValuePair.Key);
                    var cwSparkId = keyValuePair.Value;

                    var activeSparkId = _sparkHelper.ConvertCharacterWizardSparkIdToSpark(cwSparkId);
                    _sparkHelper.ActivateSpark(character, activeSparkId);
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }


}
