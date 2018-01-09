using System;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.MarketEngine
{
    public class MarketRobotPriceWriter : Process, IMarketRobotPriceWriter
    {
        private readonly IEntityDefaultReader _entityDefaultReader;
        private readonly IRobotTemplateRelations _robotTemplateRelations;

        public MarketRobotPriceWriter(IEntityDefaultReader entityDefaultReader,IRobotTemplateRelations robotTemplateRelations)
        {
            _entityDefaultReader = entityDefaultReader;
            _robotTemplateRelations = robotTemplateRelations;
        }

        public override void Update(TimeSpan time)
        {
            WriteRobotPrices();
        }

        public void WriteRobotPrices()
        {
            foreach (var ed in _entityDefaultReader.GetAll().GetByCategoryFlags(CategoryFlags.cf_robots))
            {
                var robotTemplate = _robotTemplateRelations.GetRelatedTemplate(ed);
                if (robotTemplate == null)
                    continue;

                var robot = robotTemplate.Build();

                var avg = PriceCalculator.GetAveragePrice(robot);
                if (avg <= 0)
                    continue;

                var res = Db.Query().CommandText("insert marketaveragesbycomponent (definition,price) values (@definition,@price)")
                    .SetParameter("@definition", ed.Definition)
                    .SetParameter("@price", avg)
                    .ExecuteNonQuery();

                if (res != 1)
                {
                    Logger.Error("error in WriteRobotPrices");
                    return;
                }
            }
        }
    }
}