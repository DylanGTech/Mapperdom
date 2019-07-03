using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Mapperdom
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MapperGame referencedGame;


        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add(".png");

            StorageFile f = await openPicker.PickSingleFileAsync();


            //Start new game if slelected
            if(f != null)
            {
                ImageProperties p = await f.Properties.GetImagePropertiesAsync();
                WriteableBitmap bmp = new WriteableBitmap((int)p.Width, (int)p.Height);

                bmp.SetSource((await f.OpenReadAsync()).AsStream().AsRandomAccessStream());

                try
                {
                    referencedGame = new MapperGame(bmp);
                }
                catch(Exception exc)
                {
                    //Do nothing (for now)
                    return;
                }

                mapImage.Source = referencedGame.GetCurrentMap();

                attackNorthWest.IsEnabled = true;
                attackNorth.IsEnabled = true;
                attackNorthEast.IsEnabled = true;
                attackWest.IsEnabled = true;
                attackNormal.IsEnabled = true;
                attackEast.IsEnabled = true;
                attackSouthWest.IsEnabled = true;
                attackSouth.IsEnabled = true;
                attackSouthEast.IsEnabled = true;

                annexOccupationButton.IsEnabled = true;
                RefreshNations();
            }
        }

        private void TakeTurnButton_Click(object sender, RoutedEventArgs e)
        {
            Nation n = (Nation)nationsList.SelectedItem;

            switch(((Button)sender).Tag)
            {
                case "NW":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key, -1, -1);
                    break;
                case "N":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key, 0, -2);
                    break;
                case "NE":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key, 1, -1);
                    break;
                case "W":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key,-2, 0);
                    break;
                case "C":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key);
                    break;
                case "E":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key, 2, 0);
                    break;
                case "SW":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key, -1, 1);
                    break;
                case "S":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key, 0, 2);
                    break;
                case "SE":
                    referencedGame.Advance((ushort)advanceForceSlider.Value, referencedGame.nations.Where(pair => pair.Value == n).Single().Key, 1, 1);
                    break;
            }

            mapImage.Source = referencedGame.GetCurrentMap();
        }

        private void AnnexOccupationButton_Click(object sender, RoutedEventArgs e)
        {
            Nation n = (Nation)nationsList.SelectedItem;
            referencedGame.AnnexTerritory(referencedGame.nations.Where(pair => pair.Value == n).Single().Key);
            mapImage.Source = referencedGame.GetCurrentMap();

        }


        //Get the list of nations to display (following an action that may change it in some way)
        private void RefreshNations()
        {
            nationsList.ItemsSource = referencedGame.nations.Values.ToList();

            nationsList.SelectedIndex = 0;
        }
    }
}
