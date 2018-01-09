using System;
using System.Collections.Generic;

namespace Perpetuum.Items
{
    public class ItemErrorNotifier : IDisposable
    {
        private readonly bool _rethrow;
        private readonly Dictionary<Item, PerpetuumException> _errors = new Dictionary<Item, PerpetuumException>();

        public ItemErrorNotifier(bool rethrow)
        {
            _rethrow = rethrow;
        }

        public void AddError(Item item, PerpetuumException exception)
        {
            _errors.Add(item, exception);
        }

        public void Dispose()
        {
            if (_errors.Count <= 0)
                return;

            var kvp = _errors.RandomElement();
            var item = kvp.Key;
            var exception = kvp.Value;
            item.SendErrorMessageToOwner(Commands.RelocateItems, exception.error);

            if (_rethrow)
                throw exception;
        }
    }
}