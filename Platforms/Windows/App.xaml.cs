﻿using izolabella.Music.Platforms.Windows;
using izolabella.Music.Structure.Clients;
using izolabella.Music.Structure.Music.Songs;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml;
using NAudio.Wave;
using System.Reflection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace izolabella.LoFi.App.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            //string? Dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //if (Dir != null)
            //{
            //    //Stream S = new FileStream(Path.Combine(Dir, "MidnightVisitors.wav"), FileMode.Open, FileAccess.ReadWrite);
            //    //WindowsMusicPlayer P = new(new(S, 48000, 2));
            //    //P.StartAsync();
            //}

            MainPage.VolumeChanged += async (Vol) =>
            {
                while(this.Player == null)
                {
                    await Task.Delay(50);
                }
                this.Player?.SetVolume((float)Vol);
            };
            this.ServerQueue = MainPage.Client.GetServerQueue().Result ?? new();
            this.ControlSongLoop();
        }

        private int From { get; set; } = 0;

        public TimeSpan BufferDur { get; } = TimeSpan.FromSeconds(15);

        public int Max => this.Player?.Provider.BufferLength ?? 1;

        public int BufferSize => (int)this.Max / 4;

        public List<IzolabellaSong> ServerQueue { get; set; }

        public IzolabellaSong? CurrentlyPlaying => this.ServerQueue.FirstOrDefault();

        public TimeSpan TimeLeft => this.CurrentlyPlaying != null ? this.CurrentlyPlaying.FileInformation.FileDuration.Subtract(this.CurrentlyPlaying.GetTimeFromByteLength(this.From)) : TimeSpan.Zero;

        public WindowsMusicPlayer? Player { get; private set; }

        public TimeSpan TimeToFinish => this.CurrentlyPlaying?.GetTimeFromByteLength(this.CurrentlyPlaying.FileInformation.LengthInBytes - this.From) ?? TimeSpan.Zero;

        private async Task<bool> UpdateSong()
        {
            if(this.TimeToFinish <= TimeSpan.Zero)
            {
                this.From = 0;
                this.ServerQueue.Add(this.ServerQueue.ElementAt(0));
                this.ServerQueue.RemoveAt(0);
                if(this.CurrentlyPlaying != null)
                {
                    if(this.Player != null)
                    {
                        this.Player.Dispose();
                    }
                    this.Player = new WindowsMusicPlayer(this.CurrentlyPlaying, this.BufferDur);
                    await this.Player.StartAsync();
                }
                return true;
            }
            return false;
        }

        private async void ControlSongLoop()
        {
            bool SongUpdated = await this.UpdateSong();
            if (this.CurrentlyPlaying != null)
            {
                this.Player ??= new(this.CurrentlyPlaying, this.BufferDur);
                MainPage.MPSet(this.CurrentlyPlaying, this.TimeToFinish);
                byte[] Feed = await this.FillArrayAsync(this.CurrentlyPlaying);
                TimeSpan WaitFor = TimeSpan.FromSeconds(1);
                if (!SongUpdated && this.Player.Provider.BufferedDuration != TimeSpan.Zero)
                {
                    WaitFor = this.Player.Provider.BufferedDuration.Subtract(TimeSpan.FromMilliseconds(20));
                }
                Thread.Sleep((int)WaitFor.TotalMilliseconds);
                await Task.Delay(WaitFor);
                await this.Player.FeedBytesAsync(Feed);
            }
            this.ControlSongLoop();
        }

        private async Task<byte[]> FillArrayAsync(IzolabellaSong Current)
        {
            byte[] Feed = await MainPage.Client.GetBytesAsync(Current.Id, this.From, (int)this.Max);
            this.From += Feed.Length;
            return Feed;
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}