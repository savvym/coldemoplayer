﻿using System;
using System.Windows.Controls;
using CDP.Core.Extensions;

namespace CDP.Gui.ViewModels
{
    internal class Analysis : Core.ViewModelBase
    {
        public string Header
        {
            get { return Strings.Analysis_Title.Args(demo.Name); }
        }

        public UserControl View { get; private set; }
        public Core.ViewModelBase ViewModel { get; private set; }
        public Core.DelegateCommand BackCommand { get; private set; }

        private readonly INavigationService navigationService = Core.ObjectCreator.Get<INavigationService>();
        private readonly Core.Demo demo;
        private readonly Core.ViewModelBase viewModel;

        public Analysis(Core.Demo demo)
        {
            this.demo = demo;
            viewModel = demo.Plugin.CreateAnalysisViewModel(demo);
            BackCommand = new Core.DelegateCommand(BackCommandExecute);
        }

        public override void OnNavigateComplete()
        {
            View = demo.Plugin.CreateAnalysisView();
            View.DataContext = viewModel;
            viewModel.OnNavigateComplete();
            OnPropertyChanged("View");
        }

        public void BackCommandExecute()
        {
            navigationService.Home();
        }
    }
}
