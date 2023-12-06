
using System;
using System.IO;
using System.Linq;
using TagLib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;


namespace MusicManagerClassLib;

public class MusicManagerClassLib
{
    
    private readonly ILogger<MusicManagerClassLib> _logger;

    private readonly Mp3LibraryConfiguration _configurationOptions;



    public  MusicManagerClassLib(ILogger<MusicManagerClassLib> logger, Mp3LibraryConfiguration configurationOptions)
    {
        _logger = logger;
        _configurationOptions = configurationOptions;
    }



     public void CopyAndOrganizeMp3(string sourcePath, string destinationRoot, string[] folderStructureTags)
        {
            // Ensure the source directory exists
            if (!Directory.Exists(sourcePath))
            {
                _logger.LogError($"Source directory not found: {sourcePath}");
            }

            // Iterate through each MP3 file in the source directory
            var mp3Files = Directory.GetFiles(sourcePath, "*.mp3");
            foreach (var mp3File in mp3Files)
            {
                var tagFile = TagLib.File.Create(mp3File);

                // Generate the destination path based on the specified folder structure tags
                var destinationPath = Path.Combine(destinationRoot,
                    string.Join(Path.DirectorySeparatorChar.ToString(),
                        folderStructureTags.Select(tag => tagFile.Tag)));

                // Ensure the destination directory exists or create it
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                // Generate the destination file path with Song Order - Song Name format
                var destinationFileName = $"{tagFile.Tag.Track:D2} - {tagFile.Tag.Title}.mp3";
                var destinationFilePath = Path.Combine(destinationPath, destinationFileName);

                // Copy the file to the destination
                System.IO.File.Copy(mp3File, destinationFilePath, true);

                _logger.LogInformation($"Copied {mp3File} to {destinationFilePath}");

            }
        }



}
