using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Zones.TerraformProjects;

namespace Perpetuum.Groups.Corporations
{
    /// <summary>
    /// Text based corporation document
    /// </summary>
    public class CorporationDocument
    {
        public const int MAX_REGISTERED_MEMBERS = 25;


        private readonly int _id;
        private DateTime _creation;
        private DateTime _lastModified;
        private DateTime? _validUntil;
       
     
        public Character _ownerCharacter;
        public readonly CorporationDocumentType _documentType;
        public int version;
        private string _body;


        public CorporationDocument(IDataRecord record)
        {
            _id = record.GetValue<int>("id");
            _ownerCharacter = Character.Get(record.GetValue<int>("ownercharacterid"));
            _documentType = (CorporationDocumentType) record.GetValue<int>("documenttype");
            _creation = record.GetValue<DateTime>("creation");
            _lastModified = record.GetValue<DateTime>("lastmodified");
            _validUntil = record.GetValue<DateTime?>("validuntil");
            version = record.GetValue<int>("version");
        }

        private CorporationDocument(int freshId,  Character character, CorporationDocumentType corporationDocumentType, DateTime? validUntil, string body)
        {
            _id = freshId;
            _creation = DateTime.Now;
            _lastModified = DateTime.Now;
            _validUntil = validUntil;
            _ownerCharacter = character;
            _documentType = corporationDocumentType;
            version = 0;
            _body = body;

        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                           {k.ID, _id},
                           {k.ownerCharacter, _ownerCharacter.Id},
                           {k.type, (int) _documentType},
                           {k.body, _body},
                           {k.creation, _creation},
                           {k.lastModified, _lastModified},
                           {k.validUntil, _validUntil},
                           {k.version, version},
                       };

        }

        public void ReadBody()
        {
            _body = Db.Query().CommandText("select body from corporationdocuments where id=@id").SetParameter("@id", _id).ExecuteScalar<string>();
        }

        public void SetBody(string bodyString)
        {
            _body = bodyString;
        }

        public ErrorCodes WriteBody()
        {
            version++;

            var res =
                Db.Query().CommandText("update corporationdocuments set body=@body,lastmodified=getdate(), version=version+1 where id=@id").SetParameter("@id", _id).SetParameter("@body", _body)
                    .ExecuteNonQuery();

            return res == 1 ? ErrorCodes.NoError : ErrorCodes.SQLUpdateError;
        }

        public ErrorCodes UpdateOwnerToSql()
        {
            var res = Db.Query().CommandText("update corporationdocuments set ownercharacterid=@characterId where id=@id").SetParameter("@id", _id).SetParameter("@characterId", _ownerCharacter.Id)
                    .ExecuteNonQuery();

            return res == 1 ? ErrorCodes.NoError : ErrorCodes.SQLUpdateError;
        }

        private ErrorCodes UpdateValidUntil()
        {
            var res =
                Db.Query().CommandText("update corporationdocuments set validuntil=@validUntil where id=@id ").SetParameter("@id", _id).SetParameter("@validUntil", _validUntil)
                    .ExecuteNonQuery();

            return res == 1 ? ErrorCodes.NoError : ErrorCodes.SQLUpdateError;
        }

        public ErrorCodes Delete()
        {
            var res =
                Db.Query().CommandText("delete corporationdocuments where id=@id").SetParameter("@id", _id)
                    .ExecuteNonQuery();

            return res == 1 ? ErrorCodes.NoError : ErrorCodes.SQLDeleteError;
        }

        public const string InsertQuery = "insert corporationdocuments (ownercharacterid,documenttype,validuntil,body) values (@characterId,@type,@validUntil,@body)";
        public static ErrorCodes CreateNewToSql( Character character, CorporationDocumentType corporationDocumentType,  DateTime? validUntil, string body, out CorporationDocument corporationDocument)
        {
            corporationDocument = null;
            const string cmdStr = InsertQuery + ";select cast(scope_identity() as int)";

            var freshId =
                Db.Query().CommandText(cmdStr).SetParameter("@characterId", character.Id).SetParameter("@type", (int) corporationDocumentType).SetParameter("@validUntil", validUntil).SetParameter("@body", body)
                    .ExecuteScalar<int>();


            if (freshId <= 0)
            {
                return ErrorCodes.SQLInsertError;
            }


            corporationDocument = new CorporationDocument(freshId, character,corporationDocumentType,validUntil,body);

            return ErrorCodes.NoError;

        }


       public ErrorCodes Rent(Character character, bool useCorporationWallet)
        {
            //egyaltalan rentelheto?
            if (DocumentConfig.IsRentable)
            {
                if (_validUntil == null)
                {
                    return ErrorCodes.ConsistencyError;
                }

                _validUntil = ((DateTime) _validUntil).AddDays(DocumentConfig.rentPeriodDays);

                ErrorCodes ec;
                if ((ec = UpdateValidUntil()) != ErrorCodes.NoError)
                {
                    return ec;
                }

                CorporationDocumentHelper.CashInCreditForDocument(DocumentConfig.rentPrice, character, TransactionType.DocumentRent, useCorporationWallet);
                return ErrorCodes.NoError;
            }


            return ErrorCodes.InvalidDocumentType;
        }

