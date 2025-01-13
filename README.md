# Mulib - Music Manager 2024 (Personal Project)
Ruben D. Lopez
Simple music manager to move music to a new location.

## Motive
I just wanted to move music in high quality from my iPod classic and other drives, to a USB that I can connect to a car or HiFi System.
The problem was that, music was in different qualities, and "organized" by iTunes, which makes impossible to search in Finder/Explorer and select.

Baiscally, the project just copy from one location to antoher but, it will create the folder structure based on mp3 tags.
- Artist
- Album

It will also, create Playlist in the root directory by Performer, ArtistAlbum, Album and Genre, although must of the Car Audios and HiFi system would that automatically as soon as you plug the external USB Stick/Drive.


## Options

Set options by quality (min 192kbps) and Tags completeness.

## Use

``
>/mulib/mulib dotnet run <source folder path> <destination folder path>
``
## Future Improvements

- appconfiguration file
- Allow to enter options by command line.
- A simple Avalonia UI that allows me to set Source(s), Destination, and structure of folders using any combination of <artist> <album artist> <genre> <compilation>

