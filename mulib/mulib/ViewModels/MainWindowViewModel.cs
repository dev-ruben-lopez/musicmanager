using ReactiveUI;
using Avalonia.Controls;
using System;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mulibLibrary;
using System.Diagnostics;

namespace MusicLibrary.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _sourceFolderPath;
        private string _destinationFolderPath;
        private string _executionResult;
        private bool _isLoading;
        private Avalonia.Media.Brush _executionResultColor;

        public ReactiveCommand<Unit, Unit> ExecuteOrganizerCommand { get; }

        public string SourceFolderPath
        {
            get => _sourceFolderPath;
            set => this.RaiseAndSetIfChanged(ref _sourceFolderPath, value);
        }

        public string DestinationFolderPath
        {
            get => _destinationFolderPath;
            set => this.RaiseAndSetIfChanged(ref _destinationFolderPath, value);
        }

        public string ExecutionResult
        {
            get => _executionResult;
            set => this.RaiseAndSetIfChanged(ref _executionResult, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public Avalonia.Media.Brush ExecutionResultColor
        {
            get => _executionResultColor;
            set => this.RaiseAndSetIfChanged(ref _executionResultColor, value);
        }

        public ReactiveCommand<Unit, Unit> SelectSourceFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectDestinationFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> ExecuteLibraryCallCommand { get; }

        public MainWindowViewModel()
        {
            SelectSourceFolderCommand = ReactiveCommand.CreateFromTask(SelectSourceFolder);
            SelectDestinationFolderCommand = ReactiveCommand.CreateFromTask(SelectDestinationFolder);
            ExecuteLibraryCallCommand = ReactiveCommand.CreateFromTask(ExecuteLibraryCall);

            //ExecuteOrganizerCommand = ReactiveCommand.CreateFromTask(async () =>
            //{
            //    if (string.IsNullOrWhiteSpace(SourceFolderPath) || string.IsNullOrWhiteSpace(DestinationFolderPath))
            //    {
            //        ExecutionResult = "Please select both Source and Destination folders.";
            //        ExecutionResultColor = (Avalonia.Media.Brush)Avalonia.Media.Brushes.Red;
            //        return;
            //    }

            //    IsLoading = true;
            //    ExecutionResult = string.Empty;

            //    try
            //    {
            //        // Simulated library call
            //        await Task.Run(() =>
            //        {
            //            System.Threading.Thread.Sleep(3000); // Simulate processing
            //        });

            //        ExecutionResult = "Music files organized successfully!";
            //        ExecutionResultColor = (Avalonia.Media.Brush)Avalonia.Media.Brushes.Green;
            //    }
            //    catch
            //    {
            //        ExecutionResult = "An error occurred during processing.";
            //        ExecutionResultColor = (Avalonia.Media.Brush)Avalonia.Media.Brushes.Red;

            //        throw new Exception("An error occurred.");
            //    }
            //    finally
            //    {
            //        IsLoading = false;
            //    }
            //});

            ExecuteLibraryCallCommand.ThrownExceptions.Subscribe(ex => {  ExecutionResult = ex.Message; });
        }

        private async Task SelectSourceFolder()
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(new Window()); // Replace with your parent window if needed
            if (!string.IsNullOrEmpty(result))
            {
                SourceFolderPath = result;
            }
        }

        private async Task SelectDestinationFolder()
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(new Window()); // Replace with your parent window if needed
            if (!string.IsNullOrEmpty(result))
            {
                DestinationFolderPath = result;
            }
        }

        private async Task ExecuteLibraryCall()
        {
            if (string.IsNullOrWhiteSpace(SourceFolderPath) || string.IsNullOrWhiteSpace(DestinationFolderPath))
            {
                ExecutionResult = "Please select both Source and Destination folders.";
                ExecutionResultColor = (Avalonia.Media.Brush)Avalonia.Media.Brushes.Red;
                return;
            }

            IsLoading = true;
            ExecutionResult = string.Empty;

            try
            {
                // Simulated library call
                await Task.Run(() =>
                {

                    if (!Directory.Exists(SourceFolderPath) || !Directory.Exists(DestinationFolderPath))
                    {
                        ExecutionResult = "Source or destination directory does not exist.";
                        //ExecutionResultColor = (Avalonia.Media.Brush)Avalonia.Media.Brushes.Red;
                        return;
                    }

                    // Create an instance of MP3FileManager
                    ILoggerFactory loggerFactory = new LoggerFactory();
                    MP3FileManager mp3FileManager = new MP3FileManager(SourceFolderPath, DestinationFolderPath, loggerFactory);

                    Stopwatch stopwatch = Stopwatch.StartNew();

                    //// Copy valid MP3 files from source to destination
                    //Console.WriteLine("Copying valid MP3 files...");
                    mp3FileManager.CopyValidMP3FilesRecursive(SourceFolderPath);
                    //Console.WriteLine("Copying complete.");

                    //// Create playlists in the destination folder
                    //Console.WriteLine("Creating playlists...");
                    mp3FileManager.CreatePlaylists();
                    //Console.WriteLine("Playlist creation complete.");

                    //// Format the elapsed time
                    TimeSpan elapsed = stopwatch.Elapsed;
                    string formattedTime = string.Format("{0:D2}d {1:D2}h {2:D2}m {3:D2}s {4:D3}ms",
                                                         elapsed.Days,
                                                         elapsed.Hours,
                                                         elapsed.Minutes,
                                                         elapsed.Seconds,
                                                         elapsed.Milliseconds);

                    ExecutionResult = $"Music files organized successfully!. Total Time : {formattedTime}";
                    //ExecutionResultColor = (Avalonia.Media.Brush)Avalonia.Media.Brushes.Green;


                    //// Print the elapsed time
                    //Console.WriteLine($"Execution Time: {formattedTime}");
                    //Console.WriteLine("Press any key to end ...");
                    //Console.ReadKey();



                });

                //ExecutionResult = "Music files organized successfully!";
                //ExecutionResultColor = (Avalonia.Media.Brush)Avalonia.Media.Brushes.Green;
            }
            catch
            {
                ExecutionResult = "An error occurred during processing.";
                ExecutionResultColor = (Avalonia.Media.Brush)Avalonia.Media.Brushes.Red;

                throw new Exception("Error occurred.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
