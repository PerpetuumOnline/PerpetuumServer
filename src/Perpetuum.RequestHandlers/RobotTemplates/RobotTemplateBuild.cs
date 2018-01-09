using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Items.Templates;

namespace Perpetuum.RequestHandlers.RobotTemplates
{
    public class RobotTemplateBuild : RobotTemplateRequestHandler
    {
        private readonly IRobotTemplateRepository _robotTemplateRepository;

        public RobotTemplateBuild(IRobotTemplateRepository robotTemplateRepository) : base(robotTemplateRepository)
        {
            _robotTemplateRepository = robotTemplateRepository;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var templateId = request.Data.GetOrDefault<int>(k.ID);

                var robotTemplate = _robotTemplateRepository.Get(templateId);
                var robot = robotTemplate.Build().ThrowIfNull(ErrorCodes.TemplateError);
                robot.Initialize(character);
                robot.Owner = character.Eid;
                robot.Name = robotTemplate.Name;
                robot.Repair();

                var publicContainer = character.GetPublicContainerWithItems();
                publicContainer.AddItem(robot, false);
                publicContainer.Save();

                var result = publicContainer.ToDictionary();
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}