        [CanBeNull]
        public CorporationDocumentConfig DocumentConfig
        {
            get
            {
                
                CorporationDocumentConfig documentConfig;
                if ((CorporationDocumentHelper.GetDocumentConfig(_documentType, out documentConfig)) != ErrorCodes.NoError)
                {
                    Logger.Error("consistency error. no config was found for document type:" + _documentType);
                    return null;

                }

                return documentConfig;
            }
        }

       

        public void DeleteAllRegistered()
        {
            Db.Query().CommandText("delete corporationdocumentregistration where documentid=@ID").SetParameter("@ID", _id)
                .ExecuteNonQuery();

        }

        public void SetRegistration(IEnumerable<int> registeredMembers, IEnumerable<int> writeMembers )
        {
            DeleteAllRegistered();
         
            if (registeredMembers.IsNullOrEmpty()) return;
   
            foreach (var member in registeredMembers)
            {

                Db.Query().CommandText("insert corporationdocumentregistration (documentid,characterid) values (@documentId,@characterId)").SetParameter("@documentId", _id).SetParameter("@characterId", member)
                    .ExecuteNonQuery();

            }

            if (writeMembers.IsNullOrEmpty()) return;

            foreach (var writeMember in writeMembers)
            {
                Db.Query().CommandText("update corporationdocumentregistration set role=@role where characterid=@characterId and documentid=@documentId").SetParameter("@role", (int) ReadWriteRole.write).SetParameter("@documentId", _id).SetParameter("@characterId", writeMember)
                    .ExecuteNonQuery();

            }


        }

        public Dictionary<string, object> GetRegisteredDictionary()
        {
            var result = new Dictionary<string, object>(GetRegisteredMembers().Count());
            var counter = 0;

            foreach (var pair in GetRegisteredMembers())
            {
                var characterId = pair.Key;
                var role = pair.Value;
                var oneEntry = new Dictionary<string, object>
                                   {
                                       {k.characterID, characterId},
                                       {k.role, (int) role}
                                   };

                result.Add("t" + counter++, oneEntry);

            }

            return result;

        }

        private IEnumerable<KeyValuePair<int, ReadWriteRole>> _registeredMembers;


        public IEnumerable<KeyValuePair<int, ReadWriteRole>> GetRegisteredMembers()
        {
            return LazyInitializer.EnsureInitialized(ref _registeredMembers, () => CorporationDocumentHelper.GetRegistered(_id));
        }


        public ErrorCodes IsRegistered(int characterId, out ReadWriteRole role)
        {
            role = ReadWriteRole.read;
            var pair = GetRegisteredMembers().FirstOrDefault(r => r.Key == characterId);

            if (pair.Equals(default(KeyValuePair<int, ReadWriteRole>)))
            {
                return ErrorCodes.AccessDenied;
            }

            role = pair.Value;

            return ErrorCodes.NoError;
        }
        
    }


    public class CorporationDocumentConfig
    {
        public CorporationDocumentType documentType;
        public int creationPrice;
        public int rentPrice;
        public int rentPeriodDays;
        public int maxPerCharacter;

        public CorporationDocumentConfig(IDataRecord record)
        {
            documentType = (CorporationDocumentType)record.GetValue<int>("documenttype");
            creationPrice = record.GetValue<int>("creationprice");
            rentPrice = record.GetValue<int>("rentprice");
            rentPeriodDays = record.GetValue<int>("rentperioddays");
            maxPerCharacter = record.GetValue<int>("maxpercharacter");
        }


        public Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                           {k.type, (int) documentType},
                           {k.creationPrice, creationPrice},
                           {k.rentPrice, rentPrice},
                           {k.rentPeriod, rentPeriodDays},
                           {k.maxPerCharacter, maxPerCharacter}
                           
                       };
        }

        public ErrorCodes OnCreate(Character character, bool useCorporationWallet = false)
        {
            var ec = ErrorCodes.NoError;

            //itt minden mast is ellenorzunk

            if (maxPerCharacter > 0)
            {
                var amountByType = CorporationDocumentHelper.GetAmountByType(documentType, character);
                if (amountByType >= maxPerCharacter)
                {
                    return ErrorCodes.NotEnoughDocumentSlots;
                }
            }

            if (creationPrice > 0)
            {
                CorporationDocumentHelper.CashInCreditForDocument(creationPrice, character, TransactionType.DocumentCreate, useCorporationWallet);
            }

            return ec;
        }


        public ErrorCodes OnCreateTerraformProject(Character character, Area area, bool useCorporationWallet = false)
        {
            var ec = ErrorCodes.NoError;

            //itt minden mast is ellenorzunk

            if (maxPerCharacter > 0)
            {
                var amountByType = CorporationDocumentHelper.GetAmountByType(documentType, character);
                if (amountByType >= maxPerCharacter)
                {
                    return ErrorCodes.NotEnoughDocumentSlots;
                }
            }

            if (creationPrice > 0)
            {
                var cost = creationPrice * area.Ground;
                CorporationDocumentHelper.CashInCreditForDocument(cost, character, TransactionType.DocumentCreate, useCorporationWallet);
            }

            return ec;
        }

        public bool IsRentable
        {
            get { return rentPeriodDays > 0 && rentPrice > 0; }
        }
    }

}
