using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;
using Perpetuum.Log;

namespace Perpetuum.EntityFramework
{
    public class EntityRepository : IEntityRepository
    {
        private readonly IEntityFactory _factory;

        public EntityRepository(IEntityFactory entityFactory)
        {
            _factory = entityFactory;
        }

        public void Insert(Entity entity)
        {
            if (entity.dbState != EntityDbState.New)
                return;

            entity.OnInsertToDb();

            const string cmd = @"insert into entities (eid,definition,owner,parent,health,quantity,ename,repackaged,dynprop) values
                                                        (@eid,@definition, @owner, @parent, @health, @quantity, @ename, @repackaged,@dynprop)";

            Db.Query().CommandText(cmd)
                .SetParameter("@eid", entity.Eid)
                .SetParameter("@definition", entity.Definition)
                .SetParameter("@owner", entity.Owner == 0L ? (object)null : entity.Owner)
                .SetParameter("@parent", entity.Parent == 0L ? (object)null : entity.Parent)
                .SetParameter("@health", entity.Health)
                .SetParameter("@quantity", entity.Quantity)
                .SetParameter("@ename", entity.Name)
                .SetParameter("@repackaged", entity.IsRepackaged)
                .SetParameter("@dynprop", (string)entity.DynamicProperties.ToGenxyString())
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            entity.dbState = EntityDbState.Unchanged;
            Logger.Info($"Entity created. {entity}");
        }

        public void Update(Entity entity)
        {
            entity.OnUpdateToDb();

            if (entity.dbState != EntityDbState.Updated)
                return;

            const string query = "update entities set owner = @owner,parent = @parent,health = @health,quantity = @quantity,ename = @name,repackaged = @repackaged,dynprop = @dynprop where eid = @eid";

            Db.Query().CommandText(query)
                .SetParameter("@eid", entity.Eid)
                .SetParameter("@owner", entity.Owner == 0L ? (object)null : entity.Owner)
                .SetParameter("@parent", entity.Parent == 0L ? (object)null : entity.Parent)
                .SetParameter("@health", entity.Health)
                .SetParameter("@quantity", entity.Quantity)
                .SetParameter("@name", entity.Name)
                .SetParameter("@repackaged", entity.IsRepackaged)
                .SetParameter("@dynprop", (string)entity.DynamicProperties.ToGenxyString())
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);

            entity.dbState = EntityDbState.Unchanged;
            Logger.Info($"Entity updated. {entity}");
        }

        public void Delete(Entity entity)
        {
            // ha frissen keszult entity akkor nem probaljuk letorolni
            if (entity.dbState == EntityDbState.New)
                return;

            entity.OnDeleteFromDb();
 
            foreach (var child in entity.Children)
            {
                Delete(child);
            }

            Db.Query().CommandText("delete entities where eid = @eid")
                .SetParameter("@eid", entity.Eid)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);

            entity.ParentEntity?.RemoveChild(entity);

