using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Services.Channels
{
    public class ChannelRepository : IChannelRepository
    {
        private readonly ChannelLoggerFactory _channelLoggerFactory;

        public ChannelRepository(ChannelLoggerFactory channelLoggerFactory)
        {
            _channelLoggerFactory = channelLoggerFactory;
        }

        public Channel Insert(Channel channel)
        {
            const string cmd = "insert into channels (name,type) values (@name,@type);select id from channels where id = scope_identity()";

            var id = Db.Query().CommandText(cmd)
                .SetParameter("@name",channel.Name)
                .SetParameter("@type", (int)channel.Type)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            return channel.SetId(id);
        }

        public void Delete(Channel channel)
        {
            Db.Query().CommandText("delete channels where id = @channelId")
                .SetParameter("@channelId",channel.Id)
                .ExecuteNonQuery().ThrowIfZero(ErrorCodes.SQLDeleteError);
        }

        public void Update(Channel channel)
        {
            Db.Query().CommandText("update channels set topic = @topic, password = @password, type=@type where id = @channelid")
                .SetParameter("@topic", channel.Topic)
                .SetParameter("@password", channel.Password)
                .SetParameter("@type", channel.Type)
                .SetParameter("@channelid",channel.Id)                
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public IEnumerable<Channel> GetAll()
        {
            return Db.Query().CommandText("select * from channels").Execute().Select(record =>
            {
                var id = record.GetValue<int>("id");
                var type = (ChannelType)record.GetValue<int>("type");
                var name = record.GetValue<string>("name");
                var topic = record.GetValue<string>("topic");
                var password = record.GetValue<string>("password");

                var logger = _channelLoggerFactory(name);
                return new Channel(id, type, name, topic, password,logger);
            }).ToArray();
        }
    }
}