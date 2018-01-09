using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items.Templates;

namespace Perpetuum.RequestHandlers.RobotTemplates
{
    public abstract class RobotTemplateRequestHandler : IRequestHandler
    {
        private readonly IRobotTemplateRepository _robotTemplateRepository;

        public RobotTemplateRequestHandler(IRobotTemplateRepository robotTemplateRepository)
        {
            _robotTemplateRepository = robotTemplateRepository;
        }

        public abstract void HandleRequest(IRequest request);

        protected void SendRobotTemplateListWhenTransactionCompleted(IRequest request)
        {
            Transaction.Current.OnCompleted(completed =>
            {
                SendRobotTemplateList(request);
            });
        }

        protected void SendRobotTemplateList(IRequest request)
        {
            var templates = _robotTemplateRepository.GetAll()
                    .OrderBy(t => t.Name).ToDictionary("t", t =>
                {
                    var dictionary = new Dictionary<string, object>();
                    dictionary[k.ID] = t.ID;
                    dictionary[k.name] = t.Name;
                    dictionary[k.robotInfo] = t.ToDictionary();
                    return dictionary;
                });

            Message.Builder.SetCommand(Commands.RobotTemplateList)
                .WithData(templates)
                .WithEmpty()
                .ToClient(request.Session)
                .Send();
        }
    }
}