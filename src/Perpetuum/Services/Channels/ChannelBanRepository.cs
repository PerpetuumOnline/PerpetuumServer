using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Services.Channels
{
    public class ChannelBanRepository : IChannelBanRepository
    {
        public bool IsBanned(Channel channel,Character character)
        {
            return Db.Query().CommandText("select id from channelbans where memberid = @memberid and channelid = @channelid")
                .SetParameter("@memberId", character.Id)
                .SetParameter("@channelId",channel.Id)
                .ExecuteScalar<int>() > 0;
        }

        public void Ban(Channel channel,Character character)
        {
            Db.Query().CommandText("insert into channelbans (memberid,channelid) values (@memberid,@channelid)")
                .SetParameter("@memberid", character.Id)
                .SetParameter("@channelid", channel.Id)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void UnBan(Channel channel,Character character)
        {
            Db.Query().CommandText("delete from channelbans where memberid = @memberid and channelid = @channelid")
                .SetParameter("@memberId", character.Id)
                .SetParameter("@channelId", channel.Id)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLDeleteError);
        }

        public void UnBanAll(Channel channel)
        {
            Db.Query().CommandText("delete from channelbans where channelid = @channelId").SetParameter("@channelid", channel.Id).ExecuteNonQuery();
        }

        public IEnumerable<Character> GetBannedCharacters(Channel channel)
        {
            return Db.Query().CommandText("select memberid from channelbans where channelid = @channelid")
                .SetParameter("@channelid",channel.Id)
                .Execute()
                .Select(r => Character.Get(r.GetValue<int>(0)))
                .ToArray();
        }
    }
}