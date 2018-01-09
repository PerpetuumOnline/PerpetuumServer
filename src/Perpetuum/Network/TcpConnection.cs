using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Log;
using Perpetuum.Threading;

namespace Perpetuum.Network
{
    public class TcpConnection : Disposable, ITcpConnection
    {
        private const int RECEIVE_BUFFER_SIZE = 8192;
        private const int SEND_BUFFER_SIZE = 8192;
        private const int MAX_PACKET_LENGTH = 1024 * 1024 * 8;

        private Socket _socket;

        private readonly byte[] _buffer = new byte[RECEIVE_BUFFER_SIZE];

        private int _packetLength;
        private int _packetLengthOffset;

        // ebben keszul el majd a bejovo packet
        private MemoryStream _packetStream;

        private long _isDisconnected;

        public TcpConnection(Socket socket)
        {
            _socket = socket;
            _socket.NoDelay = true;
            _socket.ReceiveBufferSize = RECEIVE_BUFFER_SIZE;
            _socket.SendBufferSize = SEND_BUFFER_SIZE;
            _socket.SetKeepAlive(true, 1000 * 60 * 60 * 24, 5000);

            RemoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (_packetStream != null)
            {
                _packetStream.Close();
                _packetStream = null;
            }

            if (_socket == null || !_socket.Connected)
                return;

            _socket.Close(5000);
            _socket = null;
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref _isDisconnected, 1, 0) == 1)
                return;

            Console.Beep(100, 200);

            Task.Run(() =>
            {
                OnDisconnected();
            }).ContinueWith(t => Dispose()).LogExceptions();
        }

        public IPEndPoint RemoteEndPoint { get; private set; }

        public event TcpConnectionEventHandler Disconnected;

        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this);
        }

        public event TcpConnectionEventHandler<byte[]> Received;

        protected virtual void OnReceived(byte[] data)
        {
            Received?.Invoke(this, data);
        }

        private bool IsConnected()
        {
            return Interlocked.Read(ref _isDisconnected) == 0 && _socket != null && _socket.Connected;
        }

        public void Receive()
        {
            if (!IsConnected())
                return;

            try
            {
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                if (ex is SocketException soex)
                {
                    OnHandleSocketException(soex);
                }
                else
                {
                    Logger.Exception(ex);
                }
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!IsConnected())
                return;

            try
            {
                var available = _socket.EndReceive(ar);

                if (available == 0)
                {
                    //HostHandler.WriteLog("socket: 0 bytes available, closing.", LogType.info);
                    Disconnect();
                    return;
                }

                OnProcessReceivedRawData(_buffer, available);

                var index = 0;
                while (index < available)
                {
                    // ha meg nincs packet meret akkor osszekaparjuk...
                    if (_packetStream == null)
                    {
                        // itt rakjuk ossze a meretet byte-bol (data << 0 | data << 8 | data << 16 | data << 24)
                        _packetLength |= _buffer[index++] << _packetLengthOffset;
                        _packetLengthOffset += 8;

                        // megvan a 4 byte? 
                        if (_packetLengthOffset >= 32)
                        {
                            // ha igen akkor legkozelebb megint 0-rol kezdunk
                            _packetLengthOffset = 0;

                            // ha nem keepalive packet jott akkor csinalunk egy memorystream-et
                            if (_packetLength > 0)
                            {
                                if (_packetLength >= MAX_PACKET_LENGTH)
                                {
                                    Disconnect();
                                    return;
                                }

                                _packetStream = new MemoryStream(_packetLength);
                            }

                            _packetLength = 0;
                        }

                        continue;
                    }

                    var length = available - index;

                    // ha nagyobbat akarunk masolni mint a packetmeret akkor clampolunk
                    if (length > (_packetStream.Capacity - _packetStream.Position))
                    {
                        length = (int)(_packetStream.Capacity - _packetStream.Position);
                    }

                    // egyszeru copy
                    _packetStream.Write(_buffer, index, length);
                    index += length;

                    // ha meg nincs meg a packet akkor meg varunk adatot
                    if (_packetStream.Position < _packetStream.Capacity)
                        continue;

                    var packetData = _packetStream.ToArray();
                    // reseteljuk a stream-et mert megint egy packetlength fog jonni
                    _packetStream.Close();
                    _packetStream = null;

                    // itt megvan a packet
                    // ez lenne az event hivas
                    OnReceived(packetData);
                }
            }
            catch (Exception ex)
            {
                var soex = ex as SocketException;
                if (soex != null)
                {
                    OnHandleSocketException(soex);
                }
                else
                {
                    Logger.Exception(ex);
                }
            }
            finally
            {
                Receive();
            }
        }

        protected virtual void OnProcessReceivedRawData(byte[] data, int length) { }

        protected virtual byte[] OnProcessOutputRawData(byte[] data)
        {
            return data;
        }

        protected virtual byte[] OnProcessOutputPacketData(byte[] data)
        {
            return data;
        }

        private unsafe byte[] CreatePacket(byte[] data)
        {
            data = OnProcessOutputRawData(data);

            var packet = new byte[data.Length + 4];

            fixed (byte* pPacket = packet)
            {
                *(int*)pPacket = data.Length;
            }

            if (data.Length > 0)
            {
                Buffer.BlockCopy(data, 0, packet, 4, data.Length);
            }

            packet = OnProcessOutputPacketData(packet);
            return packet;
        }

        private readonly Queue<byte[]> _outBuffer = new Queue<byte[]>();
        private bool _sending;
        private SpinLock _sendLock = new SpinLock();

        public virtual void Send(byte[] data)
        {
            if (!IsConnected())
                return;

            var taken = false;
            try
            {
                _sendLock.Enter(ref taken);
                if (_sending)
                {
                    _outBuffer.Enqueue(data);
                    return;
                }

                _sending = true;
                ThreadPool.UnsafeQueueUserWorkItem(_ => StartSending(data), null);
            }
            finally
            {
                if (taken)
                    _sendLock.Exit(false);
            }
        }

        private void StartSending(byte[] data)
        {
            while (true)
            {
                if (!IsConnected())
                    return;

                try
                {
                    var packet = CreatePacket(data);
                    _socket.Send(packet, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    var soex = ex as SocketException;
                    if (soex != null)
                    {
                        OnHandleSocketException(soex);
                    }
                    else
                    {
                        Logger.Exception(ex);
                    }
                }

                var taken = false;
                try
                {
                    _sendLock.Enter(ref taken);
                    if (_outBuffer.Count == 0)
                    {
                        _sending = false;
                        return;
                    }

                    data = _outBuffer.Dequeue();
                }
                finally
                {
                    if (taken)
                        _sendLock.Exit(false);
                }
            }
        }

        private void OnHandleSocketException(SocketException soex)
        {
            var e = new LogEvent
            {
                LogType = LogType.Error,
                Tag = "Socket",
                Message = $"Socket error ({soex.SocketErrorCode}) {RemoteEndPoint}"
            };

            Logger.Log(e);

            switch (soex.SocketErrorCode)
            {
                case SocketError.TimedOut:
                case SocketError.Disconnecting:
                case SocketError.ConnectionAborted:
                case SocketError.Shutdown:
                case SocketError.NotConnected:
                case SocketError.ConnectionReset:
                case SocketError.Interrupted:
                    {
                        Disconnect();
                        break;
                    }
            }
        }
    }
}