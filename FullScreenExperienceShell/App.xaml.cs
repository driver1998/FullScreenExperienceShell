using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Extensions;
using DevHome.Services;
using DevHome.Services.Core.Contracts;
using DevHome.Services.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Microsoft.Win32.SafeHandles;
using Microsoft.Windows.AppLifecycle;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Accessibility;
using Windows.Win32;
using Windows.Win32.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FullScreenExperienceShell
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application, IApp
    {
        private Window? _window;

        public IHost Host
        {
            get;
        }

        public T GetService<T>() where T : class
        {
            return Host.GetService<T>();
        }
        private static SafeFileHandle? redirectEventHandle = null;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host.
                CreateDefaultBuilder().
                UseContentRoot(AppContext.BaseDirectory).
                UseDefaultServiceProvider((context, options) =>
                {
                    options.ValidateOnBuild = true;
                }).
                ConfigureServices((context, services) =>
                {
                    // Add Serilog logging for ILogger.
                    services.AddLogging(lb => lb.AddSerilog(dispose: true));

                    // Services
                    services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                    services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                    services.AddSingleton<IPackageDeploymentService, PackageDeploymentService>();
                    services.AddSingleton<IMicrosoftStoreService, MicrosoftStoreService>();

                    services.AddSingleton<IStringResource, StringResource>();
                    services.AddTransient<AdaptiveCardRenderingService>();

                    // Core Services
                    services.AddSingleton<IFileService, FileService>();

                    //// Main window: Allow access to the main window
                    //// from anywhere in the application.
                    //services.AddSingleton(_ => MainWindow);

                    //// DispatcherQueue: Allow access to the DispatcherQueue for
                    //// the main window for general purpose UI thread access.
                    services.AddSingleton(_ => DispatcherQueue.GetForCurrentThread());

                    // Dashboard
                    services.AddDashboard(context);
                }).
                Build();
        }


        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        [STAThread]
        static int Main(string[] args)
        {
            bool isRedirect = DecideRedirection();

            if (!isRedirect)
            {
                Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(
                        DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                });
            }

            return 0;
        }

        private static bool DecideRedirection()
        {
            bool isRedirect = false;
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            ExtendedActivationKind kind = args.Kind;
            AppInstance keyInstance = AppInstance.FindOrRegisterForKey("MySingleInstanceApp");

            if (keyInstance.IsCurrent)
            {
                keyInstance.Activated += OnActivated;
            }
            else
            {
                isRedirect = true;
                RedirectActivationTo(args, keyInstance);
            }

            return isRedirect;
        }

        private static void OnActivated(object? sender, AppActivationArguments args)
        {
            ExtendedActivationKind kind = args.Kind;
        }

        private static void RedirectActivationTo(AppActivationArguments args,
                                                 AppInstance keyInstance)
        {
            redirectEventHandle = PInvoke.CreateEvent(null, true, false, null);
            Task.Run(() =>
            {
                keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                PInvoke.SetEvent(redirectEventHandle);
            });

            uint CWMO_DEFAULT = 0;
            uint INFINITE = 0xFFFFFFFF;
            _ = PInvoke.CoWaitForMultipleObjects(
               CWMO_DEFAULT, INFINITE,
               [(HANDLE)redirectEventHandle.DangerousGetHandle()], out uint handleIndex);

            // Bring the window to the foreground
            Process process = Process.GetProcessById((int)keyInstance.ProcessId);
            PInvoke.SetForegroundWindow(new HWND(process.MainWindowHandle));
        }
    }
}
