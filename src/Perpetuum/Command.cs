using System.Collections.Generic;

namespace Perpetuum
{
    public class Command
    {
        public string Text { get; set; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.admin;
        public readonly List<IArgument> Arguments = new List<IArgument>();

        public Command()
        {
            
        }

        public Command(string text)
        {
            Text = text;
        }

        public void CheckArguments(Dictionary<string, object> data)
        {
            if  (Arguments == null)
                return;

            foreach (var argument in Arguments)
            {
                argument.Check(data);
            }
        }

        public override string ToString()
        {
            return $"{Text} ({AccessLevel})";
        }
    }
}