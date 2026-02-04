using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        internal static readonly PROPERTYKEY PKEY_Tile_SuiteDisplayName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("86d40b4d-9069-443c-819a-2a54090dccec"),
            pid = 16U
        };
    }
}

namespace FullScreenExperienceShell
{
    public enum AppItemType
    {
        Container,
        Application
    }

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
        public partial WriteableBitmap? Icon { get; set; } = null;
        [ObservableProperty]
        public partial bool Expanded { get; set; } = false;
        [ObservableProperty]
        public partial ObservableCollection<AppItemViewModel> Children { get; set; } = [];

        public AppItemViewModel() { }
        public AppItemViewModel(AppItem item)
        {
            Name = item.Name;
            ParsingPath = item.ParsingPath;
            Suite = item.Suite;
            Type = item.Type;
            SetIcon(item.IconWidth, item.IconHeight, item.IconBytes);
        }

        public void SetIcon(int width, int height, byte[]? bytes)
        {
            if (bytes == null)
            {
                return;
            }
            Icon = new WriteableBitmap(width, height);
            using var stream = Icon.PixelBuffer.AsStream();
            stream.Write(bytes);
        }
    }

    public class AppItem
    {
        public string Name { get; set; } = "";
        public string ParsingPath { get; set; } = "";
        public string Suite { get; set; } = "";
        public AppItemType Type { get; set; }
        public int IconWidth { get; set; }
        public int IconHeight { get; set; }
        public byte[]? IconBytes { get; set; }
    }

    internal class AppsFolder
    {
        internal static AppItemViewModel? FindApplication(ObservableCollection<AppItemViewModel> appList, string parsingPath)
        {
            foreach (var item in appList)
            {
                if (item.Type == AppItemType.Container)
                {
                    var child = FindApplication(item.Children, parsingPath);
                    if (child != null)
                    {
                        return child;
                    }
                }
                else
                {
                    if (item.ParsingPath.Equals(parsingPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        internal static void AddApplication(ObservableCollection<AppItemViewModel> appList, AppItem item)
        {
            if (string.IsNullOrEmpty(item.Suite))
            {
                appList.Add(new AppItemViewModel(item));
            }
            else
            {
                var container = appList.Where(p => p.Type == AppItemType.Container && p.Name == item.Suite).FirstOrDefault();
                if (container == null)
                {
                    container = new AppItemViewModel
                    {
                        Name = item.Suite,
                        Suite = "Container",
                        Type = AppItemType.Container
                    };
                    appList.Add(container);
                }
                container.Children.Add(new AppItemViewModel(item));
            }
        }
        internal static void AppListFlatten(ObservableCollection<AppItemViewModel> appList)
        {
            List<(int, AppItemViewModel)> itemToRemove = [];

            foreach (var (i, item) in appList.Index())
            {
                if (item.Type == AppItemType.Container)
                {
                    if (item.Children.Count <= 1)
                    {
                        itemToRemove.Add((i, item));
                    }
                }
            }
            foreach (var (i, item) in itemToRemove)
            {
                appList.Remove(item);
                if (item.Children.Count > 0)
                {
                    appList.Insert(i, item.Children.First());
                }
            }
        }

        internal static (int width, int height, byte[] bytes) GetImage(IShellItem2 shellItem2)
        {
            IShellItemImageFactory? imageFactory = null;
            try
            {
                imageFactory = (IShellItemImageFactory)shellItem2;
                imageFactory.GetImage(new SIZE(96, 96), SIIGBF.SIIGBF_BIGGERSIZEOK | SIIGBF.SIIGBF_ICONONLY | SIIGBF.SIIGBF_SCALEUP, out var bitmap);
                
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
            }
            finally
            {
                if (imageFactory != null) Marshal.ReleaseComObject(imageFactory);
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
                if (desktopFolder != null) Marshal.ReleaseComObject(desktopFolder);
                if (appsFolderIdList != null) Marshal.FreeCoTaskMem((nint)appsFolderIdList);
            }
        }

        internal static unsafe string ShellItemGetStringProperty(IShellItem2 shellItem, PROPERTYKEY pkey)
        {
            shellItem.GetProperty(pkey, out PROPVARIANT pv);
            return Marshal.PtrToStringUni((nint)pv.Anonymous.Anonymous.Anonymous.pwszVal.Value) ?? "";
        }

        internal static unsafe void ShellFolderEnumItems(IShellFolder shellFolder, Action<IShellItem2> action)
        {
            IEnumIDList? enumIDList = null;
            Span<nint> itemIDPtrList = stackalloc nint[256];
            uint cnt = (uint)itemIDPtrList.Length;

            try
            {                
                fixed(nint* itemIDList = itemIDPtrList)
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
                            for (int i=0; i < fetched; i++)
                            {
                                Marshal.FreeCoTaskMem(itemIDPtrList[i]);
                            }
                        }

                        enumIDList.Next(cnt, (ITEMIDLIST**)itemIDList, &fetched);
                    }
                }
            }
            finally
            {
                if (enumIDList != null) Marshal.FinalReleaseComObject(enumIDList);
            }
        }

        internal static void GetApplications(List<AppItem> appList)
        {
            IShellFolder? appsFolder = null;
            try
            {
                appsFolder = GetAppsFolder();
                ShellFolderEnumItems(appsFolder, async (appShellItem) =>
                {
                    try
                    {
                        var parsingPath = ShellItemGetStringProperty(appShellItem, PInvoke.PKEY_ParsingName);
                        var name = ShellItemGetStringProperty(appShellItem, PInvoke.PKEY_ItemNameDisplay);
                        var suite = ShellItemGetStringProperty(appShellItem, PInvoke.PKEY_Tile_SuiteDisplayName);

                        var item = appList.Where(p => p.ParsingPath.Equals(parsingPath, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (item != null)
                        {
                            item.Name = name ?? parsingPath;
                            item.Suite = suite ?? "";
                        }
                        else
                        {
                            var (width, height, bytes) = GetImage(appShellItem);

                            item = new AppItem
                            {
                                Name = name ?? parsingPath,
                                Suite = suite ?? "",
                                ParsingPath = parsingPath,
                                Type = AppItemType.Application,
                                IconWidth = width,
                                IconHeight = height,
                                IconBytes = bytes
                            };
                            appList.Add(item);
                        }
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(appShellItem);
                    }
                    
                });
            }
            finally
            {
                if (appsFolder != null) Marshal.ReleaseComObject(appsFolder);
            }
        }

        internal static void InitApplicationList(List<AppItem> appList, ObservableCollection<AppItemViewModel> observableList)
        {
            foreach (var item in appList)
            {
                AppItemViewModel? app = FindApplication(observableList, item.ParsingPath);
                if (app != null)
                {
                    app.Name = item.Name;
                    app.Suite = item.Suite;
                    app.SetIcon(item.IconWidth, item.IconHeight, item.IconBytes);
                }
                else
                {
                    AddApplication(observableList, item);
                }
            }
            AppListFlatten(observableList);
        }
    }
}
