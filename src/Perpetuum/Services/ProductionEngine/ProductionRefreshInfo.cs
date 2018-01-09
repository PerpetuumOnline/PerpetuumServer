using System;

namespace Perpetuum.Services.ProductionEngine
{
    [Serializable]
    public struct ProductionRefreshInfo
    {
        public ProductionFacilityType facilityType;
        public int level;
        public long senderPBSEid;
        public long targetPBSBaseEid;
        public bool enable;
        public bool isConnected;

        public override string ToString()
        {
            return string.Format("enable:{4} facilityType:{0} level:{1} senderPBS:{2} targetBase:{3} isConnected:{4}", facilityType, level, senderPBSEid, targetPBSBaseEid, enable, isConnected);
        }
    }
}