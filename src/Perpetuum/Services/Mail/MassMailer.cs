using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Log;

namespace Perpetuum.Services.Mail
{
    public static class MassMailer
    {
        public static IEnumerable<MassMail> ListFolder(Character character, int folder)
        {
            //                                    0     1      2      3     4       5       6       7    8
            var mails = Db.Query().CommandText("select mailid,sender,folder,type,creation,wasread,targets,owner,subject from charactermessages where folder=@folder and owner=@owner")
                                .SetParameter("@folder", folder).SetParameter("@owner", character.Id)
                                .Execute();

            return mails.Select(m => new MassMail
            {
                mailID = m.GetValue<long>(0),
                sender = Character.Get(m.GetValue<int>(1)),
                folder = (MailFolder) m.GetValue<int>(2),
                type = (MailType) m.GetValue<int>(3),
                creation = m.GetValue<DateTime>(4),
                wasRead = m.GetValue<bool>(5),
                targets = GenxyConverter.DeserializeObject<int[]>(m.GetValue<string>(6)).ToCharacter().ToArray(),
                owner = Character.Get(m.GetValue<int>(7)),
                subject = m.GetValue<string>(8)
            });
        }

        [CanBeNull]
        public static MassMail OpenMail(Character character, long mailId)
        {
            //                        0      1      2      3    4       5       6       7     8      9
            var m = Db.Query().CommandText("update charactermessages set wasread=1 where mailid=@mailID and owner=@characterID; select mailid,sender,folder,type,creation,wasread,targets,owner,subject,body from charactermessages where mailid=@mailID and owner=@characterID")
                            .SetParameter("@mailID", mailId)
                            .SetParameter("@characterID", character.Id)
                            .ExecuteSingleRow();

            if (m == null) 
                return null;

            return new MassMail
            {
                mailID = m.GetValue<long>(0),
                sender = Character.Get(m.GetValue<int>(1)),
                folder = (MailFolder)m.GetValue<int>(2),
                type = (MailType)m.GetValue<int>(3),
                creation = m.GetValue<DateTime>(4),
                wasRead = m.GetValue<bool>(5),
                targets = GenxyConverter.DeserializeObject<int[]>(m.GetValue<string>(6)).ToCharacter().ToArray(),
                owner = Character.Get(m.GetValue<int>(7)),
                subject = m.GetValue<string>(8),
                body = m.GetValue<string>(9)
            };
        }

        public static ErrorCodes WriteMailToTargets(MassMail mail)
        {
            var ec = ErrorCodes.NoError;
            try
            {
                foreach (var target in mail.targets)
                {
                    Db.Query().CommandText(@"insert charactermessages (sender,folder,body,subject,type,targets,owner) values (@sender,@folder,@body,@subject,@type,@targets,@owner)")
                        .SetParameter("@sender", mail.sender.Id)
                        .SetParameter("@folder", mail.folder)
                        .SetParameter("@body", mail.body)
                        .SetParameter("@subject", mail.subject)
                        .SetParameter("@type", mail.type)
                        .SetParameter("@targets", GenxyConverter.SerializeObject(mail.targets.GetCharacterIDs().ToArray()))
                        .SetParameter("@owner", mail.owner.Id)
                        .SetParameter("@owner", target.Id)
                        .ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("error occred in writeMailToTargetsSQL: " + ex.Message);
                ec = ErrorCodes.SQLExecutionError;

            }
            return ec;
        }

        public static ErrorCodes DeleteMail(Character character, long[] mailIDs)
        {
            if (mailIDs.Length == 0) return ErrorCodes.NoError;
            
            foreach (var id in mailIDs)
            {
                var res = Db.Query().CommandText("delete charactermessages where mailid=@mailID and owner=@characterID")
                                  .SetParameter("@mailID", 0L)
                                  .SetParameter("@characterID", character.Id)
                                  .SetParameter("@mailID", id)
                                  .ExecuteNonQuery();

                if (res != 1)
                    return ErrorCodes.SQLDeleteError;
            }

            return ErrorCodes.NoError;
        }

        public static ErrorCodes WriteToOutbox(MassMail mail)
        {
            var res = Db.Query().CommandText(@"insert charactermessages (sender,folder,body,subject,type,targets,owner) values (@sender,@folder,@body,@subject,@type,@targets,@owner)")
                              .SetParameter("@sender", mail.sender.Id)
                              .SetParameter("@folder", (int)MailFolder.outbox)
                              .SetParameter("@body", mail.body)
                              .SetParameter("@subject", mail.subject)
                              .SetParameter("@type", (int)mail.type)
                              .SetParameter("@targets", GenxyConverter.SerializeObject(mail.targets.GetCharacterIDs().ToArray()))
                              .SetParameter("@owner", mail.sender.Id)
                              .ExecuteNonQuery();

            return (res != 1) ? ErrorCodes.SQLInsertError : ErrorCodes.NoError;
        }

        public static int NewMassMailCount(Character character)
        {
            return Db.Query().CommandText("newMassMailCount")
                           .SetParameter("@characterID", character.Id)
                           .ExecuteScalar<int>();
        }
    }
}
