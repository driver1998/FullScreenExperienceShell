using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace FullScreenExperienceShell
{
    public partial class BooleanValueConverter : IValueConverter
    {
        public object? TrueValue { get; set; }
        public object? FalseValue { get; set; }
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public partial class SuiteItemMarginConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            bool isSuiteItem = !string.IsNullOrEmpty(value as string);
            if (isSuiteItem)
            {
                return new Windows.UI.Xaml.Thickness(36, 0, 0, 0);
            }
            else
            {
                return new Windows.UI.Xaml.Thickness(0);
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
