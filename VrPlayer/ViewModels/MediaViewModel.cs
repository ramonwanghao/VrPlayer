﻿using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;

using VrPlayer.Helpers;
using VrPlayer.Helpers.Mvvm;
using VrPlayer.Models;
using VrPlayer.Models.State;
using System.Windows.Media.Effects;

namespace VrPlayer.ViewModels
{
	public class MediaViewModel: ViewModelBase
	{
        private readonly IApplicationState _state;

        #region Fields

        private bool _hasDuration;
        public bool HasDuration
        {
            get
            {
                return _hasDuration;
            }
            set
            {
                _hasDuration = value;
                OnPropertyChanged("HasDuration");
            }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set
            {
                _isPlaying = value;
                OnPropertyChanged("IsPlaying");
            }
        }

        private double _progress;
        public double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;
                OnPropertyChanged("Progress");
            }
        }

        #endregion

        #region Commands

        private readonly ICommand _playCommand;
        public ICommand PlayCommand
        {
            get { return _playCommand; }
        }

        private readonly ICommand _pauseCommand;
        public ICommand PauseCommand
        {
            get { return _pauseCommand; }
        }

        private readonly ICommand _stopCommand;
        public ICommand StopCommand
        {
            get { return _stopCommand; }
        }

        private readonly ICommand _seekCommand;
        public ICommand SeekCommand
        {
            get { return _seekCommand; }
        }

        private readonly ICommand _setEffectCommand;
        public ICommand SetEffectCommand
        {
            get { return _setEffectCommand; }
        }

        private readonly ICommand _loopCommand;
        public ICommand LoopCommand
        {
            get { return _loopCommand; }
        }

        #endregion

        public MediaViewModel(IApplicationState state)
        {
            _state = state;

            //Todo: VM should not register events in the model
            _state.Media.MediaOpened += new RoutedEventHandler(_media_MediaOpened);
            _state.Media.MediaEnded += new RoutedEventHandler(_media_MediaEnded);
            _state.Media.SourceUpdated += new EventHandler<DataTransferEventArgs>(_media_SourceUpdated);

            //Commands
            _playCommand = new RelayCommand(Play, CanPlay);
            _pauseCommand = new RelayCommand(Pause, CanPause);
            _stopCommand = new RelayCommand(Stop, CanStop);
            _seekCommand = new RelayCommand(Seek, CanSeek);
            _setEffectCommand = new RelayCommand(SetEffect);
            _loopCommand = new RelayCommand(Loop);

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.DataBind);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
		}

        #region Events

        void timer_Tick(object sender, EventArgs e)
        {
            if (_state.Media.MediaDuration > 0)
            {
                Progress = _state.Media.MediaPosition / (double)_state.Media.MediaDuration * 100;
            }
            CommandManager.InvalidateRequerySuggested();
        }

        void _media_MediaOpened(object sender, RoutedEventArgs e)
        {
            HasDuration = _state.Media.MediaDuration > 0;
            StopCommand.Execute(null);
            PlayCommand.Execute(null);
        }

        void _media_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            HasDuration = _state.Media.MediaDuration > 0;
            StopCommand.Execute(null);
            PlayCommand.Execute(null);
        }

        void _media_MediaEnded(object sender, RoutedEventArgs e)
        {
            StopCommand.Execute(null);
        }

        #endregion

        #region Logic

        private void Play(object o)
        {
            if (CanPlay(o))
            {
                _state.Media.Play();
                IsPlaying = true;
            }
        }

        private bool CanPlay(object o)
        {
            return HasDuration && !IsPlaying;
        }

        private void Pause(object o)
        {
            if (CanPause(o))
            {
                _state.Media.Pause();
                IsPlaying = false;
            }
        }

        private bool CanPause(object o)
        {
            return HasDuration && IsPlaying;
        }

        private void Stop(object o)
        {
            if (CanStop(o))
            {
                _state.Media.Stop();
                _state.Media.MediaPosition = 0;
                IsPlaying = false;
            }
        }

        private bool CanStop(object o)
        {
            return _state.Media.MediaPosition > 0;
        }

        private void Seek(object o)
        {
            if (CanSeek(o))
            {
                double percentComplete = (double)o;
                _state.Media.MediaPosition = (long)(_state.Media.MediaDuration * percentComplete);
            }
        }

        private bool CanSeek(object o)
        {
            return _state.Media.MediaDuration > 0;
        }

        private void SetEffect(object o)
        {
            _state.Media.Effect = (Effect)o;
        }

        private void Loop(object o)
        { 
            bool loop = (bool)o;
            _state.Media.Loop = loop;
        }

        #endregion

    }
}
