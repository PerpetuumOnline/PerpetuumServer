using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items.Templates;

namespace Perpetuum.RequestHandlers.RobotTemplates
{
    public class RobotTemplateUpdate : RobotTemplateRequestHandler
    {
        private readonly IRobotTemplateRepository _robotTemplateRepository;

        public RobotTemplateUpdate(IRobotTemplateRepository robotTemplateRepository) : base(robotTemplateRepository)
        {
            _robotTemplateRepository = robotTemplateRepository;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var template = _robotTemplateRepository.Get(id);

                var templateName = request.Data.GetOrDefault<string>(k.name);
                if (!string.IsNullOrEmpty(templateName))
                    template.Name = templateName;

                var description = request.Data.GetOrDefault<Dictionary<string, object>>(k.description);
                var robotTemplate = RobotTemplate.CreateFromDictionary(templateName, description);
                if (robotTemplate == null)
                    throw new PerpetuumException(ErrorCodes.TemplateError);

                template.EntityDefault = robotTemplate.EntityDefault;
                template.Head = robotTemplate.Head;
                template.Chassis = robotTemplate.Chassis;
                template.Leg = robotTemplate.Leg;
                template.Inventory = robotTemplate.Inventory;
                if (!template.Validate())
                    throw new PerpetuumException(ErrorCodes.TemplateError);

                _robotTemplateRepository.Update(template);
                SendRobotTemplateListWhenTransactionCompleted(request);
                
                scope.Complete();
            }
        }
    }
}