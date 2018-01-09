using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Groups.Gangs
{
    /// <summary>
	/// This class represents a group of player that formed a gang (squad ingame)
	/// </summary>
	public class Gang : IEquatable<Gang>
	{
	    private readonly ISessionManager _sessionManager;
	    public Guid Id { get; set; }
		public string Name { get; set; }
		public Character Leader { get; set; }

		private readonly ConcurrentDictionary<Character, GangRole> _members = new ConcurrentDictionary<Character, GangRole>();

	    public IEnumerable<Character> GetMembers()
		{
			return _members.Select(kvp => kvp.Key);
		}

        public delegate Gang Factory();

	    public Gang(ISessionManager sessionManager)
	    {
	        _sessionManager = sessionManager;
	    }

        public void SetMember(Character member, GangRole role = GangRole.Undefined)
        {
            _members[member] = role;
        }

        public void RemoveMember(Character member)
        {
            _members.Remove(member);
        }

        public bool IsMember(Character member)
        {
            return _members.ContainsKey(member);
        }

        public Dictionary<string, object> GetGangData()
		{
			return new Dictionary<string, object>
			{
						   {k.gangGuid,Id.ToString()},
						   {k.leaderId,Leader.Id}
					   };
		}

		public bool CanKick(Character member)
		{
			if (member == Leader)
				return true;

			return HasRole(member, GangRole.Assistant);
		}

		public bool CanInvite(Character member)
		{
			return HasRole(member, GangRole.Assistant);
		}

		public bool CanSetRole(Character member)
		{
			return HasRole(member, GangRole.Assistant);
		}

	    public bool HasRole(Character member, GangRole role)
		{
			if (member == Leader)
				return true;

            if (!_members.TryGetValue(member, out GangRole currentRole))
                return false;

            return currentRole.HasFlag(role);
		}


		public Dictionary<string, object> ToDictionary()
		{
			var result = new Dictionary<string, object>
			{
								 {k.gangGuid, Id.ToString()}, 
								 {k.leaderId, Leader.Id}, 
								 {k.name, Name}, 
							 };

		    var members = _members.ToDictionary("m", m => new Dictionary<string, object>
		    {
		        {k.memberID, m.Key.Id},
		        {k.role, (int) m.Value}
		    });

			result.Add(k.members,members);
			return result;
		}

	    public string ChannelName => $"squad_{Id}";

		bool IEquatable<Gang>.Equals(Gang other)
		{
			if ( other == null )
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return Id.Equals(other.Id);
		}

		public override string ToString()
		{
			return Id + " , members = " + GetMembers().ArrayToString();
		}

		public static bool CompareGang(Character character, Character member)
		{
			const string cmd = @"select g1.gangid from gangmembers g1 
							inner join gangmembers g2 on g1.gangid = g2.gangid 
							where g1.memberid = @memberId1 and g2.memberid = @memberId2 and g1.gangid = g2.gangid";

			var g = Db.Query().CommandText(cmd).SetParameter("@memberId1", character.Id).SetParameter("@memberId2", member.Id).ExecuteScalar<Guid>();
			return !g.Equals(Guid.Empty);
		}

	    public IEnumerable<Character> GetOnlineMembers()
	    {
	        return GetMembers().Where(m => _sessionManager.IsOnline(m));
	    }
	}
}
