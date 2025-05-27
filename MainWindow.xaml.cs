using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using TagLib;

namespace SimpleMusicReader;

public partial class MainWindow : Window
{
    private bool mediaPlayerIsPlaying = false;
    private bool userIsDraggingSlider = false;
    private bool isMusicLooping = false;

    private List<Music> currentMusicList = [];

    private List<String> songs = [];
    private List<String[]> cutSongs = [];

    private int collectionSize = 0;
    private int currentPageSongNumber;
    private static int subListSize = 30;
    private int _currentPage;
    private int CurrentPage
    {
        get { return _currentPage; }
        set
        {
            _currentPage = value;
            pageNumber.Text = (_currentPage + 1).ToString();
        }
    }

    private Music _currentMusic;
    public Music CurrentMusic
    {
        get { return _currentMusic; }
        set
        {
            _currentMusic = value;
            currentTitle.Text = _currentMusic.Title;
            currentAlbum.Text = _currentMusic.Album;
            currentArtist.Text = _currentMusic.Performers;
            currentCover.ImageSource = _currentMusic.Cover;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        InitializePropertyValues();

        CurrentMusic = new();

        DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += timer_Tick;
        timer.Start();
    }

    private void timer_Tick(object sender, EventArgs e)
    {
        if ((mePlayer.Source != null) && (mePlayer.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
        {
            sliProgress.Minimum = 0;
            sliProgress.Maximum = mePlayer.NaturalDuration.TimeSpan.TotalSeconds;
            sliProgress.Value = mePlayer.Position.TotalSeconds;
        }
    }

    private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
    private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        OpenFolderDialog openFolderDialog = new();
        if (openFolderDialog.ShowDialog() == true)
        {
            int songNumber = 0;

            string fullPathToFolder = openFolderDialog.FolderName;
            string[] folders = Directory.GetDirectories(fullPathToFolder);

            if (folders.Length > 0)
            {
                songs = [];
                foreach (string folder in folders)
                {
                    string[] files = Directory.GetFiles(folder);

                    foreach (string file in files)
                    {
                        string extension = Path.GetExtension(file);
                        if (extension == ".mp3" || extension == ".flac")
                        {
                            songs.Add(file);
                            songNumber++;
                        }
                    }
                }

                collectionSize = songs.Count;

                if (collectionSize != 0)
                {
                    HidePagination();
                    CutMusicList();

                    mePlayer.Pause();
                    playButton.Visibility = Visibility.Visible;
                    pauseButton.Visibility = Visibility.Collapsed;
                    mediaPlayerIsPlaying = false;

                    albumAndArtist.Visibility = Visibility.Visible;
                }
            }
            else
            {
                MessageBox.Show("No subfolders in this folder");
            }
        }
    }

    private void Open_FolderCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
    private void Open_FolderExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        OpenFolderDialog openFolderDialog = new();
        if (openFolderDialog.ShowDialog() == true)
        {
            string fullPathToFolder = openFolderDialog.FolderName;
            string[] files = Directory.GetFiles(fullPathToFolder);

            int songNumber = 0;

            songs = [];

            foreach(string file in files)
            {
                string extension = Path.GetExtension(file);
                if (extension == ".mp3" || extension == ".flac")
                {
                    songs.Add(file);
                    songNumber++;
                }
            }

            collectionSize = songs.Count;

            if (collectionSize != 0)
            {
                HidePagination();
                CutMusicList();

                mePlayer.Pause();
                playButton.Visibility = Visibility.Visible;
                pauseButton.Visibility = Visibility.Collapsed;
                mediaPlayerIsPlaying = false; 
                
                albumAndArtist.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("No Music File in " +  fullPathToFolder);
            }
        }
    }

    private void SetCurrentSong(Music music)
    {
        mePlayer.Source = new Uri(music.Path);
        CurrentMusic = music;
    }

    public void ScrollToItem()
    {
        musicListView.SelectedItem = musicListView.Items.GetItemAt(currentPageSongNumber);
        musicListView.ScrollIntoView(musicListView.SelectedItem);
        ListViewItem item = musicListView.ItemContainerGenerator.ContainerFromItem(musicListView.SelectedItem) as ListViewItem;
        item.Focus();
    }
    public void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem item && item.IsSelected)
        {
            SetCurrentSong(currentMusicList[musicListView.SelectedIndex]);
            currentPageSongNumber = musicListView.SelectedIndex;
            if (!mediaPlayerIsPlaying)
            {
                mePlayer.Play();
                playButton.Visibility = Visibility.Collapsed;
                pauseButton.Visibility = Visibility.Visible;
                mediaPlayerIsPlaying = true;
            }
        }
    }

    private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (mePlayer != null) && (mePlayer.Source != null);
    }
    private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        mePlayer.Play();
        playButton.Visibility = Visibility.Collapsed;
        pauseButton.Visibility = Visibility.Visible;
        mediaPlayerIsPlaying = true;
    }

    private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = mediaPlayerIsPlaying;
    }
    private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        mePlayer.Pause();
        playButton.Visibility = Visibility.Visible;
        pauseButton.Visibility = Visibility.Collapsed;
        mediaPlayerIsPlaying = false;
    }

    private void NextTrack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (collectionSize != 0) && (collectionSize > CurrentMusic.SongNumber + 1);
    }
    private void NextTrack_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (currentPageSongNumber == subListSize - 1)
        {
            CurrentPage++;
            currentPageSongNumber = 0;

            currentMusicList = StringArrayToMusicList(cutSongs[CurrentPage], CurrentPage);

            SetCurrentSong(currentMusicList[0]);
            musicListView.ItemsSource = currentMusicList;
        }
        else
        {
            currentPageSongNumber++;
            SetCurrentSong(currentMusicList[currentPageSongNumber]);
        }
        ScrollToItem();
        sliProgress.Value = 0;
    }

    private void PreviousTrack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (collectionSize != 0) && (CurrentMusic.SongNumber != 0);
    }
    private void PreviousTrack_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (currentPageSongNumber == 0)
        {
            CurrentPage--;
            currentPageSongNumber = subListSize - 1;

            currentMusicList = StringArrayToMusicList(cutSongs[CurrentPage], CurrentPage);

            SetCurrentSong(currentMusicList[subListSize - 1]);
            musicListView.ItemsSource = currentMusicList;
        }
        else
        {
            currentPageSongNumber--;
            SetCurrentSong(currentMusicList[currentPageSongNumber]);
        }
        ScrollToItem();
        sliProgress.Value = 0;
    }

    private void sliProgress_DragStarted(object sender, DragStartedEventArgs e)
    {
        userIsDraggingSlider = true;
    }
    private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        userIsDraggingSlider = false;
        mePlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
    }
    private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        lblProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        lblReverseProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Maximum - sliProgress.Value).ToString(@"hh\:mm\:ss");
    }
    
    private void Loop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (collectionSize > 0);
    }
    private void Loop_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (isMusicLooping)
        {
            isMusicLooping = false;
            loopButton.Visibility = Visibility.Visible;
            noLoopButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            isMusicLooping = true; 
            loopButton.Visibility = Visibility.Collapsed;
            noLoopButton.Visibility = Visibility.Visible;
        }
    }

    private void MediaEnded(object sender, EventArgs e)
    {
        if (isMusicLooping)
        {
            mePlayer.Position = TimeSpan.Zero;
            mePlayer.Play();
        }
        else
        {
            if ((collectionSize != 0) && (collectionSize != CurrentMusic.SongNumber + 1))
            {
                if (currentPageSongNumber == subListSize - 1)
                {
                    CurrentPage++;
                    currentPageSongNumber = 0;

                    currentMusicList = StringArrayToMusicList(cutSongs[CurrentPage], CurrentPage);

                    SetCurrentSong(currentMusicList[0]);
                    musicListView.ItemsSource = currentMusicList;
                }
                else
                {
                    currentPageSongNumber++;
                    SetCurrentSong(currentMusicList[currentPageSongNumber]);
                }
                ScrollToItem();
                sliProgress.Value = 0;
            }
            else
            {
                mePlayer.Pause();
                playButton.Visibility = Visibility.Visible;
                pauseButton.Visibility = Visibility.Collapsed;
                mediaPlayerIsPlaying = false;
            }
        }
    }
    
    private void Randomize_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (collectionSize > 0);
    }
    private void Randomize_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Random rng = new();
        var shuffledcards = songs.OrderBy(_ => rng.Next()).ToList();
        songs = [];
        for (int i = 0; i < shuffledcards.Count; i++)
        {
            songs.Add(shuffledcards[i]);
        }

        CutMusicList();

        mePlayer.Pause();
        playButton.Visibility = Visibility.Visible;
        pauseButton.Visibility = Visibility.Collapsed;
        mediaPlayerIsPlaying = false;
    }

    void InitializePropertyValues()
    {
        mePlayer.Volume = volumeSlider.Value/100;
    }
    private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
    {
        mePlayer.Volume = volumeSlider.Value/100;
    }

    private void CutMusicList()
    {
        cutSongs = [];

        for (int i = 0; i < collectionSize; i+=subListSize)
        {
            string[] subList = new string[subListSize];

            for (int j = 0; j < subListSize && i+j< collectionSize; j++)
            {
                subList[j] = songs[i + j];
            }

            cutSongs.Add(subList);
        }
        //previousMusicList = [];
        currentMusicList = StringArrayToMusicList(cutSongs[0], 0);

        if (collectionSize > subListSize)
        {
            DisplayPagination();
            //nextMusicList = StringArrayToMusicList(cutSongs[1], 1);
            pageTotal.Text = cutSongs.Count.ToString();
        }

        CurrentPage = 0;
        currentPageSongNumber = 0;
        SetCurrentSong(currentMusicList[0]);
        musicListView.ItemsSource = currentMusicList;
        musicListView.SelectedItem = musicListView.Items.GetItemAt(0);
    }

    private static List<Music> StringArrayToMusicList(string[] cutSongsArray, int songIndex)
    {
        List<Music> musicList = [];
        for (int i = 0; i < cutSongsArray.Length; i++)
        {
            if (cutSongsArray[i] == null) break;
            musicList.Add(new Music(cutSongsArray[i], songIndex*subListSize + i));
        }
        return musicList;
    }

    private void DisplayPagination()
    {
        listCommands.Visibility = Visibility.Visible;
        pageChooser.Visibility = Visibility.Visible;
    }
    private void HidePagination()
    {
        listCommands.Visibility = Visibility.Collapsed;
    }
    
    private void FirstPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CurrentMusic != null && CurrentPage > 0;
    }
    private void FirstPage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        //previousMusicList = [];
        currentMusicList = StringArrayToMusicList(cutSongs[0], 0);
        //nextMusicList = StringArrayToMusicList(cutSongs[1], 1);
        
        CurrentPage = 0;
        currentPageSongNumber = 0;
        SetCurrentSong(currentMusicList[0]);
        musicListView.ItemsSource = currentMusicList;
        ScrollToItem();
        sliProgress.Value = 0;
    }
    
    private void LastPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CurrentMusic != null && CurrentPage < (cutSongs.Count - 1);
    }
    private void LastPage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        int pageNumber = cutSongs.Count - 1;

        //previousMusicList = StringArrayToMusicList(cutSongs[pageNumber - 1], pageNumber - 1);
        currentMusicList = StringArrayToMusicList(cutSongs[pageNumber], pageNumber);
        //nextMusicList = [];

        CurrentPage = pageNumber;
        currentPageSongNumber = 0;

        SetCurrentSong(currentMusicList[0]);
        musicListView.ItemsSource = currentMusicList;
        ScrollToItem();
        sliProgress.Value = 0;
    }

    private void PreviousPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CurrentMusic != null && CurrentPage > 0;
    }
    private void PreviousPage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        //nextMusicList = currentMusicList;
        currentMusicList = StringArrayToMusicList(cutSongs[CurrentPage - 1], CurrentPage - 1);

        CurrentPage--;
        currentPageSongNumber = 0;

        SetCurrentSong(currentMusicList[0]);
        musicListView.ItemsSource = currentMusicList;

        ScrollToItem();
        sliProgress.Value = 0;
    }

    private void NextPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CurrentMusic != null && CurrentPage < (cutSongs.Count - 1);
    }
    private void NextPage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        CurrentPage++;
        currentPageSongNumber = 0;

        //previousMusicList = currentMusicList;
        currentMusicList = StringArrayToMusicList(cutSongs[CurrentPage], CurrentPage);

        SetCurrentSong(currentMusicList[0]);
        musicListView.ItemsSource = currentMusicList;

        ScrollToItem();
        sliProgress.Value = 0;
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (int.TryParse(pageNumberBox.Text, out int n))
            {
                if (n > 0 && n < cutSongs.Count + 1) ChosenPage_Executed(n);
                else MessageBox.Show("Page Number must be between 1 and " + pageTotal.Text);
            }
            else
            {
                MessageBox.Show("Page Number must be numerical");
            }
        }
    }

    private void ChosenPage_Executed(int n)
    {
        CurrentPage = n - 1;
        currentPageSongNumber = 0;

        currentMusicList = StringArrayToMusicList(cutSongs[CurrentPage], CurrentPage);

        SetCurrentSong(currentMusicList[0]);
        musicListView.ItemsSource = currentMusicList;

        ScrollToItem();
        sliProgress.Value = 0;
    }
}