namespace Perpetuum.Services.Channels
{
    public static class PresetChannelRoles
    {
        public const ChannelMemberRole ROLE_CAN_MODIFY_MEMBER_ROLE = ChannelMemberRole.Operator;
        public const ChannelMemberRole ROLE_CAN_CHANGE_PASSWORD = ChannelMemberRole.Operator;
        public const ChannelMemberRole ROLE_CAN_CHANGE_TOPIC = ChannelMemberRole.Operator;
        public const ChannelMemberRole ROLE_CAN_KICK_MEMBER = ChannelMemberRole.Operator;
        public const ChannelMemberRole ROLE_CAN_BAN_MEMBER = ChannelMemberRole.Operator;
        public const ChannelMemberRole ROLE_CAN_REMOVE_BAN = ChannelMemberRole.Operator | ROLE_CAN_BAN_MEMBER;
        public const ChannelMemberRole ROLE_CAN_LIST_BANNED_MEMBERS = ChannelMemberRole.Operator;

        public const ChannelMemberRole ROLE_GOD = ROLE_CAN_CHANGE_PASSWORD | 
                                                  ROLE_CAN_CHANGE_TOPIC | 
                                                  ROLE_CAN_KICK_MEMBER | 
                                                  ROLE_CAN_MODIFY_MEMBER_ROLE | ROLE_CAN_BAN_MEMBER | ROLE_CAN_REMOVE_BAN;
    }
}