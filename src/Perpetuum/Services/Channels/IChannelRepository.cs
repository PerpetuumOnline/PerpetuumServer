using System.Collections.Generic;

namespace Perpetuum.Services.Channels
{
    public interface IChannelRepository
    {
        Channel Insert(Channel channel);
        void Delete(Channel channel);
        void Update(Channel channel);
        IEnumerable<Channel> GetAll();
    }
}