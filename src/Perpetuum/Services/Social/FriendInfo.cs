using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Social
{
    internal class FriendInfo
    {
        public readonly Character character;
        public SocialState socialState;
        public string note;
        public DateTime lastStateUpdate;

        public FriendInfo(Character character, SocialState socialState)
        {
            this.character = character;
            this.socialState = socialState;
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {"characterId", character.Id}, 
                {"socialState", (int) socialState}, 
                {"note", note}, 
                {"lastStateUpdate", lastStateUpdate}
            };
        }

        public override string ToString()
        {
            return $"Character: {character}, SocialState: {socialState}, Note: {note}, LastStateUpdate {lastStateUpdate}";
        }
    }
}