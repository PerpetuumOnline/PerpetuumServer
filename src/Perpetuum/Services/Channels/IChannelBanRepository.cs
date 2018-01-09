using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Channels
{
    public interface IChannelBanRepository
    {
        bool IsBanned(Channel channel,Character character);
        void Ban(Channel channel,Character character);
        void UnBan(Channel channel,Character character);
        void UnBanAll(Channel channel);
        IEnumerable<Character> GetBannedCharacters(Channel channel);
    }
}