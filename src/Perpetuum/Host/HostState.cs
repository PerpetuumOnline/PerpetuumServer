namespace Perpetuum.Host
{
    /// <summary>
    /// Controls the host's state. Used when the server is being maintained.
    /// </summary>
    public enum HostState
    {
        Off = 0,
        Online,
        Init,
        Starting,
        Stopping
    }
}