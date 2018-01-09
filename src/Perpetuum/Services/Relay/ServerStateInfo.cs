using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Network;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Services.Relay
{
    public class ServerInfo
    {
        public static readonly ServerInfo None = new ServerInfo();

        public string Name { get; set; }
        public string Description { get; set; }
        public string Contact { get; set; }
        public bool IsOpen { get; set; }
        public bool IsBroadcast { get; set; }
        public int UsersCount { get; set; }

        public Dictionary<string, object> Serialize()
        {
            return new Dictionary<string, object>()
            {
                {k.name,Name},
                {k.description,Description},
                {k.contact,Contact},
                {k.isOpen,IsOpen},
                {k.isBroadcast,IsBroadcast},
                {"usersCount",UsersCount }
            };
        }

        public static ServerInfo Deserialize(IDictionary<string, object> dictionary)
        {
            return new ServerInfo
            {
                Name = dictionary.GetOrDefault<string>(k.name),
                Description = dictionary.GetOrDefault<string>(k.description),
                Contact = dictionary.GetOrDefault<string>(k.contact),
                IsOpen = dictionary.GetOrDefault<int>(k.isOpen) == 1,
                IsBroadcast = dictionary.GetOrDefault<int>(k.isBroadcast) == 1,
            };
        }
    }

    public interface IServerInfoManager
    {
        ServerInfo GetServerInfo();
        void SaveServerInfoToDb(ServerInfo serverInfo);
        void PostCurrentServerInfoToWebService();
    }

    public class ServerInfoManager : IServerInfoManager
    {
        private readonly ISessionManager _sessionManager;

        public ServerInfoManager(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public ServerInfo GetServerInfo()
        {
            var record = Db.Query().CommandText("saServerInfoGet").ExecuteSingleRow();
            if (record == null)
                return null;

            var serverInfo = new ServerInfo
            {
                Name = record.GetValue<string>("name"),
                Description = record.GetValue<string>("description"),
                Contact = record.GetValue<string>("contact"),
                IsOpen = record.GetValue<bool>("isOpen"),
                IsBroadcast = record.GetValue<bool>("isBroadcast"),
                UsersCount = _sessionManager.Sessions.Count()
            };

            return serverInfo;
        }

        public void SaveServerInfoToDb(ServerInfo serverInfo)
        {
            Db.Query().CommandText("saServerInfoSet")
                .SetParameter("name",serverInfo.Name)
                .SetParameter("description",serverInfo.Description)
                .SetParameter("contact",serverInfo.Contact)
                .SetParameter("isopen",serverInfo.IsOpen)
                .SetParameter("isbroadcast",serverInfo.IsBroadcast)
                .ExecuteSingleRow();
        }

        public void PostCurrentServerInfoToWebService()
        {
            var serverInfo = GetServerInfo();
            var data = serverInfo.Serialize();
            var reply = Http.Post("http://www.perpetuum-online.com/Server_list", data);
            Logger.DebugInfo(reply);
        }
    }
}
