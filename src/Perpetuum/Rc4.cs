namespace Perpetuum
{
    public class Rc4
    {
        public readonly byte[] streamKey;
        private readonly byte[] _inSBox = new byte[256];
        private readonly byte[] _outSbox = new byte[256];
        private readonly object _outSboxLock = new object();

        public Rc4(byte[] streamKey)
        {
            this.streamKey = streamKey;
            Rc4InitializeByte(streamKey, _inSBox);
            Rc4InitializeByte(streamKey, _outSbox);
        }

        private static void Rc4InitializeByte(byte[] keyArray, byte[] sbox)
        {
            //Initializes the sbox and the key array                               
            var keyBuffer = new byte[256];
            var intLength = keyArray.Length;

            //repeats the key through the key array
            //fills the sbox with numbers 0-255
            for (var a = 0; a <= 255; a++)
            {
                keyBuffer[a] = keyArray[a % intLength];
                sbox[a] = (byte)a;
            }

            var b = 0;

            for (var a = 0; a <= 255; a++)
            {
                //(B + number + key) mod 256 => the result is a number smaller than 256
                b = (b + sbox[a] + keyBuffer[a]) & 255;
                //put the number to the tempSwap
                var tempSwap = sbox[a];
                //put the Bth number to the Ath place
                sbox[a] = sbox[b];
                //put the Ath number to the Bth place
                sbox[b] = tempSwap;
            }
        }

        private static void Crypt(byte[] sbox,byte[] data,int index,int length)
        {
            var i = 0;
            var j = 0;

            for (var a = 0; a < length; a++)
            {
                //cycles from 0-255 then again 0...255 etc depending on the length of the plaintext
                i = (i + 1) & 0xff;

                //we use the cycling i here to pick values from the sbox
                j = (j + sbox[i]) & 0xff;

                //magic swapping again in the sbox...
                var temp = sbox[i];
                sbox[i] = sbox[j];
                sbox[j] = temp;

                //and this is the number we xor the text with
                data[index + a] ^= sbox[(sbox[i] + sbox[j]) & 0xff];
            }
        }

        private static void Crypt(byte[] sbox, ref byte[] data)
        {
            var i = 0;
            var j = 0;

            for (var a = 0; a < data.Length; a++)
            {
                //cycles from 0-255 then again 0...255 etc depending on the length of the plaintext
                i = (i + 1) & 0xff;

                //we use the cycling i here to pick values from the sbox
                j = (j + sbox[i]) & 0xff;

                //magic swapping again in the sbox...
                var temp = sbox[i];
                sbox[i] = sbox[j];
                sbox[j] = temp;

                //and this is the number we xor the text with
                data[a] ^= sbox[(sbox[i] + sbox[j]) & 0xff];
            }
        }

        public void Encrypt(byte[] data)
        {
            Encrypt(data,0,data.Length);
        }

        public void Encrypt(byte[] data,int index,int length)
        {
            lock (_outSboxLock)
            {
                Crypt(_outSbox,data,index,length);
            }
        }

        public void Decrypt(ref byte[] data)
        {
            Crypt(_inSBox,ref data);
        }

        public void Decrypt(byte[] data,int index,int length)
        {
            Crypt(_inSBox,data,index,length);
        }
    }

}