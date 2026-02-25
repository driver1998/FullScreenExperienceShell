using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TinyPinyin;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRT;

namespace FullScreenExperienceShell
{
    [GeneratedBindableCustomProperty]
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial ObservableCollection<AppItemViewModel> Applications { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<AppItemViewModel> AppList { get; set; } = [];

        [ObservableProperty]
        public partial List<AppItemGroup> Groups { get; set; } = [];

        [ObservableProperty]
        public partial AppItemGroup? CurrentGroup { get; set; }
    }

    [GeneratedBindableCustomProperty]
    public partial class AppItemGroup
    {
        public string? GroupKey { get; set; }
        
        public ObservableCollection<AppItemViewModel>? GroupItems { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel = new();       

        public MainPage()
        {
            InitializeComponent();
            this.DataContext = ViewModel;
        }

        [RelayCommand]
        public async Task RefreshAppList()
        {
            List<AppItem> appList = [];
            await Task.Run(() =>
            {
                appList = AppsFolder.GetApplications();
            });

            AppsFolder.InitApplicationList(appList, ViewModel.Applications);
            ViewModel.AppList = AppsFolder.InitSuiteView(ViewModel.Applications);            
            ViewModel.Groups = ViewModel.AppList.GroupBy(p => p.GroupKey)
                .Select(g => new AppItemGroup { GroupKey = g.Key, GroupItems = [.. g.ToList()] })
                .OrderBy(g => g.GroupKey)
                .ToList();

            Debug.WriteLine(cvsGroups.View.Count);
            await AppsFolder.LoadAllIconsAsync(ViewModel.Applications);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAppListCommand.ExecuteAsync(null);
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var appItem = e.ClickedItem as AppItemViewModel;
            if (appItem != null)
            {
                if (appItem.Type == AppItemType.Container)
                {
                    appItem.Expanded = !appItem.Expanded;
                    var group = ViewModel.Groups.Where(p => p.GroupKey == appItem.GroupKey).FirstOrDefault();
                    var index = group?.GroupItems?.IndexOf(appItem) ?? -1;
                    if (group != null && index > -1)
                    {
                        if (appItem.Expanded)
                        {
                            foreach (var (i, item) in appItem.Children.Index())
                            {
                                group.GroupItems?.Insert(index + i + 1, item);
                            }
                        }
                        else
                        {
                            var itemsToRemove = group.GroupItems?.Where(p => p.Suite == appItem.Name).ToList() ?? [];
                            foreach (var item in itemsToRemove)
                            {
                                group.GroupItems?.Remove(item);
                            }
                        }

                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(appItem.ParsingPath))
                    {
                        return;
                    }

                    var processStartInfo = new ProcessStartInfo
                    {
                        
                        FileName = "explorer.exe",
                        Arguments = $@"/e,shell:appsfolder\{appItem.ParsingPath}"
                    };
                    Process.Start(processStartInfo);
                }
            }
        }

        private void SemanticZoom_ViewChangeStarted(object sender, SemanticZoomViewChangedEventArgs e)
        {
            if (e.IsSourceZoomedInView == false)
            {
                e.DestinationItem.Item = e.SourceItem.Item;
            }
            else
            {
                ViewModel.CurrentGroup = e.SourceItem.Item as AppItemGroup;
            }
        }
    }
}
