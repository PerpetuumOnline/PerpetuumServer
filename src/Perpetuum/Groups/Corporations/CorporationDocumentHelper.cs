using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Zones.TerraformProjects;

namespace Perpetuum.Groups.Corporations
{
    public class CorporationDocumentViewer
    {
        private ImmutableHashSet<Character> _viewerCharacters = ImmutableHashSet<Character>.Empty;

        public IReadOnlyCollection<Character> Viewers
        {
            get { return _viewerCharacters; }
        }

        public void AddViewer(Character character)
        {
            ImmutableInterlocked.Update(ref _viewerCharacters, v => v.Add(character));
        }

        public void RemoveViewer(Character character)
        {
            ImmutableInterlocked.Update(ref _viewerCharacters, v => v.Remove(character));
        }
    }

    public static class CorporationDocumentHelper
    {
        public static readonly IDictionary<int, CorporationDocumentConfig> corporationDocumentConfig = Database.CreateCache<int,CorporationDocumentConfig>("corporationdocumentconfig", "documenttype", r => new CorporationDocumentConfig(r));

        private static readonly ConcurrentDictionary<int,CorporationDocumentViewer>  _corporationDocumentViewers = new ConcurrentDictionary<int, CorporationDocumentViewer>();

        private static CorporationDocumentViewer GetViewerByDocumentId(int documentId)
        {
            return _corporationDocumentViewers.GetOrAdd(documentId, new CorporationDocumentViewer());
        }

        public static void DeleteViewerByDocumentId(int documentId)
        {
            _corporationDocumentViewers.Remove(documentId);
        }

        public static IReadOnlyCollection<Character> GetRegisteredCharactersFromDocument(int documentId)
        {
            var viewer = GetViewerByDocumentId(documentId);
            return viewer.Viewers;
        }

        public static void RegisterCharacterToDocument(int documentId,Character character)
        {
            var viewer = GetViewerByDocumentId(documentId);

            viewer.AddViewer(character);
        }

        public static void UnRegisterCharacterFromDocument(int documentId,Character character)
        {
            var viewer = GetViewerByDocumentId(documentId);

            viewer.RemoveViewer(character);

            if (viewer.Viewers.Count == 0)
            {
                //cleanup, no viewer
                _corporationDocumentViewers.TryRemove(documentId, out viewer);
            }
        }

        public static void OnCorporationLeave(Character character, long corporationEid)
        {
            //remove registrations
            RemoveFromAllDocuments(character);
        }

        public static void RemoveFromAllDocuments(Character character)
        {
            foreach (var pair in _corporationDocumentViewers)
            {
                var documentId = pair.Key;
                var viewer = pair.Value;

                viewer.RemoveViewer(character);

                if (viewer.Viewers.Count == 0)
                {
                    CorporationDocumentViewer tmpViewer;
                    _corporationDocumentViewers.TryRemove(documentId, out tmpViewer);
                }

            }
        }

        public static ErrorCodes CheckOwnerAccess(int documentId, Character character, out CorporationDocument corporationDocument)
        {
            
            ErrorCodes ec;
            if ((ec = GetSingleDocumentFromSql(documentId, out corporationDocument)) != ErrorCodes.NoError)
            {
                return ec;
            }

            //owner mindig tudja
            if (corporationDocument._ownerCharacter == character)
            {
                return ErrorCodes.NoError;
            }
            
            return ErrorCodes.AccessDenied;
        }
        
        public static ErrorCodes CheckRegisteredAccess(int documentId, Character character, out CorporationDocument corporationDocument, bool forWrite = false)
        {
            ErrorCodes ec;
            if ((ec = GetSingleDocumentFromSql(documentId, out corporationDocument)) != ErrorCodes.NoError)
            {
                return ec;
            }

            //owner mindig tudja
            if (corporationDocument._ownerCharacter == character)
            {
                return ErrorCodes.NoError;
            }

            ReadWriteRole role;
            if ((ec = corporationDocument.IsRegistered(character.Id, out role)) != ErrorCodes.NoError)
            {
                return ec;
            }

            if (forWrite)
            {
                if (role == ReadWriteRole.read)
                {
                    return ErrorCodes.InsufficientPrivileges;
                }
            }
            
            return ec;
        }

        private const string LowDetailSelect = "select id,ownercharacterid,documenttype,creation,lastmodified,validuntil,version from corporationdocuments";

