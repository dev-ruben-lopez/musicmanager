using System;
using System.IO;
using TagLib;
using Spectre.Console;

 
namespace mulibLibrary
{
    public class MP3FileManager
    {
        //cons
        private string[] Extensions = new string[] {".mp3",".m4a", ".flac"};
        
        // Properties
        public string SourceFolder { get; set; }
        public string DestinationFolder { get; set; }

        // Constructor
        public MP3FileManager(string sourceFolder, string destinationFolder)
        {
            SourceFolder = sourceFolder;
            DestinationFolder = destinationFolder;
        }

        /// <summary>
        /// Validate if audio file meet conditions.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsAudioFileValid(string filePath)
        {
            try
            {
                var file = TagLib.File.Create(filePath);
                if (file.Tag == null || !file.Tag.Performers.Any() || !file.Tag.Genres.Any() || file.Properties.AudioBitrate < 192)
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Method to copy MP3 files if conditions are met
        /// </summary>
        /// <param name="fileName"></param>
        public void CopyAudioFileIfValid(string fileName)
        {
            try
            {
                if (!Extensions.Contains(Path.GetExtension(fileName)))
                {
                    return;
                }

                //string destinationFilePath = Path.Combine(DestinationFolder, Path.GetFileName(fileName));

                if (IsAudioFileValid(fileName))
                {
                    // Create directory structure for AlbumArtist/Album
                    var audioFile = TagLib.File.Create(fileName);

                    string performer = audioFile.Tag.Performers.FirstOrDefault().ToString();
                    string album = audioFile.Tag.Album;

                    //Change the extension to mp3
                    //string destinationFileName = Path.ChangeExtension(Path.GetFileName(fileName), ".mp3");

                    string destinationDirectory = Path.Combine(DestinationFolder, performer, album);
                    Directory.CreateDirectory(destinationDirectory);

                    // Copy file to destination directory
                    var fileFilteredNameOnly = Path.GetFileName(fileName);
                    fileFilteredNameOnly = Path.ChangeExtension(Path.GetFileName(fileName), ".mp3");
                    fileFilteredNameOnly = ReplaceInvalidChars(fileFilteredNameOnly);

                    System.IO.File.Copy(fileName, Path.Combine(destinationDirectory, fileFilteredNameOnly), true);

                    EzLogger.Log($"File {fileFilteredNameOnly} copied.", EzLogger.MessageType.INFO);


                }
                else
                {
                    EzLogger.Log($"File {fileName} does not meet the conditions and was not copied.", EzLogger.MessageType.WARNING);
                }
            }
            catch(Exception)
            {
                EzLogger.Log($"File {fileName} does not meet the conditions and was not copied.", EzLogger.MessageType.WARNING);
            }

        }

        /// <summary>
        // Method to recursively search for MP3 files in source directory and copy valid files to destination directory
        /// </summary>
        /// <param name="directory"></param>
        public void CopyValidMP3FilesRecursive(string directory)
        {
            try
            {
                foreach(var fileName in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                {
                    var fullFilePathName = Path.Combine(directory, fileName);
                    CopyAudioFileIfValid(fullFilePathName);
                }

            }
            catch (Exception ex)
            {
                EzLogger.Log($"An error occurred: {ex.Message}", EzLogger.MessageType.ERROR);
            }
        }


        /// <summary>
        /// Method to create playlists from mp3 files in root dir
        /// </summary>
        public void CreatePlaylists()
        {
            try
            {
                Dictionary<string, List<string>> playLists = new Dictionary<string, List<string>>();
                
                // Get MP3 files in destination directory
                foreach (string file in Directory.GetFiles(DestinationFolder, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        TagLib.File audioFile = TagLib.File.Create(file);
                        string artist = audioFile.Tag.Performers.FirstOrDefault().ToString() ?? "Default";
                        string albumArtist = audioFile.Tag.AlbumArtists.FirstOrDefault() ?? "Default";
                        string album = audioFile.Tag.Album ?? "Default";
                        string genre = audioFile.Tag.Genres.FirstOrDefault() ?? "Default";

                        //Create playlists by artist
                        if (!playLists.ContainsKey(artist))
                        {
                            playLists.Add(artist, new List<string>());
                        }
                        if (!playLists[artist].Contains(file))
                        {
                            playLists[artist].Add(file);
                        }

                        //create playlists by Album Artist
                        if (!playLists.ContainsKey(albumArtist))
                        {
                            playLists.Add(albumArtist, new List<string>());
                        }
                        if (!playLists[albumArtist].Contains(file))
                        {
                            playLists[albumArtist].Add(file);
                        }

                        //create playlist by album
                        if (!playLists.ContainsKey(album))
                        {
                            playLists.Add(album, new List<string>());
                        }
                        if (!playLists[album].Contains(file))
                        {
                           playLists[album].Add(file);
                        }

                        //create playlist by genre
                        if (!playLists.ContainsKey(genre))
                        {
                            playLists.Add(genre, new List<string>());
                        }
                        if (!playLists.ContainsKey(album))
                        {
                            playLists[genre].Add(file);
                        }

                    }
                    catch(Exception ex) 
                    {
                        EzLogger.Log($"An error occurred: {ex.Message}", EzLogger.MessageType.ERROR);
                    }

                }

                // Write playlists to files
                foreach (var kvp in playLists)
                {
                    string playlistFilePath = Path.Combine(DestinationFolder, $"{kvp.Key}.m3u");
                    using (StreamWriter writer = new StreamWriter(playlistFilePath,true))
                    {
                        foreach (string filePath in kvp.Value)
                        {
                            writer.WriteLine(filePath);
                        }
                    }
                    EzLogger.Log($"Playlist for {kvp.Key} created: {playlistFilePath}", EzLogger.MessageType.INFO);
                }
            }
            catch (Exception ex)
            {
                EzLogger.Log($"An error occurred: {ex.Message}", EzLogger.MessageType.ERROR);
            }
        }

        /// <summary>
        /// Method to replace Invalid chars from a File path
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string ReplaceInvalidChars(string filename)
        {
            return string.Join("-", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }



    public static class EzLogger
    {
        public static void Log(string message, MessageType type)
        {
            try
            {
                switch (type)
                {
                    case MessageType.INFO: AnsiConsole.MarkupLine($"[green]{message}[/]");
                        break;
                    case MessageType.WARNING: AnsiConsole.MarkupLine($"[yellow]{message}[/]");
                        break;
                    case MessageType.ERROR: AnsiConsole.MarkupLine($"[red]{message}[/]");
                        break;
                    default:    AnsiConsole.MarkupLine($"[white]{message}[/]");
                        break;
                }
            }
            catch
            {
                Console.WriteLine(message);
            }
        }

        public enum MessageType{
            INFO, WARNING, ERROR
        }

    }
}
