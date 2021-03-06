﻿using System;
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
using Mapperdom.Services;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using System.Text;
using Windows.UI.Core;
using Windows.UI;

namespace Mapperdom.ViewModels
{
    public class MainViewModel : Observable
    {
        private readonly CoreDispatcher dispatcher;
        private readonly Canvas mapCanvas;

        /*
        private async Task OnUiThread(Action action)
        {
            await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }
        private async Task OnUiThread(Func<Task> action)
        {
            await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await action());
        }
        */

        public bool CanUndo
        {
            get
            {
                return ReferencedGame != null && ReferencedGame.CanUndo;
            }
        }
        public bool CanRedo
        {
            get
            {
                return ReferencedGame != null && ReferencedGame.CanRedo;
            }
        }

        public bool CanLoad
        {
            get
            {
                return SaveService.CanLoad("LastSave");
            }
        }

        public bool IsTreatyMode
        {
            get
            {
                if (ReferencedGame == null) return false;

                return ReferencedGame.IsTreatyMode;
            }
            set
            {
                if (ReferencedGame != null)
                    Set(ref ReferencedGame.IsTreatyMode, value, "IsTreatyMode");
                OnPropertyChanged("TreatyOptionsVisibility");
                OnPropertyChanged("WarOptionsVisibility");
            }
        }


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
                SetAttackEntries();
            }
        }


        public bool NationIsTalking
        {
            get
            {
                return Nations.Where(n => n.IsSelected).ToList().Count > 0;
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
                SetAttackEntries();
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
                OnPropertyChanged("NavyForcesEnabled");
                OnPropertyChanged("ForceStrength");
            }
        }

        private ObservableCollection<FrontEntry> _frontEntries;
        public ObservableCollection<FrontEntry> FrontEntries
        {
            get
            {
                return _frontEntries;
            }
            set
            {
                Set(ref _frontEntries, value);
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

        public Visibility WarOptionsVisibility
        {
            get
            {
                return IsTreatyMode ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        public Visibility TreatyOptionsVisibility
        {
            get
            {
                return IsTreatyMode ? Visibility.Visible : Visibility.Collapsed;
            }
        }


        private ICommand _executePlanCommand;
        public ICommand ExecutePlanCommand
        {
            get
            {
                if (_executePlanCommand == null)
                    _executePlanCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Advance();
                        UpdateMapAsync();
                        ReferencedGame.Backup();
                    });

                return _executePlanCommand;
            }
        }


        private ICommand _refreshMapCommand;
        public ICommand RefreshMapCommand
        {
            get
            {
                if (_refreshMapCommand == null)
                    _refreshMapCommand = new RelayCommand(() =>
                    {
                        UpdateMapAsync();
                        ReferencedGame.Backup();
                    });

                return _refreshMapCommand;
            }
        }


        private ICommand _saveProjectCommand;
        public ICommand SaveProjectCommand
        {
            get
            {
                if (_saveProjectCommand == null)
                    _saveProjectCommand = new RelayCommand(async () =>
                    {
                        FileSavePicker savePicker = new FileSavePicker();
                        List<string> zipList = new List<string>();
                        zipList.Add(".zip");
                        savePicker.FileTypeChoices.Add("Zip File", zipList);
                        StorageFile file = await savePicker.PickSaveFileAsync();
                        if (file == null) return;

                        SaveService.SaveAsync(ReferencedGame, file);
                    });

                return _saveProjectCommand;
            }
        }

        private ICommand _loadProjectCommand;
        public ICommand LoadProjectCommand
        {
            get
            {
                if (_loadProjectCommand == null)
                    _loadProjectCommand = new RelayCommand(async () =>
                    {
                        FileOpenPicker openPicker = new FileOpenPicker();
                        openPicker.FileTypeFilter.Add(".zip");
                        StorageFile file = await openPicker.PickSingleFileAsync();

                        if (file == null) return;

                        try
                        {
                            MapperGame lastGame = await SaveService.LoadAsync(file);

                            if (lastGame != null)
                            {
                                ReferencedGame = lastGame;
                                ReferencedGame.CleanFronts();
                                UpdateMapAsync();
                            }
                            else
                            {
                                MessageDialog errorDialog = new MessageDialog("There was an error loading your most recent project", "Error");
                                await errorDialog.ShowAsync();
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            MessageDialog errorDialog = new MessageDialog("Save file not found", "Error");
                            await errorDialog.ShowAsync();
                        }
                        catch (DecoderFallbackException)
                        {
                            MessageDialog errorDialog = new MessageDialog("There was an error reading your project save file. It is most likely outdated", "Error");
                            await errorDialog.ShowAsync();
                        }
                        catch (FormatException)
                        {
                            MessageDialog errorDialog = new MessageDialog("There was an error reading your project save file. It is most likely outdated", "Error");
                            await errorDialog.ShowAsync();
                        }
                    });

                return _loadProjectCommand;
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
                        NewProjectDialog d1 = new NewProjectDialog();


                        ContentDialogResult res = new ContentDialogResult();
                        res = await d1.ShowAsync();

                        //Start new game if selected
                        if (res == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary && d1.ViewModel.Map != null)
                        {
                            try
                            {
                                ReferencedGame = new MapperGame(d1.ViewModel.Map, useCustomColors: d1.ViewModel.UseColoredNations);
                            }
                            catch (Exception e)
                            {
                                MessageDialog errorDialog = new MessageDialog(e.Message, "Error");
                                await errorDialog.ShowAsync();

                                return;
                            }

                            UpdateMapAsync();
                            ReferencedGame.CleanFronts();
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

                            // Set the software bitmap
                            WriteableBitmap wb = await ReferencedGame.GetCurrentMapAsync();


                            Stream pixelStream = wb.PixelBuffer.AsStream();
                            byte[] Pixels = new byte[pixelStream.Length];
                            await pixelStream.ReadAsync(Pixels, 0, Pixels.Length);

                            // Set additional encoding parameters, if needed
                            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)wb.PixelWidth, (uint)wb.PixelHeight, 96.0, 96.0, Pixels);

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
                        UpdateMapAsync();
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
                        UpdateMapAsync();
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
                        UpdateMapAsync();
                    });

                return _annexOccupationCommand;
            }
        }

        private ICommand _beginNavalInvasionCommand;
        public ICommand BeginNavalInvasionCommand
        {
            get
            {
                if (_beginNavalInvasionCommand == null)
                    _beginNavalInvasionCommand = new RelayCommand(async () =>
                    {
                        ObservableCollection<Nation> options = new ObservableCollection<Nation>();
                        foreach (Nation n in ReferencedGame.Nations.Values.ToList())
                        {
                            if(n.WarSide.HasValue && n.WarSide != SelectedDisplayEntry.Nation.WarSide)
                                options.Add(n);
                        }

                        ContentDialogResult res = new ContentDialogResult();
                        PickNationDialog d1 = new PickNationDialog(this, options, SelectedDisplayEntry.Nation);
                        res = await d1.ShowAsync();

                        if (res != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                            return;
                        if (d1.ViewModel.Nation2 == null)
                            return;

                        ReferencedGame.Backup();
                        ReferencedGame.BeginNavalInvasion(
                            ReferencedGame.Nations.First(n => n.Value == d1.ViewModel.Nation1).Key,
                            ReferencedGame.Nations.First(n => n.Value == d1.ViewModel.Nation2).Key);

                    });

                return _beginNavalInvasionCommand;
            }
        }



        private ICommand _declareWarCommand;
        public ICommand DeclareWarCommand
        {
            get
            {
                if (_declareWarCommand == null)
                    _declareWarCommand = new RelayCommand(async () =>
                    {
                        ObservableCollection<Nation> options = new ObservableCollection<Nation>();
                        foreach (Nation n in ReferencedGame.Nations.Values.ToList())
                            options.Add(n);


                        if (SelectedNationIsAtWar)
                        {
                            foreach (Nation n in ReferencedGame.Nations.Values.Where(nat => nat.WarSide != null))
                                options.Remove(n);
                            options.Remove(SelectedDisplayEntry.Nation);
                        }

                        options.Remove(SelectedDisplayEntry.Nation);

                        ContentDialogResult res = new ContentDialogResult();
                        PickNationDialog d1 = new PickNationDialog(this, options, SelectedDisplayEntry.Nation);
                        res = await d1.ShowAsync();

                        if (res != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                            return;
                        if (d1.ViewModel.Nation2 == null)
                            return;


                        WarSide n1Side = d1.ViewModel.Nation1.WarSide.HasValue ? ReferencedGame.Sides[d1.ViewModel.Nation1.WarSide.Value] : null;
                        WarSide n2Side = d1.ViewModel.Nation2.WarSide.HasValue ? ReferencedGame.Sides[d1.ViewModel.Nation2.WarSide.Value] : null;


                        if (n1Side == null)
                        {
                            ObservableCollection<WarSide> sideOptions = new ObservableCollection<WarSide>(ReferencedGame.Sides.Values);
                            if (n2Side != null) sideOptions.Remove(n2Side);

                            PickSideDialog d2 = new PickSideDialog(this, options.Count > 0, sideOptions, d1.ViewModel.Nation1);
                            res = await d2.ShowAsync();
                            if (res != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            if (d2.ViewModel.IsNewWarSide && d2.ViewModel.NewWarSide != null)
                                n1Side = d2.ViewModel.NewWarSide;
                            else if (!d2.ViewModel.IsNewWarSide && d2.ViewModel.SelectedWarSide != null)
                                n1Side = d2.ViewModel.SelectedWarSide;
                            else return;
                        }

                        if (n2Side == null)
                        {
                            ObservableCollection<WarSide> sideOptions = new ObservableCollection<WarSide>(ReferencedGame.Sides.Values);
                            if (n1Side != null) sideOptions.Remove(n1Side);

                            PickSideDialog d2 = new PickSideDialog(this, options.Count > 0, new ObservableCollection<WarSide>(ReferencedGame.Sides.Values), d1.ViewModel.Nation2);
                            res = await d2.ShowAsync();
                            if (res != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            if (d2.ViewModel.IsNewWarSide && d2.ViewModel.NewWarSide != null)
                                n2Side = d2.ViewModel.NewWarSide;
                            else if (!d2.ViewModel.IsNewWarSide && d2.ViewModel.SelectedWarSide != null)
                                n2Side = d2.ViewModel.SelectedWarSide;
                            else return;
                        }

                        ReferencedGame.Backup();
                        ReferencedGame.DeclareWar(ReferencedGame.Nations.FirstOrDefault(kvp => kvp.Value == d1.ViewModel.Nation1).Key, ReferencedGame.Nations.FirstOrDefault(kvp => kvp.Value == d1.ViewModel.Nation2).Key, n1Side, n2Side);

                        UpdateMapAsync();
                    });

                return _declareWarCommand;
            }
        }


        private ICommand _withdrawFromWarCommand;
        public ICommand WithdrawFromWarCommand
        {
            get
            {
                if (_withdrawFromWarCommand == null)
                    _withdrawFromWarCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.WithdrawFromWar(ReferencedGame.Nations.FirstOrDefault(n => n.Value == SelectedDisplayEntry.Nation).Key);
                        UpdateMapAsync();
                    });

                return _withdrawFromWarCommand;
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
                        ContentDialogResult res = new ContentDialogResult();
                        if (SelectedNationIsAtWar)
                        {
                            ObservableCollection<WarSide> options = new ObservableCollection<WarSide>(ReferencedGame.Sides.Values);
                            options.Remove(ReferencedGame.Sides[SelectedDisplayEntry.Nation.WarSide.Value]);

                            PickSideDialog d1 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, options, new Nation(SelectedDisplayEntry.Nation.Name + " Rebels", System.Drawing.Color.FromArgb(0x0000B33C)));
                            res = await d1.ShowAsync();

                            if (res != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary || (d1.ViewModel.IsNewWarSide && d1.ViewModel.NewWarSide == null) || (!d1.ViewModel.IsNewWarSide && d1.ViewModel.SelectedWarSide == null))
                                return;

                            ReferencedGame.Backup();
                            ReferencedGame.StartUprising(ReferencedGame.Nations.FirstOrDefault(n => n.Value == SelectedDisplayEntry.Nation).Key, d1.ViewModel.SelectedNation, ReferencedGame.Sides[SelectedDisplayEntry.Nation.WarSide.Value], d1.ViewModel.IsNewWarSide ? d1.ViewModel.NewWarSide : d1.ViewModel.SelectedWarSide);
                        }
                        else
                        {
                            PickSideDialog d1 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, new ObservableCollection<WarSide>(ReferencedGame.Sides.Values), SelectedDisplayEntry.Nation);
                            res = await d1.ShowAsync();
                            if (res != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary || (d1.ViewModel.IsNewWarSide && d1.ViewModel.NewWarSide == null) || (!d1.ViewModel.IsNewWarSide && d1.ViewModel.SelectedWarSide == null))
                                return;

                            PickSideDialog d2 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, new ObservableCollection<WarSide>(ReferencedGame.Sides.Values.ToList()), new Nation(SelectedDisplayEntry.Nation.Name + " Rebels", System.Drawing.Color.FromArgb(0x0000B33C)));
                            if ((await d2.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary || (d2.ViewModel.IsNewWarSide && d1.ViewModel.NewWarSide == null) || (!d2.ViewModel.IsNewWarSide && d2.ViewModel.SelectedWarSide == null))
                                return;

                            ReferencedGame.Backup();
                            ReferencedGame.StartUprising(ReferencedGame.Nations.FirstOrDefault(n => n.Value == SelectedDisplayEntry.Nation).Key, d2.ViewModel.SelectedNation, d1.ViewModel.IsNewWarSide ? d1.ViewModel.NewWarSide : d1.ViewModel.SelectedWarSide, d2.ViewModel.IsNewWarSide ? d2.ViewModel.NewWarSide : d2.ViewModel.SelectedWarSide);

                        }

                        UpdateMapAsync();
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
                        UpdateMapAsync();
                    });

                return _surrenderCommand;
            }
        }

        private ICommand _editNationCommand;
        public ICommand EditNationCommand
        {
            get
            {
                if (_editNationCommand == null)
                    _editNationCommand = new RelayCommand(async () =>
                    {
                        EditNationDialog d1 = new EditNationDialog(SelectedDisplayEntry.Nation);

                        if (await d1.ShowAsync() == ContentDialogResult.Secondary)
                        {
                            SelectedDisplayEntry.Nation.Name = d1.ViewModel.NationName;
                            OnPropertyChanged("Nations");
                        }
                    });
                return _editNationCommand;
            }
        }

        public async void UpdateMapAsync()
        {
            SourceImage = await ReferencedGame.GetCurrentMapAsync();
        }

        public MainViewModel(Canvas canvas)
        {
            this.mapCanvas = canvas;

            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                return;
            }
            this.dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void SetNationEntries()
        {
            ObservableCollection<Nation> entries = new ObservableCollection<Nation>();

            if (ReferencedGame != null)
            {
                if (Nations == null)
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

                if (SelectedDisplayEntry == null) SelectedDisplayEntry = MapEntries.First();
            }
        }

        private void SetAttackEntries()
        {
            if (ReferencedGame != null)
            {
                if (FrontEntries == null)
                    FrontEntries = new ObservableCollection<FrontEntry>();

                FrontEntries.Clear();
                foreach (KeyValuePair<UnorderedBytePair, sbyte> pair in ReferencedGame.Fronts)
                {
                    FrontEntries.Add(new FrontEntry(pair.Key, ReferencedGame));
                }
            }
        }


        private void SetMapEntries()
        {
            Nation n = SelectedDisplayEntry != null ? SelectedDisplayEntry.Nation : null;
            ObservableCollection<MapDisplayEntry> entries = new ObservableCollection<MapDisplayEntry>();


            if (ReferencedGame != null)
            {
                if (MapEntries == null)
                {
                    foreach (Nation nat in ReferencedGame.Nations.Values.ToList())
                    {
                        entries.Add(new MapDisplayEntry(nat, nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null, this));
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
                        if (currentNationList.FirstOrDefault(nat => nat == entry.Nation) == null)
                        {
                            MapEntries.Remove(entry);
                        }
                    }

                    foreach (Nation nat in currentNationList)
                    {
                        if (MapEntries.FirstOrDefault(e => e.Nation == nat) == null)
                        {
                            MapEntries.Add(new MapDisplayEntry(nat, nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null, this));
                        }
                    }

                    foreach (Nation nat in currentNationList)
                    {
                        MapDisplayEntry entry = MapEntries.First(e => e.Nation == nat);
                        entry.Update(nat, nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null);
                    }
                }
            }
            OnPropertyChanged("SelectedNationIsAtWar");
        }

        private ICommand _changeBordersCommand;
        public ICommand ChangeBordersCommand
        {
            get
            {
                if (_changeBordersCommand == null)
                    _changeBordersCommand = new RelayCommand(async () =>
                    {
                        ContentDialogResult res = new ContentDialogResult();
                        ChangeBordersDialog d1 = new ChangeBordersDialog(ReferencedGame);
                        res = await d1.ShowAsync();

                        if (res != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                            return;

                        if(d1.ViewModel.Map.PixelWidth != ReferencedGame.Pixels.GetLength(0) || d1.ViewModel.Map.PixelHeight != ReferencedGame.Pixels.GetLength(1))
                        {
                            //TODO: Error message
                            return;
                        }

                        byte[] imageArray = new byte[d1.ViewModel.Map.PixelWidth * d1.ViewModel.Map.PixelHeight * 4];

                        using (Stream stream = d1.ViewModel.Map.PixelBuffer.AsStream())
                        {
                            stream.Read(imageArray, 0, imageArray.Length);
                        }

                        for (int y = 0; y < d1.ViewModel.Map.PixelHeight; y++)
                        {
                            for (int x = 0; x < d1.ViewModel.Map.PixelWidth; x++)
                            {
                                if (ReferencedGame.Pixels[x, y].IsOcean)
                                    continue;

                                System.Drawing.Color c = System.Drawing.Color.FromArgb(
                                    imageArray[4 * (y * d1.ViewModel.Map.PixelWidth + x) + 3],
                                    imageArray[4 * (y * d1.ViewModel.Map.PixelWidth + x) + 2],
                                    imageArray[4 * (y * d1.ViewModel.Map.PixelWidth + x) + 1],
                                    imageArray[4 * (y * d1.ViewModel.Map.PixelWidth + x) + 0]);

                                if (!d1.ViewModel.NationColors.ContainsKey(c))
                                {
                                    //TODO: Error message
                                    return;
                                }
                                else
                                {
                                    //This is in the case the nation is NOT at war and exchanges borders with another nation
                                    //This prevents non-rebel nations from existing even when they are annexed
                                    //Also prevents other nations from occupying neutral territories
                                    if(!Nations[ReferencedGame.Pixels[x, y].OwnerId].WarSide.HasValue)
                                    {
                                        ReferencedGame.Pixels[x, y].OccupierId = d1.ViewModel.NationColors[c];
                                    }

                                    ReferencedGame.Pixels[x, y].OwnerId = d1.ViewModel.NationColors[c];
                                }
                            }
                        }
                        ReferencedGame.CheckForCollapse();
                        ReferencedGame.CleanFronts();
                        ReferencedGame.GenerateNationLabels();
                        UpdateMapAsync();
                        ReferencedGame.Backup();
                    });

                return _changeBordersCommand;
            }
        }
    }
}
