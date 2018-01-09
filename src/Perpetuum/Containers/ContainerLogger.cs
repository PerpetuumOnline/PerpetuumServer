using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;

namespace Perpetuum.Containers
{
    /// <summary>
    /// This class logs the container operations
    /// </summary>
    public class ContainerLogger
    {
        private readonly Container _container;
        private readonly Queue<ContainerLogEntry> _logs = new Queue<ContainerLogEntry>();

        public ContainerLogger(Container container)
        {
            _container = container;
        }

        public void AddLogEntry(Character character, ContainerAccess access, int definition = 0, int quantity = 0)
        {
            if ( character == Character.None)
                return;

            var logEntry = new ContainerLogEntry
                               {
                                       character = character,
                                       definition = definition,
                                       quantity = quantity,
                                       access = access
                               };

            _logs.Enqueue(logEntry);
        }

        public void SaveToDb()
        {
            if (_logs.Count == 0)
                return;

            var logs = _logs.ToArray();

            var strBuilder = new StringBuilder();

            void WriteLog(string message) => Logger.DebugInfo($"[Container logwriter] ({_container.Eid}) {message}");

            WriteLog("Start");

            foreach (var logEntryList in logs.Slice(50))
            {
                var cnt = 0;
                strBuilder.Clear();

                var parameters = new Dictionary<string, object>
                {
                    ["containerEID"] = _container.Eid
                };

                foreach (var logEntry in logEntryList)
                {
                    strBuilder.AppendFormat(@"insert containerlog (containerEID,memberID,containeraccess,quantity,definition) values 
                                            (@containerEID,@memberID{0},@containerAccess{0},@quantity{0},@definition{0});", cnt);

                    parameters["memberID" + cnt] = logEntry.character.Id;
                    parameters["containerAccess" + cnt] = logEntry.access;

                    if (logEntry.definition == 0)
                    {
                        parameters["definition" + cnt] = null;
                        parameters["quantity" + cnt] = null;
                    }
                    else
                    {
                        parameters["definition" + cnt] = logEntry.definition;
                        parameters["quantity" + cnt] = logEntry.quantity;
                    }

                    cnt++;
                }

                Db.Query().CommandText(strBuilder.ToString()).SetParameters(parameters).ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLInsertError);

                WriteLog("Batch executed");
            }

            WriteLog("Finished");
        }

        public void ClearLog(Character character)
        {
            var folder = EntityDefault.GetByName(DefinitionNames.CORPORATE_HANGAR_FOLDER);
            var loggedEiDs = Db.Query().CommandText("select eid from entities where parent=@hangarEID and definition=@folderDefinition")
                                     .SetParameter("@hangarEID", _container.Eid)
                                     .SetParameter("@folderDefinition",folder.Definition)
                                     .Execute().Select(r => r.GetValue<long>(0)).ToArray();

            var folderString = loggedEiDs.Any() ? " or containerEID in (" + loggedEiDs.ArrayToString() + ")" : string.Empty;

            Db.Query().CommandText("delete containerlog where containerEID=@containerEID" + folderString)
                    .SetParameter("@containerEID", _container.Eid)
                    .ExecuteNonQuery();

            AddLogEntry(character, ContainerAccess.LogClear);
        }

        public Dictionary<string, object> LogsToDictionary(int offsetInDays = 0)
        {
            var later = DateTime.Now.AddDays(-1 * offsetInDays);
            var earlier = later.AddDays(-14);

            const string sqlCmd = @"select memberID,containeraccess,quantity,definition,operationdate 
                          from containerlog 
                          where containerEID = @containerEid or 
                          containerEID in (select eid from entities where parent = @containerEid and definition = @folderDefinition)  
                          and (operationdate between @earlier and @later)";

            var folder = EntityDefault.GetByName(DefinitionNames.CORPORATE_HANGAR_FOLDER);
            var records = Db.Query().CommandText(sqlCmd).SetParameter("@containerEID", _container.Eid)
                                  .SetParameter("@folderDefinition", folder.Definition)
                                  .SetParameter("@earlier", earlier)
                                  .SetParameter("@later", later)
                                  .Execute();

            var result = new Dictionary<string, object>();
            var counter = 0;
            foreach (var record in records)
            {
                var oneEntry = new Dictionary<string, object>
                                   {
                                           {k.memberID, record.GetValue<int>(0)},
                                           {k.operation, record.GetValue<int>(1)}
                                   };

                if (!record.IsDBNull(2)) oneEntry.Add(k.quantity, record.GetValue<int>(2));
                if (!record.IsDBNull(3)) oneEntry.Add(k.definition, record.GetValue<int>(3));

                oneEntry.Add(k.date, record.GetValue<DateTime>(4));

                result.Add("c" + counter++, oneEntry);
            }

            var totalDict = new Dictionary<string, object> { { k.eid, _container.Eid }, { k.log, result } };
            return totalDict;
        }

        private struct ContainerLogEntry
        {
            public Character character;
            public int definition;
            public int quantity;
            public ContainerAccess access;
        }
    }
}