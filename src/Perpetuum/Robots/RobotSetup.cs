using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

namespace Perpetuum.Robots
{
    public class RobotSetup
    {
        private static readonly List<RobotSetup> _setups;

        private readonly EntityDefault _robotShell;
        private readonly EntityDefault _head;
        private readonly EntityDefault _chassis;
        private readonly EntityDefault _leg;
        private readonly EntityDefault _container;
        private readonly EntityDefault _hybridShell;

        static RobotSetup()
        {
            _setups = new List<RobotSetup>();

            foreach (var record in Db.Query().CommandText("select * from robotsetup").Execute())
            {
                var robotShell = EntityDefault.Get(record.GetValue<int>(k.robotShell.ToLower()));
                if ( robotShell == EntityDefault.None )
                    continue;

                var head = EntityDefault.Get(record.GetValue<int>(k.head));
                if ( head == EntityDefault.None )
                    continue;

                var chassis = EntityDefault.Get(record.GetValue<int>(k.chassis));
                if ( chassis == EntityDefault.None )
                    continue;

                var leg = EntityDefault.Get(record.GetValue<int>(k.leg));
                if ( leg == EntityDefault.None )
                    continue;

                var container = EntityDefault.Get(record.GetValue<int>(k.container));
                if ( container == EntityDefault.None )
                    continue;

                var hybridShell = EntityDefault.Get(record.GetValue<int>(k.hybridShell.ToLower()));
                if ( hybridShell == EntityDefault.None )
                    continue;

                _setups.Add(new RobotSetup(robotShell, head, chassis, leg, container, hybridShell));
            }
        }

        private RobotSetup(EntityDefault robotShell, EntityDefault head, EntityDefault chassis, EntityDefault leg, EntityDefault container, EntityDefault hybridShell)
        {
            _robotShell = robotShell;
            _head = head;
            _chassis = chassis;
            _leg = leg;
            _container = container;
            _hybridShell = hybridShell;
        }

        public static IEnumerable<RobotSetup> All
        {
            get { return _setups; }
        }

        public EntityDefault RobotShell
        {
            get { return _robotShell; }
        }

        public EntityDefault Chassis
        {
            get { return _chassis; }
        }

        public EntityDefault Container
        {
            get { return _container; }
        }

        public EntityDefault HybridShell
        {
            get { return _hybridShell; }
        }

        public EntityDefault Head
        {
            get { return _head; }
        }

        public EntityDefault Leg
        {
            get { return _leg; }
        }
    }
}
