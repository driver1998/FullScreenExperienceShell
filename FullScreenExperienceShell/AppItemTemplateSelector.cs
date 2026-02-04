using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace FullScreenExperienceShell
{
    public class AppItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ContainerTemplate { get; set; }
        public DataTemplate? ApplicationTemplate { get; set; }
        protected override DataTemplate? SelectTemplateCore(object item)
        {
            var explorerItem = (AppItemViewModel)item;
            if (explorerItem.Type == AppItemType.Container) return ContainerTemplate;

            return ApplicationTemplate;
        }
    }
}
