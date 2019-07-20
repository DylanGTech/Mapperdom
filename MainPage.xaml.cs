using System;
using System.Collections.Generic;
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

                MapImage.Source = referencedGame.GetCurrentMap();

                SaveButton.IsEnabled = true;
                UndoButton.IsEnabled = true;
                RefreshNations();
                RefreshButtons();
                referencedGame.Backup();
            }
        }

        private void TakeTurnButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
            Nation n = (Nation)NationsList.SelectedItem;

            switch(((Button)sender).Tag)
            {
                case "NW":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, -1, -1);
                    break;
                case "N":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, 0, -2);
                    break;
                case "NE":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, 1, -1);
                    break;
                case "W":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key,-2, 0);
                    break;
                case "C":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key);
                    break;
                case "E":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, 2, 0);
                    break;
                case "SW":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, -1, 1);
                    break;
                case "S":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, 0, 2);
                    break;
                case "SE":
                    referencedGame.Advance((ushort)AdvanceForceSlider.Value, referencedGame.Nations.Single(pair => pair.Value == n).Key, 1, 1);
                    break;
            }
            
            MapImage.Source = referencedGame.GetCurrentMap();
            RefreshNations();
        }

        private void AnnexOccupationButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
            Nation n = (Nation)NationsList.SelectedItem;
            referencedGame.AnnexTerritory(referencedGame.Nations.Single(pair => pair.Value == n).Key);
            MapImage.Source = referencedGame.GetCurrentMap();
        }

        //Get the list of nations to display (following an action that may change it in some way)
        private void RefreshNations()
        {
            Nation n = (Nation)NationsList.SelectedItem;
            NationsList.ItemsSource = referencedGame.Nations.Values.ToList();
            NationsList.SelectedItem = ((List<Nation>)NationsList.ItemsSource).Contains(n) ? n : ((List<Nation>)NationsList.ItemsSource).First();
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
            referencedGame.Surrender(referencedGame.Nations.Single(pair => pair.Value == (Nation)NationsList.SelectedItem).Key);
            RefreshNations();
            MapImage.Source = referencedGame.GetCurrentMap();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.SwapBanks();
            RefreshNations();
            MapImage.Source = referencedGame.GetCurrentMap();
        }
        
        private void RefreshButtons()
        {
            if (referencedGame != null && NationsList.SelectedItem != null)
            {
                Nation selectedNation = (Nation)(NationsList.SelectedItem);

                if (selectedNation.WarSide.HasValue)
                {
                    AttackNorthWest.IsEnabled = true;
                    AttackNorth.IsEnabled = true;
                    AttackNorthEast.IsEnabled = true;
                    AttackWest.IsEnabled = true;
                    AttackNormal.IsEnabled = true;
                    AttackEast.IsEnabled = true;
                    AttackSouthWest.IsEnabled = true;
                    AttackSouth.IsEnabled = true;
                    AttackSouthEast.IsEnabled = true;

                    AdvanceForceSlider.IsEnabled = true;

                    AnnexOccupationButton.IsEnabled = true;
                    SurrenderButton.IsEnabled = true;
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
            MapImage.Source = referencedGame.GetCurrentMap();
            RefreshNations();
        }
    }
}
