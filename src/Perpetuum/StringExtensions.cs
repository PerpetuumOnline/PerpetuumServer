using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Perpetuum
{
    public static class StringExtensions
    {
        [UsedImplicitly]
        public static bool IsAttributeName(this string attributeName)
        {
            if (!string.IsNullOrEmpty(attributeName))
            {
                return Regex.IsMatch(attributeName, "^attribute[a-zA-Z]$");
            }

            return false;
        }

        /// <summary>
        /// Filters the character's nick
        /// </summary>
        public static bool IsNickAllowedForPlayers(this string nick)
        {
            nick = nick.ToLowerInvariant();
            return !nick.StartsWith("gm ") && !nick.StartsWith("dev ");
        }

        [CanBeNull]
        public static string Clamp(this string str,int length)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            return str.Length >= length ? str.Substring(0, length) : str;
        }

        public static string[] GetLines(this string str)
        {
            if ( string.IsNullOrEmpty(str))
                return new string[0];
            
            var delimiters = new[] { '\r', '\n' };
            return str.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).ToArray();
        }

        /// <summary>
        /// this method allows: a-z A-Z 0-9 _
        /// </summary>
        /// <param name="source"></param>
        /// <returns>true if the evil character is NOT found
        /// </returns>
        /// <remarks></remarks>
        public static bool AllowAscii(this string source)
        {
            //returns false if an illegal char is found
            return Regex.IsMatch(source, "^[a-zA-Z0-9_ ]*$");
        }

        /// <summary>
        /// Extended character set filter
        /// </summary>
        public static bool AllowExtras(this string source)
        {
            //returns false if an illegal char is found
            return Regex.IsMatch(source, "^[a-zA-Z0-9_\\[\\]\\|\\!\\#\\+\\-\\.\\$\\%\\^\\(\\)\\>\\<\\:\\{\\}\\&\\' ]*$");
        }

        public static bool IsEmail(this string email)
        {
            return (Regex.Match(email, "([\\w\\-]+\\.)*[\\w\\-]+@([\\w\\-]+\\.)+([\\w\\-]{2,3})")).Success;
        }

        public static bool IsIPv4(this string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            var splitValues = ip.Split('.');
            if (splitValues.Length != 4)
                return false;

            return splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
        }


        /// <summary>
        /// removes the // comment //  from the string
        /// </summary>
        public static string RemoveComment(this string text)
        {
            return string.IsNullOrEmpty(text) ? null : Regex.Replace(text, @"\/\/.*\/\/|\/\/.*", "");
        }

        public static string RemoveSpecialCharacters(this string input)
        {
            return Regex.Replace(input,"(?:[^a-z0-9 ]|(?<=['\"])s)",string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

        public static T ToEnum<T>(this string input)
        {
            Debug.Assert(typeof (T).IsEnum,"t.IsEnum");
            return (T) Enum.Parse(typeof (T), input, true);
        }
    }
}
