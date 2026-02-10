using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TinyPinyin;
using Windows.Data.Text;
using Windows.Foundation.Collections;

namespace FullScreenExperienceShell
{

    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial ObservableCollection<AppItemViewModel> Applications { get; set; } = [];

        [ObservableProperty]
        public partial List<AppItemGroup> Groups { get; set; } = [];

        public List<AppItem> AppItems = [];
    }

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
        }

        private string GetGroupKey(string Name)
        {
            string firstCharStr = string.Empty;

            var firstChar = Name[0];
            if (char.IsHighSurrogate(firstChar))
            {
                firstCharStr = Name.Substring(0, 2);
            }
            else
            {
                firstCharStr = Name[0].ToString();

                if (firstChar >= '0' && firstChar <= '9')
                {                    
                    return "#";
                } 
                else if ((firstChar >= 'a' && firstChar <= 'z') || (firstChar >= 'A' && firstChar <= 'Z')) 
                {
                    return firstCharStr.ToUpper();
                }
                else if (firstChar <= 127)
                {
                    return "&";
                }
            }

            if (CultureInfo.CurrentCulture.Name == "zh-CN")
            {
                var pinyin = PinyinHelper.GetPinyin(firstCharStr);
                if (string.IsNullOrEmpty(pinyin))
                {
                    return "🌐";
                }
                else
                {
                    return pinyin[0].ToString().ToUpper();
                }
            }
            else
            {
                // 其它语言环境直接返回默认的分组键
                return "🌐";
            }
        }

        [RelayCommand]
        public async Task RefreshAppList()
        {
            await Task.Run(() =>
            {
                AppsFolder.GetApplications(ViewModel.AppItems);
            });

            await AppsFolder.InitApplicationList(ViewModel.AppItems, ViewModel.Applications);
            ViewModel.Groups = ViewModel.Applications.GroupBy(p => GetGroupKey(p.Name))
                .Select(g => new AppItemGroup { GroupKey = g.Key, GroupItems = [.. g.ToList()] })
                .OrderBy(g => g.GroupKey)
                .ToList();
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

                    var groupKey = GetGroupKey(appItem.Name);
                    var group = ViewModel.Groups.Find(p => p.GroupKey == groupKey);
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
                            foreach(var item in itemsToRemove)
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
                        FileName = $@"shell:appsfolder\{appItem.ParsingPath}",
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                }
            }
        }
    }
}
