using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Mapperdom.Helpers;
using Mapperdom.Views;
using Mapperdom.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Mapperdom.ViewModels
{
    public class MainViewModel : Observable
    {
        private MapperGame _referencedGame;
        public MapperGame ReferencedGame
        {
            get
            {
                return _referencedGame;
            }
            set
            {
                Set(ref _referencedGame, value);
                OnPropertyChanged("IsActiveGame");
                SetMapEntries();
                SetNationEntries();
            }
        }



        private bool _nationIsTalking;
        public bool NationIsTalking
        {
            get
            {
                return _nationIsTalking;
            }
            set
            {
                Set(ref _nationIsTalking, value);

                if(value == false)
                {
                    TalkingNation = null;
                }
            }
        }

        private ObservableCollection<Nation> _nations;
        public ObservableCollection<Nation> Nations
        {
            get
            {
                return _nations;
            }
            set
            {
                Set(ref _nations, value);
            }
        }

        public Nation TalkingNation
        {
            get
            {
                if (ReferencedGame != null)
                    return ReferencedGame.TalkingNation;
                else
                    return null;
            }
            set
            {
                if(ReferencedGame != null)
                {
                    Set(ref ReferencedGame.TalkingNation, value);
                    SourceImage = ReferencedGame.GetCurrentMap();

                }
            }
        }

        public bool IsActiveGame
        {
            get
            {
                return _referencedGame != null;
            }
        }

        public bool SelectedNationIsAtWar
        {
            get
            {
                if (SelectedDisplayEntry == null) return false;
                return SelectedDisplayEntry.Nation.WarSide.HasValue;
            }
        }

        private bool _navyForcesEnabled;
        public bool NavyForcesEnabled
        {
            get
            {
                return _navyForcesEnabled;
            }
            set
            {
                Set(ref _navyForcesEnabled, value);
            }
        }

        private int _forceStrength;
        public int ForceStrength
        {
            get
            {
                return _forceStrength;
            }
            set
            {
                Set(ref _forceStrength, value);
            }
        }


        private WriteableBitmap _sourceImage;
        public WriteableBitmap SourceImage
        {
            get
            {
                return _sourceImage;
            }
            set
            {
                Set(ref _sourceImage, value);
                SetMapEntries();
                SetNationEntries();
            }
        }

        private MapDisplayEntry _selectedDisplayEntry;

        public MapDisplayEntry SelectedDisplayEntry
        {
            get
            {
                return _selectedDisplayEntry;
            }
            set
            {
                Set(ref _selectedDisplayEntry, value);
                OnPropertyChanged("SelectedNationIsAtWar");
            }
        }

        private ObservableCollection<MapDisplayEntry> _mapEntries;
        public ObservableCollection<MapDisplayEntry> MapEntries
        {
            get
            {
                return _mapEntries;
            }
            set
            {
                Set(ref _mapEntries, value);
            }
        }


        private ICommand _newGameCommand;
        public ICommand NewGameCommand
        {
            get
            {
                if (_newGameCommand == null)
                    _newGameCommand = new RelayCommand(async () =>
                    {
                        FileOpenPicker openPicker = new FileOpenPicker();
                        openPicker.ViewMode = PickerViewMode.Thumbnail;
                        openPicker.FileTypeFilter.Add(".png");

                        StorageFile f = await openPicker.PickSingleFileAsync();

                        //Start new game if selected
                        if (f != null)
                        {
                            ImageProperties p = await f.Properties.GetImagePropertiesAsync();
                            WriteableBitmap bmp = new WriteableBitmap((int)p.Width, (int)p.Height);

                            bmp.SetSource((await f.OpenReadAsync()).AsStream().AsRandomAccessStream());

                            try
                            {
                                ReferencedGame = new MapperGame(bmp);
                            }
                            catch (Exception)
                            {
                                //Do nothing (for now)
                                return;
                            }
                            SourceImage = ReferencedGame.GetCurrentMap();
                            ReferencedGame.Backup();
                        }
                    });

                return _newGameCommand;
            }
        }

        private ICommand _saveImageCommand;
        public ICommand SaveImageCommand
        {
            get
            {
                if (_saveImageCommand == null)
                    _saveImageCommand = new RelayCommand(async () =>
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
                            WriteableBitmap wb = ReferencedGame.GetCurrentMap();


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
                    });

                return _saveImageCommand;
            }
        }

        private ICommand _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand == null)
                    _undoCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Undo();
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _undoCommand;
            }
        }
        private ICommand _redoCommand;
        public ICommand RedoCommand
        {
            get
            {
                if (_redoCommand == null)
                    _redoCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Redo();
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _redoCommand;
            }
        }

        private ICommand _annexOccupationCommand;
        public ICommand AnnexOccupationCommand
        {
            get
            {
                if (_annexOccupationCommand == null)
                    _annexOccupationCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        Nation n = SelectedDisplayEntry.Nation;
                        ReferencedGame.AnnexTerritory(ReferencedGame.Nations.Single(pair => pair.Value == n).Key);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _annexOccupationCommand;
            }
        }

        private ICommand _startUprisingCommand;
        public ICommand StartUprisingCommand
        {
            get
            {
                if (_startUprisingCommand == null)
                    _startUprisingCommand = new RelayCommand(async () =>
                    {
                        if(SelectedNationIsAtWar)
                        {
                            ObservableCollection<WarSide> options = new ObservableCollection<WarSide>(ReferencedGame.Sides.Values);
                            options.Remove(ReferencedGame.Sides[SelectedDisplayEntry.Nation.WarSide.Value]);

                            PickSideDialog d1 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, ReferencedGame.Sides[SelectedDisplayEntry.Nation.WarSide.Value], options, new Nation(SelectedDisplayEntry.Nation.Name + " Rebels"));
                            if ((await d1.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            ReferencedGame.Backup();
                            ReferencedGame.StartUprising(ReferencedGame.Nations.FirstOrDefault(n => n.Value == SelectedDisplayEntry.Nation).Key, d1.ViewModel.SelectedNation, ReferencedGame.Sides[SelectedDisplayEntry.Nation.WarSide.Value], d1.ViewModel.IsNewWarSide ? d1.ViewModel.NewWarSide : d1.ViewModel.SelectedWarSide);
                        }
                        else
                        {
                            PickSideDialog d1 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, new WarSide("", System.Drawing.Color.FromArgb(255, 0, 0, 0), System.Drawing.Color.FromArgb(255, 0, 0, 0), System.Drawing.Color.FromArgb(255, 0, 0, 0), System.Drawing.Color.FromArgb(255, 0, 0, 0)), new ObservableCollection<WarSide>(ReferencedGame.Sides.Values), SelectedDisplayEntry.Nation);
                            if ((await d1.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            PickSideDialog d2 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, d1.ViewModel.IsNewWarSide ? d1.ViewModel.NewWarSide : d1.ViewModel.SelectedWarSide, new ObservableCollection<WarSide>(ReferencedGame.Sides.Values.ToList()), new Nation(SelectedDisplayEntry.Nation.Name + " Rebels"));
                            if ((await d2.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            ReferencedGame.Backup();
                            ReferencedGame.StartUprising(ReferencedGame.Nations.FirstOrDefault(n => n.Value == SelectedDisplayEntry.Nation).Key, d2.ViewModel.SelectedNation, d1.ViewModel.IsNewWarSide ? d1.ViewModel.NewWarSide : d1.ViewModel.SelectedWarSide, d2.ViewModel.IsNewWarSide ? d2.ViewModel.NewWarSide : d2.ViewModel.SelectedWarSide);

                        }

                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _startUprisingCommand;
            }
        }

        private ICommand _surrenderCommand;
        public ICommand SurrenderCommand
        {
            get
            {
                if (_surrenderCommand == null)
                    _surrenderCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Surrender(ReferencedGame.Nations.Where(pair => pair.Value == SelectedDisplayEntry.Nation).Single().Key);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _surrenderCommand;
            }
        }

        private ICommand _attackNWCommand;
        public ICommand AttackNWCommand
        {
            get
            {
                if (_attackNWCommand == null)
                    _attackNWCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, -1, -1);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackNWCommand;
            }
        }

        private ICommand _attackNCommand;
        public ICommand AttackNCommand
        {
            get
            {
                if (_attackNCommand == null)
                    _attackNCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, 0, -2);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackNCommand;
            }
        }

        private ICommand _attackNECommand;
        public ICommand AttackNECommand
        {
            get
            {
                if (_attackNECommand == null)
                    _attackNECommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, 1, -1);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackNECommand;
            }
        }


        private ICommand _attackWCommand;
        public ICommand AttackWCommand
        {
            get
            {
                if (_attackWCommand == null)
                    _attackWCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, -2, -0);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackWCommand;
            }
        }

        private ICommand _attackCCommand;
        public ICommand AttackCCommand
        {
            get
            {
                if (_attackCCommand == null)
                    _attackCCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, 0, 0);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackCCommand;
            }
        }

        private ICommand _attackECommand;
        public ICommand AttackECommand
        {
            get
            {
                if (_attackECommand == null)
                    _attackECommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, 2, 0);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackECommand;
            }
        }


        private ICommand _attackSWCommand;
        public ICommand AttackSWCommand
        {
            get
            {
                if (_attackSWCommand == null)
                    _attackSWCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, -1, 1);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackSWCommand;
            }
        }

        private ICommand _attackSCommand;
        public ICommand AttackSCommand
        {
            get
            {
                if (_attackSCommand == null)
                    _attackSCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, 0, 2);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackSCommand;
            }
        }

        private ICommand _attackSECommand;
        public ICommand AttackSECommand
        {
            get
            {
                if (_attackSECommand == null)
                    _attackSECommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance((ushort)ForceStrength, ReferencedGame.Nations.Single(pair => pair.Value == SelectedDisplayEntry.Nation).Key, NavyForcesEnabled, 1, 1);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _attackSECommand;
            }
        }
        public MainViewModel()
        {

        }

        private void SetNationEntries()
        {
            Nation n = TalkingNation;
            ObservableCollection<Nation> entries = new ObservableCollection<Nation>();
            
            if(ReferencedGame != null)
            {
                if(Nations == null)
                {
                    foreach (Nation nat in ReferencedGame.Nations.Values.ToList())
                    {
                        entries.Add(nat);
                    }

                    Nations = entries;
                }
                else
                {
                    List<Nation> currentNationList = ReferencedGame.Nations.Values.ToList();
                    ObservableCollection<Nation> currentEntries = new ObservableCollection<Nation>(Nations);

                    foreach (Nation entry in currentEntries)
                    {
                        if (currentNationList.FirstOrDefault(nat => nat == entry) == null)
                        {
                            Nations.Remove(entry);
                        }
                    }

                    foreach (Nation nat in currentNationList)
                    {
                        if (Nations.FirstOrDefault(e => e == nat) == null)
                        {
                            Nations.Add(nat);
                        }
                    }
                }
            }
        }

        private void SetMapEntries()
        {
            Nation n = SelectedDisplayEntry != null ? SelectedDisplayEntry.Nation : null;
            ObservableCollection<MapDisplayEntry> entries = new ObservableCollection<MapDisplayEntry>();


            if (ReferencedGame != null)
            {
                if(MapEntries == null)
                {
                    foreach (Nation nat in ReferencedGame.Nations.Values.ToList())
                    {
                        entries.Add(new MapDisplayEntry(nat, nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null));
                    }

                    MapEntries = entries;

                    if (n != null)
                        SelectedDisplayEntry = entries.FirstOrDefault(e => e.Nation == n);
                    else SelectedDisplayEntry = entries.FirstOrDefault();
                }
                else
                {
                    List<Nation> currentNationList = ReferencedGame.Nations.Values.ToList();
                    ObservableCollection<MapDisplayEntry> currentEntries = new ObservableCollection<MapDisplayEntry>(MapEntries);

                    foreach (MapDisplayEntry entry in currentEntries)
                    {
                        if(currentNationList.FirstOrDefault(nat => nat == entry.Nation) == null)
                        {
                            MapEntries.Remove(entry);
                        }
                    }

                    foreach(Nation nat in currentNationList)
                    {
                        if(MapEntries.FirstOrDefault(e => e.Nation == nat) == null)
                        {
                            MapEntries.Add(new MapDisplayEntry(nat, nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null));
                        }
                    }

                    foreach (Nation nat in currentNationList)
                    {
                        MapDisplayEntry entry = MapEntries.First(e => e.Nation == nat);
                        entry.Update(nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null);
                    }
                }
            }
            OnPropertyChanged("SelectedNationIsAtWar");
        }
    }
}
