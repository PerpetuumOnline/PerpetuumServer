using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionHistory : IRequestHandler
    {
        private readonly IExtensionReader _extensionReader;

        public ExtensionHistory(IExtensionReader extensionReader)
        {
            _extensionReader = extensionReader;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var dictionary = new Dictionary<string, object>
            {
                { k.characterID, character.Id }, 
                { k.history, GetExtensionHistory(character) }
            };

            Message.Builder.FromRequest(request)
                .WithData(dictionary)
                .WrapToResult()
                .Send();
        }

        private Dictionary<string, object> GetExtensionHistory(Character character)
        {
            var records = Db.Query().CommandText("select extensionid,extensionlevel,eventtime,points from accountextensionspent where characterid=@characterID")
                                  .SetParameter("@characterID", character.Id)
                                  .Execute();

            var counter = 0;
            return records.Where(e => _extensionReader.GetExtensions().ContainsKey(DataRecordExtensions.GetValue<int>(e, 0)))
                          .Select(e => (object)new Dictionary<string, object>
            {
                {k.extensionID, DataRecordExtensions.GetValue<int>(e, 0)},
                {k.extensionLevel, DataRecordExtensions.GetValue<int>(e, 1)},
                {k.learnedTime, DataRecordExtensions.GetValue<DateTime>(e, 2)},
                {k.points, DataRecordExtensions.GetValue<int>(e, 3)}
            }).ToDictionary(d => "e" + counter++);
        }
    }
}