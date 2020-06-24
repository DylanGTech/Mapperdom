using Mapperdom.Helpers;
using Mapperdom.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Mapperdom.ViewModels
{
    public class ChangeBordersViewModel : Observable
    {
        private MapperGame game;

        private Dictionary<Color, byte> _nationColors;

        public Dictionary<Color, byte> NationColors
        {
            get
            {
                return _nationColors;
            }
        }

        private WriteableBitmap _map = null;

        public WriteableBitmap Map
        {
            get { return _map; }
            set { Set(ref _map, value); }
        }

        private ICommand _downloadMapCommand;
        public ICommand DownloadMapCommand
        {
            get
            {
                if (_downloadMapCommand == null)
                    _downloadMapCommand = new RelayCommand(async () =>
                    {
                        FileSavePicker fileSavePicker = new FileSavePicker
                        {
                            SuggestedStartLocation = PickerLocationId.PicturesLibrary
                        };
                        fileSavePicker.FileTypeChoices.Add("PNG File", new List<string>() { ".png" });
                        fileSavePicker.SuggestedFileName = "borders";

                        StorageFile outputFile = null;
                        outputFile = await fileSavePicker.PickSaveFileAsync();

                        if (outputFile == null)
                        {
                            // The user cancelled the picking operation
                            return;
                        }

                        using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            // Create an encoder with the desired format
                            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);


                            Stream pixelStream = Map.PixelBuffer.AsStream();
                            byte[] Pixels = new byte[pixelStream.Length];
                            await pixelStream.ReadAsync(Pixels, 0, Pixels.Length);

                            // Set additional encoding parameters, if needed
                            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)Map.PixelWidth, (uint)Map.PixelHeight, 96.0, 96.0, Map.PixelBuffer.ToArray());

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
                    });

                return _downloadMapCommand;
            }
        }

        private ICommand _uploadMapCommand;
        public ICommand UploadMapCommand
        {
            get
            {
                if (_uploadMapCommand == null)
                    _uploadMapCommand = new RelayCommand(async () =>
                    {
                        FileOpenPicker openPicker = new FileOpenPicker();
                        openPicker.ViewMode = PickerViewMode.Thumbnail;
                        openPicker.FileTypeFilter.Add(".png");

                        StorageFile f = await openPicker.PickSingleFileAsync();

                        //Start new game if selected
                        if (f != null)
                        {
                            ImageProperties p = await f.Properties.GetImagePropertiesAsync();
                            WriteableBitmap map = new WriteableBitmap((int)p.Width, (int)p.Height);

                            map.SetSource((await f.OpenReadAsync()).AsStream().AsRandomAccessStream());

                            Map = map;
                        }
                    });

                return _uploadMapCommand;
            }
        }


        public ChangeBordersViewModel(MapperGame game)
        {
            this.game = game;
            _nationColors = new Dictionary<Color, byte>();


            float increment = 360 / game.Nations.Count;

            foreach(KeyValuePair<byte, Nation> kvp in game.Nations.OrderBy(k => Guid.NewGuid()))
            {
                _nationColors.Add(Nation.ColorFromHsl(kvp.Key * increment, 0.75f, 0.60f), kvp.Key);
            }


            WriteableBitmap newImage = new WriteableBitmap(game.baseImage.PixelWidth, game.baseImage.PixelHeight);
            byte[] imageArray = new byte[newImage.PixelHeight * newImage.PixelWidth * 4];

            using (Stream stream = game.baseImage.PixelBuffer.AsStream())
            {
                stream.Read(imageArray, 0, imageArray.Length);
            }

            for (int y = 0; y < game.baseImage.PixelHeight; y++)
            {
                for (int x = 0; x < game.baseImage.PixelWidth; x++)
                {
                    if (game.Pixels[x, y].IsOcean)
                    {
                        imageArray[4 * (y * game.baseImage.PixelWidth + x)] = 0; // Blue
                        imageArray[4 * (y * game.baseImage.PixelWidth + x) + 1] = 0; // Green
                        imageArray[4 * (y * game.baseImage.PixelWidth + x) + 2] = 0; // Red
                        imageArray[4 * (y * game.baseImage.PixelWidth + x) + 3] = 255; // Alpha
                    }
                    else
                    {
                        Color c = _nationColors.Where(kvp => kvp.Value == game.Pixels[x, y].OwnerId).First().Key;
                        imageArray[4 * (y * game.baseImage.PixelWidth + x)] = c.B; // Blue
                        imageArray[4 * (y * game.baseImage.PixelWidth + x) + 1] = c.G; // Green
                        imageArray[4 * (y * game.baseImage.PixelWidth + x) + 2] = c.R; // Red
                        imageArray[4 * (y * game.baseImage.PixelWidth + x) + 3] = 255; // Alpha
                    }
                }
            }


            using (Stream stream = newImage.PixelBuffer.AsStream())
            {
                Task.Run(async () =>
                {
                    await stream.WriteAsync(imageArray, 0, imageArray.Length);
                }).Wait();
            }
            Map = newImage;
        }
    }
}
