using Perpetuum.Common.Loggers.Transaction;

namespace Perpetuum.Accounting.Characters
{
    public static class TransactionLogEventBuilderExtensions
    {
        public static TransactionLogEventBuilder SetCharacter(this TransactionLogEventBuilder builder,Character character)
        {
            return builder.SetCharacter(character.Id);
        }

        public static TransactionLogEventBuilder SetInvolvedCharacter(this TransactionLogEventBuilder builder, Character character)
        {
            return builder.SetInvolvedCharacter(character.Id);
        }
    }
}
