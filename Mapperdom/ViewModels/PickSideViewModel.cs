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

        private string _selectedTemplate;

        public string SelectedTemplate
        {
            get
            {
                return _selectedTemplate;
            }
            set
            {
                Set(ref _selectedTemplate, value);

                switch (_selectedTemplate)
                {
                    default:
                        break;
                    case "Red":
                        NewWarSide.MainColor = System.Drawing.Color.FromArgb(0xff, 0x7d, 0x00, 0x00);
                        NewWarSide.PuppetColor = System.Drawing.Color.FromArgb(0xff, 0xbf, 0x00, 0x00);
                        NewWarSide.OccupiedColor = System.Drawing.Color.FromArgb(0xff, 0xff, 0x17, 0x17);
                        NewWarSide.GainColor = System.Drawing.Color.FromArgb(0xff, 0xff, 0x80, 0x80);
                        break;
                    case "Blue":
                        NewWarSide.MainColor = System.Drawing.Color.FromArgb(0xff, 0x00, 0x00, 0x7d);
                        NewWarSide.PuppetColor = System.Drawing.Color.FromArgb(0xff, 0x00, 0x00, 0xbf);
                        NewWarSide.OccupiedColor = System.Drawing.Color.FromArgb(0xff, 0x17, 0x17, 0xff);
                        NewWarSide.GainColor = System.Drawing.Color.FromArgb(0xff, 0x80, 0x80, 0xff);
                        break;
                    case "Yellow":
                        NewWarSide.MainColor = System.Drawing.Color.FromArgb(0xff, 0x7d, 0x7d, 0x00);
                        NewWarSide.PuppetColor = System.Drawing.Color.FromArgb(0xff, 0xbf, 0xbf, 0x00);
                        NewWarSide.OccupiedColor = System.Drawing.Color.FromArgb(0xff, 0xff, 0xff, 0x17);
                        NewWarSide.GainColor = System.Drawing.Color.FromArgb(0xff, 0xff, 0xff, 0x80);
                        break;
                    case "Purple":
                        NewWarSide.MainColor = System.Drawing.Color.FromArgb(0xff, 0x7d, 0x00, 0x7d);
                        NewWarSide.PuppetColor = System.Drawing.Color.FromArgb(0xff, 0xbf, 0x00, 0xbf);
                        NewWarSide.OccupiedColor = System.Drawing.Color.FromArgb(0xff, 0xff, 0x17, 0xff);
                        NewWarSide.GainColor = System.Drawing.Color.FromArgb(0xff, 0xff, 0x80, 0xff);
                        break;
                }
                OnPropertyChanged("NewWarSide");
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
