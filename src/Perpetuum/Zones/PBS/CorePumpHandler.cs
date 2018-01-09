using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.PBS.Connections;

namespace Perpetuum.Zones.PBS
{

    public interface ICorePumpHandler
    {
        double LastUsedCore { get; set; }

    }


    public class CorePumpHandler<T> : ICorePumpHandler where T : Unit, IPBSCorePump, IPBSObject
    {
        private readonly T _sourceUnit;

        public CorePumpHandler(T source)
        {
            _sourceUnit = source;
        }

        public double LastUsedCore { get; set; }


        public void AddToDictionary(IDictionary<string, object> info)
        {
            var u = _sourceUnit as Unit;
            if (u == null) return;
            
            info[k.currentCore] = u.Core;
            info[k.lastUsedCore] = LastUsedCore;

        }

        private const double MINIMUM_CORE_LEVEL = 5.0;

        public void TransferToConnections()
        {
            var zone = _sourceUnit.Zone;
            if (zone == null) return;


            var sumCoreUsed = 0.0;
            if (_sourceUnit.Core < MINIMUM_CORE_LEVEL)
            {
                _sourceUnit.CorePumpHandler.LastUsedCore = 0;
                return;
            }

            //ennyi coret toltunk
            var coreTransfered = _sourceUnit.GetCoreTransferred();

            //ez minden kimeno connectionbol CSAK a consumerek
            var connections = _sourceUnit.ConnectionHandler.OutConnections.Where(c => c.TargetPbsObject is IPBSAcceptsCore && c.Weight > 0).ToArray();

            //ha mind 0-as sullyal van akkor kilepunk
            if (connections.Length == 0)
            {
                _sourceUnit.CorePumpHandler.LastUsedCore = 0;
                return;
            }

            //ide gyujtjuk azokat akiknek majd tenylet kell adni core-t
            // connection - core charge threshold
            var targetList = new List<PBSConnection>(connections.Length);


            foreach (var connection in connections)
            {
                var targetUnit = connection.TargetPbsObject as Unit;

                if (targetUnit == null || targetUnit.States.Dead || !targetUnit.InZone)
                {
                    continue;
                }

                //o egy consumer kellene hogy legyen
                var consumer = (Unit) connection.TargetPbsObject;
                if (consumer != null)
                {
                    //tele van nem kell vele most torodni
                    if (consumer.IsCoreFull()) continue;


                    //nincs emergency, toltunk bele
                    targetList.Add(connection);
                }
            }

            //weight alapjan sort
            targetList = targetList.OrderByDescending(p => p.Weight).ToList();
            

            //a szurt listarol a sum
            var weightSum = targetList.Sum(c => c.Weight);

            //sorban vesszuk vegig oket   nagy->kicsi
            foreach (var connection in targetList)
            {
                var targetUnit = connection.TargetPbsObject as Unit;

                if (targetUnit == null || targetUnit.States.Dead || !targetUnit.InZone)
                {
                    continue;
                }

                //nekunk van-e eleg core-unk egyaltalan
                if (_sourceUnit.Core > MINIMUM_CORE_LEVEL)
                {

                    //dupla csekk, kell a targetnek
                    if (targetUnit.Core < targetUnit.CoreMax)
                    {
                        //ennyi hianyzik a targetnek
                        var neededCore = targetUnit.CoreMax - targetUnit.Core;

                        //erre a connection-re ez a szorzo esik
                        var connectionRatio = connection.Weight / weightSum;

                        //ennyi core jut neki a teljes nevleges mennyisegbol
                        var rawAmountToConnection = connectionRatio * coreTransfered;

                        //persze nem kell tobb mint ami neki kellett
                        rawAmountToConnection = Math.Min(neededCore, rawAmountToConnection);

                        //es nem is lehet tobb mint ami nekunk egyaltalan van
                        rawAmountToConnection = Math.Min(_sourceUnit.Core, rawAmountToConnection);

                        //ezt mindenkepp le is vonjuk majd
                        var decreaseFromCore = rawAmountToConnection;

                        //es nem a teljesen toljuk at hanem van neki hatekonysaga 
                        var realAmountToConnection = rawAmountToConnection * _sourceUnit.GetTransferEfficiency();

                        //minimum kuszob. 
                        if (realAmountToConnection > 1)
                        {
                            //hozzaadjuk a target corejahoz
                            targetUnit.Core += realAmountToConnection;

                            //a mienkbol meg levonjuk
                            _sourceUnit.Core -= decreaseFromCore;

                            sumCoreUsed += decreaseFromCore;

                            //bamulatos grafika
                            zone.CreateBeam(BeamType.pbs_energy, b => b.WithSource(_sourceUnit)
                                .WithTarget(targetUnit)
                                .WithState(BeamState.Hit)
                                .WithBulletTime(60)
                                .WithDuration(1337));
                        }
                    }

                }
            }

            _sourceUnit.CorePumpHandler.LastUsedCore = sumCoreUsed; 
            

        }


    }




    


}
