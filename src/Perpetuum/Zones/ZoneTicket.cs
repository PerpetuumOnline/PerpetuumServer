using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Zones
{
    public struct ZoneTicket
    {
        private static readonly TimeSpan _ticketLifetime = TimeSpan.FromHours(1);

        private static readonly MD5 _md5 = MD5.Create();
        private static readonly int _md5HashSize = _md5.HashSize / 8;
        private static readonly Rijndael _rijndael = Rijndael.Create();

        private readonly int _characterId;
        private readonly DateTime _created;

        public int CharacterId
        {
            get { return _characterId; }
        }

        static ZoneTicket()
        {
            _rijndael.GenerateKey();
            _rijndael.GenerateIV();
        }

        private ZoneTicket(int characterId) : this()
        {
            _characterId = characterId;
            _created = DateTime.Now;
        }

        public bool IsExpired
        {
            get { return DateTime.Now.Subtract(_created) > _ticketLifetime; }
        }

        private static byte[] Encrypt(ZoneTicket ticket)
        {
            using (var ms = new MemoryStream())
            {
                var encryptor = _rijndael.CreateEncryptor(_rijndael.Key, _rijndael.IV);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    var e = ticket.ToByteArray();
                    var h = _md5.ComputeHash(e);
                    cs.Write(h, 0, h.Length);
                    cs.Write(e, 0, e.Length);
                }

                return ms.ToArray();
            }
        }

        public static bool TryDecrypt(byte[] data,out ZoneTicket ticket)
        {
            ticket = default(ZoneTicket);

            using (var ms = new MemoryStream())
            {
                var decryptor = _rijndael.CreateDecryptor(_rijndael.Key, _rijndael.IV);

                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }

                var decryptedData = ms.ToArray();
                var ticketData = decryptedData.Skip(_md5HashSize).ToArray();

                var size = Marshal.SizeOf(ticket);

                if (ticketData.Length != size)
                    return false;

                if ( !decryptedData.Take(_md5HashSize).SequenceEqual(_md5.ComputeHash(ticketData)))
                    return false;

                ticket = ticketData.ToStruct<ZoneTicket>();
                return true;
            }
        }

        public static byte[] CreateAndEncryptFor(Character character)
        {
            return Encrypt(new ZoneTicket(character.Id));
        }

        public static Character GetCharacterFromEncryptedTicket(byte[] encrypted)
        {
            ZoneTicket ticket;
            TryDecrypt(encrypted, out ticket).ThrowIfFalse(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            ticket.IsExpired.ThrowIfTrue(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            return Character.Get(ticket.CharacterId);
        }
    }
}