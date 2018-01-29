using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Perpetuum.Network
{
    public static class Http
    {
        /// <summary>
        /// Posts a request and returns the reply
        /// </summary>
        public static string Post(string address, IEnumerable<KeyValuePair<string, object>> data)
        {
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "PerpetuumServer/1.0");
                var response = client.UploadValues(address, data.ToNameValueCollecion());
                return Encoding.UTF8.GetString(response);
            }
        }
    }
}
