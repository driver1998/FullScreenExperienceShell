using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FullScreenExperienceShell
{

    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial ObservableCollection<AppItemViewModel> Applications { get; set; } = [];

        public List<AppItem> AppItems = [];
    }

    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel = new();

        public MainPage()
        {
            InitializeComponent();
        }

        [RelayCommand]
        public async Task RefreshAppList()
        {
            await Task.Run(() =>
            {
                AppsFolder.GetApplications(ViewModel.AppItems);
            });

            AppsFolder.InitApplicationList(ViewModel.AppItems, ViewModel.Applications);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAppListCommand.ExecuteAsync(null);
        }

        private void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            var appItem = (AppItemViewModel)args.InvokedItem;
            if (appItem.Type == AppItemType.Container)
            {
                appItem.Expanded = !appItem.Expanded;
            }
            else
            {
                if (string.IsNullOrEmpty(appItem.ParsingPath))
                {
                    return;
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = $@"shell:appsfolder\{appItem.ParsingPath}",
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            }
        }

    }
}