        private static ErrorCodes GetSingleDocumentFromSql(int documentId, out CorporationDocument corporationDocument)
        {
            corporationDocument = null;
            const string commandStr = LowDetailSelect + " where id=@id";
            

            var record =
            Db.Query().CommandText(commandStr).SetParameter("@id", documentId)
                .ExecuteSingleRow();

            if (record == null)
                return ErrorCodes.ItemNotFound;

            corporationDocument = new CorporationDocument(record);
            
            return ErrorCodes.NoError;

        }


        private static List<CorporationDocument> GetMyCorporationDocuments( Character character)
        {
            var myRegistered =
            Db.Query().CommandText("select documentid from corporationdocumentregistration where characterid=@characterId").SetParameter("@characterId", character.Id)
                .Execute()
                .Select(r => r.GetValue<int>(0));

            var myDocuments =
                Db.Query().CommandText("select id from corporationdocuments where ownercharacterid=@characterId").SetParameter("@characterId", character.Id)
                .Execute()
                .Select(r => r.GetValue<int>(0));


            var finalIds = myRegistered.Concat(myDocuments).Distinct().ToArray();

            if (finalIds.Length > 0)
            {

                var commandStr = LowDetailSelect + " where id in (" + finalIds.ArrayToString() + ")";

                return

                    Db.Query().CommandText(commandStr).SetParameter("@characterId", character.Id)
                        .Execute()
                        .Select(r => new CorporationDocument(r))
                        .ToList();
            }
            
            return new List<CorporationDocument>();
            
        }





        public static Dictionary<string,object> GetMyDocumentsToDictionary(Character character)
        {
            return GenerateResultFromDocuments(GetMyCorporationDocuments( character));
        }

        public static Dictionary<string,object> GenerateResultFromDocuments(ICollection<CorporationDocument> documents )
        {
            var documentsDict = new Dictionary<string, object>(documents.Count);
            var counter = 0;
            foreach (var corporationDocument in documents)
            {
                documentsDict.Add("d" + counter ++, corporationDocument.ToDictionary());
            }

            var result = new Dictionary<string, object>
                             {
                                 {k.data, documentsDict}
                             };

            return result;
        }

        public static void CashInCreditForDocument(int price, Character character, TransactionType transactionType, bool useCorporationWallet = false)
        {
            var wallet = character.GetWalletWithAccessCheck(useCorporationWallet,transactionType,CorporationRole.editPBS);
            wallet.Balance -= price;

            var b = TransactionLogEvent.Builder().SetCharacter(character).SetTransactionType(transactionType).SetCreditBalance(wallet.Balance).SetCreditChange(-price);

            var corpWallet = wallet as CorporationWallet;
            if (corpWallet != null)
            {
                b.SetCorporation(corpWallet.Corporation);
                corpWallet.Corporation.LogTransaction(b);
            }
            else
            {
                character.LogTransaction(b);
            }
        }

        public static ErrorCodes GetDocumentConfig(CorporationDocumentType documentType, out CorporationDocumentConfig documentConfig)
        {
            return corporationDocumentConfig.TryGetValue((int) documentType, out documentConfig) ? ErrorCodes.NoError : ErrorCodes.InvalidDocumentType;
        }

        public static ErrorCodes OnDocumentTransfer(CorporationDocument corporationDocument, Character targetCharacter)
        {
            var ec = ErrorCodes.NoError;

            //check amount per type on target character
            if (corporationDocument.DocumentConfig.maxPerCharacter > 0)
            {
                var amount = GetAmountByType(corporationDocument._documentType, targetCharacter);

                if (amount >= corporationDocument.DocumentConfig.maxPerCharacter)
                {
                    return ErrorCodes.NotEnoughDocumentSlots;
                }

            }


            return ec;
        }

        



        public static int GetAmountByType(CorporationDocumentType documentType, Character character)
        {
            return 
            Db.Query().CommandText("select count(*) from corporationdocuments where ownercharacterid=@characterId and documenttype=@type").SetParameter("@type", (int) documentType).SetParameter("@characterId", character.Id)
                .ExecuteScalar<int>();

        }


        public static IEnumerable<KeyValuePair<int, ReadWriteRole>> GetRegistered(int corporationDocumentId)
        {
            return
            Db.Query().CommandText("select characterid,role from corporationdocumentregistration where documentid=@documentId").SetParameter("@documentId", corporationDocumentId)
                .Execute()
                .Select(r => new KeyValuePair<int, ReadWriteRole>(r.GetValue<int>(0), (ReadWriteRole)r.GetValue<int>(1)));

        }





    }
}
