using System;
using System.Runtime.InteropServices;

namespace Perpetuum.Services.Steam
{
    public interface ISteamManager
    {
        int SteamAppID { get; }
        string GetSteamId(byte[] encryptedTicket);
    }

    public class SteamManager : ISteamManager
    {
        private readonly byte[] _steamKey;

        [DllImport(@"sdkencryptedappticket64.dll",CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamEncryptedAppTicket_BDecryptTicket([In, Out] byte[] rgubTicketEncrypted,uint cubTicketEncrypted,[In, Out] byte[] rgubTicketDecrypted,ref uint pcubTicketDecrypted,[In, Out] byte[] rgubKey,int cubKey);

        [DllImport(@"sdkencryptedappticket64.dll",CallingConvention = CallingConvention.Cdecl)]
        private static extern void SteamEncryptedAppTicket_GetTicketSteamID([In] byte[] rgubTicketDecrypted,uint cubTicketDecrypted,ref ulong steamId);

        [DllImport(@"sdkencryptedappticket64.dll",CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamEncryptedAppTicket_BIsTicketForApp([In] byte[] rgubTicketDecrypted,uint cubTicketDecrypted,uint nAppID);

        [DllImport(@"sdkencryptedappticket64.dll",CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SteamEncryptedAppTicket_GetTicketIssueTime([In] byte[] rgubTicketDecrypted,uint cubTicketDecrypted);

        [DllImport(@"sdkencryptedappticket64.dll",CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SteamEncryptedAppTicket_GetTicketAppID([In] byte[] rgubTicketDecrypted,uint cubTicketDecrypted);

        [DllImport(@"sdkencryptedappticket64.dll",CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamEncryptedAppTicket_BUserOwnsAppInTicket([In] byte[] rgubTicketDecrypted,uint cubTicketDecrypted,uint nAppID);

        [DllImport(@"sdkencryptedappticket64.dll",CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamEncryptedAppTicket_BUserIsVacBanned([In] byte[] rgubTicketDecrypted,uint cubTicketDecrypted);

        [DllImport(@"sdkencryptedappticket64.dll",CallingConvention = CallingConvention.Cdecl)]
        private static extern byte[] SteamEncryptedAppTicket_GetUserVariableData([In] byte[] rgubTicketDecrypted,uint cubTicketDecrypted,ref uint pcubUserData);

        public SteamManager(int steamAppId,byte[] steamKey)
        {
            SteamAppID = steamAppId;
            _steamKey = steamKey;
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return new DateTime(1970,1,1,0,0,0,0).AddSeconds(unixTimeStamp).ToLocalTime();
        }

        public ulong GetSteamId(byte[] encryptedTicket, out DateTime ticketTime)
        {
            ulong steamId = 0L;
            var rgubTicketDecrypted = new byte[1024];
            uint pcubTicketDecrypted = 1024;

            ticketTime = DateTime.MinValue;

            var success = SteamEncryptedAppTicket_BDecryptTicket(encryptedTicket,(uint)encryptedTicket.Length,rgubTicketDecrypted,ref pcubTicketDecrypted,_steamKey,_steamKey.Length);
            if (success)
            {
                var timeStamp = SteamEncryptedAppTicket_GetTicketIssueTime(rgubTicketDecrypted,pcubTicketDecrypted);
                ticketTime = UnixTimeStampToDateTime(timeStamp);

                var isCorrectApp = SteamEncryptedAppTicket_BIsTicketForApp(rgubTicketDecrypted,pcubTicketDecrypted,(uint) SteamAppID);
                if (isCorrectApp)
                {
                    var userOwnsApp = SteamEncryptedAppTicket_BUserOwnsAppInTicket(rgubTicketDecrypted,pcubTicketDecrypted,(uint) SteamAppID);
                    if (userOwnsApp)
                    {
                        SteamEncryptedAppTicket_GetTicketSteamID(rgubTicketDecrypted,pcubTicketDecrypted,ref steamId);
                        SteamEncryptedAppTicket_BUserOwnsAppInTicket(rgubTicketDecrypted,pcubTicketDecrypted,(uint) SteamAppID);
                    }
                }
            }

            return steamId;
        }

        public int SteamAppID { get; }

        public string GetSteamId(byte[] encryptedTicket)
        {
            var steamId = GetSteamId(encryptedTicket, out DateTime ticketTime);
            return Convert.ToString(steamId);
        }
    }

    /*

    public static class SteamHelper
    {
        public static Dictionary<uint, int> steamAppStoreIdToPackageId = new Dictionary<uint, int>()
        {
            {STEAM_APP_ID,1},
            {SteamHelper.DLC0, 4},
            {SteamHelper.DLC1, 5},
        };

        private static readonly byte[] _key = new byte[] { 0x7b, 0xe2, 0x9d, 0xf1, 
            0xfe, 0xa3, 0x15, 0x7a, 
            0xb4, 0x56, 0x29, 0xd8, 
            0xe1, 0x84, 0xb4, 0x39, 
            0x41, 0x07, 0x1a, 0xc4, 
            0x65, 0x38, 0x05, 0xff, 
            0x66, 0x98, 0x6e, 0xee, 
            0x49, 0x38, 0xb3, 0x3b };

        public const uint STEAM_APP_ID = 223410; // Perpetuum Steam App ID
        public const uint DLC0 = 426280;
        public const uint DLC1 = 426281;

        [DllImport(@"sdkencryptedappticket64.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamEncryptedAppTicket_BDecryptTicket([In, Out] byte[] rgubTicketEncrypted, uint cubTicketEncrypted, [In, Out] byte[] rgubTicketDecrypted, ref uint pcubTicketDecrypted, [In, Out] byte[] rgubKey, int cubKey);

        [DllImport(@"sdkencryptedappticket64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SteamEncryptedAppTicket_GetTicketSteamID([In] byte[] rgubTicketDecrypted, uint cubTicketDecrypted, ref ulong steamId);

        [DllImport(@"sdkencryptedappticket64.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamEncryptedAppTicket_BIsTicketForApp([In] byte[] rgubTicketDecrypted, uint cubTicketDecrypted, uint nAppID);

        [DllImport(@"sdkencryptedappticket64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SteamEncryptedAppTicket_GetTicketIssueTime([In] byte[] rgubTicketDecrypted, uint cubTicketDecrypted);

        [DllImport(@"sdkencryptedappticket64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SteamEncryptedAppTicket_GetTicketAppID([In] byte[] rgubTicketDecrypted, uint cubTicketDecrypted);

        [DllImport(@"sdkencryptedappticket64.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamEncryptedAppTicket_BUserOwnsAppInTicket([In] byte[] rgubTicketDecrypted, uint cubTicketDecrypted, uint nAppID);

        [DllImport(@"sdkencryptedappticket64.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SteamEncryptedAppTicket_BUserIsVacBanned([In] byte[] rgubTicketDecrypted, uint cubTicketDecrypted);

        [DllImport(@"sdkencryptedappticket64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte[] SteamEncryptedAppTicket_GetUserVariableData([In] byte[] rgubTicketDecrypted, uint cubTicketDecrypted, ref uint pcubUserData);

        public static string GetSteamIdAsString(byte[] encryptedTicket, out DateTime ticketTime)
        {
            return Convert.ToString(GetSteamId(encryptedTicket, out ticketTime));
        }

        public static ulong GetSteamId(byte[] encryptedTicket, out DateTime ticketTime)
        {
            ulong steamId = 0L;
            var rgubTicketDecrypted = new byte[1024];
            uint pcubTicketDecrypted = 1024;

            ticketTime = DateTime.MinValue;

            var success = SteamEncryptedAppTicket_BDecryptTicket(encryptedTicket, (uint)encryptedTicket.Length, rgubTicketDecrypted, ref pcubTicketDecrypted, _key, _key.Length);
            if (success)
            {
                var timeStamp = SteamEncryptedAppTicket_GetTicketIssueTime(rgubTicketDecrypted, pcubTicketDecrypted);
                ticketTime = UnixTimeStampToDateTime(timeStamp);

                var isCorrectApp = SteamEncryptedAppTicket_BIsTicketForApp(rgubTicketDecrypted, pcubTicketDecrypted, STEAM_APP_ID);
                if (isCorrectApp)
                {
                    var userOwnsApp = SteamEncryptedAppTicket_BUserOwnsAppInTicket(rgubTicketDecrypted, pcubTicketDecrypted, STEAM_APP_ID);
                    if (userOwnsApp)
                    {
                        SteamEncryptedAppTicket_GetTicketSteamID(rgubTicketDecrypted, pcubTicketDecrypted, ref steamId);
                        SteamEncryptedAppTicket_BUserOwnsAppInTicket(rgubTicketDecrypted, pcubTicketDecrypted, STEAM_APP_ID);
                    }
                }
            }

            return steamId;
        }

        public static bool HasSteamApp(byte[] encryptedTicket, uint steamAppID)
        {
            var rgubTicketDecrypted = new byte[1024];
            uint pcubTicketDecrypted = 1024;

            var success = SteamEncryptedAppTicket_BDecryptTicket(encryptedTicket, (uint)encryptedTicket.Length, rgubTicketDecrypted, ref pcubTicketDecrypted, _key, _key.Length);
            if (!success)
                return false;

            var isCorrectApp = SteamEncryptedAppTicket_BIsTicketForApp(rgubTicketDecrypted, pcubTicketDecrypted, STEAM_APP_ID);
            if (isCorrectApp)
            {
                return SteamEncryptedAppTicket_BUserOwnsAppInTicket(rgubTicketDecrypted, pcubTicketDecrypted, steamAppID);
            }

            return false;
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(unixTimeStamp).ToLocalTime();
        }
    }

    */
}