using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSetMoodMessage : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var moodMessage = request.Data.GetOrDefault<string>(k.moodMessage);
                var character = request.Session.Character;
                character.MoodMessage = moodMessage;

                Transaction.Current.OnCommited(() =>
                {
                    var targets = new List<Character> { character };
                    targets.AddRange(character.GetSocial().GetFriends());
                    var data = new Dictionary<string, object> { { k.characterID, character.Id }, { k.moodMessage, moodMessage } };
                    Message.Builder.SetCommand(Commands.UpdateMoodMessage).WithData(data).ToCharacters(targets).Send();
                });
                
                scope.Complete();
            }
        }
    }
}