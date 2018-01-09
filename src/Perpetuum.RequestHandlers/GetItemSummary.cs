using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

namespace Perpetuum.RequestHandlers
{
    public class GetItemSummary : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var rootEID = request.Data.GetOrDefault<long>(k.eid);

            var character = request.Session.Character;
            var owner = character.Eid;

            var records = Enumerable.Select(Db.Query().CommandText("getitemsummary")
                    .SetParameter("@ownerEID", owner)
                    .SetParameter("@rootEID", rootEID)
                    .Execute(), r =>
                {
                    return new
                    {
                        definition = r.GetValue<int>(0),
                        parent = r.GetValue<long>(1),
                        qty = r.GetValue<long>(2)
                    };
                }).ToList();

            var itemsDict = new Dictionary<string, object>();
            var count = 0;
            var parents = new List<long>(records.Count);

            foreach (var record in records)
            {
                var l = record.qty;
                if (l >= int.MaxValue)
                {
                    l = int.MaxValue;
                }

                var quantitySum = (int)l;

                if (!EntityDefault.TryGet(record.definition, out EntityDefault ed))
                    continue;

                if (!(ed.CategoryFlags.IsCategory(CategoryFlags.cf_robot_equipment) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_robots) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_ammo) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_material) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_documents) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_dogtags) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_production_items) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_mission_items) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_field_accessories) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_container) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_dynamic_cprg) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_scan_result) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_redeemables) ||
                      ed.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_capsules)))
                {
                    continue;
                }

                var oneEntry = new Dictionary<string, object>
                {
                    {k.definition, record.definition},
                    {k.parent, record.parent},
                    {k.quantity, quantitySum}
                };

                parents.Add(record.parent);

                itemsDict.Add("c" + count++, oneEntry);
                Logger.DebugInfo($"{record.parent} {EntityDefault.Get(record.definition).Name} {quantitySum}");
            }

            var result = new Dictionary<string, object>
            {
                {k.items, itemsDict},
                {k.rootEID, rootEID}
            };

            if (parents.Count > 0)
            {
                var parentStr = parents.ArrayToString();
                var counter = 0;
                var parentsDict = new Dictionary<string, object>();
                var secondParents = new List<long>();

                var dataRecords = Db.Query().CommandText($"select eid,ename,parent,definition from entities where eid in ({parentStr})").Execute();

                foreach (var r in dataRecords)
                {

                    var secondDefinition = r.GetValue<int>(3);
                    var secondParent = r.GetValue<long?>(2);

                    EntityDefault ed;
                    if (!EntityDefault.TryGet(secondDefinition, out ed))
                    {
                        continue;
                    }

                    if (ed.CategoryFlags.IsCategory(CategoryFlags.cf_volume_wrapper_container))
                    {
                        continue;
                    }


                    if (secondParent != null && secondParent > 0)
                    {
                        secondParents.Add((long)secondParent);
                    }

                    var oneEntry =
                        new Dictionary<string, object>
                        {
                            {k.eid, r.GetValue<long>(0)},
                            {k.eName, r.GetValue<string>(1)},
                            {k.parent, secondParent},
                            {k.definition, secondDefinition}
                        };


                    parentsDict.Add("p" + counter++, oneEntry);

                }


                if (secondParents.Count > 0)
                {

                    var secondParentStr = secondParents.Except(parents).ArrayToString();


                    foreach (var r in Db.Query().CommandText("select eid,ename,parent,definition from entities where eid in (" + secondParentStr + ")").Execute())
                    {
                        var oneEntry =
                            new Dictionary<string, object>
                            {
                                {k.eid, r.GetValue<long>(0)},
                                {k.eName, r.GetValue<string>(1)},
                                {k.parent, r.GetValue<long>(2)},
                                {k.definition, r.GetValue<int>(3)}
                            };

                        parentsDict.Add("p" + counter++, oneEntry);
                    }
                }


                result.Add(k.data, parentsDict);


                foreach (var pair in parentsDict)
                {
                    var v = (Dictionary<string, object>)pair.Value;
                    Logger.DebugInfo($"{v[k.eid]} {v[k.parent]} {v[k.eName]} {EntityDefault.Get((int)v[k.definition]).Name}");
                }

            }

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}