using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Channels
{
    public class ChannelMember
    {
        public readonly Character character;
        public readonly ChannelMemberRole role;

        public ChannelMember(Character character, ChannelMemberRole role)
        {
            this.character = character;
            this.role = role;
        }

        public ChannelMember WithRole(ChannelMemberRole newRole)
        {
            if (newRole == role)
                return this;

            return new ChannelMember(character,newRole);
        }

        public bool CanTalk
        {
            get { return !character.GlobalMuted; }
        }

        public bool HasRole(ChannelMemberRole r)
        {
            return role.HasFlag(r);
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                               {k.memberID, character.Id},
                               {k.role, (int) role}
                       };
        }

        public override string ToString()
        {
            return $"Member: {character}, Role: {role}";
        }
    }
}