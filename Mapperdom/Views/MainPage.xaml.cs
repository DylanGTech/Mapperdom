using System;

using Mapperdom.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Mapperdom.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
