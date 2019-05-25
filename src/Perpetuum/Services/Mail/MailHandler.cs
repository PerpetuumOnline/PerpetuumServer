using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Services.Mail
{
    public static class MailHandler
    {

        // Returns the list of emails from the specified folder
        public static IEnumerable<TrackedMail> ListMails(Character character,int folder)
        {
            return from mail in Db.Query().CommandText("select subject, creation, mailid, sender, type, wasread, sourceid, target from cmails where owner = @characterID and folder = @folder")
                                        .SetParameter("@characterID", character.Id)
                                        .SetParameter("@folder", folder).Execute()
                   select new TrackedMail
                              {
                                  subject = mail.GetValue<string>(0),
                                  creation = mail.GetValue<DateTime>(1),
                                  mailID = mail.GetValue<Guid>(2),
                                  sender = Character.Get(mail.GetValue<int>(3)),
                                  type = mail.GetValue<byte>(4),
                                  wasRead = mail.GetValue<bool>(5),
                                  sourceID = mail.GetValue<Guid?>(6),
                                  folder = folder,
                                  target = mail.GetValue<int>(7)
                              };
        }

        /// <summary>
        /// Returns the used folders.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static IEnumerable<int> ListUsedFolders(Character character)
        {
            return Db.Query().CommandText("select folder from cmails where owner=@characterID group by folder")
                           .SetParameter("@characterID", character.Id)
                           .Execute()
                           .Select(record => (int) record.GetValue<byte>(0));
        }


        public static TrackedMail OpenMail(Character character,string mailID)
        {
            var mailGuid = new Guid(mailID);

            var record = Db.Query().CommandText("select sender,subject,body,type,creation,wasread,sourceid,folder,target from cmails where owner = @characterID and mailid = @mailID")
                    .SetParameter("@characterID", character.Id)
                    .SetParameter("@mailID", mailGuid)
                    .ExecuteSingleRow().ThrowIfNull(ErrorCodes.MailNotFound);

            var mail = new TrackedMail
            {
                sender = Character.Get(record.GetValue<int>(0)),
                subject = record.GetValue<string>(1),
                body = record.GetValue<string>(2),
                type = record.GetValue<byte>(3),
                creation = record.GetValue<DateTime>(4),
                wasRead = record.GetValue<bool>(5),
                mailID = mailGuid,
                sourceID = record.GetValue<Guid?>(6),
                folder = record.GetValue<byte>(7),
                target = record.GetValue<int>(8)
            };

            if (mail.wasRead) 
                return mail;

            Db.Query().CommandText("update cmails set wasread = 1 where owner = @characterID and mailid = @mailID")
                    .SetParameter("@characterID", character.Id)
                    .SetParameter("@mailID", mailGuid)
                    .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);

            mail.wasRead = true;

            if (mail.sourceID != null)
            {
                MailEventReport((Guid)mail.sourceID, mail.sender, Commands.MailGotRead);
            }

            return mail;
        }

        public static ErrorCodes SendMail(Character sender,Character target, string subject, string body, MailType mailType, out Guid mailID, out Guid sourceID)
        {
            var senderGuid = sourceID = Guid.NewGuid();
            var targetGuid = mailID = Guid.NewGuid();

            var res = Db.Query().CommandText("mailsend")
                              .SetParameter("@sender", sender.Id)
                              .SetParameter("@target", target.Id)
                              .SetParameter("@subject", subject)
                              .SetParameter("@body", body)
                              .SetParameter("@type", (byte)mailType)
                              .SetParameter("@infolder", MailFolder.inbox)
                              .SetParameter("@outfolder", MailFolder.outbox)
                              .SetParameter("@senderID", senderGuid)
                              .SetParameter("@targetID", targetGuid)
                              .ExecuteNonQuery();

            if (res == 0)
            {
                return ErrorCodes.SQLExecutionError;
            }

            //inform target
            var eHash = new Dictionary<string, object>
                            {
                                {k.subject, subject}, 
                                {k.type, (int) mailType}, 
                                {k.sender, sender}, 
                                {k.folder, (int) MailFolder.inbox}
                            };

            MailEventReport(mailID, target, Commands.MailReceived, eHash);

            return ErrorCodes.NoError;
        }



        public static void DeleteMail(Character character,string mailId)
        {
            var mailGuid = new Guid(mailId);

            var record = Db.Query().CommandText("select sender,sourceid from cmails where owner = @characterID and mailid = @mailID")
                    .SetParameter("@characterID", character.Id).SetParameter("@mailID", mailGuid)
                    .ExecuteSingleRow().ThrowIfNull(ErrorCodes.MailNotFound);

            if (!record.IsDBNull(1))
            {
                var sender = Character.Get(record.GetValue<int>(0));
                var sourceGuid = record.GetValue<Guid>(1);
                MailEventReport(sourceGuid, sender, Commands.MailGotDeleted);
            }

            Db.Query().CommandText("delete from cmails where owner = @characterID and mailid = @mailID")
                    .SetParameter("@characterID", character.Id)
                    .SetParameter("@mailID", mailGuid)
                    .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.MailNotFound);
        }

        public static void MoveToFolder(Character character,string mailID,int folder)
        {
            folder.ThrowIfGreater(255, ErrorCodes.MailFolderIndexIsOutOfRange);
            folder.ThrowIfLess(0, ErrorCodes.MailFolderIndexIsOutOfRange);

            Db.Query().CommandText("update cmails set folder=@folder where owner=@characterID and mailid=@mailID")
                    .SetParameter("@characterID", character.Id)
                    .SetParameter("@mailID", new Guid(mailID))
                    .SetParameter("@folder", folder)
                    .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.MailNotFound);
        }


        public static void DeleteFolder(Character character,int folder)
        {
            Db.Query().CommandText("delete from cmails where owner=@characterID and folder = @folder")
                    .SetParameter("@characterID", character.Id)
                    .SetParameter("@folder", folder)
                    .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.MailNotFound);
        }

        private static void MailEventReport(Guid mailID,Character character,Command command, IDictionary<string, object> extras = null)
        {
            if (!character.IsOnline) 
                return;

            var rData = new Dictionary<string, object> {{k.ID, mailID.ToString()}};

            //transfer extra data
            rData.AddRange(extras);

            Message.Builder.SetCommand(command)
                           .WithData(rData)
                           .ToCharacter(character)
                           .Send();
        }

        public static int NewMailCount(Character character)
        {
            return Db.Query().CommandText("newMailCount").SetParameter("@characterID", character.Id).ExecuteScalar<int>();
        }

        public static ErrorCodes SendWelcomeMailBeginTutorial(Character newPlayer)
        {
            return SendWelcomeMail(newPlayer, PreMadeMailNames.TUTORIAL_ARRIVE);
        }

        public static ErrorCodes SendWelcomeMailExitTutorial(Character newPlayer)
        {
            return SendWelcomeMail(newPlayer, PreMadeMailNames.TUTORIAL_FINISH);
        }

        private static ErrorCodes SendWelcomeMail(Character newPlayer, string mailName)
        {
            // TODO: query for this character once upon startup
            Character sender = Character.GetByNick("[OPP] Sparky - The Syndicate Welcome Agent");

            string subject, body;
            using (var scope = Db.CreateTransaction())
            {
                var records = Db.Query()
                    .CommandText("SELECT subject, body FROM premademail WHERE name = @mailName")
                    .SetParameter("@mailName", mailName)
                    .Execute();
                subject = records.First().GetValue<string>("subject");
                body = records.First().GetValue<string>("body");

                subject = subject.Replace("$USER$", newPlayer.Nick);
                body = body.Replace("$USER$", newPlayer.Nick);

                scope.Complete();
            }

            return SendMail(sender, newPlayer, subject, body, MailType.character, out _, out _);
        }
    }
}