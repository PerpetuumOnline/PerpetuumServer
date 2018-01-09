namespace Perpetuum.Services.Social
{
    public enum SocialState : byte
    {
        Undefined = 0,
        Friend,
        Blocked,
        FriendRequest,
        PendingFriendRequest,
    }
}