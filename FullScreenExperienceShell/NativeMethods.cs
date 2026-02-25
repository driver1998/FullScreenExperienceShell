using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell.PropertiesSystem;


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

namespace Windows.Win32.UI.Shell
{

    [Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), GeneratedComInterface()]
    internal unsafe partial interface IShellItem2 : IShellItem
    {
        void GetPropertyStore(GETPROPERTYSTOREFLAGS flags, global::System.Guid* riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        void GetPropertyStoreWithCreateObject(GETPROPERTYSTOREFLAGS flags, [MarshalAs(UnmanagedType.Interface)] object punkCreateObject, global::System.Guid* riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        void GetPropertyStoreForKeys(PROPERTYKEY* rgKeys, uint cKeys, GETPROPERTYSTOREFLAGS flags, global::System.Guid* riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        void GetPropertyDescriptionList(PROPERTYKEY* keyType, global::System.Guid* riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        void Update(Windows.Win32.System.Com.IBindCtx pbc);
        void GetProperty(PROPERTYKEY* key, PROPVARIANT* ppropvar);
        void GetCLSID(PROPERTYKEY* key, global::System.Guid* pclsid);
        void GetFileTime(PROPERTYKEY* key, global::System.Runtime.InteropServices.ComTypes.FILETIME* pft);
        void GetInt32(PROPERTYKEY* key, out int pi);
        void GetString(PROPERTYKEY* key, PWSTR* ppsz);
        void GetUInt32(PROPERTYKEY* key, out uint pui);
        void GetUInt64(PROPERTYKEY* key, out ulong pull);
        void GetBool(PROPERTYKEY* key, BOOL* pf);
    }
}
