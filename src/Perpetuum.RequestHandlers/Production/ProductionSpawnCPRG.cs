using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionSpawnCPRG : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var definition = request.Data.GetOrDefault<int>(k.definition);

                var ed = EntityDefault.Get(definition);

                ed.CategoryFlags.IsCategory(CategoryFlags.cf_calibration_programs).ThrowIfFalse(ErrorCodes.DefinitionNotSupported);

                var container = character.GetPublicContainerWithItems();

                var cprg = container.CreateAndAddItem(definition, false, item =>
                {
                    item.Owner = character.Eid;
                    item.Quantity = 1;
                });
                cprg.DynamicProperties.Update(k.nextMaterialEfficiency, 0.5);
                cprg.DynamicProperties.Update(k.timeEfficiency, 0.5);
                cprg.Save();

                Message.Builder.FromRequest(request).WithData(cprg.BaseInfoToDictionary()).Send();
                
                scope.Complete();
            }
        }
    }
}