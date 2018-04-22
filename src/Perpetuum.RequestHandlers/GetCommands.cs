using Perpetuum.Host.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.RequestHandlers
{
    public class GetCommands : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var commands = typeof(Commands).GetFields(BindingFlags.Static | BindingFlags.Public).Select(info => (Command)info.GetValue(null));

            var x = commands.ToDictionary("c", r =>
            {
                var d = new Dictionary<string, object>
                {
                    [k.command] = (string)r.Text,
                    [k.accessLevel] = (int)r.AccessLevel
                };
                return d;
            });

            Message.Builder.FromRequest(request).WithData(x).Send();
        }
    }
}