// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DevHome.Common.Contracts;
using Windows.Storage;

namespace DevHome.Common.Services;

public class LocalSettingsService : ILocalSettingsService
{
    public async Task<bool> HasSettingAsync(string key)
    {        
        return ApplicationData.Current.LocalSettings.Values.ContainsKey(key);     
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
        {
            return await Helpers.Json.ToObjectAsync<T>((string)obj);
        }

        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {        
        ApplicationData.Current.LocalSettings.Values[key] = await Helpers.Json.StringifyAsync(value!);     
    }
}
