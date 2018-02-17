using Perpetuum.Bootstrapper;
using System;
using System.ComponentModel;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Autofac;
using Perpetuum.Host;
using System.Threading;

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

        protected override void OnStart(string[] args)
        {
            // assumes the server is in the default installation directory.
            string gameroot = Properties.Settings.Default.GameRoot;

            try
            {
                Bootstrapper.Init(gameroot);
            }
            catch
            {
                // we failed to init. no idea why. we need to log this.
                // usual reasons are db connection failures because we are running as a user with no privs.
                return;
            }

            // start a task for our server.
            Task task = Task.Run((Action)StartServer);
           
        }

        protected override void OnStop()
        {
            StopServer();
        }

        // windows requests this when shutting down.
        // need to make sure this is called.
        protected override void OnShutdown()
        {
            StopServer();
        }

        public void StartServer()
        {            
            Bootstrapper.Start();
            Bootstrapper.WaitForStop();
        }

        private void StopServer()
        {
            Autofac.IContainer _container = Bootstrapper.GetContainer();

            Bootstrapper.Stop();

            // get the host container and check it's status.
            // if we are running still or stopping we ask windows to not kill us.
            var s = _container.Resolve<IHostStateService>();
            while (s.State == HostState.Online || s.State == HostState.Stopping)
            {
                RequestAdditionalTime(10000); // ask for more time. Hope we get it.
            }
            Thread.Sleep(10000); // wait 10 seconds for their logging process to finish.
        }
    }
}
