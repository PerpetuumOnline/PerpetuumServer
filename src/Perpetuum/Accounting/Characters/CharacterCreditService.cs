using Perpetuum.Data;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterCreditService : ICharacterCreditService
    {
        public double GetCredit(int characterID)
        {
            var credit = Db.Query().CommandText("select credit from characters with (UPDLOCK) where characterid = @characterID")
                .SetParameter("@characterID", characterID)
                .ExecuteScalar<double>();
            return credit;
        }

        public void SetCredit(int characterID, double credit)
        {
            Db.Query().CommandText("update characters with (UPDLOCK) set credit = @credit where characterid = @characterID")
                .SetParameter("@characterID", characterID)
                .SetParameter("@credit",credit)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }
    }
}