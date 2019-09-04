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
    public sealed partial class PickNationDialog : ContentDialog
    {
        public MainViewModel OriginalViewModel { get; } = new MainViewModel();
        public PickNationViewModel ViewModel { get; }
        public PickNationDialog(MainViewModel originalViewModel, ObservableCollection<Nation> nationsList, Nation nation)
        {
            this.InitializeComponent();
            ViewModel = new PickNationViewModel(nationsList, nation);
        }
    }
}
