using Ceras;
using Mapperdom.Core.Helpers;
using Mapperdom.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Mapperdom.Services
{
    public static class SaveService
    {
        private static SerializerConfig GetConfig()
        {
            SerializerConfig config = new SerializerConfig();
            config.DefaultTargets = TargetMember.AllProperties;
            //config.ConfigType<MapState>().ConfigMember(ms => ms.TalkingNation).Exclude().ConstructBy(typeof(MapState).GetConstructors()[0]);
            config.ConfigType<Nation>().ConstructBy(typeof(Nation).GetConstructors()[0]);
            config.ConfigType<WarSide>().ConstructBy(typeof(WarSide).GetConstructors()[0]);
            config.ConfigType<PixelData>().ConstructBy(typeof(PixelData).GetConstructors()[0]);
            config.ConfigType<UnorderedBytePair>().ConstructBy(typeof(UnorderedBytePair).GetConstructors()[0]);

            return config;
        }


        public static async void SaveAsync(MapperGame map, StorageFile zipFile)
        {

            CerasSerializer serializer = new CerasSerializer(GetConfig());

            MapState state = new MapState()
            {
                Pixels = map.Pixels,
                Nations = map.Nations,
                Sides = map.Sides,
                Fronts = new Dictionary<UnorderedBytePair, sbyte>(map.Fronts),
                DialogText = map.DialogText,
                DialogRectangle = map.DialogRectangle,
                IsTreatyMode = map.IsTreatyMode
            };


            using (Stream fileStream = await zipFile.OpenStreamForWriteAsync())
            {
                using (ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Update))
                {
                    for (int i = zipArchive.Entries.Count - 1; i >= 0; i--) zipArchive.Entries[i].Delete();


                    ZipArchiveEntry entry = zipArchive.CreateEntry("map.png");


                    using (Stream pngStream = entry.Open())
                    {


                        using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
                        {
                            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ms);
                            Stream pixelStream = map.baseImage.PixelBuffer.AsStream();
                            byte[] Pixels = new byte[pixelStream.Length];
                            await pixelStream.ReadAsync(Pixels, 0, Pixels.Length);
                            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)map.baseImage.PixelWidth, (uint)map.baseImage.PixelHeight, 96.0, 96.0, Pixels);
                            await encoder.FlushAsync();

                            IBuffer data = (new byte[ms.Size]).AsBuffer();

                            ms.Seek(0);
                            await ms.ReadAsync(data, (uint)ms.Size, InputStreamOptions.None);

                            pngStream.SetLength(data.ToArray().Length);

                            await pngStream.WriteAsync(data.ToArray(), 0, (int)pngStream.Length);
                            await pngStream.FlushAsync();
                        }

                    }

                    entry = zipArchive.CreateEntry("state.json");

                    using (Stream stateStream = entry.Open())
                    {
                        byte[] data = Encoding.Unicode.GetBytes(await Json.StringifyAsync(state));

                        stateStream.SetLength(data.Length);
                        await stateStream.WriteAsync(data, 0, data.Length);
                        await stateStream.FlushAsync();
                    }

                    entry = zipArchive.CreateEntry("pixels.bin");

                    using (Stream stateStream = entry.Open())
                    {
                        byte[] data = (new CerasSerializer(GetConfig())).Serialize(state.Pixels);

                        stateStream.SetLength(data.Length);
                        await stateStream.WriteAsync(data, 0, data.Length);
                        await stateStream.FlushAsync();
                    }
                }
            }
        }

        public static async void DeleteAsync(string name)
        {
            StorageFolder saveLocation = await ApplicationData.Current.LocalFolder.CreateFolderAsync("SavedMaps", CreationCollisionOption.OpenIfExists);

            IStorageItem folder = await saveLocation.TryGetItemAsync(name);

            if(folder != null)
            {
                await folder.DeleteAsync();
            }

        }


        public static async Task<MapperGame> LoadAsync(StorageFile zipFile)
        {
            CerasSerializer serializer = new CerasSerializer(GetConfig());

            if (zipFile == null) return null;


            WriteableBitmap bmp;
            MapState state;

            using (Stream fileStream = await zipFile.OpenStreamForReadAsync())
            {
                using (ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry entry = zipArchive.GetEntry("map.png");


                    if (entry == null) return null;

                    using (Stream pngStream = entry.Open())
                    {

                        using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
                        {
                            byte[] readBuffer = new byte[4096];

                            int bytesRead;

                            while ((bytesRead = pngStream.Read(readBuffer, 0, 4096)) > 0)
                            {
                                byte[] b = new byte[bytesRead];

                                Array.Copy(readBuffer, b, bytesRead);

                                await ms.WriteAsync(b.AsBuffer());
                            }


                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.PngDecoderId, ms);

                            using (SoftwareBitmap swbmp = await decoder.GetSoftwareBitmapAsync())
                            {
                                bmp = new WriteableBitmap(swbmp.PixelWidth, swbmp.PixelHeight);
                                swbmp.CopyToBuffer(bmp.PixelBuffer);
                            }
                        }

                            

                    }

                    entry = zipArchive.GetEntry("state.json");

                    if (entry == null) return null;

                    using (Stream stateStream = entry.Open())
                    {
                        byte[] readBuffer = new byte[4096];

                        int bytesRead;

                        MemoryStream ms = new MemoryStream();

                        while ((bytesRead = stateStream.Read(readBuffer, 0, 4096)) > 0)
                        {
                            byte[] b = new byte[bytesRead];

                            Array.Copy(readBuffer, b, bytesRead);

                            await ms.WriteAsync(b, 0, bytesRead);
                        }

                        state = await Json.ToObjectAsync<MapState>(Encoding.Unicode.GetString(ms.ToArray()));
                    }

                    entry = zipArchive.GetEntry("pixels.bin");

                    if (entry == null) return null;

                    using (Stream stateStream = entry.Open())
                    {
                        byte[] readBuffer = new byte[4096];

                        int bytesRead;

                        MemoryStream ms = new MemoryStream();

                        while ((bytesRead = stateStream.Read(readBuffer, 0, 4096)) > 0)
                        {
                            byte[] b = new byte[bytesRead];

                            Array.Copy(readBuffer, b, bytesRead);

                            await ms.WriteAsync(b, 0, bytesRead);
                        }

                        state.Pixels = (new CerasSerializer(GetConfig())).Deserialize<PixelData[,]>(ms.ToArray());
                    }
                }

                MapperGame map = new MapperGame(bmp);
                map.Nations = state.Nations;
                map.Sides = state.Sides;
                map.Pixels = state.Pixels;
                map.Fronts = state.Fronts;
                map.DialogText = state.DialogText;
                map.DialogRectangle = state.DialogRectangle;

                return map;
            }
        }
        public static bool CanLoad(string name)
        {
            StorageFolder saveLocation = Task.Run(async () => await ApplicationData.Current.LocalFolder.CreateFolderAsync("SavedMaps", CreationCollisionOption.OpenIfExists)).Result;

            return Task.Run(async () => await saveLocation.TryGetItemAsync(name)).Result != null;
        }


        /*
        public static async void ExportAsync(MapperGame map)
        {

        }
        public static async Task<MapperGame> ImportAsync()
        {
            return null;
        }
        */
    }
}
