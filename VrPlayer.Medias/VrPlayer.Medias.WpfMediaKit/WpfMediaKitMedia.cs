﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using VrPlayer.Contracts.Medias;
using VrPlayer.Helpers;
using VrPlayer.Helpers.Mvvm;
using WPFMediaKit.DirectShow.Controls;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace VrPlayer.Medias.WpfMediaKit
{
    public class WpfMediaKitMedia : MediaBase
    {
        private MediaElementBase _player;
        private DispatcherTimer _timer;

        public override FrameworkElement Media
        {
            get { return _player; }
        }

        public static readonly DependencyProperty PositionalAudioProperty =
            DependencyProperty.Register("PositionalAudio", typeof(bool),
            typeof(WpfMediaKitMedia), new FrameworkPropertyMetadata(false));
        public bool PositionalAudio
        {
            get { return (bool)GetValue(PositionalAudioProperty); }
            set { SetValue(PositionalAudioProperty, value); }
        }

        public static readonly DependencyProperty EvrRenderingProperty =
            DependencyProperty.Register("EvrRendering", typeof(bool),
            typeof(WpfMediaKitMedia), new FrameworkPropertyMetadata(false));
        public bool EvrRendering
        {
            get { return (bool)GetValue(EvrRenderingProperty); }
            set { SetValue(EvrRenderingProperty, value); }
        }

        public WpfMediaKitMedia()
        {
            //Commands
            OpenFileCommand = new RelayCommand(OpenFile);
            OpenDiscCommand = new RelayCommand(OpenDisc);
            OpenStreamCommand = new RelayCommand(OpenStream);
            OpenDeviceCommand = new RelayCommand(OpenDevice);
            OpenProcessCommand = new RelayCommand(o => { }, o => false);
            PlayCommand = new RelayCommand(Play, CanPlay);
            PauseCommand = new RelayCommand(Pause, CanPause);
            StopCommand = new RelayCommand(Stop, CanStop);
            SeekCommand = new RelayCommand(Seek, CanSeek);
            LoopCommand = new RelayCommand(Loop);

            _timer = new DispatcherTimer(DispatcherPriority.Send);
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 125);
            _timer.Tick += timer_Tick;
        }

        public override void Load()
        {
            Reset();
            _timer.Start();
        }

        public override void Unload()
        {
            Reset();
            _timer.Stop();
            if (_player != null)
                _player.Stop();
            _player = null;
        }

        private MediaUriElement CreateMediaUriElement()
        {
            var player = PositionalAudio ? new MediaGraphElement() : new MediaUriElement();
            player.BeginInit();
            if (EvrRendering)
                player.VideoRenderer = VideoRendererType.EnhancedVideoRenderer;
            player.MediaOpened += PlayerOnMediaOpened;
            player.EndInit();
            return player;
        }

        private void PlayerOnMediaOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_player is MediaSeekingElement)
                Duration = TimeSpan.FromTicks(((MediaSeekingElement)_player).MediaDuration);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (_player == null) return;
            UpdateMediaPosition();
            UpdatePositionalAudio();
        }

        private void UpdateMediaPosition()
        {
            if (!(_player is MediaSeekingElement)) return;
            Position = TimeSpan.FromTicks(((MediaSeekingElement)_player).MediaPosition);
        }

        private void UpdatePositionalAudio()
        {
            if (!(_player is MediaGraphElement)) return;
            var audioEngine = ((MediaGraphElement)_player).MediaGraphPlayer.AudioEngine;
            if (audioEngine == null) return;
            audioEngine.Position = AudioPosition;
            audioEngine.Rotation = AudioRotation;
        }

        #region Commands

        private void OpenFile(object o)
        {
            var path = o.ToString();
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var player = CreateMediaUriElement();
                player.Source = new Uri(path, UriKind.Absolute);
                player.Play();
                IsPlaying = true;
                _player = player;
            }
            catch (Exception exc)
            {
                var message = String.Format("Unable to load file '{0}'.", path);
                Logger.Instance.Warn(message, exc);
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            OnPropertyChanged("Media");
        }

        private void OpenDisc(object o)
        {
            if (o == null) return;
            var drive = (DriveInfo)o;
            try
            {
                var player = new DvdPlayerElement();
                player.BeginInit();
                player.PlayOnInsert = true;
                player.DvdDirectory = new Uri(string.Format(@"{0}\VIDEO_TS", drive.Name)).AbsolutePath;
                player.EndInit();
                _player.Play();
                IsPlaying = true;
                _player = player;
            }
            catch (Exception exc)
            {
                var message = String.Format("Unable to read disc '{0}'.", drive);
                Logger.Instance.Warn(message, exc);
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            OnPropertyChanged("Media");
        }

        private void OpenStream(object o)
        {
            var url = o.ToString();
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                var player = CreateMediaUriElement();
                player.Source = new Uri(url, UriKind.Absolute);
                player.Play();
                IsPlaying = true;
                _player = player;
            }
            catch (Exception exc)
            {
                var message = String.Format("Unable to load stream at '{0}'.", url);
                Logger.Instance.Warn(message, exc);
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            OnPropertyChanged("Media");
        }

        private void OpenDevice(object o)
        {
            var deviceIndex = (int) o;
            try
            {
                var player = new VideoCaptureElement();
                player.BeginInit();
                player.VideoCaptureDevice = MultimediaUtil.VideoInputDevices[deviceIndex];
                player.VideoCaptureSource = MultimediaUtil.VideoInputDevices[deviceIndex].Name;
                //player.Width = 320;
                //player.DesiredPixelWidth = 320;
                //player.Height = 240;
                //player.DesiredPixelHeight = 240;
                //player.FPS = 30;
                player.EndInit();

                player.Play();
                IsPlaying = true;
                _player = player;
            }
            catch (Exception exc)
            {
                const string message = "Unable to load selected device.";
                Logger.Instance.Warn(message, exc);
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            OnPropertyChanged("Media");
        }

        private void Play(object o)
        {
            _player.Play();
            IsPlaying = true;
        }

        private void Pause(object o)
        {
            _player.Pause();
            IsPlaying = false;
        }

        private void Stop(object o)
        {
            _player.Stop();
            IsPlaying = false;
        
            if (!(_player is MediaSeekingElement)) return;
            var player = ((MediaSeekingElement)_player);
            player.MediaPosition = 0;
            Position = TimeSpan.Zero;
        }

        private void Seek(object o)
        {
            if (!(_player is MediaSeekingElement)) return;
            var player = ((MediaSeekingElement) _player);
            player.MediaPosition = (long)(player.MediaDuration * Convert.ToDouble(o));
        }

        private void Loop(object o)
        {
            if(_player is MediaUriElement)
                ((MediaUriElement)_player).Loop = (bool)o;
        }

        #endregion
    }
}
