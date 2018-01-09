using System.Net;

namespace Perpetuum.Network
{

    public delegate void TcpConnectionEventHandler(ITcpConnection connection);
    public delegate void TcpConnectionEventHandler<in T>(ITcpConnection connection,T arg);
    public delegate void TcpConnectionEventHandler<in T1, in T2>(ITcpConnection connection,T1 arg1,T2 arg2);

    public interface ITcpConnection
    {
        void Receive();                                
        void Send(byte[] data);
        void Disconnect();

        IPEndPoint RemoteEndPoint { get; }

        event TcpConnectionEventHandler Disconnected;
        event TcpConnectionEventHandler<byte[]> Received;
    }
}