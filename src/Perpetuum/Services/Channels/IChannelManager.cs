using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;

namespace Perpetuum.Services.Channels
{
    public interface IChannelManager
    {
        IEnumerable<Channel> Channels { get; }

        [CanBeNull]
        Channel GetChannelByName(string name);

        void CreateChannel(ChannelType type, string name);
        void DeleteChannel(string channelName);
        void JoinChannel(string channelName, Character member, ChannelMemberRole role, string password);

        void LeaveAllChannels(Character character);
        void LeaveChannel(string channelName, Character character);
        void SetMemberRole(string channelName, Character issuer, Character character, ChannelMemberRole role);
        void SetPassword(string channelName, Character issuer, string password);
        void SetTopic(string channelName, Character issuer, string topic);
        void Talk(string channelName, Character sender, string message, IRequest request);
        void Announcement(string channelName, Character sender, string message);
        void KickOrBan(string channelName, Character issuer, Character character, string message, bool ban);
        void UnBan(string channelName, Character issuer, Character character);

        IEnumerable<Character> GetBannedCharacters(string channelName, Character issuer);

        IEnumerable<Channel> GetChannelsByMember(Character member);
        IEnumerable<Channel> GetPublicChannels();
        IEnumerable<Channel> GetAllChannels();
    }
}