using Perpetuum.Log;
using Perpetuum.Threading.Process;
using System;


namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class TestService : Process
    {

        public TestService()
        {

        }
        public override void Update(TimeSpan time)
        {
            Console.WriteLine("=====================Update TestService==========================");
            Logger.Error("hey!!!!");
        }

        public override void Start()
        {
            Console.WriteLine("====================Start TestService============================");
            Logger.Error("this shit startin???!!!!");
        }

        public override void Stop()
        {
            Console.WriteLine("==================Stopping TestService==============================");
        }
    }
}
