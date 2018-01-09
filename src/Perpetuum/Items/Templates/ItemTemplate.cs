using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Log;

namespace Perpetuum.Items.Templates
{
    public class ItemTemplate<T> where T:Item
    {
        private readonly bool _repackaged;

        protected ItemTemplate(int quantity,bool repackaged)
        {
            Quantity = quantity;
            _repackaged = repackaged;
        }

        public EntityDefault EntityDefault { get; set; }

        public int Quantity { get; }

        public bool Validate()
        {
            var item = Build();
            if (item != null)
                return OnValidate(item);

            Logger.Error("item definition is not a " + typeof(T).Name + ". definition:" + EntityDefault.Definition);
            return false;
        }

        protected virtual bool OnValidate(T item)
        {
            return true;
        }

        [CanBeNull]
        public T Build()
        {
            var item = Entity.Factory.CreateWithRandomEID(EntityDefault) as T;
            if (item == null)
                return null;

            item.Quantity = Quantity;
            item.IsRepackaged = _repackaged;

            OnBuild(item);
            return item;
        }

        protected virtual void OnBuild(T item)
        {
            
        }

        public virtual Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.definition, EntityDefault.Definition},
                {k.quantity,Quantity},
                {k.repackaged,_repackaged}
            };
        }

        protected static ItemTemplate<T> Create(int definition, int quantity, bool repackaged)
        {
            return new ItemTemplate<T>(quantity,repackaged)
            {
                EntityDefault = EntityDefault.Get(definition)
            };
        }
    }
}