            entity.dbState = EntityDbState.Deleted;
            Logger.Info($"Entity deleted. {entity}");
        }

        public Entity Load(long eid)
        {
            if (eid == 0L)
                return null;

            // szokasos betoltes
            var record = Db.Query().CommandText("select eid,definition, owner, parent, health, ename, quantity, repackaged, dynprop from entities where eid = @eid")
                .SetParameter("@eid", eid)
                .ExecuteSingleRow();

            if (record == null)
                return null;

            var entity = CreateEntityFromRecord(record);
            entity.OnLoadFromDb();
            return entity;
        }

        public List<Entity> LoadByOwner(long rootEid, long? ownerEid)
        {
            return Db.Query().CommandText("getlist2")
                .SetParameter("@rootEID", rootEid)
                .SetParameter("@ownerEID", ownerEid)
                .Execute().Select(CreateEntityFromRecord).ToList();
        }

        public Entity LoadTree(long rootEid, long? ownerEid)
        {
            var entities = LoadByOwner(rootEid, ownerEid);
            return BuildTreeFromList(rootEid, entities);
        }

        public Entity LoadRawTree(long rootEid)
        {
            var entities = Db.Query().CommandText("getFullTreeByRoot")
                .SetParameter("@rootEID", rootEid)
                .Execute().Select(CreateEntityFromRecord).ToList();
            return BuildTreeFromList(rootEid, entities);
        }

        [CanBeNull]
        private static Entity BuildTreeFromList(long rootEid, List<Entity> entities)
        {
            if (entities.Count == 0)
                return null;

            // elso entity a fonok
            var entity = entities.FirstOrDefault(e => e.Eid == rootEid);

            // azaz remeljuk,h az volt...
            if (entity == null)
                return null;

            // ossze is kell rakni a fat
            entity.RebuildTree(entities);

            // valszeg mindegyik NoError volt
            return entity;
        }

        private Entity CreateEntityFromRecord(IDataRecord record)
        {
            var definition = record.GetValue<int>("definition");
            var entity = _factory.CreateWithRandomEID(definition);

            entity.Eid = record.GetValue<long>("eid");
            entity.Owner = record.GetValue<long?>("owner") ?? 0L;
            entity.Parent = record.GetValue<long?>("parent") ?? 0L;
            entity.Health = record.GetValue<double>("health");
            entity.Name = record.GetValue<string>("ename");
            entity.Quantity = record.GetValue<int>("quantity");
            entity.IsRepackaged = record.GetValue<bool>("repackaged");
            entity.DynamicProperties.Items = new GenxyString(record.GetValue<string>("dynprop")).ToDictionary().ToImmutableDictionary();

            entity.dbState = EntityDbState.Unchanged;
            return entity;
        }

        public string GetName(long eid)
        {
            return Db.Query().CommandText("select ename from entities where eid = @eid")
                .SetParameter("@eid", eid)
                .ExecuteScalar<string>();
        }

        public long? GetOwner(long eid)
        {
            return Db.Query().CommandText("select owner from entities where eid = @eid")
                .SetParameter("@eid", eid)
                .ExecuteScalar<long?>();
        }

        public int GetChildrenCount(long eid)
        {
            return Db.Query().CommandText("select count(*) from entities where parent=@eid")
                .SetParameter("@eid", eid)
                .ExecuteScalar<int>();
        }

        public long[] GetFirstLevelChildren(long eid)
        {
            return Db.Query().CommandText("select eid from entities where parent = @eid")
                .SetParameter("@eid", eid)
                .Execute()
                .Select(r => r.GetValue<long>(0)).ToArray();
        }

        public long[] GetFirstLevelChildrenByCategoryflags(long eid, CategoryFlags categoryFlags)
        {
            return Db.Query().CommandText("select e.eid from entities e join entitydefaults d on e.definition=d.definition  where e.parent=@parent and (d.categoryflags & @cfMask)=@cf")
                .SetParameter("@parent", eid)
                .SetParameter("@cfMask", (long)categoryFlags.GetCategoryFlagsMask())
                .SetParameter("@cf", (long)categoryFlags)
                .Execute()
                .Select(r => r.GetValue<long>(0)).ToArray();
        }

        public Entity GetChildByDefinition(Entity parent, int childDefinition)
        {
            var serviceEid = Db.Query().CommandText("select top(1) eid from entities where definition = @definition and parent = @parentEID")
                .SetParameter("@definition", childDefinition)
                .SetParameter("@parentEID", parent.Eid)
                .ExecuteScalar<long>();

            var child = Load(serviceEid);
            child.ParentEntity = parent;
            return child;
        }

        public IEnumerable<Entity> GetFirstLevelChildren_(long rootEid)
        {
            return Db.Query().CommandText("select eid from entities where parent = @eid")
                           .SetParameter("@eid",rootEid)
                           .Execute()
                           .Select(r =>
                            {
                                var eid = r.GetValue<long>(0);
                                return Load(eid);
                            }).ToArray();
        }

        public IEnumerable<Entity> GetFirstLevelChildrenByOwner(long parent, long owner)
        {
            return Db.Query().CommandText("select eid from entities where parent = @eid and owner=@owner")
                .SetParameter("@eid",parent)
                .SetParameter("@owner",owner)
                .Execute()
                .Select(r =>
                {
                    var eid = r.GetValue<long>(0);
                    return Load(eid);
                }).ToArray();
        }
    }
}