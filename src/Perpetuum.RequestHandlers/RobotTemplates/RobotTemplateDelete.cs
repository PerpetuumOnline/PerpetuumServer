using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items.Templates;

namespace Perpetuum.RequestHandlers.RobotTemplates
{
    public class RobotTemplateDelete : RobotTemplateRequestHandler
    {
        private readonly IRobotTemplateRepository _robotTemplateRepository;

        public RobotTemplateDelete(IRobotTemplateRepository robotTemplateRepository) : base(robotTemplateRepository)
        {
            _robotTemplateRepository = robotTemplateRepository;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                _robotTemplateRepository.DeleteByID(id);

                SendRobotTemplateListWhenTransactionCompleted(request);
                
                scope.Complete();
            }
        }
    }
}