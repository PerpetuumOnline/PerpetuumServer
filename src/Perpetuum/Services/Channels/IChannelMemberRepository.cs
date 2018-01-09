using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Channels
{
    public interface IChannelMemberRepository
    {
        bool IsMember(Channel channel, Character character);
        bool HasMembers(Channel channel);
        void Insert(Channel channel, ChannelMember member);
        void Update(Channel channel,ChannelMember member);
        void Delete(Channel channel,ChannelMember member);

        IEnumerable<KeyValuePair<string, ChannelMember>> GetAllByCharacter(Character character);

        [CanBeNull]
        ChannelMember Get(Channel channel, Character character);
    }
}