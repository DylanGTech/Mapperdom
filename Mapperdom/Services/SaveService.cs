using Ceras;
using Mapperdom.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
            config.ConfigType<MapState>().ConfigMember(ms => ms.TalkingNation).Exclude().ConstructBy(typeof(MapState).GetConstructors()[0]);
            config.ConfigType<Nation>().ConstructBy(typeof(Nation).GetConstructors()[0]);
            config.ConfigType<WarSide>().ConstructBy(typeof(WarSide).GetConstructors()[0]);

            return config;
        }


        public static async void SaveAsync(MapperGame map, string name)
        {

            CerasSerializer serializer = new CerasSerializer(GetConfig());

            MapState state = new MapState(map.ownershipData, map.occupationData, map.newCapturesData, map.Nations, map.Sides);
            state.TalkingNation = map.TalkingNation;


            StorageFolder saveLocation = await ApplicationData.Current.LocalFolder.CreateFolderAsync("SavedMaps", CreationCollisionOption.OpenIfExists);

            StorageFolder projectfolder = await saveLocation.CreateFolderAsync(name, CreationCollisionOption.ReplaceExisting);

            StorageFile file = await projectfolder.CreateFileAsync("state.bin");
            byte[] bytes = serializer.Serialize(state);
            await FileIO.WriteBytesAsync(file, bytes);

            //TODO: Save seed as well

            file = await projectfolder.CreateFileAsync("map.png");
            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            Stream pixelStream = map.baseImage.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)map.baseImage.PixelWidth, (uint)map.baseImage.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();
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


        public static async Task<MapperGame> LoadAsync(string name)
        {
            CerasSerializer serializer = new CerasSerializer(GetConfig());

            StorageFolder saveLocation = await ApplicationData.Current.LocalFolder.CreateFolderAsync("SavedMaps", CreationCollisionOption.OpenIfExists);

            IStorageItem folder = await saveLocation.TryGetItemAsync(name);

            if (folder == null) return null;

            StorageFile file = await ((StorageFolder)folder).GetFileAsync("map.png");
            ImageProperties p = await file.Properties.GetImagePropertiesAsync();
            WriteableBitmap bmp = new WriteableBitmap((int)p.Width, (int)p.Height);
            bmp.SetSource((await file.OpenReadAsync()).AsStream().AsRandomAccessStream());


            file = await ((StorageFolder)folder).GetFileAsync("state.bin");
            MapState state = serializer.Deserialize<MapState>((await FileIO.ReadBufferAsync(file)).ToArray());



            MapperGame map = new MapperGame(bmp);
            map.Nations = state.Nations;
            map.Sides = state.Sides;
            map.newCapturesData = state.NewCapturesData;
            map.occupationData = state.OccupationData;
            map.ownershipData = state.OwnershipData;
            map.TalkingNation = null;

            return map;
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
