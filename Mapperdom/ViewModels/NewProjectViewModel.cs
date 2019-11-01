using Mapperdom.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;

namespace Mapperdom.ViewModels
{
    public class NewProjectViewModel : Observable
    {
        private WriteableBitmap _map = null;

        public WriteableBitmap Map
        {
            get { return _map; }
            set { Set(ref _map, value); }
        }

        private bool _useColoredNations;

        public bool UseColoredNations
        {
            get { return _useColoredNations; }
            set { Set(ref _useColoredNations, value); }
        }

        private ICommand _uploadPictureCommand;
        public ICommand UploadPictureCommand
        {
            get
            {
                if (_uploadPictureCommand == null)
                    _uploadPictureCommand = new RelayCommand(async () =>
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

                return _uploadPictureCommand;
            }
        }
    }
}
