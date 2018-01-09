namespace Perpetuum.Accounting.Characters
{
    public interface ICharacterCreditService
    {
        double GetCredit(int characterID);
        void SetCredit(int characterID, double credit);
    }
}