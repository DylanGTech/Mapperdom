using Mapperdom.Helpers;
using Mapperdom.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapperdom.ViewModels
{
    public class PickNationViewModel : Observable
    {
        private ObservableCollection<Nation> _nationsAvailable;
        public ObservableCollection<Nation> NationsAvailable
        {
            get
            {
                return _nationsAvailable;
            }
        }
        private Nation _nation1;
        public Nation Nation1
        {
            get
            {
                return _nation1;
            }
            set
            {
                Set(ref _nation1, value);
            }
        }

        private Nation _nation2;
        public Nation Nation2
        {
            get
            {
                return _nation2;
            }
            set
            {
                Set(ref _nation2, value);
            }
        }

        public PickNationViewModel(ObservableCollection<Nation> nationsList, Nation nation)
        {
            Set(ref _nationsAvailable, new ObservableCollection<Nation>(nationsList), "NationsAvailable");
            Nation1 = nation;
        }
    }
}
