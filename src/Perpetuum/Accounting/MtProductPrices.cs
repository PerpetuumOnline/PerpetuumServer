using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Accounting
{
    public class MtProduct
    {
        public static readonly MtProduct None = new MtProduct();

        public string name;
        public int price;
    }

    public interface IMtProductRepository : IReadOnlyRepository<string, MtProduct>
    {
        
    }

    public class MtProductRepository : IMtProductRepository
    {
        public MtProduct Get(string name)
        {
            var record = Db.Query().CommandText("select * from mtproductprices where productkey=@key")
                                .SetParameter("@key", name.ToLower())
                                .ExecuteSingleRow();

            if (record == null)
                return MtProduct.None;

            var p = CreateMtProductFromRecord(record);
            return p;
        }

        public IEnumerable<MtProduct> GetAll()
        {
            return Db.Query().CommandText("select * from mtproductprices")
                          .Execute()
                          .Select(CreateMtProductFromRecord).ToArray();
        }

        private static MtProduct CreateMtProductFromRecord(IDataRecord record)
        {
            var p = new MtProduct();
            p.name = record.GetValue<string>("productkey");
            p.price = record.GetValue<int>("price");
            return p;
        }
    }

    public class MtProductHelper
    {
        private readonly IMtProductRepository _mtProductRepository;

        public MtProductHelper(IMtProductRepository mtProductRepository)
        {
            _mtProductRepository = mtProductRepository;
        }

        public MtProduct GetByAccountTransactionType(AccountTransactionType type)
        {
            return _mtProductRepository.Get(type.ToString());
        }

        public IEnumerable<MtProduct> GetAllProducts()
        {
            return _mtProductRepository.GetAll();
        }

        public Dictionary<string, object> GetProductInfos()
        {
            return GetAllProducts().ToDictionary(p => p.name, p => (object)p.price);
        }
    }

}