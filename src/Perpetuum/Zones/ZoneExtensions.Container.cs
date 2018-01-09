using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units.FieldTerminals;

namespace Perpetuum.Zones
{
    partial class ZoneExtensions
    {
        [NotNull]
        public static Container FindContainerOrThrow(this IZone zone, Player player, long containerEid)
        {
            return zone.FindContainer(player, containerEid).ThrowIfNull(ErrorCodes.ContainerNotFound);
        }

        [CanBeNull]
        public static Container FindContainer(this IZone zone, Player player, long containerEid)
        {
            var finder = new ContainerFinder(player, containerEid);

            foreach (var unit in zone.Units)
            {
                unit.AcceptVisitor(finder);

                var container = finder.Container;
                if (container == null)
                    continue;

                return container;
            }

            return null;
        }

        private class ContainerFinder : IEntityVisitor<Robot>,IEntityVisitor<FieldTerminal>
        {
            private readonly Player _player;
            private readonly long _containerEid;

            public ContainerFinder(Player player, long containerEid)
            {
                _player = player;
                _containerEid = containerEid;
            }

            internal Container Container { get; private set; }

            public void Visit(Robot robot)
            {
                var container = robot.GetContainer();

                if (container?.Eid == _containerEid)
                    Container = container;
            }

            public void Visit(FieldTerminal fieldTerminal)
            {
                var ftpc = fieldTerminal.GetPublicContainer();
                if (ftpc.Eid == _containerEid)
                {
                    Container = ftpc;
                    Container.ReloadItems(_player.Character);
                    return;
                }

                ftpc.ReloadItems(_player.Character);
                var foundItem = ftpc.GetItem(_containerEid, true);

                var foundContainer = foundItem as Container;
                if (foundContainer == null)
                    return;

                Container = foundContainer;
                
            }
        }
    }
}
