using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevHome.Dashboard
{
    public class DashboardResources : ResourceDictionary
    {
        public DashboardResources() : base()
        {
            MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("ms-appx:///DevHome.Dashboard/Styles/Dashboard_ThemeResources.xaml")
            });
        }
    }
}
