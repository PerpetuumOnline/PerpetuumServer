using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Sessions
{
    public interface ISessionManager
    {
        ISession Get(SessionID sessionId);
        ISession GetByCharacter(Character character);
        ISession GetByAccount(Account account);
        ISession GetByAccount(int accountId);
        ISession GetByCharacter(int characterid);
        IEnumerable<ISession> Sessions { get;}
        IEnumerable<Character> SelectedCharacters { get; }
        bool Contains(SessionID sessionId);
        bool IsOnline(Character character);

        int MaxSessions { get; set; }

        event SessionEventHandler SessionAdded;

        event SessionEventHandler<Character> CharacterDeselected;
    }
}