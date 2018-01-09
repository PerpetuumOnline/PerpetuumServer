using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Autofac;
using Autofac.Builder;
using Perpetuum.AdminTool.ViewModel;
using Perpetuum.Services.Relay;

namespace Perpetuum.AdminTool
{
    public partial class App : Application
    {
        public App()
        {
            Trace.WriteLine("autofac starts");
            SetupContainer();
            Trace.WriteLine("autofac done");
        }

        public static Dictionary<string,string> CommandLineArgs = new Dictionary<string, string>();

        // format: 
        // -e a:\work\PerpetuumServer\bin\x64\Release\Perpetuum.Server\Perpetuum.Server.exe
        // -g a:\work\server\genxy
        // -f 1
        private void App_Startup(object sender,StartupEventArgs e)
        {
            // e.Args is never null
            var cmdArgs = e.Args;
            if (cmdArgs.Length == 0) return;

            for (var i=0; i < e.Args.Length; i++)
            {
                var arg = cmdArgs[i];
                Trace.WriteLine(arg);
                var keyStr = "";
                var valStr = "";
                if (arg.Length == 2 && arg.StartsWith("-"))
                {
                    keyStr = arg;
                    if (cmdArgs.Length-1 < i+1) break;
                    valStr = cmdArgs[i + 1];
                    CommandLineArgs[keyStr] = valStr;
                    i++;
                }
                
            }
            Trace.WriteLine($"{e.Args.Length} commandline args processed");
        }



        // autofac setup
        public void SetupContainer()
        {
            var builder = new ContainerBuilder();

            IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterViewModel<T>() where T : IViewModel
            {
                return builder.RegisterType<T>().Named<IViewModel>(typeof(T).Name);
            }

            builder.Register<Locator.Resolver>(x =>
            {
                var ctx = x.Resolve<IComponentContext>();
                return (name => !ctx.IsRegisteredWithName<IViewModel>(name) ? null : ctx.ResolveNamed<IViewModel>(name));
            });

            builder.RegisterType<Locator>();

            RegisterViewModel<ServerInfoViewModel>();
            RegisterViewModel<ConnectionStateViewModel>();
            RegisterViewModel<LocalServerStateViewModel>();

            builder.Register(c =>
                {
                    var a  = new AdminCreds();
                    a.LoadFromFile();
                    return a;
                })
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<MainWindow>();
            builder.RegisterType<LedControl>().SingleInstance();
            builder.RegisterType<LoggerControl>().SingleInstance();
            builder.RegisterType<StatusTextControl>().SingleInstance();
            builder.RegisterType<AuthPage>().SingleInstance();
            builder.RegisterType<AccountsPage>().SingleInstance();
            builder.RegisterType<AccountEditControl>().SingleInstance();
            builder.RegisterType<AccountCreateControl>().SingleInstance();
            builder.RegisterType<NetworkHandler>().SingleInstance();
            builder.RegisterType<AccountsHandler>().SingleInstance();
            builder.RegisterType<LocalServerPage>().SingleInstance();

            builder.RegisterType<AccountInfo>();
            builder.RegisterType<ServerInfoPage>().SingleInstance();
            builder.RegisterType<ServerInfoViewModel>().SingleInstance();
            builder.RegisterType<ServerInfo>();
            builder.RegisterType<LogHandler>().SingleInstance();
            builder.RegisterType<AccountFormValidator>();

            builder.RegisterType<LocalServerRunner>().SingleInstance();

            builder.RegisterType<AccountInfoFactory>().As<IAccountInfoFactory>();

            var container = builder.Build();
            using (container.BeginLifetimeScope())
            {
                Resources["Locator"] = container.Resolve<Locator>();

                var adminCreds = container.Resolve<AdminCreds>();
                var led = container.Resolve<LedControl>();
                var statusText = container.Resolve<StatusTextControl>();
                var logger = container.Resolve<LoggerControl>();

                var logHandler = container.Resolve<LogHandler>();
                var serverInfoView = container.Resolve<ServerInfoViewModel>();

                var networkHandler = container.Resolve<NetworkHandler>();
                var accountsHandler = container.Resolve<AccountsHandler>();

                var localServerRunner = container.Resolve<LocalServerRunner>();
                localServerRunner.Init(logHandler);
                var localServerPage = container.Resolve<LocalServerPage>();
                localServerPage.Init(logHandler, localServerRunner);

                var authPage = container.Resolve<AuthPage>();
                authPage.Init(logHandler, adminCreds, networkHandler, accountsHandler);

                var accountFromValidator = container.Resolve<AccountFormValidator>();
                accountFromValidator.Init(logHandler);

                var accountEditControl = container.Resolve<AccountEditControl>();
                accountEditControl.Init(accountFromValidator, accountsHandler);

                var accountCreateControl = container.Resolve<AccountCreateControl>();

                var accountsPage = container.Resolve<AccountsPage>();
                accountsPage.Init(logHandler, accountEditControl, accountCreateControl, accountsHandler, adminCreds);
                accountCreateControl.Init(accountFromValidator, accountsHandler, accountsPage.CancelAccountCreate);

                //server info
                var serverInfoPage = container.Resolve<ServerInfoPage>();
                serverInfoPage.serverInfoGrid.DataContext = serverInfoView;
                serverInfoPage.Init(logHandler, serverInfoView, networkHandler);


                //main window
                var mainWindow = container.Resolve<MainWindow>();

                //events
                accountsHandler.AccountInfosDisplay += accountsPage.DisplayAccounts;
                authPage.inputStack.DataContext = adminCreds;
                accountsPage.FilterChanged += accountsHandler.HandleFilterChange;
                accountsPage.SelectionChanged += accountsHandler.HandleSelectedInfoChange;
                networkHandler.LoginStateChanged += mainWindow.SessionOnLoginStateChanged;
                networkHandler.ConnectionStateChanged += mainWindow.HandleConnectionStateChange;




                //build up gui
                //main window
                mainWindow.ledRoot.Children.Add(led);
                mainWindow.statusTextRoot.Children.Add(statusText);
                mainWindow.logRoot.Children.Add(logger);

                //tab pages
                mainWindow.localServerPageRoot.Children.Add(localServerPage);
                mainWindow.authPageRoot.Children.Add(authPage);
                mainWindow.accountsPageRoot.Children.Add(accountsPage);
                mainWindow.serverInfoPageRoot.Children.Add(serverInfoPage);

                mainWindow.Init(logHandler,accountsHandler,networkHandler,authPage,serverInfoPage,accountsPage, localServerPage);

                mainWindow.Show();
                 

            }

        }

    }
}
