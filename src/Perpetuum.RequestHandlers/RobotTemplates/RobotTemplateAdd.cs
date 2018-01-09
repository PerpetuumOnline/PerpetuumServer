using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items.Templates;

namespace Perpetuum.RequestHandlers.RobotTemplates
{
    public class RobotTemplateAdd : RobotTemplateRequestHandler
    {
        private readonly IRobotTemplateRepository _robotTemplateRepository;

        public RobotTemplateAdd(IRobotTemplateRepository robotTemplateRepository) : base(robotTemplateRepository)
        {
            _robotTemplateRepository = robotTemplateRepository;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var templateName = request.Data.GetOrDefault<string>(k.name);
                var description = request.Data.GetOrDefault<Dictionary<string, object>>(k.description);

                if (string.IsNullOrEmpty(templateName))
                    templateName = "template_" + FastRandom.NextString(7);

                var template = RobotTemplate.CreateFromDictionary(templateName, description).ThrowIfNull(ErrorCodes.TemplateError);
                template.Validate().ThrowIfFalse(ErrorCodes.TemplateError);
                _robotTemplateRepository.Insert(template);

                SendRobotTemplateListWhenTransactionCompleted(request);
                
                scope.Complete();
            }
        }
    }
}