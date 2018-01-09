
namespace Perpetuum.Zones.PBS
{
    public interface IPBSEventHandler
    {
        void HandlePBSEvent(IPBSObject sender, PBSEventArgs e);
    }
}