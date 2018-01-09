using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers
{
    public class TransferData : IRequestHandler
    {
        private readonly ISessionManager _sessionManager;

        public TransferData(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void HandleRequest(IRequest request)
        {
            var targets = request.Data.GetOrDefault<int[]>(k.target).Select(Character.Get);

            var character = request.Session.Character;
            var data = new Dictionary<string, object>(2)
            {
                {k.data, request.Data[k.data]},
                {k.source, character.Id}
            };

            var onliners = targets.Intersect(_sessionManager.SelectedCharacters).ToArray();

            if (onliners.Length > 0)
            {
                Message.Builder.SetCommand(Commands.TransferData).WithData(data)
                    .ToCharacters(onliners)
                    .Send();

                var dictionary = new Dictionary<string, object>
                {
                    { k.target, onliners.Select(o => o.Id).ToArray() }
                };

                Message.Builder.FromRequest(request)
                    .WithData(dictionary)
                    .WrapToResult()
                    .WithEmpty()
                    .Send();
                return;
            }

            Message.Builder.FromRequest(request)
                .WithData(new Dictionary<string, object> { { k.state, k.empty } })
                .WrapToResult()
                .WithEmpty().Send();
        }
    }
}