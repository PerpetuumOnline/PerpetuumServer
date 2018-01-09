using System.Collections.Generic;

namespace Perpetuum.Accounting.Characters
{
    public interface ICharacterProfileRepository : IReadOnlyRepository<int,CharacterProfile>
    {
        IEnumerable<CharacterProfile> GetAllByAccount(Account account);
    }
}