using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Perpetuum.GenXY;

namespace Perpetuum
{
    public class Message : IMessage
    {
        public static MessageBuilder.Factory MessageBuilderFactory { private get; set; }

        public static MessageBuilder Builder => MessageBuilderFactory();

        public Command Command { get; }
        IDictionary<string, object> IMessage.Data => Data;
        public ImmutableDictionary <string,object> Data { get; set; }

        public string Sender { get; set; } = string.Empty;
        //public IReadOnlyDictionary<string, object> Data { get; }

        public Message(Command command) : this(command,ImmutableDictionary<string,object>.Empty)
        {
        }

        public Message(Command command,IReadOnlyDictionary<string,object> data)
        {
            this.Command = command;
            this.Data = data?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Command?.Text);
            sb.Append(':');
            sb.Append(Sender);
            sb.Append(':');
            sb.Append(GenxyConverter.Serialize(Data));
            return sb.ToString();
        }

        private byte[] _cachedBytes;

        public byte[] ToBytes()
        {
            return _cachedBytes ?? (_cachedBytes = Encoding.UTF8.GetBytes(ToString()));
        }

        public static Message Parse(string s)
        {
            var x = s.Split(':');
            var command = Commands.GetCommandByText(x[0]) ?? new Command(x[0]);
            var sender = x[1];
            var data = GenxyConverter.Deserialize(x[2]);
            return new Message(command,data) { Sender = sender};
        }
    }
}