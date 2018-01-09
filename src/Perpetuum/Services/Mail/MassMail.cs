using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Mail
{
    public class MassMail
    {
        public long mailID;
        public Character owner;
        public Character sender;
        public MailFolder folder;
        public Character[] targets;
        public MailType type;
        public bool wasRead;
        public DateTime creation;
        public string subject;
        public string body;

        public Dictionary<string, object> ToDetailedDictionary()
        {

            //OWNER not included
            return new Dictionary<string, object>
            {
                {k.ID, mailID},
                {k.sender, sender.Id},
                {k.folder, (int)folder},
                {k.target, targets.GetCharacterIDs().ToArray()},
                {k.type,(int)type},
                {k.wasRead, wasRead},
                {k.creation, creation},
                {k.subject, subject},
                {k.body, body}
            };

        }

        public Dictionary<string, object> ToSimpleDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.ID, mailID},
                {k.sender, sender.Id},
                {k.folder, (int)folder},
                {k.target, targets.GetCharacterIDs().ToArray()},
                {k.type, (int)type},
                {k.wasRead, wasRead},
                {k.creation, creation},
            };
        }
    }
}