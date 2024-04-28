using System;
using mulibLibrary;
using Spectre.Console; 

namespace mulib
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: mulib <source_directory> <destination_directory>");
                return;
            }

            string sourceFolder = args[0];
            string destinationFolder = args[1];

            if (!Directory.Exists(sourceFolder) || !Directory.Exists(destinationFolder))
            {
                Console.WriteLine("Source or destination directory does not exist.");
                return;
            }

            // Create an instance of MP3FileManager
            MP3FileManager mp3FileManager = new MP3FileManager(sourceFolder, destinationFolder);

            // Copy valid MP3 files from source to destination
            Console.WriteLine("Copying valid MP3 files...");
            mp3FileManager.CopyValidMP3FilesRecursive(sourceFolder);
            Console.WriteLine("Copying complete.");

            // Create playlists in the destination folder
            Console.WriteLine("Creating playlists...");
            mp3FileManager.CreatePlaylists();
            Console.WriteLine("Playlist creation complete.");
        }
    }
}
