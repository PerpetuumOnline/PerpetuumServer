using System.Linq;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.Items.Ammos;
using Perpetuum.Players;
using Perpetuum.Zones;

namespace Perpetuum.Modules
{
    partial class ActiveModule
    {
        private Ammo _ammo;

        public bool IsAmmoable => _ammoCategoryFlags > 0 && AmmoCapacity > 0;

        public int AmmoCapacity { get; private set; }

        private void InitAmmo()
        {
            AmmoCapacity = ED.Options.AmmoCapacity;
        }

        public void VisitAmmo(IEntityVisitor visitor)
        {
            var ammo = GetAmmo();
            ammo?.AcceptVisitor(visitor);
        }

        [CanBeNull]
        public Ammo GetAmmo()
        {
            if (!IsAmmoable)
                return null;

            return _ammo ?? (_ammo = Children.OfType<Ammo>().FirstOrDefault());
        }

        public void SetAmmo(Ammo ammo)
        {
            if (!IsAmmoable)
                return;

            _ammo = null;
            ClearChildren();

            if (ammo != null)
            {
                ammo.Owner = Owner;
                AddChild(ammo);
                ammo.Initialize();
            }

            SendAmmoUpdatePacketToPlayer();
        }

        protected void ConsumeAmmo()
        {
            if (!IsAmmoable || !ParentIsPlayer())
                return;

            var ammo = GetAmmo();
            if (ammo == null)
                return;

            if (ammo.Quantity > 0)
                ammo.Quantity--;

            SendAmmoUpdatePacketToPlayer();
        }

        private void SendAmmoUpdatePacketToPlayer()
        {
            var player = ParentRobot as Player;
            if (player == null)
                return;

            var packet = new Packet(ZoneCommand.AmmoQty);
            packet.AppendLong(Eid);

            var ammo = GetAmmo();
            if (ammo != null)
            {
                packet.AppendLong(ammo.Eid);
                packet.AppendInt(ammo.Definition);
                packet.AppendInt(ammo.Quantity);
            }
            else
            {
                packet.AppendLong(0L);
                packet.AppendInt(0);
                packet.AppendInt(0);
            }

            player.Session.SendPacket(packet);
        }

        public bool CheckLoadableAmmo(int ammoDefinition)
        {
            if (!IsAmmoable)
                return false;

            var ammoEntityDefault = EntityDefault.GetOrThrow(ammoDefinition);
            return ammoEntityDefault.CategoryFlags.IsCategory(_ammoCategoryFlags);
        }

        [CanBeNull]
        public Ammo UnequipAmmoToContainer(Container container)
        {
            var ammo = GetAmmo();
            if (ammo != null)
            {
                if (ammo.Quantity > 0)
                {
                    container.AddItem(ammo, true);
                }
                else
                {
                    Repository.Delete(ammo);
                }
            }

            SetAmmo(null);
            return ammo;
        }
    }
}
