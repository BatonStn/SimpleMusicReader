using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SimpleMusicReader;

public partial class MainWindow : Window
{
    private bool mediaPlayerIsPlaying = false;
    private bool userIsDraggingSlider = false;
    private bool isMusicLooping = false;

    private List<Music> listMusic = [];
    private Music[] currentListMusic = [];
    private List<Music[]> allMusicLists = [];
    private Music _currentMusic;

    private int subListSize;

    public Music CurrentMusic
    {
        get { return _currentMusic; }
        set
        {
            _currentMusic = value;
            currentTitle.Text = _currentMusic.Title;
            currentAlbum.Text = _currentMusic.Album;
            currentArtist.Text = _currentMusic.Artists;
            currentCover.ImageSource = _currentMusic.Cover;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        InitializePropertyValues();

        subListSize = 50;
        CurrentMusic = new();

        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };
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
                listMusic = [];
                foreach (string folder in folders)
                {
                    string[] files = Directory.GetFiles(folder);

                    foreach (string file in files)
                    {
                        string extension = Path.GetExtension(file);
                        if (extension == ".mp3" || extension == ".flac")
                        {
                            listMusic.Add(new Music(file, songNumber));
                            songNumber++;
                        }
                    }
                }

                if (listMusic.Count != 0)
                {
                    HidePagination();
                    CutMusicList();
                    if (listMusic.Count > subListSize)
                    {
                        DisplayPagination();
                    }

                    SetCurrentSong(currentListMusic[0]);
                    musicListView.ItemsSource = currentListMusic;
                    musicListView.SelectedItem = musicListView.Items.GetItemAt(0);

                    mePlayer.Pause();
                    playButton.Visibility = Visibility.Visible;
                    pauseButton.Visibility = Visibility.Collapsed;
                    mediaPlayerIsPlaying = false;
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

            listMusic = [];

            foreach(string file in files)
            {
                string extension = Path.GetExtension(file);
                if (extension == ".mp3" || extension == ".flac")
                {
                    listMusic.Add(new Music(file, songNumber));
                    songNumber++;
                }
            }

            if (listMusic.Count != 0)
            {
                HidePagination();
                CutMusicList();
                if (listMusic.Count > subListSize)
                {
                    DisplayPagination();
                }

                SetCurrentSong(currentListMusic[0]);
                musicListView.ItemsSource = currentListMusic;
                musicListView.SelectedItem = musicListView.Items.GetItemAt(0);

                mePlayer.Pause();
                playButton.Visibility = Visibility.Visible;
                pauseButton.Visibility = Visibility.Collapsed;
                mediaPlayerIsPlaying = false;
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
        musicListView.SelectedItem = musicListView.Items.GetItemAt(CurrentMusic.SongNumber % subListSize);
        musicListView.ScrollIntoView(musicListView.SelectedItem);
        ListViewItem item = musicListView.ItemContainerGenerator.ContainerFromItem(musicListView.SelectedItem) as ListViewItem;
        item.Focus();
    }

    void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem item && item.IsSelected)
        {
            SetCurrentSong(currentListMusic[musicListView.SelectedIndex]);
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
        int listSize = listMusic.Count;
        e.CanExecute = (listSize != 0) && (listSize != CurrentMusic.SongNumber + 1);
    }

    private void NextTrack_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if ((CurrentMusic.SongNumber + 1) % subListSize == 0)
        {
            currentListMusic = allMusicLists[(CurrentMusic.SongNumber + 1) / subListSize];
            SetCurrentSong(currentListMusic[0]);
            musicListView.ItemsSource = currentListMusic;
        }
        else
        {
            SetCurrentSong(listMusic[CurrentMusic.SongNumber + 1]);
        }
        ScrollToItem();
        sliProgress.Value = 0;
    }
    private void PreviousTrack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (listMusic.Count != 0) && (CurrentMusic.SongNumber != 0);
    }

    private void PreviousTrack_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (CurrentMusic.SongNumber % subListSize == 0)
        {
            currentListMusic = allMusicLists[(CurrentMusic.SongNumber / subListSize) - 1];
            SetCurrentSong(currentListMusic[subListSize - 1]);
            musicListView.ItemsSource = currentListMusic;
        }
        else
        {
            SetCurrentSong(listMusic[CurrentMusic.SongNumber - 1]);
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
        e.CanExecute = (listMusic.Count > 0);
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
            int listSize = listMusic.Count;
            if ((listSize != 0) && (listSize != CurrentMusic.SongNumber + 1))
            {
                if ((CurrentMusic.SongNumber + 1) % subListSize == 0)
                {
                    currentListMusic = allMusicLists[(CurrentMusic.SongNumber + 1) / subListSize];
                    SetCurrentSong(currentListMusic[0]);
                    musicListView.ItemsSource = currentListMusic;
                }
                else
                {
                    SetCurrentSong(listMusic[CurrentMusic.SongNumber + 1]);
                }
                ScrollToItem();
                sliProgress.Value = 0;
            }
        }
    }
    private void Randomize_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (listMusic.Count > 0);
    }

    private void Randomize_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Random rng = new();
        var shuffledcards = listMusic.OrderBy(_ => rng.Next()).ToList();
        listMusic = [];
        for (int i = 0; i < shuffledcards.Count; i++)
        {
            listMusic.Add(shuffledcards[i]);
            listMusic[i].SongNumber = i;
        }

        CutMusicList();

        SetCurrentSong(currentListMusic[0]);
        musicListView.ItemsSource = currentListMusic;
        musicListView.SelectedItem = musicListView.Items.GetItemAt(0);

        mePlayer.Pause();
        playButton.Visibility = Visibility.Visible;
        pauseButton.Visibility = Visibility.Collapsed;
        mediaPlayerIsPlaying = false;
    }

    void InitializePropertyValues()
    {
        // Set the media's starting Volume to the current value of their respective slider controls.
        mePlayer.Volume = volumeSlider.Value/100;
    }

    // Change the volume of the media.
    private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
    {
        mePlayer.Volume = volumeSlider.Value/100;
    }

    private void CutMusicList()
    {
        allMusicLists = [];
        int size = listMusic.Count;

        for(int i = 0; i < size; i+=subListSize)
        {
            Music[] subList = new Music[subListSize];
            for (int j = 0; j < subListSize && i+j<size; j++)
            {
                subList[j] = listMusic[i + j];
            }

            allMusicLists.Add(subList);
        }
        currentListMusic = allMusicLists[0];
    }

    private void DisplayPagination()
    {
        listCommands.Visibility = Visibility.Visible;
    }

    private void HidePagination()
    {
        listCommands.Visibility = Visibility.Collapsed;
    }
    private void FirstPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (CurrentMusic != null) && CurrentMusic.SongNumber >= subListSize;
    }

    private void FirstPage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        currentListMusic = allMusicLists[0];
        SetCurrentSong(currentListMusic[0]);
        musicListView.ItemsSource = currentListMusic;
        ScrollToItem();
        sliProgress.Value = 0;
    }
    private void LastPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (CurrentMusic != null) && (CurrentMusic.SongNumber / subListSize) < allMusicLists.Count - 1;
    }

    private void LastPage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        currentListMusic = allMusicLists[allMusicLists.Count - 1];
        SetCurrentSong(currentListMusic[0]);
        musicListView.ItemsSource = currentListMusic;
        ScrollToItem();
        sliProgress.Value = 0;
    }

    private void PreviousPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (CurrentMusic != null) && CurrentMusic.SongNumber >= subListSize;
    }

    private void PreviousPage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        currentListMusic = allMusicLists[(CurrentMusic.SongNumber / subListSize) - 1];
        SetCurrentSong(currentListMusic[0]);
        musicListView.ItemsSource = currentListMusic;
        ScrollToItem();
        sliProgress.Value = 0;
    }

    private void NextPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = (CurrentMusic != null) && (CurrentMusic.SongNumber / subListSize) < allMusicLists.Count - 1;
    }

    private void NextPage_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        currentListMusic = allMusicLists[(CurrentMusic.SongNumber / subListSize) + 1];
        SetCurrentSong(currentListMusic[0]);
        musicListView.ItemsSource = currentListMusic;
        ScrollToItem();
        sliProgress.Value = 0;
    }
}