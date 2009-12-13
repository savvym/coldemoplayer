﻿using System;
using CDP.Core;

namespace CDP.Gui.ViewModels
{
    internal class Header : Core.ViewModelBase
    {
        public DelegateCommand PreferencesCommand { get; private set; }
        public DelegateCommand AboutCommand { get; private set; }

        private readonly INavigationService navigationService = Core.ObjectCreator.Get<INavigationService>();

        public Header()
        {
            PreferencesCommand = new DelegateCommand(OptionsCommandCanExecute, PreferencesCommandExecute);
            AboutCommand = new DelegateCommand(AboutCommandExecute);
        }

        public void PreferencesCommandExecute()
        {
            navigationService.Navigate(new Views.Preferences(), new Preferences());
        }

        public bool OptionsCommandCanExecute()
        {
            return (navigationService.CurrentPageTitle != "Preferences");
        }

        public void AboutCommandExecute()
        {
            System.Windows.MessageBox.Show("about");
        }
    }
}