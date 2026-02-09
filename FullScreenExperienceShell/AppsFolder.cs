using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        internal static readonly PROPERTYKEY PKEY_Tile_SuiteDisplayName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("86d40b4d-9069-443c-819a-2a54090dccec"),
            pid = 16U
        };

        internal static readonly PROPERTYKEY PKEY_AppUserModel_PackageFamilyName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("9f4c2855-9f79-4b39-a8d0-e1d42de1d5f3"),
            pid = 17U
        };

        internal static readonly PROPERTYKEY PKEY_AppUserModel_PackageFullName = new PROPERTYKEY
        {
            fmtid = Guid.Parse("9f4c2855-9f79-4b39-a8d0-e1d42de1d5f3"),
            pid = 21U
        };

        internal static readonly PROPERTYKEY PKEY_AppUserModel_PackageInstallPath = new PROPERTYKEY
        {
            fmtid = Guid.Parse("9f4c2855-9f79-4b39-a8d0-e1d42de1d5f3"),
            pid = 15U
        };

        internal static readonly PROPERTYKEY PKEY_Tile_SmallLogoPath = new PROPERTYKEY
        {
            fmtid = Guid.Parse("{86d40b4d-9069-443c-819a-2a54090dccec}"),
            pid = 2U
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
        public partial BitmapSource? Icon { get; set; } = null;        
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

            if (!string.IsNullOrEmpty(item.IconLocation))
            {
                SetIcon(item.IconLocation);
            }
            else
            {
                SetIcon(item.IconWidth, item.IconHeight, item.IconBytes);
            }
        }

        public void SetIcon(string url)
        {
            Icon = new BitmapImage(new Uri(url));
        }
        public void SetIcon(int width, int height, byte[]? bytes)
        {
            if (width == 0 || height == 0 || bytes == null)
            {
                return;
            }
            var bmp = new WriteableBitmap(width, height);
            using var stream = bmp.PixelBuffer.AsStream();
            stream.Write(bytes);
            Icon = bmp;
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
        public string IconLocation { get; set; } = "";
    }

    internal partial class AppsFolder
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
                    var onlyChild = item.Children.First();
                    onlyChild.Suite = "";
                    appList.Insert(i, onlyChild);
                }
            }
        }

        internal static void AppListSort(ObservableCollection<AppItemViewModel> appList)
        {
            foreach(var item in appList.Where(p => p.Type == AppItemType.Container))
            {
                AppListSort(item.Children);
            }
            
            var sorted = appList.OrderBy(p => p.Name).ToList();
            sorted.ForEach(p =>
            {
                appList.Remove(p);
                appList.Insert(sorted.IndexOf(p), p);
            });
        }


        internal static void GetImage(AppItem appItem, IShellItem2 shellItem2, IExtractIconW extractIcon)
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

                var indirectString = $"@{{{packageFullName}?ms-resource:///Files/{smallLogoPath}}}";
                PInvoke.SHLoadIndirectString(indirectString, charBuf);
                var logoPath = new string(charBuf.SliceAtNull());

                if (string.IsNullOrEmpty(logoPath) && Path.Exists(staticLogoPath))
                {
                    logoPath = staticLogoPath;
                }
                appItem.IconLocation = logoPath;
            }
            else
            {

                var iconPath = string.Empty;
                int iconIndex = 0;
                try
                {
                    //Debug.WriteLine(appItem.Name);                    
                    unsafe
                    {
                        extractIcon.GetIconLocation(0, charBuf, out iconIndex, out var flags);
                        iconPath = new string(charBuf.SliceAtNull());

                        if (string.IsNullOrEmpty(iconPath))
                        {
                            return;
                        }

                        fixed (char* str = charBuf)
                        {
                            HICON largeIcon = HICON.Null;
                            ICONINFO iconInfo = new();                            
                            var hdc = PInvoke.CreateCompatibleDC(HDC.Null);                            
                            try
                            {
                                int width = 32;
                                int height = 32;
                                int bytesPerLine = width * 4;
                                int imageSizeBytes = bytesPerLine * height;

                                extractIcon.Extract(new PCWSTR(str), (uint)iconIndex, &largeIcon, null, (uint)width);
                                PInvoke.GetIconInfo(largeIcon, &iconInfo);

                                BITMAPINFO bmi;
                                bmi.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
                                bmi.bmiHeader.biWidth = width;
                                bmi.bmiHeader.biHeight = -height;
                                bmi.bmiHeader.biPlanes = 1;
                                bmi.bmiHeader.biBitCount = 32;
                                bmi.bmiHeader.biCompression = (uint)BI_COMPRESSION.BI_RGB;
                                var hBitmap = PInvoke.CreateDIBSection(hdc, &bmi, (uint)DIB_USAGE.DIB_RGB_COLORS, out var bits, null, 0);
                                if (!hBitmap.IsInvalid)
                                {
                                    var oldObj = PInvoke.SelectObject(hdc, hBitmap);
                                    PInvoke.DrawIconEx(hdc, 0, 0, largeIcon, width, height, 0, HBRUSH.Null, DI_FLAGS.DI_NORMAL);
                                    PInvoke.SelectObject(hdc, oldObj);
                                }

                                var srcSpan = new Span<byte>(bits, imageSizeBytes);
                                var bytes = new byte[imageSizeBytes];
                                srcSpan.CopyTo(bytes);

                                appItem.IconWidth = 32;
                                appItem.IconHeight = 32;
                                appItem.IconBytes = bytes;
                            }
                            finally
                            {
                                if (!iconInfo.hbmColor.IsNull) PInvoke.DeleteObject(iconInfo.hbmColor);
                                if (!iconInfo.hbmMask.IsNull) PInvoke.DeleteObject(iconInfo.hbmMask);
                                if (!largeIcon.IsNull) PInvoke.DestroyIcon(largeIcon);
                                PInvoke.DeleteDC(hdc);
                            }
                        }
                    }


                }
                catch { }
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

        internal static unsafe void ShellFolderEnumItems(IShellFolder shellFolder, Action<IShellItem2, IExtractIconW> action)
        {
            IEnumIDList? enumIDList = null;
            Span<nint> itemIDPtrList = stackalloc nint[256];
            Span<char> iconPath = stackalloc char[65535];
            uint cnt = (uint)itemIDPtrList.Length;

            var IID_IExtractIconW = typeof(IExtractIconW).GUID;
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
                                shellFolder.GetUIObjectOf(HWND.Null, 1, (ITEMIDLIST**)&itemIDList[i], &IID_IExtractIconW, null, out var ppv);
                                action?.Invoke((IShellItem2)ppsi, (IExtractIconW)ppv);
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
                ShellFolderEnumItems(appsFolder, async (appShellItem, extractIcon) =>
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
                            GetImage(item, appShellItem, extractIcon);
                        }
                        else
                        {
                            item = new AppItem
                            {
                                Name = name ?? parsingPath,
                                Suite = suite ?? "",
                                ParsingPath = parsingPath,
                                Type = AppItemType.Application,
                            };
                            GetImage(item, appShellItem, extractIcon);
                            appList.Add(item);
                        }
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(appShellItem);
                        Marshal.FinalReleaseComObject(extractIcon);
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
                    if (!string.IsNullOrEmpty(item.IconLocation))
                    {
                        app.SetIcon(item.IconLocation);
                    }
                    else
                    {
                        app.SetIcon(item.IconWidth, item.IconHeight, item.IconBytes);
                    }
                }
                else
                {
                    AddApplication(observableList, item);
                }
            }
            AppListFlatten(observableList);
            AppListSort(observableList);
        }
    }
}
