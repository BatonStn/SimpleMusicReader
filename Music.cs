using System.IO;
using System.Windows.Media.Imaging;

namespace SimpleMusicReader;

public class Music
{
    public string Path { get; set; }
    public string Title { get; set; }
    public string Artists { get; set; }
    public string Album { get; set; }
    public BitmapImage Cover { get; set; }
    public int SongNumber { get; set; }

    public Music() { }

    public Music(Music music, int newNumber)
    {
        Path = music.Path;
        Title = music.Title;
        Artists = music.Artists;
        Album = music.Album;
        Cover = music.Cover;
        SongNumber = newNumber;
    }

    public Music(string path, int songNumber)
    {
        Path = path;

        TagLib.File file = TagLib.File.Create(path);

        Title = file.Tag.Title;

        Artists = "";
        foreach (string artist in file.Tag.AlbumArtists)
        {
            Artists += artist + ", ";
        }
        if (Artists.Length > 0) Artists = Artists.Remove(Artists.Length - 2);

        Album = file.Tag.Album;

        if (file.Tag.Pictures.Length > 0) Cover = LoadImage(file.Tag.Pictures[0].Data.Data);
        else
        {
            string folder = Directory.GetParent(path).FullName;
            foreach (string fileFromFolder in Directory.GetFiles(folder))
            {
                string extension = System.IO.Path.GetExtension(fileFromFolder);
                if (extension == ".jpg" || extension == ".png" || extension == ".jpeg")
                {
                    Cover = LoadImage(File.ReadAllBytes(fileFromFolder));
                    break;
                }
            }
            Cover ??= LoadImage(null);
        }

        SongNumber = songNumber;
    }

    private static BitmapImage LoadImage(byte[] imageData)
    {
        var image = new BitmapImage();
        if (imageData == null || imageData.Length == 0)
        {
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad; 
            image.UriSource = new Uri(@"questionmark.jpg", UriKind.Relative);
            image.EndInit();
        }
        else
        {
            using var mem = new MemoryStream(imageData);
            mem.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = mem;
            image.EndInit();
        }
        image.Freeze();
        return image;
    }
}

/*
//Local reference to the file
TagLib.File file = TagLib.File.Create("band1.mp3");

//Get the file metadata
Console.WriteLine("Tags on disk: " + file.TagTypesOnDisk);
Console.WriteLine("Tags in object: " + file.TagTypes);

Write ("Grouping", file.Tag.Grouping);
Write ("Title", file.Tag.Title);
Write ("Album Artists", file.Tag.AlbumArtists);
Write ("Performers", file.Tag.Performers);
Write ("Composers", file.Tag.Composers);
Write ("Conductor", file.Tag.Conductor);
Write ("Album", file.Tag.Album);
Write ("Genres", file.Tag.Genres);
Write ("BPM", file.Tag.BeatsPerMinute);
Write ("Year", file.Tag.Year);
Write ("Track", file.Tag.Track);
Write ("TrackCount", file.Tag.TrackCount);
Write ("Disc", file.Tag.Disc);
Write ("DiscCount", file.Tag.DiscCount);
*/