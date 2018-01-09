using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Services.Social
{
    internal class CharacterSocial : ICharacterSocial
    {
        public static readonly ICharacterSocial None = new CharacterSocial(null);
        public Character character { get; private set; }
        private readonly ConcurrentDictionary<Character, FriendInfo> _friends = new ConcurrentDictionary<Character, FriendInfo>();

        private CharacterSocial(Character character)
        {
            this.character = character;
        }

        [NotNull]
        public static ICharacterSocial LoadFromDb(Character character)
        {
            var social = new CharacterSocial(character);
            social.LoadFromDb();
            return social;
        }

        private void LoadFromDb()
        {
            if ( character == Character.None )
                return;

            var records = Db.Query().CommandText("select friendid,socialstate,note,laststateupdate from charactersocial where characterid = @characterId").SetParameter("@characterId",character.Id).Execute();

            foreach (var record in records)
            {
                var friend = Character.Get(record.GetValue<int>(0));
                var socialState = (SocialState)record.GetValue<byte>(1);

                var info = new FriendInfo(friend, socialState)
                {
                    note = record.GetValue<string>("note"), 
                    lastStateUpdate = record.GetValue<DateTime>("laststateupdate")
                };

                _friends[friend] = info;
            }
        }

        public SocialState GetFriendSocialState(Character friend)
        {
            FriendInfo friendInfo;
            return !_friends.TryGetValue(friend, out friendInfo) ? SocialState.Undefined : friendInfo.socialState;
        }

        public ErrorCodes SetFriendSocialState(Character friend, SocialState socialState,string note = null)
        {
            if (character.Id == 0 || character == friend)
                return ErrorCodes.NoError;

            if (socialState == SocialState.Blocked)
            {
                var friendAccessLevel = friend.AccessLevel;

                if (friendAccessLevel.IsAdminOrGm())
                    return ErrorCodes.AdminIsNotBlockable;
            }

            var lastStateUpdate = DateTime.Now;

            FriendInfo friendInfo;
            if (_friends.TryGetValue(friend, out friendInfo))
            {
                if (friendInfo.socialState == socialState)
                    return ErrorCodes.NoError;

                friendInfo.socialState = socialState;

                // update
                const string updateCommandText = "update charactersocial set socialstate = @socialState,laststateupdate = @lastStateUpdate,note = @note  where characterid = @characterId and friendid = @friendId";

                var sqlResult = Db.Query().CommandText(updateCommandText)
                                       .SetParameter("@characterId",character.Id)
                                       .SetParameter("@friendId",friend.Id)
                                       .SetParameter("@socialState",socialState)
                                       .SetParameter("@lastStateUpdate",lastStateUpdate)
                                       .SetParameter("@note",note)
                                       .ExecuteNonQuery();

                if (sqlResult == 0)
                    return ErrorCodes.SQLUpdateError;
            }
            else
            {
                // insert
                const string insertCommandText = "insert into charactersocial (characterid,friendid,socialstate,laststateupdate,note) values (@characterId,@friendId,@socialState,@lastStateUpdate,@note)";
                var sqlResult = Db.Query().CommandText(insertCommandText)
                                       .SetParameter("@characterId",character.Id)
                                       .SetParameter("@friendId",friend.Id)
                                       .SetParameter("@socialState",socialState)
                                       .SetParameter("@lastStateUpdate",lastStateUpdate)
                                       .SetParameter("@note",note)
                                       .ExecuteNonQuery();

                if (sqlResult == 0)
                    return ErrorCodes.SQLInsertError;

                friendInfo = new FriendInfo(friend, socialState);
            }
                
            friendInfo.lastStateUpdate = lastStateUpdate;
            friendInfo.note = note;

            Transaction.Current.OnCommited(() =>
            {
                _friends[friend] = friendInfo;
            });

            return ErrorCodes.NoError;
        }

        public void RemoveFriend(Character friend)
        {
            if (character.Id == 0 || character == friend)
                return;

            // delete
            Db.Query().CommandText("delete from charactersocial where characterid = @characterId and friendid = @friendId").SetParameter("@characterId",character.Id).SetParameter("@friendId",friend.Id).ExecuteNonQuery();

            Transaction.Current.OnCommited(() => _friends.Remove(friend));
        }

        public IEnumerable<Character> GetFriends()
        {
            return _friends.Values.Where(f => f.socialState == SocialState.Friend).Select(f => f.character).ToArray();
        }

        public IDictionary<string, object> ToDictionary()
        {
            return _friends.Values.ToDictionary<FriendInfo, string, object>(f => "f" + f.character.Id, f => f.ToDictionary());
        }
    }
}