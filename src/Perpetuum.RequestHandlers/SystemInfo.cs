using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class SystemInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var gfxcard = request.Data.GetOrDefault<string>("gfxCard");
            var gfxdriver = request.Data.GetOrDefault<string>("gfxDriver");
            var gfxvendorid = request.Data.GetOrDefault<int>("gfxVendorID");
            var gfxdeviceid = request.Data.GetOrDefault<int>("gfxDeviceID");
            var gfxdriverversion = request.Data.GetOrDefault<long>("gfxDriverversion");
            var pixelshader = request.Data.GetOrDefault<long>("pixelShader");
            var vertexshader = request.Data.GetOrDefault<long>("vertexShader");
            var maxtexturex = request.Data.GetOrDefault<int>("maxTexturex");
            var maxtexturey = request.Data.GetOrDefault<int>("maxTexturey");
            var osversion = request.Data.GetOrDefault<string>("osVersion");

            Db.Query().CommandText("writeHardwareInfo")
                .SetParameter("@accountID", request.Session.AccountId)
                .SetParameter("@gfxcard", gfxcard)
                .SetParameter("@gfxdriver", gfxdriver)
                .SetParameter("@gfxvendorid", gfxvendorid)
                .SetParameter("@gfxdeviceid", gfxdeviceid)
                .SetParameter("@gfxdriverversion", gfxdriverversion)
                .SetParameter("@pixelshader", pixelshader)
                .SetParameter("@vertexshader", vertexshader)
                .SetParameter("@maxtexturex", maxtexturex)
                .SetParameter("@maxtexturey", maxtexturey)
                .SetParameter("@osversion", osversion)
                .ExecuteNonQuery();
        }
    }
}