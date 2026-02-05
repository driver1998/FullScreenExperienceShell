using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevHome.Common
{
    public class DevHomeResources : ResourceDictionary
    {
        public DevHomeResources() : base()
        {
            MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("ms-appx:///DevHome.Common/Styles/LayoutSizes.xaml")
            });

            MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("ms-appx:///DevHome.Common/Styles/Thickness.xaml")
            });

            MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("ms-appx:///DevHome.Common/Styles/TextBlock.xaml")
            });

            MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("ms-appx:///DevHome.Common/Styles/FontSizes.xaml")
            });
        }
    }
}
