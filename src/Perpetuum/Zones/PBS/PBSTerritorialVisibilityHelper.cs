using System.Collections.Generic;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Zones.PBS
{

    /// <summary>
    /// Controls the connected network's visibility on the client.
    /// Only positional information and radius.
    /// </summary>
    public class PBSTerritorialVisibilityHelper
    {
        private readonly PBSDockingBase _pbsDockingBase;

        private PBSDockingBaseVisibility _networkMapVisibility; //On Territory Map - blob like stuff;
        private PBSDockingBaseVisibility _dockingBaseMapVisibility; //docking base is on zone maps

        public PBSTerritorialVisibilityHelper(PBSDockingBase dockingBase)
        {
            _pbsDockingBase = dockingBase;
        }

        public PBSDockingBaseVisibility NetworkMapVisibility()
        {
            return _networkMapVisibility;
        }

        public void SetNetworkVisibleOnTerritoryMap(PBSDockingBaseVisibility value)
        {
            if (_networkMapVisibility == value)
            {
                return;
            }

            _networkMapVisibility = value;
        }

        public PBSDockingBaseVisibility DockingBaseMapVisibility()
        {
            return _dockingBaseMapVisibility;
        }

        public void SetDockingBaseVisibleOnMap(PBSDockingBaseVisibility value)
        {
            if (_dockingBaseMapVisibility == value)
            {
                return; //nothing to do
            }

            _dockingBaseMapVisibility = value;

        }




        public void Init()
        {
            if (_pbsDockingBase.DynamicProperties.Contains(k.territoryMapVisible))
            {
                _networkMapVisibility = (PBSDockingBaseVisibility)_pbsDockingBase.DynamicProperties.GetOrAdd<int>(k.territoryMapVisible);
            }
            else
            {
                _pbsDockingBase.DynamicProperties.Update(k.territoryMapVisible,(int)_networkMapVisibility);
            }

            if (_pbsDockingBase.DynamicProperties.Contains(k.mapVisible))
            {
                _dockingBaseMapVisibility = (PBSDockingBaseVisibility)_pbsDockingBase.DynamicProperties.GetOrAdd<int>(k.mapVisible);
            }
            else
            {
                _pbsDockingBase.DynamicProperties.Update(k.territoryMapVisible, (int)_dockingBaseMapVisibility);
            }


        }

        public void OnSave()
        {
            _pbsDockingBase.DynamicProperties.Update(k.territoryMapVisible,(int)_networkMapVisibility);
            _pbsDockingBase.DynamicProperties.Update(k.mapVisible, (int)_dockingBaseMapVisibility  );
        }

        public void AddToDictionary(IDictionary<string, object> info)
        {
            info.Add(k.territoryMapVisible, (int)_networkMapVisibility);
            info.Add(k.mapVisible, (int) _dockingBaseMapVisibility);
        }
    }
}