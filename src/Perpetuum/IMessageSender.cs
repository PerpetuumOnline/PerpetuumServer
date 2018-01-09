using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Services.Sessions;

namespace Perpetuum
{
    public interface ICorporationMessageSender
    {
        void SendToAll(IMessage message,Corporation corporation);
        void SendByCorporationRole(IMessage message,long corporationEid,CorporationRole role);
        void SendByCorporationRole(IMessage message,Corporation corporation,CorporationRole role);
    }

    public interface IMessageSender 
    {
        void SendToClient(IMessage message,ISession session);
        void SendToAccount(IMessage message, Account account);
        void SendToCharacter(IMessage message,Character character);
        void SendToCharacters(IMessage message,IEnumerable<Character> characters);
        void SendToAll(IMessage message);
        void SendToOnlineCharacters(IMessage message);
    }
}