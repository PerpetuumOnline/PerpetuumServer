using Perpetuum.Bootstrapper;
using System;
using System.ComponentModel;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Autofac;
using Perpetuum.Host;
using System.Threading;
using Perpetuum.Log;

namespace Perpetuum.ServerService
{
    public partial class PerpServer : ServiceBase
    {

        public PerpServer()
        {
            InitializeComponent();
            Bootstrapper = new PerpetuumBootstrapper();            

            base.CanShutdown = true;
            base.CanStop = true;
        }

        PerpetuumBootstrapper Bootstrapper { get; set; }
        Autofac.IContainer container { get; set; }
        IHostStateService hostStateService { get; set; }

        protected override void OnStart(string[] args)
        {
            ServerStart();
        }

        public void ServerStart()
        {
            // assumes the server is in the default installation directory.
            string gameroot = Properties.Settings.Default.GameRoot;

            try
            {
                Bootstrapper.Init(gameroot);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                return;
            }

            container = Bootstrapper.GetContainer();
            hostStateService = container.Resolve<IHostStateService>();

            Task task = Task.Run((Action)StartServer);            

        }

        void StartServer()
        {
            Bootstrapper.Start();
            Bootstrapper.WaitForStop(); // this blocks !            
            base.Stop(); // must call or the service will hang.
        }

        void StopServer()
        {
            // if we are online. stop.
            if (hostStateService.State == HostState.Online)
            {
                // state change from online => stopping
                Bootstrapper.Stop();
            }
            // wait until we are stopped. (off)
            while (hostStateService.State != HostState.Off)
            {
                // we need to wait for a clean shutdown. Windows... Please wait for us :)
                RequestAdditionalTime(10000); // ask for 10 seconds. usually is not required.
            }

            Thread.Sleep(10000); // wait 10 seconds for the logging stuff to flush
        }

        // SCM stopping or server rebooting (?)
        protected override void OnStop()
        {
            StopServer();
        }

        // this is called when the system is SHUT DOWN. not when it's rebooting ??
        // either way we need to exit cleanly if possible.
        protected override void OnShutdown()
        {
            StopServer();
        }

    }
}
