using Mapperdom.Helpers;
using Mapperdom.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Mapperdom.ViewModels
{
    public class PickSideViewModel : Observable
    {
        private readonly bool _canJoinSide;
        public Visibility SideOptionVisibility
        {
            get
            {
                return _canJoinSide ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Nation _selectedNation;
        public Nation SelectedNation
        {
            get
            {
                return _selectedNation;
            }
            set
            {
                Set(ref _selectedNation, value);
            }
        }


        private bool _isNewWarSide;
        public bool IsNewWarSide
        {
            get
            {
                return _isNewWarSide;
            }
            set
            {
                Set(ref _isNewWarSide, value);
                OnPropertyChanged("SideCreationVisibility");
                OnPropertyChanged("PickSideVisibility");
            }
        }

        private WarSide _selectedWarSide;
        public WarSide SelectedWarSide
        {
            get
            {
                return _selectedWarSide;
            }
            set
            {
                Set(ref _selectedWarSide, value);
            }
        }

        private WarSide _newWarSide;
        public WarSide NewWarSide
        {
            get
            {
                return _newWarSide;
            }
            set
            {
                Set(ref _newWarSide, value);
            }
        }

        private ObservableCollection<WarSide> _sidesAvailable;
        public ObservableCollection<WarSide> SidesAvailable
        {
            get
            {
                return _sidesAvailable;
            }
        }

        public Visibility SideCreationVisibility
        {
            get
            {
                return IsNewWarSide ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public Visibility PickSideVisibility
        {
            get
            {
                return IsNewWarSide ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        public PickSideViewModel(bool canJoinSide, ObservableCollection<WarSide> sidesList, Nation nation)
        {
            _canJoinSide = canJoinSide;
            NewWarSide = new WarSide("New Side", System.Drawing.Color.FromArgb(255, 0, 0, 0), System.Drawing.Color.FromArgb(255, 0, 0, 0), System.Drawing.Color.FromArgb(255, 0, 0, 0), System.Drawing.Color.FromArgb(255, 0, 0, 0));
            Set(ref _sidesAvailable, new ObservableCollection<WarSide>(sidesList), "SidesAvailable");
            SelectedNation = nation;

            if (!_canJoinSide)
                IsNewWarSide = true;
            else IsNewWarSide = false;
            OnPropertyChanged("SideOptionVisibility");
        }
    }
}
