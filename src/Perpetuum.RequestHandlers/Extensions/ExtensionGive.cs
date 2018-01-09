using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionGive : IRequestHandler
    {
        private readonly IExtensionReader _extensionReader;

        public ExtensionGive(IExtensionReader extensionReader)
        {
            _extensionReader = extensionReader;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character; //target character or current character

                if (request.Data.TryGetValue(k.characterID, out int characterID))
                {
                    character = Character.Get(characterID);
                }
                var level = request.Data.GetOrDefault(k.level, 9);//target level or default value

                var giveAll = false;
                if (request.Data.TryGetValue(k.extensionID, out int extensionID))
                {
                    _extensionReader.GetExtensions().ContainsKey(extensionID).ThrowIfFalse(ErrorCodes.ItemNotFound);
                }
                else
                {
                    giveAll = true;
                }

                if (giveAll)
                {
                    var extensionz = from e in Db.Query().CommandText("select extensionid from extensions where active=1 and hidden=0").Execute()
                        select new Extension(DataRecordExtensions.GetValue<int>(e, 0), level);

                    character.SetExtensions(extensionz);
                }
                else
                {
                    character.SetExtension(new Extension(extensionID, level));
                }

                Db.Query().CommandText("delete accountextensionspent where accountid=@accountID")
                    .SetParameter("@accountID", request.Session.AccountId)
                    .ExecuteNonQuery();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}