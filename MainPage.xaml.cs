using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
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
                catch(Exception exc)
                {
                    //Do nothing (for now)
                    return;
                }

                mapImage.Source = referencedGame.GetCurrentMap();

                saveButton.IsEnabled = true;
                undoButton.IsEnabled = true;
                RefreshNations();
                RefreshButtons();
                referencedGame.Backup();
            }

        }

        private void TakeTurnButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
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


            RefreshNations();
        }

        private void AnnexOccupationButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
            Nation n = (Nation)nationsList.SelectedItem;
            referencedGame.AnnexTerritory(referencedGame.nations.Where(pair => pair.Value == n).Single().Key);
            mapImage.Source = referencedGame.GetCurrentMap();
        }


        //Get the list of nations to display (following an action that may change it in some way)
        private void RefreshNations()
        {
            Nation n = (Nation)nationsList.SelectedItem;

            nationsList.ItemsSource = referencedGame.nations.Values.ToList();

            if (((List<Nation>)nationsList.ItemsSource).Contains(n))
                nationsList.SelectedItem = n;
            else nationsList.SelectedItem = ((List<Nation>)nationsList.ItemsSource).First();
        }

        private async void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
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
            referencedGame.Surrender(referencedGame.nations.Where(pair => pair.Value == (Nation)nationsList.SelectedItem).Single().Key);
            RefreshNations();
            mapImage.Source = referencedGame.GetCurrentMap();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.SwapBanks();
            RefreshNations();
            mapImage.Source = referencedGame.GetCurrentMap();
        }


        private void RefreshButtons()
        {
            if (referencedGame != null && nationsList.SelectedItem != null)
            {
                Nation selectedNation = (Nation)(nationsList.SelectedItem);

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

                    annexOccupationButton.IsEnabled = true;
                    surrenderButton.IsEnabled = true;
                }
                rebellionButton.IsEnabled = true;
            }
            else
            {
                attackNorthWest.IsEnabled = false;
                attackNorth.IsEnabled = false;
                attackNorthEast.IsEnabled = false;
                attackWest.IsEnabled = false;
                attackNormal.IsEnabled = false;
                attackEast.IsEnabled = false;
                attackSouthWest.IsEnabled = false;
                attackSouth.IsEnabled = false;
                attackSouthEast.IsEnabled = false;

                advanceForceSlider.IsEnabled = false;

                annexOccupationButton.IsEnabled = false;
                surrenderButton.IsEnabled = false;
                rebellionButton.IsEnabled = false;
            }
        }

        private void NationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshButtons();
        }

        private void RebellionButton_Click(object sender, RoutedEventArgs e)
        {
            referencedGame.Backup();
            referencedGame.StartUprising(referencedGame.nations.Where(pair => pair.Value == (Nation)nationsList.SelectedItem).Single().Key);
            mapImage.Source = referencedGame.GetCurrentMap();
            RefreshNations();
        }
    }
}
