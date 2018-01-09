using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Alliances;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

namespace Perpetuum.RequestHandlers
{
    public class BaseReown : IRequestHandler
    {
        private readonly IEntityServices _entityServices;

        public BaseReown(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var rootEid = request.Data.GetOrDefault<long>(k.eid);
                var fractionString = request.Data.GetOrDefault<string>(k.alliance);
                var targetOwner = request.Data.GetOrDefault<long>(k.target);

                if (rootEid == 0)
                {
                    var character = request.Session.Character;
                    rootEid = character.CurrentDockingBaseEid;
                }

                rootEid.ThrowIfEqual(0, ErrorCodes.EntityNotFound);

                if (!fractionString.IsNullOrEmpty())
                {
                    AllianceHelper.GetAllianceEidByFractionString(fractionString, out targetOwner).ThrowIfError();
                }

                targetOwner.ThrowIfLessOrEqual(0, ErrorCodes.NoSuchAlliance);

                ReownDefaultStructure(rootEid, targetOwner);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }

        //ez default bazisra mukodik, masra nem jo, alapbazisoknak hasznaljuk
        private void ReownDefaultStructure(long rootEid, long targetOwner)
        {
            var childrenQueue = new Queue<long>(_entityServices.Repository.GetFirstLevelChildren(rootEid));

            childrenQueue.Enqueue(rootEid);

            while (childrenQueue.Count > 0)
            {
                var eid = childrenQueue.Dequeue();
                var ed = _entityServices.Defaults.GetByEid(eid);

                if (ed.CategoryFlags.IsCategory(CategoryFlags.cf_base) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_station_services) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_public_container) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_system_container) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_public_corporation_hangar_storage)
                    )
                {
                    Logger.Info("setting owner for " + ed.Name + " eid:" + eid);

                    var res = Db.Query().CommandText("update entities set owner=@owner where eid=@eid")
                        .SetParameter("@owner", targetOwner)
                        .SetParameter("@eid", eid)
                        .ExecuteNonQuery();

                    if (res != 1)
                    {
                        Logger.Error("update error on " + eid);
                        continue;
                    }

                    //enqueue new children

                    if (!ed.CategoryFlags.IsCategory(CategoryFlags.cf_production_facilities))
                        continue;

                    foreach (var childrenEid in _entityServices.Repository.GetFirstLevelChildren(eid))
                    {
                        childrenQueue.Enqueue(childrenEid);
                    }
                }
            }
        }
       
    }
}