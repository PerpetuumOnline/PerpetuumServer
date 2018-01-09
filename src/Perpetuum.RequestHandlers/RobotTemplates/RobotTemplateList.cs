using Perpetuum.Host.Requests;
using Perpetuum.Items.Templates;

namespace Perpetuum.RequestHandlers.RobotTemplates
{
    public class RobotTemplateList : RobotTemplateRequestHandler
    {
        public RobotTemplateList(IRobotTemplateRepository robotTemplateRepository) : base(robotTemplateRepository)
        {
        }

        public override void HandleRequest(IRequest request)
        {
            SendRobotTemplateList(request);
        }
    }
}