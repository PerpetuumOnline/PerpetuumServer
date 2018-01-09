using System;
using System.Net.Sockets;
using Perpetuum.Log;

namespace Perpetuum.Network
{
    public static class TcpListenerExtensions
    {
        public static void Start(this TcpListener listener, Action<Socket> onConnectionAccepted)
        {
            listener.Start();
            var helper = new ListenerHelper(listener,onConnectionAccepted);
            listener.BeginAcceptSocket(AcceptSocketCallback, helper);
        }

        private static void AcceptSocketCallback(IAsyncResult ar)
        {
            try
            {
                if (ar == null)
                    return;

                var helper = (ListenerHelper) ar.AsyncState;
                var socket = helper.listener.EndAcceptSocket(ar);
                helper.listener.BeginAcceptSocket(AcceptSocketCallback, helper);
                Console.Beep(200, 200);
                helper.onConnectionAccepted(socket);
            }
            catch (ObjectDisposedException)
            {

            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        private class ListenerHelper
        {
            public readonly TcpListener listener;
            public readonly Action<Socket> onConnectionAccepted;

            public ListenerHelper(TcpListener listener,Action<Socket> onConnectionAccepted)
            {
                this.listener = listener;
                this.onConnectionAccepted = onConnectionAccepted;
            }
        }
    }
}