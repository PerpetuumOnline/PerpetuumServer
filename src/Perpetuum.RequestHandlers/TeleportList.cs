using Perpetuum.Host.Requests;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.RequestHandlers
{
    public class TeleportList : IRequestHandler
    {
        private readonly ITeleportDescriptionRepository _descriptionRepository;

        public TeleportList(ITeleportDescriptionRepository descriptionRepository)
        {
            _descriptionRepository = descriptionRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var infos = _descriptionRepository.GetAll().ToDictionary();
            Message.Builder.FromRequest(request).WithData(infos).Send();
        }
    }
}