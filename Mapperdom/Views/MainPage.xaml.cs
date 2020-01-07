using System;
using System.Threading.Tasks;
using Mapperdom.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Mapperdom.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public MainPage()
        {
            InitializeComponent();
            ViewModel = new MainViewModel(mapCanvas);
        }
    }
}
