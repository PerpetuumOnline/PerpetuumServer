using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSCheckDeployment : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var ec = ErrorCodes.NoError;
            var character = request.Session.Character;
            var definition = request.Data.GetOrDefault<int>(k.definition);
            var x = request.Data.GetOrDefault<int>(k.x);
            var y = request.Data.GetOrDefault<int>(k.y);
            var info = request.Data.GetOrDefault(k.info, new Dictionary<string, object>());

            var dc = EntityDefault.Get(definition).Config;

            if (dc.constructionRadius == null || dc.blockingradius == null)
            {
                Logger.Error("consistency error. no constructionradius or blockingradius is defined for definition:" + definition);
                throw PerpetuumException.Create(ErrorCodes.ConsistencyError).SetData(info);
            }

            character.GetPrivateCorporationOrThrow()
                .GetMemberRole(character)
                .IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.editPBS)
                .ThrowIfFalse(ErrorCodes.InsufficientPrivileges,gex => gex.SetData(info));

            var position = new Position(x, y).Center;

            var ed = EntityDefault.Get(definition);


            List<Position> badSlopes;
            List<Position> badBlocks;
            if ((ec = PBSHelper.CheckZoneForDeployment(request.Zone,position,ed, out badSlopes, out badBlocks, true )) != ErrorCodes.NoError)
            {

                info.Add(k.zoneID, request.Zone.Id);

                if (badBlocks.Count > 0)
                {
                    var array = badBlocks.SelectMany(p => new[] {p.intX, p.intY}).ToArray();
                    info.Add(k.blocks, array);
                }

                if (badSlopes.Count > 0)
                {
                    var array = badSlopes.SelectMany(p => new[] {p.intX, p.intY}).ToArray();
                    info.Add(k.slope, array);
                }

                throw PerpetuumException.Create(ec).SetData(info);
            }

            request.Zone.CreateBeam(BeamType.artifact_found,builder => builder.WithPosition(position).WithVisibility(200).WithDuration(1337));

            var result = new Dictionary<string, object>
            {
                {k.message, k.oke}
            };

            if (info.Count > 0)
            {
                result.Add(k.info, info);
            }

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}