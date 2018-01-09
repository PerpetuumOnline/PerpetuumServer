using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace Perpetuum.AdminTool
{ 
    public class AdminCreds
    {
        public static readonly AdminCreds DefaultAdminCreds = new AdminCreds() { Email = "ADMIN",Password = "ADMIN",Ip = "127.0.0.1",Port = "17700" };

        const string CREDS_FILENAME = "admincreds.json";
        public string Ip { get; set; } = "127.0.0.1";
        public string Port { get; set; } = "17700";
        public string Email { get; set; } = "ADMIN";
        public string Password { get; set; }
        public bool IsDefaultPassword => string.Equals(Password, DefaultAdminCreds.Password);
        public string PasswordAsSha1 => Password.ToSha1();
        public bool IsEmailDefault => string.Equals(Email, DefaultAdminCreds.Email);

        public AdminCreds Duplicate()
        {
            return new AdminCreds()
            {
                Email = Email,
                Password = Password,
                Ip = Ip,
                Port = Port
            };
        }

        public override string ToString()
        {
            return $"Ip:{Ip} port:{Port} user:{Email} pass:{Password}";
        }

        // would be nicer to decouple the wpf element's name... but this is sufficient for now
        // the only valid reason is that the class itself is so tightly wired to the purpose -> makes it singular
        public string Validate()
        {
            if (!ValidateIp()) { return "ipTextBox"; }
            if (!ValidatePort()) { return "portTextBox"; }
            if (!ValidateEmail()) { return "emailTextBox"; }
            if (!ValidatePassword()) { return "passwordBox"; }

            return null;
        }

        private bool ValidateEmail()
        {
            return !Email.IsNullOrEmpty();
        }

        private bool ValidatePassword()
        {
            return !Password.IsNullOrEmpty();
        }

        private bool ValidatePort()
        {
            if (Port.IsNullOrEmpty()) return false;
            int tmp;
            return Int32.TryParse(Port, out tmp);
        }

        private bool ValidateIp()
        {
            if (Ip.IsNullOrEmpty()) return false;
            IPAddress addr;
            return IPAddress.TryParse(Ip, out addr);
        }


        private string ToJson()
        {
            return JsonConvert.SerializeObject(
                new
                {
                    Ip,
                    Port,
                    Email
                }, Formatting.Indented);
        }

        public void SaveToFile()
        {
            File.WriteAllText(CREDS_FILENAME, ToJson());
        }

        public void LoadFromFile()
        {
            if (!File.Exists(CREDS_FILENAME))
            {
                SaveToFile();
            }

            var json = File.ReadAllText(CREDS_FILENAME);
            var credsData = JsonConvert.DeserializeAnonymousType(json, new
            {
                Ip = "",
                Port = 0,
                Email = ""
            });

            this.Ip = credsData.Ip;
            this.Port = credsData.Port.ToString();
            this.Email = credsData.Email;
        }


        //public static AdminCreds LoadCreds()
        //{
        //    AdminCreds creds = null;

        //    if (!File.Exists(CREDS_FILENAME))
        //    {
        //        creds = new AdminCreds();
        //        creds.SaveToFile();
        //    }


        //    try
        //    {
        //        var json = File.ReadAllText(CREDS_FILENAME);
        //        var credsData = JsonConvert.DeserializeAnonymousType(json, new
        //        {
        //            Ip = "",
        //            Port = 0,
        //            Email = ""
        //        });

        //        creds = new AdminCreds()
        //        {
        //            Ip = credsData.Ip,
        //            Port = credsData.Port.ToString(),
        //            Email = credsData.Email,
        //        };
        //    }
        //    catch (Exception) { }

        //    if (creds == null) return new AdminCreds();
        //    return creds;
        //}
    }
}
