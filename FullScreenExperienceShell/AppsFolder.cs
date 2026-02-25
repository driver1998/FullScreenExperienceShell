using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using TinyPinyin;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using WinRT;

namespace FullScreenExperienceShell
{
    public enum AppItemType
    {
        Container,
        Application
    }

    [GeneratedBindableCustomProperty]
    public partial class AppItemViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; set; } = "";
        [ObservableProperty]
        public partial string ParsingPath { get; set; } = "";
        [ObservableProperty]
        public partial string Suite { get; set; } = "";
        [ObservableProperty]
        public partial AppItemType Type { get; set; }
        [ObservableProperty]
        public partial BitmapSource? Icon { get; set; } = null;
        [ObservableProperty]
        public partial bool Expanded { get; set; } = false;

        public string IconPath { get; set; } = "";

        public string SortKey => (Suite ?? "") + "\\" + Name;

        [ObservableProperty]
        public partial string GroupKey { get; set; } = "";

        [ObservableProperty]
        public partial ObservableCollection<AppItemViewModel> Children { get; set; } = [];

        public AppItemViewModel() { }

        public AppItemViewModel(AppItem item)
        {
            this.Name = item.Name;
            this.Suite = item.Suite;
            this.ParsingPath = item.ParsingPath;
            this.Type = item.Type;
            this.IconPath = item.IconPath;
            this.GroupKey = AppsFolder.GetGroupKey(item.Name);
        }

        public async Task LoadIconAsync()
        {
            if (!string.IsNullOrEmpty(IconPath))
            {
                Icon = new BitmapImage(new Uri(IconPath));
            }
            else
            {
                var (width, height, bytes) = await AppsFolder.GetThumbnailBytes(ParsingPath);
                if (width == 0 || height == 0 || bytes == null)
                {
                    return;
                }
                var bmp = new WriteableBitmap(width, height);
                using var stream = bmp.PixelBuffer.AsStream();
                await stream.WriteAsync(bytes);
                Icon = bmp;
            }
        }
    }

    public class AppItem
    {
        public string Name { get; set; } = "";
        public string ParsingPath { get; set; } = "";
        public string Suite { get; set; } = "";
        public AppItemType Type { get; set; }
        public string IconPath { get; set; } = "";
    }

    internal partial class AppsFolder
    {

        internal static string GetGroupKey(string Name)
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

        internal static Task<(int width, int size, byte[] bytes)> GetThumbnailBytes(string parsingPath)
        {
            return Task.Run<(int, int, byte[])>(() =>
            {
                var path = $@"shell:appsfolder\{parsingPath}";
                PInvoke.SHCreateItemFromParsingName(path, null, typeof(IShellItemImageFactory).GUID, out var ppv);
                var imageFactory = (IShellItemImageFactory)ppv;
                imageFactory.GetImage(new SIZE(32, 32), SIIGBF.SIIGBF_BIGGERSIZEOK | SIIGBF.SIIGBF_RESIZETOFIT, out var bitmap);

                using (bitmap)
                {
                    unsafe
                    {
                        BITMAP bmp;
                        Span<byte> span = new Span<byte>(&bmp, Marshal.SizeOf<BITMAP>());
                        PInvoke.GetObject(bitmap, span);

                        int size = bmp.bmWidthBytes * bmp.bmHeight;
                        var bytes = new byte[size];
                        PInvoke.GetBitmapBits(bitmap, bytes);
                        return (bmp.bmWidth, bmp.bmHeight, bytes);
                    }
                }
            });

        }

        internal static void GetPackagedAppIcon(AppItem appItem, IShellItem2 shellItem2)
        {
            var packageFamilyName = ShellItemGetStringProperty(shellItem2, PInvoke.PKEY_AppUserModel_PackageFamilyName);
            var packaged = !string.IsNullOrEmpty(packageFamilyName);
            Span<char> charBuf = stackalloc char[65536];

            if (packaged)
            {
                var packageFullName = ShellItemGetStringProperty(shellItem2, PInvoke.PKEY_AppUserModel_PackageFullName);
                var packageInstallPath = ShellItemGetStringProperty(shellItem2, PInvoke.PKEY_AppUserModel_PackageInstallPath);
                var smallLogoPath = ShellItemGetStringProperty(shellItem2, PInvoke.PKEY_Tile_SmallLogoPath);

                var staticLogoPath = Path.Combine(packageInstallPath, smallLogoPath);

                var logoPath = string.Empty;

                var indirectString = $"@{{{packageFullName}?ms-resource:///Files/{smallLogoPath}}}?theme=light";
                PInvoke.SHLoadIndirectString(indirectString, charBuf);
                logoPath = new string(charBuf.SliceAtNull());

                if (string.IsNullOrEmpty(logoPath) || !Path.Exists(logoPath))
                {
                    var indirectStringUnthemed = $"@{{{packageFullName}?ms-resource:///Files/{smallLogoPath}}}";
                    PInvoke.SHLoadIndirectString(indirectStringUnthemed, charBuf);
                    logoPath = new string(charBuf.SliceAtNull());
                }

                if (string.IsNullOrEmpty(logoPath) && Path.Exists(staticLogoPath))
                {
                    logoPath = staticLogoPath;
                }
                appItem.IconPath = logoPath;
            }
        }

        internal static unsafe IShellFolder GetAppsFolder()
        {
            HRESULT hr;
            IShellFolder? desktopFolder = null;
            ITEMIDLIST* appsFolderIdList = null;
            try
            {
                hr = PInvoke.SHGetDesktopFolder(out desktopFolder);
                Marshal.ThrowExceptionForHR(hr);

                hr = PInvoke.SHGetKnownFolderIDList(PInvoke.FOLDERID_AppsFolder, (uint)KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, out appsFolderIdList);
                Marshal.ThrowExceptionForHR(hr);

                desktopFolder.BindToObject(*appsFolderIdList, null, typeof(IShellFolder).GUID, out var ppv);
                return (IShellFolder)ppv;
            }
            finally
            {
                if (appsFolderIdList != null) Marshal.FreeCoTaskMem((nint)appsFolderIdList);
            }
        }

        internal static unsafe string ShellItemGetStringProperty(IShellItem2 shellItem, PROPERTYKEY pkey)
        {
            PROPVARIANT variant;
            shellItem.GetProperty(&pkey, &variant);
            return new string(variant.Anonymous.Anonymous.Anonymous.pwszVal.AsSpan().SliceAtNull());
        }

        internal static unsafe void ShellFolderEnumItems(IShellFolder shellFolder, Action<IShellItem2> action)
        {
            IEnumIDList? enumIDList = null;
            Span<nint> itemIDPtrList = stackalloc nint[256];
            Span<char> iconPath = stackalloc char[65535];
            uint cnt = (uint)itemIDPtrList.Length;

            fixed (nint* itemIDList = itemIDPtrList)
            {
                shellFolder.EnumObjects(HWND.Null, 0, out enumIDList);

                uint fetched = 0;
                enumIDList.Next(cnt, (ITEMIDLIST**)itemIDList, &fetched);
                while (fetched > 0)
                {
                    try
                    {
                        PInvoke.SHCreateShellItemArray(null, shellFolder, fetched, (ITEMIDLIST**)itemIDList, out IShellItemArray itemArray);
                        for (uint i = 0; i < fetched; i++)
                        {

                            itemArray.GetItemAt(i, out IShellItem ppsi);
                            action?.Invoke((IShellItem2)ppsi);
                        }

                    }
                    finally
                    {
                        for (int i = 0; i < fetched; i++)
                        {
                            Marshal.FreeCoTaskMem(itemIDPtrList[i]);
                        }

                    }

                    enumIDList.Next(cnt, (ITEMIDLIST**)itemIDList, &fetched);
                }
            }
        }

        internal static List<AppItem> GetApplications()
        {
            var appList = new List<AppItem>();
            IShellFolder? appsFolder = null;            
            appsFolder = GetAppsFolder();
            ShellFolderEnumItems(appsFolder, async (appShellItem) =>
            {
                var parsingPath = ShellItemGetStringProperty(appShellItem, PInvoke.PKEY_ParsingName);
                var name = ShellItemGetStringProperty(appShellItem, PInvoke.PKEY_ItemNameDisplay);
                var suite = ShellItemGetStringProperty(appShellItem, PInvoke.PKEY_Tile_SuiteDisplayName);

                var item = new AppItem
                {
                    Name = name ?? parsingPath,
                    Suite = suite ?? "",
                    ParsingPath = parsingPath,
                    Type = AppItemType.Application,
                };
                GetPackagedAppIcon(item, appShellItem);
                appList.Add(item);
            });            
            return appList;
        }

        internal static void AppListSort(ObservableCollection<AppItemViewModel> appList)
        {
            var sorted = appList.OrderBy(p => p.SortKey).ToList();
            sorted.ForEach(p =>
            {
                appList.Remove(p);
                appList.Insert(sorted.IndexOf(p), p);
            });
        }

        internal static void InitApplicationList(List<AppItem> appList, ObservableCollection<AppItemViewModel> observableList)
        {
            var lookup = observableList.ToLookup(p => p.ParsingPath);
            foreach (var item in appList)
            {
                AppItemViewModel? app = lookup[item.ParsingPath].FirstOrDefault();
                if (app != null)
                {
                    app.Name = item.Name;
                    app.Suite = item.Suite;
                    app.IconPath = item.IconPath;
                    app.GroupKey = GetGroupKey(item.Name);
                }
                else
                {
                    app = new AppItemViewModel(item);
                    observableList.Add(app);
                }
            }
            AppListSort(observableList);
        }

        internal static ObservableCollection<AppItemViewModel> InitSuiteView(ObservableCollection<AppItemViewModel> appList)
        {
            var list = new ObservableCollection<AppItemViewModel>(appList);
            var suiteLookup = list.Where(p => !string.IsNullOrEmpty(p.Suite)).GroupBy(p => p.Suite);

            foreach (var group in suiteLookup)
            {

                if (group.Count() == 1)
                {
                    group.First().Suite = "";
                }
                else
                {
                    foreach (var item in group)
                    {
                        list.Remove(item);
                    }
                    list.Add(new AppItemViewModel
                    {
                        Type = AppItemType.Container,
                        Name = group.Key,
                        GroupKey = GetGroupKey(group.Key),
                        Children = [.. group]
                    });
                }

            }
            return list;
        }

        internal static Task LoadAllIconsAsync(ObservableCollection<AppItemViewModel> appList) =>
            Task.WhenAll(appList.Select(p => p.LoadIconAsync()));
    }
}
