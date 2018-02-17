using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.EntityFramework
{
    public class Entity
    {
        public static IEntityServices Services { get; set; }

        public static IEntityFactory Factory => Services.Factory;
        public static IEntityRepository Repository => Services.Repository;

        public IEntityServices EntityServices { protected get; set; }

        private ImmutableHashSet<Entity> _children = ImmutableHashSet<Entity>.Empty;
        internal EntityDbState dbState = EntityDbState.New;
        private readonly EntityDynamicProperties _dynamicProperties;

        private double _health;
        private string _name;
        private long _owner;
        private long _parent;
        private int _quantity;
        private bool _repackaged;
        private Entity _parentEntity;

        public Entity()
        {
            _dynamicProperties = new EntityDynamicProperties();
            _dynamicProperties.Updated += OnDynamicPropertiesUpdated;
        }

        public EntityDynamicProperties DynamicProperties => _dynamicProperties;

        public long Eid { get; set; }
        public EntityDefault ED { get; set; }

        public int Definition => ED.Definition;

        public long Owner
        {
            get { return _owner; }
            set
            {
                if (_owner != value)
                {
                    _owner = value;
                    OnPropertyChanged();
                }

                foreach (var child in _children)
                {
                    child.Owner = value;
                }
            }
        }

        public long Parent
        {
            get { return _parent; }
            set
            {
                if (_parent == value)
                    return;

                _parent = value;
                OnPropertyChanged();
            }
        }

        [CanBeNull]
        public Entity ParentEntity
        {
            get { return _parentEntity; }
            set { _parentEntity = value; }
        }

        [CanBeNull]
        public Entity GetOrLoadParentEntity()
        {
            if ((_parentEntity == null || _parentEntity.Eid <= 0) && Parent > 0)
            {
                _parentEntity = LoadParentEntity(Parent);
            }

            return _parentEntity;
        }

        protected virtual Entity LoadParentEntity(long parent)
        {
            return Repository.LoadOrThrow(parent);
        }

        public virtual double Health
        {
            get { return _health; }
            set
            {
                if (Equals(_health, value))
                    return;
                
                _health = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if ( _name == value )
                    return;
                
                _name = value;
                OnPropertyChanged();
            }
        }

        public int Quantity
        {
            get { return _quantity; }
            set
            {
                if ( _quantity == value )
                    return;
                
                _quantity = value;
                OnPropertyChanged();
            }
        }

        public bool IsRepackaged
        {
            get { return _repackaged; }
            set
            {
                if ( _repackaged == value )
                    return;
                
                _repackaged = value;
                OnPropertyChanged();
            }
        }

        public virtual double Volume
        {
            get { return ED.CalculateVolume(IsRepackaged, Quantity); }
        }

        public virtual double Mass
        {
            get { return ED.Mass; }
        }

        public IReadOnlyCollection<Entity> Children
        {
            get { return _children; }
        }

        public bool HasChildren
        {
            get { return _children.Count > 0; }
        }

        public double HealthRatio
        {
            get { return (Health/ED.Health).Clamp(); }
        }

        protected static bool TryAcceptVisitor<T>(T entity,IEntityVisitor visitor) where T:Entity
        {
            var v = visitor as IEntityVisitor<T>;
            if (v == null)
                return false;

            v.Visit(entity);
            return true;
        }

        public virtual void AcceptVisitor(IEntityVisitor visitor)
        {
            TryAcceptVisitor(this,visitor);
        }

        private void OnDynamicPropertiesUpdated()
        {
            OnPropertyChanged();
        }

        private void OnPropertyChanged()
        {
            if ( dbState == EntityDbState.New )
                return;

            dbState = EntityDbState.Updated;
        }

        public virtual void OnLoadFromDb() { }
        public virtual void OnSaveToDb() { }
        public virtual void OnInsertToDb() { }
        public virtual void OnUpdateToDb() { }
        public virtual void OnDeleteFromDb() { }

        public List<Entity> GetFullTree()
        {
            var entities = new List<Entity>();

            foreach (var child in Children)
            {
                entities.AddRange(child.GetFullTree());
            }

            entities.Add(this);
            return entities;
        }

        public virtual Dictionary<string, object> ToDictionary()
        {
            return BaseInfoToDictionary();
        }

        public Dictionary<string, object> BaseInfoToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.eid, Eid},
                {k.definition, Definition},
                {k.parent, Parent},
                {k.owner, Owner},
                {k.health, Health},
                {k.repackaged, IsRepackaged},
                {k.quantity, Quantity},
                {k.name, Name},
                {k.volume, Volume},
            };
        }

        public override string ToString()
        {
            return string.Format("eid:{0} {2} ({1}) o:{3} p:{4} h:{5} n:{6} q:{7} r:{8}", Eid, Definition, ED.Name, Owner, Parent, Health,
                                 Name, Quantity, IsRepackaged);
        }

        public bool IsCategory(CategoryFlags targetCategoryFlags)
        {
            return ED.CategoryFlags.IsCategory(targetCategoryFlags);
        }

        public void AddChild(Entity entity)
        {
            if (entity == null)
                return;

            entity._parentEntity?.RemoveChild(entity);

            ImmutableInterlocked.Update(ref _children, c => c.Add(entity));

            entity.Parent = Eid;
            entity._parentEntity = this;
        }


        public void RemoveChild(Entity entity)
        {
            ImmutableInterlocked.Update(ref _children, c => c.Remove(entity));

            entity.Parent = 0;
            entity._parentEntity = null;
        }

        protected void ClearChildren()
        {
            ImmutableInterlocked.Update(ref _children, c => c.Clear());
        }

        protected internal void RebuildTree(IEnumerable<Entity> entities)
        {
            var x = entities.ToDictionary(e => e.Eid);

            foreach (var child in x.Values.GroupBy(kvp => kvp.Parent))
            {
                var parentEntity = child.Key == Eid ? this : x.GetOrDefault(child.Key);
                parentEntity?.AddManyChild(child);
            }
        }

        private void AddManyChild(IEnumerable<Entity> children)
        {
            ImmutableInterlocked.Update(ref _children, c =>
            {
                var b = ImmutableHashSet<Entity>.Empty.ToBuilder();
                foreach (var child in children)
                {
                    b.Add(child);
                    child.Parent = Eid;
                    child._parentEntity = this;
                    child.OnLoadFromDb();
                }
                return b.ToImmutable();
            });
        }

        [ThreadStatic] 
        private static HashSet<Entity> _txEntities;
        private readonly AutoResetEvent _txSync = new AutoResetEvent(true);

        public void EnlistTransaction()
        {
            var currentTx = Transaction.Current;
            if (currentTx == null)
                return;

            if (_txEntities == null)
                _txEntities = new HashSet<Entity>();
            else
            {
                if (_txEntities.Contains(this))
                    return;
            }

            _txEntities.Add(this);
            _txSync.WaitOne(10000);

            // na ez itt a trukk, lokal mentjuk el...
            var owner = _owner;
            var parent = _parent;
            var health = _health;
            var name = _name;
            var quantity = _quantity;
            var repackaged = _repackaged;
            var parentEntity = _parentEntity;
            var dbState = this.dbState;
            var children = _children;
            var dynProps = DynamicProperties.Items;

            OnEnlistTransaction();

            var txEntities = _txEntities;

            currentTx.EnlistVolatile(onCommit: OnCommitedTransaction, 
                                     onRollback: () =>
                                     {
                                        try
                                        {
                                            OnRollbackTransaction();
                                        }
                                        finally
                                        {
                                            _owner = owner;
                                            _parent = parent;
                                            _health = health;
                                            _name = name;
                                            _quantity = quantity;
                                            _repackaged = repackaged;
                                            _parentEntity = parentEntity;
                                            this.dbState = dbState;
                                            _children = children;
                                            _dynamicProperties.Items = dynProps;
                                        }
                                    }, 
                                    onCompleted: () =>
                                    {
                                        try
                                        {
                                            OnCompletedTransaction();
                                        }
                                        finally
                                        {
                                            txEntities.Remove(this);
                                            _txSync.Set();
                                        }
                                    });

            foreach (var child in Children)
            {
                child.EnlistTransaction();
            }
        }

        protected virtual void OnEnlistTransaction()    { }
        protected virtual void OnCommitedTransaction()  { }
        protected virtual void OnRollbackTransaction()  { }
        protected virtual void OnCompletedTransaction() { }

        public void SetMaxHealth()
        {
            Health = ED.Health;
        }

        public void Save()
        {
            OnSaveToDb();

            foreach (var child in Children)
            {
                child.Save();
            }

            switch (dbState)
            {
                case EntityDbState.New:
                {
                    EntityServices.Repository.Insert(this);
                    break;
                }
                case EntityDbState.Updated:
                {
                    EntityServices.Repository.Update(this);
                    break;
                }
            }
        }
    }
}