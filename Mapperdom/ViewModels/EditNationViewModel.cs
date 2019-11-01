using Mapperdom.Helpers;
using Mapperdom.Models;
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
    public class EditNationViewModel : Observable
    {
        private readonly Nation _nation;

        public Nation Nation
        {
            get
            {
                return _nation;
            }
        }

        private string _nationName;
        public string NationName
        {
            get
            {
                return _nationName;
            }
            set
            {
                Set(ref _nationName, value);
            }
        }

        public EditNationViewModel(Nation n)
        {
            NationName = n.Name;
        }
    }
}
