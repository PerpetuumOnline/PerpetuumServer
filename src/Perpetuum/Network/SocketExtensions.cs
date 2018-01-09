using System;
using System.Net.Sockets;
using Perpetuum.Log;

namespace Perpetuum.Network
{
    public static class SocketExtensions
    {
        [UsedImplicitly]
        public static void SetKeepAlive(this Socket socket, bool state, TimeSpan time, TimeSpan interval)
        {
            socket.SetKeepAlive(state, (uint)time.TotalMilliseconds, (uint)interval.TotalMilliseconds);
        }
        
        public static void SetKeepAlive(this Socket socket, bool state, uint time, uint interval)
        {
            try
            {
                var data = new byte[12];

                unsafe
                {
                    fixed (byte* p = data)
                    {
                        *(uint*)p = (uint)(state ? 1 : 0);
                        *(uint*)(p + 4) = time;
                        *(uint*)(p + 8) = interval;
                    }
                }

                socket.IOControl(IOControlCode.KeepAliveValues, data, null);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

    }
}