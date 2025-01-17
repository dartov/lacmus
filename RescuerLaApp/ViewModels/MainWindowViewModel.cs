﻿using Avalonia.Media;
using RescuerLaApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Docker.DotNet;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using MetadataExtractor;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Newtonsoft.Json;
using Directory = System.IO.Directory;

namespace RescuerLaApp.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private int _frameLoadProgressIndex;
        private NeuroModel _model = null;
        private List<Frame> _frames = new List<Frame>();
        
        public MainWindowViewModel()
        {
            var canGoNext = this
                .WhenAnyValue(x => x.SelectedIndex)
                .Select(index => index < Frames.Count - 1);
            
            // The bound button will stay disabled, when
            // there is no more frames left.
            NextImageCommand = ReactiveCommand.Create(
                () => { SelectedIndex++; },
                canGoNext);

            var canGoBack = this
                .WhenAnyValue(x => x.SelectedIndex)
                .Select(index => index > 0);

            var canExecute = this
                .WhenAnyValue(x => x.Status)
                .Select(status => status.Status != Enums.Status.Working);
            
            var canShowPedestrians = this
                .WhenAnyValue(x => x._frames)
                .Select(frames => frames.Any(x => x.IsVisible));
            
            // The bound button will stay disabled, when
            // there are no frames before the current one.
            PrevImageCommand = ReactiveCommand.Create(
                () => { SelectedIndex--; }, 
                canGoBack);
            
            // Add here newer commands
            IncreaseCanvasCommand = ReactiveCommand.Create(IncreaseCanvas);
            ShrinkCanvasCommand = ReactiveCommand.Create(ShrinkCanvas);
            PredictAllCommand = ReactiveCommand.Create(PredictAll, canExecute);
            OpenFileCommand = ReactiveCommand.Create(OpenFile, canExecute);
            SaveAllCommand = ReactiveCommand.Create(SaveAll, canExecute);
            LoadModelCommand = ReactiveCommand.Create(LoadModel, canExecute);
            UpdateModelCommand = ReactiveCommand.Create(UpdateModel, canExecute);
            ShowPerestriansCommand = ReactiveCommand.Create(ShowPedestrians, canExecute);
            ImportAllCommand = ReactiveCommand.Create(ImportAll, canExecute);
            SaveAllImagesWithObjectsCommand = ReactiveCommand.Create(SaveAllImagesWithObjects, canExecute);
            ShowAllMetadataCommand = ReactiveCommand.Create(ShowAllMetadata, canExecute);
            ShowGeoDataCommand = ReactiveCommand.Create(ShowGeoData, canExecute);
            HelpCommand = ReactiveCommand.Create(Help);
            AboutCommand = ReactiveCommand.Create(About);
            ExitCommand = ReactiveCommand.Create(Exit, canExecute);
        }

        public void UpdateFramesRepo()
        {
            this.WhenAnyValue(x => x.SelectedIndex)
                .Skip(1)
                .Subscribe(x =>
                {
                    if (Status.Status == Enums.Status.Ready)
                        Status = new AppStatusInfo
                        {
                            Status = Enums.Status.Ready, 
                            StringStatus = $"{Enums.Status.Ready.ToString()} | {Frames[SelectedIndex].Patch}"
                        };
                    UpdateUi();
                });
        }
        
        #region Public API

        [Reactive] public List<BoundBox> BoundBoxes { get; set; } = new List<BoundBox>();
        
        [Reactive] public double CanvasWidth { get; set; } = 500;
        
        [Reactive] public double CanvasHeight { get; set; } = 500;
        
        [Reactive] public int SelectedIndex { get; set; } = 0;

        [Reactive] public List<Frame> Frames { get; set; } = new List<Frame>();
        
        [Reactive] public AppStatusInfo Status { get; set; } = new AppStatusInfo { Status = Enums.Status.Ready };
        
        [Reactive] public ImageBrush ImageBrush { get; set; } = new ImageBrush { Stretch = Stretch.Uniform };

        [Reactive] public bool IsShowPedestrians { get; set; } = false;
        
        public ReactiveCommand<Unit, Unit> PredictAllCommand { get; }
        
        public ReactiveCommand<Unit, Unit> NextImageCommand { get; }
        
        public ReactiveCommand<Unit, Unit> PrevImageCommand { get; }
        
        public ReactiveCommand<Unit, Unit> ShrinkCanvasCommand { get; }
        
        public ReactiveCommand<Unit, Unit> IncreaseCanvasCommand { get; }
        
        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
        
        public ReactiveCommand<Unit, Unit> SaveAllCommand { get; }
        
        public ReactiveCommand<Unit, Unit> ImportAllCommand { get; }
        
        public ReactiveCommand<Unit, Unit> LoadModelCommand { get; }
        
        public ReactiveCommand<Unit, Unit> UpdateModelCommand { get; }
        
        public ReactiveCommand<Unit, Unit> ShowPerestriansCommand { get; }
        
        public ReactiveCommand<Unit, Unit> SaveAllImagesWithObjectsCommand { get; }
        
        public ReactiveCommand<Unit, Unit> ShowAllMetadataCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowGeoDataCommand { get; }
        public ReactiveCommand<Unit, Unit> HelpCommand { get; }
        public ReactiveCommand<Unit, Unit> AboutCommand { get; }
        public ReactiveCommand<Unit, Unit> ExitCommand { get; }
        
        

        #endregion

        private void ShowPedestrians()
        {
            if (IsShowPedestrians)
            {
                //fix bug when application stop if focus was set on image without object
                if (!_frames.Any(x => x.IsVisible))
                    return;
                SelectedIndex = Frames.FindIndex(x => x.IsVisible);
                Console.WriteLine(SelectedIndex);
                Frames = _frames.FindAll(x => x.IsVisible);
                UpdateUi();
            }
            else
            {
                Frames = new List<Frame>(_frames);
                UpdateUi();
            }
        }

        private async void LoadModel()
        {
            Status = new AppStatusInfo()
            {
                Status = Enums.Status.Working, 
                StringStatus = $"Working | loading model..."
            };
            
            if (_model == null)
            {
                _model = new NeuroModel();
            }

            await _model.Load();
            
            Status = new AppStatusInfo()
            {
                Status = Enums.Status.Ready
            };
        }
        
        private async void UpdateModel()
        {
            Status = new AppStatusInfo()
            {
                Status = Enums.Status.Working, 
                StringStatus = $"Working | updating model..."
            };
            
            if (_model == null)
            {
                _model = new NeuroModel();
            }
            
            await _model.UpdateModel();
            
            Status = new AppStatusInfo()
            {
                Status = Enums.Status.Ready
            };
        }

        private async void PredictAll()
        {
            if (Frames == null || Frames.Count < 1) return;
            Status = new AppStatusInfo()
            {
                Status = Enums.Status.Working, 
                StringStatus = $"Working | loading model..."
            };
            
            if (_model == null)
            {
                _model = new NeuroModel();
            }

            var isLoaded = await _model.Run();
            if (!isLoaded)
            {
                Status = new AppStatusInfo()
                {
                    Status = Enums.Status.Error, 
                    StringStatus = $"Error: unable to load model"
                };
                _model.Dispose();
                _model = null;
                return;
            }
                    
            var index = 0;
            Status = new AppStatusInfo()
            {
                Status = Enums.Status.Working, 
                StringStatus = $"Working | processing images: {index} / {Frames.Count}"
            };
            foreach (var frame in Frames)
            {
                index++;
                frame.Rectangles = await _model.Predict(frame);
                if(index < Frames.Count)
                    Status = new AppStatusInfo()
                    {
                        Status = Enums.Status.Working, 
                        StringStatus = $"Working | processing images: {index} / {Frames.Count}"
                    };

                if (frame.Rectangles.Count > 0)
                    frame.IsVisible = true;
            }
            _frames = new List<Frame>(Frames);
            await _model.Stop();
            SelectedIndex = 0; //Fix bug when application stopped if index > 0
            UpdateUi();
            Status = new AppStatusInfo()
            {
                Status = Enums.Status.Ready
            };
        }
        
        private void ShrinkCanvas()
        {
            Zoomer.Zoom(0.8);
        }
        
        private void IncreaseCanvas()
        {
            Zoomer.Zoom(1.2);
        }

        private async void OpenFile()
        {
            Status = new AppStatusInfo() {Status = Enums.Status.Working};
            try
            {
                var openDig = new OpenFolderDialog()
                {
                    Title = "Choose a directory with images"
                };
                var dirName = await openDig.ShowAsync(new Window());
                if (string.IsNullOrEmpty(dirName) || !Directory.Exists(dirName))
                {
                    Status = new AppStatusInfo() {Status = Enums.Status.Ready};
                    return;
                }
                var fileNames = Directory.GetFiles(dirName);
                _frameLoadProgressIndex = 0;
                Frames.Clear(); _frames.Clear(); GC.Collect();
                var loadingFrames = new List<Frame>();
                foreach (var fileName in fileNames)
                {
                    // TODO: Проверка IsImage вне зависимости от расширений.
                    if(!Path.HasExtension(fileName))
                        continue;
                    if (Path.GetExtension(fileName).ToLower() != ".jpg" &&
                        Path.GetExtension(fileName).ToLower() != ".jpeg" &&
                        Path.GetExtension(fileName).ToLower() != ".png" &&
                        Path.GetExtension(fileName).ToLower() != ".bmp")
                        continue;
                    
                    var frame = new Frame();
                    frame.OnLoad += FrameLoadingProgressUpdate;
                    frame.Load(fileName, Enums.ImageLoadMode.Miniature);
                    loadingFrames.Add(frame);
                }

                if (loadingFrames.Count == 0)
                {
                    Status = new AppStatusInfo() {Status = Enums.Status.Ready};
                    return;
                }
                
                Frames = loadingFrames;
                if (SelectedIndex < 0)
                    SelectedIndex = 0;
                UpdateFramesRepo();
                UpdateUi();
                _frames = new List<Frame>(Frames);
                Status = new AppStatusInfo() {Status = Enums.Status.Ready};
            }
            catch (Exception ex)
            {
                Status = new AppStatusInfo()
                {
                    Status = Enums.Status.Error, 
                    StringStatus = $"Error | {ex.Message.Replace('\n', ' ')}"
                };
            }
        }

        private async void SaveAll()
        {
            try
            {
                if (Frames == null || Frames.Count < 1)
                {
                    Status = new AppStatusInfo() {Status = Enums.Status.Ready};
                    return;
                }
                Status = new AppStatusInfo() {Status = Enums.Status.Working};
                var dirName = Path.GetDirectoryName(Frames.First().Patch);
                if (string.IsNullOrEmpty(dirName) || !Directory.Exists(dirName))
                {
                    Status = new AppStatusInfo() {Status = Enums.Status.Ready};
                    return;
                }
                
                foreach (var frame in Frames)
                {
                    if (frame.Rectangles == null || frame.Rectangles.Count <= 0)
                        continue;
                    var annotation = new Annotation();
                    annotation.Filename = Path.GetFileName(frame.Patch);
                    annotation.Folder = Path.GetRelativePath(dirName, Path.GetDirectoryName(frame.Patch));
                    annotation.Segmented = 0;
                    annotation.Size = new Models.Size()
                    {
                        Depth = 3,
                        Height = frame.Height,
                        Width = frame.Width
                    };
                    foreach (var rectangle in frame.Rectangles)
                    {
                        var o = new Models.Object();
                        o.Name = "Pedestrian";
                        o.Box = new Box()
                        {
                            Xmax = rectangle.XBase + rectangle.WidthBase,
                            Ymax = rectangle.YBase + rectangle.HeightBase,
                            Xmin = rectangle.XBase,
                            Ymin = rectangle.YBase
                        };
                        annotation.Objects.Add(o);
                    }

                    annotation.SaveToXml(Path.Join(dirName,$"{annotation.Filename}.xml"));
                }
                Console.WriteLine($"Saved to {dirName}");
                Status = new AppStatusInfo() {Status = Enums.Status.Ready, StringStatus = $"Success | saved to {dirName}"};
            }
            catch (Exception ex)
            {
                Status = new AppStatusInfo()
                {
                    Status = Enums.Status.Error, 
                    StringStatus = $"Error | {ex.Message.Replace('\n', ' ')}"
                };
            }
        }

        private async void SaveAllImagesWithObjects()
        {
            try
            {
                if (Frames == null || Frames.Count < 1)
                {
                    Status = new AppStatusInfo() {Status = Enums.Status.Ready};
                    return;
                }
                Status = new AppStatusInfo() {Status = Enums.Status.Working};

                var openDig = new OpenFolderDialog()
                {
                    Title = "Choose a directory to save images with objects"
                };
                var dirName = await openDig.ShowAsync(new Window());

                
                if (string.IsNullOrEmpty(dirName) || !Directory.Exists(dirName))
                {
                    Status = new AppStatusInfo() {Status = Enums.Status.Ready};
                    return;
                }
                
                foreach (var frame in Frames)
                {
                    if (frame.Rectangles == null || frame.Rectangles.Count <= 0)
                        continue;
                    File.Copy(frame.Patch, Path.Combine(dirName, Path.GetFileName(frame.Patch)));
                }
                Console.WriteLine($"Saved to {dirName}");
                Status = new AppStatusInfo() {Status = Enums.Status.Ready, StringStatus = $"Success | saved to {dirName}"};
            }
            catch (Exception ex)
            {
                Status = new AppStatusInfo()
                {
                    Status = Enums.Status.Error, 
                    StringStatus = $"Error | {ex.Message.Replace('\n', ' ')}"
                };
            }
        }
        
        private async void ImportAll()
        {
            Status = new AppStatusInfo() {Status = Enums.Status.Working};
            try
            {
                var openDig = new OpenFolderDialog()
                {
                    Title = "Choose a directory with xml annotations"
                };
                var dirName = await openDig.ShowAsync(new Window());
                if (string.IsNullOrEmpty(dirName) || !Directory.Exists(dirName))
                {
                    Status = new AppStatusInfo() {Status = Enums.Status.Ready};
                    return;
                }
                
                var fileNames = Directory.GetFiles(dirName);
                _frameLoadProgressIndex = 0;
                Frames.Clear(); _frames.Clear(); GC.Collect();
                
                var loadingFrames = new List<Frame>();
                var annotations = new List<Annotation>();
                foreach (var fileName in fileNames)
                {
                    if (Path.GetExtension(fileName).ToLower() != ".xml")
                        continue;
                    annotations.Add(Annotation.ParseFromXml(fileName));
                }

                foreach (var ann in annotations)
                {
                    var fileName = Path.Combine(dirName, ann.Filename);
                    // TODO: Проверка IsImage вне зависимости от расширений.
                    if (!File.Exists(fileName))
                        continue;
                    if(!Path.HasExtension(fileName))
                        continue;
                    if (Path.GetExtension(fileName).ToLower() != ".jpg" &&
                        Path.GetExtension(fileName).ToLower() != ".jpeg" &&
                        Path.GetExtension(fileName).ToLower() != ".png" &&
                        Path.GetExtension(fileName).ToLower() != ".bmp")
                        continue;
                    
                    var frame = new Frame();
                    frame.OnLoad += FrameLoadingProgressUpdate;
                    frame.Load(fileName, Enums.ImageLoadMode.Miniature);
                    frame.Rectangles = new List<BoundBox>();
                    foreach (var obj in ann.Objects)
                    {
                        var bbox = new BoundBox(
                            x: obj.Box.Xmin,
                            y: obj.Box.Ymin,
                            height: obj.Box.Ymax - obj.Box.Ymin,
                            width: obj.Box.Xmax - obj.Box.Xmin
                            );
                        frame.Rectangles.Add(bbox);
                    }

                    if (frame.Rectangles.Count > 0)
                    {
                        frame.IsVisible = true;
                    }
                    loadingFrames.Add(frame);
                }

                if (loadingFrames.Count == 0)
                {
                    Status = new AppStatusInfo() {Status = Enums.Status.Ready};
                    return;
                }
                    
                
                Frames = loadingFrames;
                if (SelectedIndex < 0)
                    SelectedIndex = 0;
                UpdateFramesRepo();
                UpdateUi();
                _frames = new List<Frame>(Frames);
                Status = new AppStatusInfo() {Status = Enums.Status.Ready};
            }
            catch (Exception ex)
            {
                Status = new AppStatusInfo()
                {
                    Status = Enums.Status.Error, 
                    StringStatus = $"Error | {ex.Message.Replace('\n', ' ')}"
                };
            }
        }
        

        private void FrameLoadingProgressUpdate()
        {
            _frameLoadProgressIndex++;
            if(_frameLoadProgressIndex < Frames.Count)
                Status = new AppStatusInfo()
                {
                    Status = Enums.Status.Working, 
                    StringStatus = $"Working | loading images: {_frameLoadProgressIndex} / {Frames.Count}"
                };
            else
            {
                Status = new AppStatusInfo()
                {
                    Status = Enums.Status.Ready
                };
            }
        }

        public void Help()
        {
            OpenUrl("https://github.com/lizaalert/lacmus/wiki");
        }
        
        public void ShowGeoData()
        {
            string msg = string.Empty;
            int rows = 0;
            var directories = ImageMetadataReader.ReadMetadata(Frames[SelectedIndex].Patch);
            foreach (var directory in directories)
            foreach (var tag in directory.Tags)
            {
                if (directory.Name.ToLower() == "gps")
                {
                    if (tag.Name.ToLower() == "gps latitude" ||
                        tag.Name.ToLower() == "gps longitude" ||
                        tag.Name.ToLower() == "gps altitude")
                    {
                        rows++;
                        msg += $"{tag.Name}: {tag.Description}\n";
                    }
                }
            }

            if (rows != 3)
                msg = "This image have hot geo tags.\nUse `Show all metadata` more for more details.";
            var msgbox = new MessageBox.Avalonia.MessageBoxWindow(new MessageBoxParams
            {
                Button = ButtonEnum.Ok,
                ContentTitle = $"Geo position of {Path.GetFileName(Frames[SelectedIndex].Patch)}",
                ContentMessage = msg,
                Icon = Icon.Info,
                Style = Style.None,
                ShowInCenter = true
            });
            msgbox.Show();
        }

        public void ShowAllMetadata()
        {
            var tb = new TextTableBuilder();
            tb.AddRow("Group", "Tag name", "Description");
            tb.AddRow("-----", "--------", "-----------");

            
            var directories = ImageMetadataReader.ReadMetadata(Frames[SelectedIndex].Patch);
            foreach (var directory in directories)
            foreach (var tag in directory.Tags)
                tb.AddRow(directory.Name, tag.Name, tag.Description);
            
            var msgbox = new MessageBox.Avalonia.MessageBoxWindow(new MessageBoxParams
            {
                Button = ButtonEnum.Ok,
                ContentTitle = $"Metadata of {Path.GetFileName(Frames[SelectedIndex].Patch)}",
                ContentMessage = tb.Output(),
                Icon = Icon.Info,
                Style = Style.None,
                ShowInCenter = true
            });
            msgbox.Show();
        }

        public async void About()
        {
            var message =
                "Copyright (c) 2019 Georgy Perevozghikov <gosha20777@live.ru>\nGithub page: https://github.com/lizaalert/lacmus/. Press `Github` button for more details.\nProvided by Yandex Cloud: https://cloud.yandex.com/." +
                "\nThis program comes with ABSOLUTELY NO WARRANTY." +
                "\nThis is free software, and you are welcome to redistribute it under GNU GPLv3 conditions.\nPress `License` button to learn more about the license";

            var msgBoxCustomParams = new MessageBoxCustomParams
            {
                ButtonDefinitions =  new []
                {
                    new ButtonDefinition{Name = "Ok", Type = ButtonType.Colored},
                    new ButtonDefinition{Name = "License"},
                    new ButtonDefinition{Name = "Github"}
                },
                ContentTitle = "About",
                ContentHeader = "Lacmus desktop application. Version 0.3.0 alpha.",
                ContentMessage = message,
                Icon = Icon.Avalonia,
                Style = Style.None,
                ShowInCenter = true
            };
            var msgbox = MessageBox.Avalonia.MessageBoxWindow.CreateCustomWindow(msgBoxCustomParams);
            var result = await msgbox.Show();
            switch (result.ToLower())
            {
                case "ok": return;
                case "license": OpenUrl("https://github.com/lizaalert/lacmus/blob/master/LICENSE"); break;
                case "github": OpenUrl("https://github.com/lizaalert/lacmus"); break;
            }
        }
        
        public async void Exit()
        {
            var message = "Do you really want to exit?";
            
            var msgbox = new MessageBox.Avalonia.MessageBoxWindow(new MessageBoxParams
            {
                Button = ButtonEnum.YesNo,
                ContentTitle = "Exit",
                ContentMessage = message,
                Icon = Icon.Info,
                Style = Style.None,
                ShowInCenter = true
            });
            var result = await msgbox.Show();
            if(result.ToLower() == "yes")
                Application.Current.MainWindow.Close();
        }

        private void OpenUrl(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    System.Diagnostics.Process.Start(url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    System.Diagnostics.Process.Start("x-www-browser", url);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }

        private void UpdateUi()
        {
            /*TODO: Вынести сюда все функции обновления UI*/
            ImageBrush.Source = new Bitmap(Frames[SelectedIndex].Patch); //replace to frame.load(...)
            CanvasHeight = ImageBrush.Source.PixelSize.Height;
            CanvasWidth = ImageBrush.Source.PixelSize.Width;
            if (Frames[SelectedIndex].Rectangles != null && Frames[SelectedIndex].Rectangles.Count > 0)
            {
                BoundBoxes = new List<BoundBox>(Frames[SelectedIndex].Rectangles);
            }
            else
            {
                BoundBoxes = null;
            }
        }
    }
}
