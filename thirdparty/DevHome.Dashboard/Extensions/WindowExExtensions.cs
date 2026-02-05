// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Extensions;

/// <summary>
/// This class add extension methods to the <see cref="Window"/> class.
/// </summary>
public static class WindowExExtensions
{
    public const int FilePickerCanceledErrorCode = unchecked((int)0x800704C7);

    /// <summary>
    /// Show an error message on the window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="content">Dialog content.</param>
    /// <param name="buttonText">Close button text.</param>
    public static async Task ShowErrorMessageDialogAsync(this Window window, string title, string content, string buttonText)
    {
        await window.ShowMessageDialogAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new TextBlock()
            {
                Text = content,
                TextWrapping = TextWrapping.WrapWholeWords,
            };
            dialog.PrimaryButtonText = buttonText;
        });
    }

    /// <summary>
    /// Generic implementation for creating and displaying a message dialog on
    /// a window.
    ///
    /// This extension method overloads <see cref="Window.ShowMessageDialogAsync"/>.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="action">Action performed on the created dialog.</param>
    public static async Task ShowMessageDialogAsync(this Window window, Action<ContentDialog> action)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = window.Content.XamlRoot,
        };
        action(dialog);
        await dialog.ShowAsync();
    }

    /// <summary>
    /// Gets the native HWND pointer handle for the window
    /// </summary>
    /// <param name="window">The window to return the handle for</param>
    /// <returns>HWND handle</returns>
    public static IntPtr GetWindowHandle(this Microsoft.UI.Xaml.Window window)
        => window is null ? throw new ArgumentNullException(nameof(window)) : WinRT.Interop.WindowNative.GetWindowHandle(window);
}
