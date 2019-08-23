using Mapperdom.Models;
using Mapperdom.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Mapperdom.Views
{
    public sealed partial class PickSideDialog : ContentDialog
    {
        public MainViewModel OriginalViewModel { get; } = new MainViewModel();
        public PickSideViewModel ViewModel { get; }
        public PickSideDialog(MainViewModel originalViewModel, bool canJoinSide, WarSide sideToRival, ObservableCollection<WarSide> sidesList, Nation newNation)
        {
            this.InitializeComponent();
            ViewModel = new PickSideViewModel(canJoinSide, sideToRival, sidesList, newNation);
        }
    }
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Windows.UI.Color.FromArgb(((System.Drawing.Color)value).A, ((System.Drawing.Color)value).R, ((System.Drawing.Color)value).G, ((System.Drawing.Color)value).B);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return System.Drawing.Color.FromArgb(((Windows.UI.Color)value).A, ((Windows.UI.Color)value).R, ((Windows.UI.Color)value).G, ((Windows.UI.Color)value).B);

        }
    }
}
