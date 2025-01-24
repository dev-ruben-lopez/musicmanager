using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Logging;
using mulibLibrary;
using Spectre.Console; 

namespace MusicLibrary
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);



            //if (args.Length != 2)
            //{
            //    Console.WriteLine("Usage: MusicLibrary <source_directory> <destination_directory>");
            //    return;
            //}

            //string sourceFolder = args[0];
            //string destinationFolder = args[1];

            //if (!Directory.Exists(sourceFolder) || !Directory.Exists(destinationFolder))
            //{
            //    Console.WriteLine("Source or destination directory does not exist.");
            //    return;
            //}

            //// Create an instance of MP3FileManager
            //ILoggerFactory loggerFactory = new LoggerFactory();
            //MP3FileManager mp3FileManager = new MP3FileManager(sourceFolder, destinationFolder, loggerFactory);

            //Stopwatch stopwatch = Stopwatch.StartNew();

            //// Copy valid MP3 files from source to destination
            //Console.WriteLine("Copying valid MP3 files...");
            //mp3FileManager.CopyValidMP3FilesRecursive(sourceFolder);
            //Console.WriteLine("Copying complete.");

            //// Create playlists in the destination folder
            //Console.WriteLine("Creating playlists...");
            //mp3FileManager.CreatePlaylists();
            //Console.WriteLine("Playlist creation complete.");

            //// Format the elapsed time
            //TimeSpan elapsed = stopwatch.Elapsed;
            //string formattedTime = string.Format("{0:D2}d {1:D2}h {2:D2}m {3:D2}s {4:D3}ms",
            //                                     elapsed.Days,
            //                                     elapsed.Hours,
            //                                     elapsed.Minutes,
            //                                     elapsed.Seconds,
            //                                     elapsed.Milliseconds);

            //// Print the elapsed time
            //Console.WriteLine($"Execution Time: {formattedTime}");
            //Console.WriteLine("Press any key to end ...");
            //Console.ReadKey();
        }


        public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
              .UsePlatformDetect()
              .LogToTrace()
              .UseReactiveUI();

    }
}
