using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Mail
{
    public struct TrackedMail
    {
        public Character sender;
        public int target;
        public string subject;
        public string body;
        public Guid mailID;
        public DateTime creation;
        public bool wasRead;
        public int type;
        public int folder;
        public Guid? sourceID;

        public bool wasDeleted
        {
            get
            {
                return sourceID == null;
            }
        }

        public Dictionary<string, object> toDictionary()
        {
            var result = new Dictionary<string, object>
            {
                {k.sender, sender.Id}, 
                {k.target, target}, 
                {k.subject, subject},
                {k.ID,mailID.ToString()},
                {k.creation, creation},
                {k.wasRead, wasRead},
                {k.type, type},
                {k.wasDeleted, wasDeleted},
                {k.folder, folder}
            };

            if (body != null)
                result.Add(k.body, body);

            return result;
        }
    }
}