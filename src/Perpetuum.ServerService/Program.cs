using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.ServerService
{
    [RunInstaller(true)]
    public class PerpServerInstaller : Installer
    {
        private ServiceInstaller serviceInstaller1;
        private ServiceProcessInstaller processInstaller;

        public PerpServerInstaller()
        {
            // Instantiate installer for process and service.
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller1 = new ServiceInstaller();

            // The service runs under the system account.
            processInstaller.Account = ServiceAccount.LocalSystem;

            // The service is started manually.
            serviceInstaller1.StartType = ServiceStartMode.Manual;

            // ServiceName must equal those on ServiceBase derived classes.
            serviceInstaller1.ServiceName = "Perpetuum Server";

            // Add installer to collection. Order is not important if more than one service.
            Installers.Add(serviceInstaller1);
            Installers.Add(processInstaller);
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new PerpServer()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
