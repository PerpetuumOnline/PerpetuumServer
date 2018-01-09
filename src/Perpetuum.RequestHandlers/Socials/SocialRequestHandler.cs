using Perpetuum.Host.Requests;
using Perpetuum.Services.Social;

namespace Perpetuum.RequestHandlers.Socials
{
    public abstract class SocialRequestHandler : IRequestHandler
    {
        public abstract void HandleRequest(IRequest request);

        protected static MessageBuilder CreateMessageToClient(Command command, ICharacterSocial social)
        {
            return Message.Builder.SetCommand(command)
                .SetData(k.friends, social.ToDictionary())
                .ToCharacter(social.character);
        }
    }
}