using System.IO;
using System.Windows.Media.Imaging;

namespace SimpleMusicReader;

public class Music
{
    public string Path { get; set; }
    public string Title { get; set; }
    public string Performers { get; set; }
    public string Album { get; set; }
    public BitmapImage Cover { get; set; }
    public int SongNumber { get; set; }

    public Music() { }

    public Music(string path, int songNumber)
    {
        Path = path;

        TagLib.File file = TagLib.File.Create(path);

        Title = file.Tag.Title;

        Performers = "";
        foreach (string artist in file.Tag.Performers)
        {
            Performers += artist + ", ";
        }
        if (Performers.Length > 0) Performers = Performers.Remove(Performers.Length - 2);

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