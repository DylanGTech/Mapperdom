using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Mapperdom
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private MapperGame referencedGame;
        public ObservableCollection<MapDisplayEntry> nationEntries = new ObservableCollection<MapDisplayEntry>();

        public MainPage()
        {
            InitializeComponent();
        }

        private async void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add(".png");

            StorageFile f = await openPicker.PickSingleFileAsync();

            //Start new game if selected
            if(f != null)
            {
                ImageProperties p = await f.Properties.GetImagePropertiesAsync();
                WriteableBitmap bmp = new WriteableBitmap((int)p.Width, (int)p.Height);

                bmp.SetSource((await f.OpenReadAsync()).AsStream().AsRandomAccessStream());

                try
                {
                    referencedGame = new MapperGame(bmp);
                }
                catch(Exception)
                {
                    //Do nothing (for now)
                    return;
                }
                SaveButton.IsEnabled = true;
                UndoButton.IsEnabled = true;
                RefreshNations();
                RefreshButtons();
                UpdateImage();
                referencedGame.Backup();

            }
        }

        private void TakeTurnButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
            Nation n = ((MapDisplayEntry)NationsList.SelectedItem).nation;

            switch(((Button)sender).Tag)
            {
                case "NW":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, -1, -1);
                    break;
                case "N":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, 0, -2);
                    break;
                case "NE":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, 1, -1);
                    break;
                case "W":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, -2, 0);
                    break;
                case "C":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, navalActivityCheckbox.IsChecked.Value,);
                    break;
                case "E":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, 2, 0);
                    break;
                case "SW":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, -1, 1);
                    break;
                case "S":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, 0, 2);
                    break;
                case "SE":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, navalActivityCheckbox.IsChecked.Value, 1, 1);
                    break;
            }
            
            UpdateImage();
            RefreshNations();
        }

        private void AnnexOccupationButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
            Nation n = ((MapDisplayEntry)NationsList.SelectedItem).nation;
            referencedGame.AnnexTerritory(referencedGame.Nations.Single(pair => pair.Value == n).Key);
            UpdateImage();
        }

        //Get the list of nations to display (following an action that may change it in some way)
        private void RefreshNations()
        {
            MapDisplayEntry selectedItem = (MapDisplayEntry)NationsList.SelectedItem;

            nationEntries.Clear();
            foreach (Nation nat in referencedGame.Nations.Values.ToList())
            {
                nationEntries.Add(new MapDisplayEntry(nat, nat.WarSide.HasValue ? referencedGame.sides[nat.WarSide.Value] : null));
            }



            if (selectedItem != null)
                NationsList.SelectedItem = ((ObservableCollection<MapDisplayEntry>)NationsList.ItemsSource).FirstOrDefault(e => e.nation == selectedItem.nation);
            else NationsList.SelectedItem = ((ObservableCollection<MapDisplayEntry>)NationsList.ItemsSource).FirstOrDefault();
        }

        private async void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker fileSavePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            fileSavePicker.FileTypeChoices.Add("PNG File", new List<string>() { ".png" });
            fileSavePicker.SuggestedFileName = "image";

            StorageFile outputFile = await fileSavePicker.PickSaveFileAsync();

            if (outputFile == null)
            {
                // The user cancelled the picking operation
                return;
            }

            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

                // Set the software bitmap
                WriteableBitmap wb = referencedGame.GetCurrentMap();


                Stream pixelStream = wb.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                // Set additional encoding parameters, if needed
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)wb.PixelHeight, (uint)wb.PixelHeight, 96.0, 96.0, pixels);

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                    switch (err.HResult)
                    {
                        case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                            // If the encoder does not support writing a thumbnail, then try again
                            // but disable thumbnail generation.
                            encoder.IsThumbnailGenerated = false;
                            break;
                        default:
                            throw;
                    }
                }
            }
        }

        private void SurrenderButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
            referencedGame.Surrender(referencedGame.Nations.Where(pair => pair.Value == ((MapDisplayEntry)NationsList.SelectedItem).nation).Single().Key);
            RefreshNations();
            UpdateImage();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.SwapBanks();
            RefreshNations();
            UpdateImage();
        }
        
        private void RefreshButtons()
        {
            if (referencedGame != null && NationsList.SelectedItem != null)
            {
                Nation selectedNation = ((MapDisplayEntry)NationsList.SelectedItem).nation;

                if (selectedNation.WarSide.HasValue)
                {
                    attackNorthWest.IsEnabled = true;
                    attackNorth.IsEnabled = true;
                    attackNorthEast.IsEnabled = true;
                    attackWest.IsEnabled = true;
                    attackNormal.IsEnabled = true;
                    attackEast.IsEnabled = true;
                    attackSouthWest.IsEnabled = true;
                    attackSouth.IsEnabled = true;
                    attackSouthEast.IsEnabled = true;

                    advanceForceSlider.IsEnabled = true;
                    navalActivityCheckbox.IsEnabled = true;

                    annexOccupationButton.IsEnabled = true;
                    surrenderButton.IsEnabled = true;
                }
                RebellionButton.IsEnabled = true;
            }
            else
            {
                AttackNorthWest.IsEnabled = false;
                AttackNorth.IsEnabled = false;
                AttackNorthEast.IsEnabled = false;
                AttackWest.IsEnabled = false;
                AttackNormal.IsEnabled = false;
                AttackEast.IsEnabled = false;
                AttackSouthWest.IsEnabled = false;
                AttackSouth.IsEnabled = false;
                AttackSouthEast.IsEnabled = false;

                AdvanceForceSlider.IsEnabled = false;

                AnnexOccupationButton.IsEnabled = false;
                SurrenderButton.IsEnabled = false;
                RebellionButton.IsEnabled = false;
            }
        }

        private void NationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshButtons();
        }

        private void RebellionButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
            referencedGame.StartUprising(referencedGame.Nations.Single(pair => pair.Value == (Nation)NationsList.SelectedItem).Key);
            UpdateImage();
            RefreshNations();
        }



        public void UpdateImage()
        {
            mapImage.Source = referencedGame.GetCurrentMap();
            RefreshNations();
            RefreshButtons();
        }
    }


    public class MapDisplayEntry
    {
        public Nation nation;
        public string sideName;
        public Brush mainBrush;
        public Brush puppetBrush;
        public Brush occupiedBrush;
        public Brush gainBrush;

        public MapDisplayEntry(Nation nation, WarSide ws)
        {
            this.nation = nation;
            if(ws != null)
            {
                sideName = ws.Name;
                mainBrush = ws.MainBrush;
                puppetBrush = ws.PuppetBrush;
                occupiedBrush = ws.OccupiedBrush;
                gainBrush = ws.GainBrush;
            }
            else
            {
                Brush neutralBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0x00, 0x80, 0x55));
                sideName = "";
                mainBrush = neutralBrush;
                puppetBrush = neutralBrush;
                occupiedBrush = neutralBrush;
                gainBrush = neutralBrush;
            }
        }
    }
}
