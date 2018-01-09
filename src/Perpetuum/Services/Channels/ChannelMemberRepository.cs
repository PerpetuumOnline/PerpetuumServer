using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Services.Channels
{
    public class ChannelMemberRepository : IChannelMemberRepository
    {
        public bool IsMember(Channel channel, Character character)
        {
            var id = Db.Query().CommandText("select top 1 id from channelmembers where channelid = @channelid and memberid = @id")
                           .SetParameter("@channelid", channel.Id)
                           .SetParameter("@id", character.Id)
                           .ExecuteScalar<int>();

            return id > 0;
        }

        public bool HasMembers(Channel channel)
        {
            var id = Db.Query().CommandText("select top 1 id from channelmembers where channelid = @channelid").SetParameter("@channelid", channel.Id).ExecuteScalar<int>();
            return id > 0;
        }

        public void Insert(Channel channel, ChannelMember member)
        {
            Db.Query().CommandText("insert into channelmembers (memberid,role,channelid) values (@memberid,@role,@channelid)")
                .SetParameter("@role", member.role)
                .SetParameter("@memberId", member.character.Id)
                .SetParameter("@channelId", channel.Id)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Update(Channel channel,ChannelMember member)
        {
            const string cmd = "update channelmembers set role = @newRole where memberId = @memberId and channelId = @channelId";

            Db.Query().CommandText(cmd)
                .SetParameter("@newRole",member.role)
                .SetParameter("@memberId",member.character.Id)
                .SetParameter("@channelId",channel.Id)
                .ExecuteNonQuery().ThrowIfZero(ErrorCodes.SQLUpdateError);
        }

        public void Delete(Channel channel,ChannelMember member)
        {
            Db.Query().CommandText("delete channelmembers where memberid = @memberId and channelid = @channelId")
                .SetParameter("@memberId",member.character.Id)
                .SetParameter("@channelId", channel.Id)
                .ExecuteNonQuery().ThrowIfZero(ErrorCodes.SQLDeleteError);
        }

        public IEnumerable<KeyValuePair<string, ChannelMember>> GetAllByCharacter(Character character)
        {
            var x = Db.Query().CommandText("select channels.name,channelmembers.role from channelmembers inner join channels on channelmembers.channelid = channels.id where memberid = @memberid")
                .SetParameter("@memberid", character.Id)
                .Execute()
                .Select(r =>
                {
                    return new
                    {
                        name = r.GetValue<string>(0),
                        role = (ChannelMemberRole)r.GetValue<int>(1)
                    };
                }).ToDictionary(a => a.name, a => new ChannelMember(character,a.role));

            return x;
        }

        public IEnumerable<string> GetAllChannelNamesByCharacter(Character character)
        {
            return Db.Query().CommandText("select channels.name from channelmembers inner join channels on channelmembers.channelid = channels.id where memberid = @memberid")
                .SetParameter("@memberid", character.Id)
                .Execute()
                .Select(r => r.GetValue<string>(0));
        }

        public ChannelMember Get(Channel channel, Character character)
        {
            var record = Db.Query().CommandText("select role from channelmembers where memberid = @memberid and channelid = @channelid")
                .SetParameter("@memberid", character.Id)
                .SetParameter("@channelid", channel.Id)
                .ExecuteSingleRow();

            if (record == null)
                return null;

            var role = (ChannelMemberRole)record.GetValue<int>(0);
            return new ChannelMember(character,role);
        }
    }
}