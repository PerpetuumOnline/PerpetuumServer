using System.Collections.Generic;

namespace Perpetuum.Services.ExtensionService
{
    public static class ExtensionHelper
    {
        public static MessageBuilder CreateExtensionPointsIncreasedMessage(int points)
        {
            var data = new Dictionary<string, object>
            {
                {k.points, points}
            };

            return Message.Builder.SetCommand(Commands.ExtensionPointsIncreased).WithData(data);
        }
    }
}