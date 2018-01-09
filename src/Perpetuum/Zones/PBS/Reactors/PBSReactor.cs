using System;
using System.Collections.Generic;
using System.Threading;
using Perpetuum.Players;

namespace Perpetuum.Zones.PBS.Reactors
{
    /// <summary>
    /// This class generates core from items on user request
    /// Transfers energy to connected nodes on action
    /// </summary>
    public class PBSReactor : PBSActiveObject, IPBSCorePump, IPBSFeedable
    {
        private readonly CorePumpHandler<PBSReactor> _corePumpHandler;

        public PBSReactor()
        {
            _corePumpHandler = new CorePumpHandler<PBSReactor>(this);
        }

        public ICorePumpHandler CorePumpHandler
        {
            get { return _corePumpHandler; }
        }

        protected override void PBSActiveObjectAction(IZone zone)
        {
            _corePumpHandler.TransferToConnections();

            _lastCycleFromEnergyWell = Interlocked.Exchange(ref _collectFromWell, 0);

        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();
            _corePumpHandler.AddToDictionary(info);
            info["fromWell"] = _lastCycleFromEnergyWell;
            AddReactorInfo(info);

            return info;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _corePumpHandler.AddToDictionary(info);
            info["fromWell"] = _lastCycleFromEnergyWell;
            AddReactorInfo(info);
            return info;
        }

        

        public void FeedWithItems(Player player, IEnumerable<long> eids)
        {
            PBSHelper.FeedWithItems(this, player, eids);
        }

        private int _lastCycleFromEnergyWell;
        private int _collectFromWell;

        public void CoreFromEnergyWell(double corefromEnergyWell)
        {
            Core += corefromEnergyWell;
            Interlocked.Add(ref _collectFromWell,(int)corefromEnergyWell);
        }

        public void AddReactorInfo(IDictionary<string,object> info )
        {
            
            var fromWell = (double) _lastCycleFromEnergyWell;
            var lastPumpedOut = _corePumpHandler.LastUsedCore;

            var realConsumption = 0.0;

            if (fromWell > 0)
            {
                //volt bejovo a wellektol

                if (lastPumpedOut <= fromWell)
                {
                    //tobb jon 
                    info["coreStable"] = 1;
                    return;
                }

                //kevesebb jon a wellektol
                realConsumption = lastPumpedOut - fromWell;

            }
            else
            {
                //nem jon a wellektol

                if (lastPumpedOut > 0)
                {
                    //ennyit tolunk ki
                    realConsumption = lastPumpedOut;

                }
                else
                {
                    //no well, no pump
                    info["workIsIdle"] = 1;
                    return;
                }


            }

            var cyclesLeft = Core/realConsumption;
            var secondsLeft = cyclesLeft*30;

            info["willBeEmpty"] = DateTime.Now.AddSeconds(secondsLeft);


        }


    }
}
