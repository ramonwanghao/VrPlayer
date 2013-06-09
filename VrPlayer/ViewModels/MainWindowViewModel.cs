﻿using System.Linq;
using System.Windows;
using System.Windows.Input;

using VrPlayer.Helpers.Mvvm;
using VrPlayer.Models.State;

namespace VrPlayer.ViewModels
{
    public class MainWindowViewModel: ViewModelBase
	{
        private readonly IApplicationState _state;

        public IApplicationState State
        {
            get { return _state; }
        }

        #region Commands

        private readonly ICommand _keyBoardCommand;
        public ICommand KeyboardCommand
        {
            get { return _keyBoardCommand; }
        }

        #endregion

        public MainWindowViewModel(IApplicationState state)
        {
            _state = state;

            //Commands
            _keyBoardCommand = new RelayCommand(ExecuteShortcut);
        }

        private void ExecuteShortcut(object o)
        {
            //Todo: Key mapping customization
            Key key = (Key)o;
            switch (key)
            {
                case Key.F1:
                    _state.TrackerPlugin.Content.Calibrate();
                    break;
                case Key.MediaPlayPause:
                case Key.Space:
                    TogglePlayPause();
                    break;
                case Key.LeftCtrl:
                    SettingsWindow.ShowSettings();
                    break;
                default:
                    break;
            }
        }

        private void TogglePlayPause()
        {
            if (_state.MediaPlayer.IsPlaying)
            {
                _state.MediaPlayer.Pause();
            }
            else
            {
                _state.MediaPlayer.Play();
            }
        }
	}
}