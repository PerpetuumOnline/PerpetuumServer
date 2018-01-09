using System;
using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Threading.Process;

namespace Perpetuum.RequestHandlers.Zone.MissionRequests
{
    public class ZoneDisplayMissionSpots : IRequestHandler<IZoneRequest>
    {
        private readonly IProcessManager _processManager;
        private readonly DisplayMissionSpotsProcess.Factory _displayMissionSpotsFactory;

        public ZoneDisplayMissionSpots(IProcessManager processManager, DisplayMissionSpotsProcess.Factory displayMissionSpotsFactory)
        {
            _processManager = processManager;
            _displayMissionSpotsFactory = displayMissionSpotsFactory;
        }

        public void HandleRequest(IZoneRequest request)
        {
            _processManager.RemoveFirstProcess(p =>
            {
                var dms = p.As<DisplayMissionSpotsProcess>();
                return dms?.Zone == request.Zone;
            });

            var d = _displayMissionSpotsFactory(request.Zone);
            d.LiveMode = request.Data.GetOrDefault<int>("live") == 1;
            d.Start();
            _processManager.AddProcess(d.ToAsync().AsTimed(TimeSpan.FromSeconds(20)));

            var result = new Dictionary<string, object>
            {
                {
                    k.state, "running"
                }
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}