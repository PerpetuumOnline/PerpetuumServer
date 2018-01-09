namespace Perpetuum.AdminTool
{
    public enum LoginState
    {
        Unknown,
        AlreadyLoggedIn,
        Success,
        NoSuchUser,
        NoAuthYet,
        Disconnected,
        UnableToConnect,
    }

    public enum AccountEditorMode
    {
        unknown,
        edit,
        create,
    }

    public enum LocalServerState
    {
        unknown,
        starting,
        listening,
        shutdownok,
        exitwitherror,
        upnperror
    }

}
