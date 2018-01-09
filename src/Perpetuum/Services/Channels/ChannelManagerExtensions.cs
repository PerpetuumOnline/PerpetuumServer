using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;

namespace Perpetuum.Services.Channels
{
    public static class ChannelManagerExtensions
    {
        public static void CreateAndJoinChannel(this IChannelManager channelManager,ChannelType channelType,string name, Character member)
        {
            channelManager.CreateChannel(channelType,name);
            channelManager.JoinChannel(name,member, PresetChannelRoles.ROLE_GOD, null);
        }

        public static void JoinChannel(this IChannelManager channelManager,string name, Character member)
        {
            channelManager.JoinChannel(name, member,ChannelMemberRole.Undefined,null);
        }

        public static void JoinChannel(this IChannelManager channelManager,string name, Character member, CorporationRole corporationRole)
        {
            channelManager.JoinChannel(name, member,GetChannelMemberRoleByCorporationRole(corporationRole),null);
        }

        public static void SetMemberRole(this IChannelManager channelManager,string name, Character member, CorporationRole corporationRole)
        {
            channelManager.SetMemberRole(name,member,GetChannelMemberRoleByCorporationRole(corporationRole));
        }

        public static void SetMemberRole(this IChannelManager channelManager,string name, Character member,ChannelMemberRole newRole)
        {
            channelManager.SetMemberRole(name,Character.None, member,newRole);
        }

        private static ChannelMemberRole GetChannelMemberRoleByCorporationRole(CorporationRole corporationRole)
        {
            return corporationRole.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.HRManager, CorporationRole.PRManager) ?
                ChannelMemberRole.Operator :
                ChannelMemberRole.Undefined;
        }
    }
}