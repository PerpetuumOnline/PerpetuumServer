using System.Net.Sockets;

namespace Perpetuum.Network
{
    public class EncryptedTcpConnection : TcpConnection
    {
        public byte outEncodingByte = 0xCA;
        public byte outIncrement = 0x5B;

        public byte inDecodingByte  = 0xAC;
        public byte inIncrement = 0xB5;

        public EncryptedTcpConnection(Socket socket) : base(socket) {}

        protected override void OnProcessReceivedRawData(byte[] data, int length)
        {
            ProcessData(data, length, ref inDecodingByte, inIncrement);
        }

        protected override byte[] OnProcessOutputPacketData(byte[] data)
        {
            ProcessData(data, data.Length, ref outEncodingByte, outIncrement);
            return data;
        }

        private static void ProcessData(byte[] data,int length,ref byte code,byte increment)
        {
            unchecked
            {
                for (var i = 0; i < length; i++)
                {
                    data[i] ^= code;
                    code += increment;
                }
            }
        }
    }
}