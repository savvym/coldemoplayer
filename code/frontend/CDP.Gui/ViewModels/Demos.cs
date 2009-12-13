﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Threading;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using CDP.Core;

namespace CDP.Gui.ViewModels
{
    internal class Demos : ViewModelBase
    {
        public class Item : NotifyPropertyChanged
        {
            public Core.Demo Demo { get; private set; }

            public BitmapImage Icon { get; private set; }
            public string Name
            {
                get { return Demo.Name; }
            }
            public string Game
            {
                get { return Demo.GameName; }
            }
            public string Map
            {
                get { return Demo.MapName; }
            }
            public string Perspective
            {
                get { return Demo.Perspective; }
            }
            public TimeSpan Duration
            {
                get { return Demo.Duration; }
            }

            public Item(Core.Demo demo, BitmapImage icon)
            {
                Demo = demo;
                Icon = icon;
            }
        }

        private Item selectedItem;

        public ObservableCollection<Item> Items { get; private set; }
        public Item SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (value != selectedItem)
                {
                    settings["LastFileName"] = (value == null ? null : fileSystem.GetFileName(value.Demo.FileName));
                    selectedItem = value;
                    mediator.Notify<Core.Demo>(Messages.SelectedDemoChanged, selectedItem == null ? null : selectedItem.Demo, true);
                }
            }
        }

        private readonly IMediator mediator = ObjectCreator.Get<IMediator>();
        private readonly INavigationService navigationService = ObjectCreator.Get<INavigationService>();
        private readonly IFileSystem fileSystem = ObjectCreator.Get<IFileSystem>();
        private readonly IconCache iconCache = new IconCache();
        private readonly IDemoManager demoManager = ObjectCreator.Get<IDemoManager>();
        private readonly ISettings settings = ObjectCreator.Get<ISettings>();
        private string fileNameToSelect = null;
        private Queue<Core.Demo> demosToLoad = new Queue<Core.Demo>();

        public Demos()
        {
            Items = new ObservableCollection<Item>();
            mediator.Register<SelectedFolderChangedMessageParameters>(Messages.SelectedFolderChanged, SelectedFolderChanged, this);
        }

        public void SelectedFolderChanged(SelectedFolderChangedMessageParameters parameters)
        {
            Items.Clear();
            SelectedItem = null;
            OnPropertyChanged("SelectedItem");

            if (!Directory.Exists(parameters.Path))
            {
                return;
            }

            fileNameToSelect = parameters.FileNameToSelect;
            string[] demoFileExtensions = demoManager.GetAllPluginFileExtensions();
            demosToLoad.Clear();

            foreach (string fileName in Directory.GetFiles(parameters.Path).Where(f => demoFileExtensions.Contains(fileSystem.GetExtension(f))).OrderBy(f => f))
            {
                Core.Demo demo = demoManager.CreateDemo(fileName);

                if (demo != null)
                {
                    demosToLoad.Enqueue(demo);
                }
            }

            // Start loading demos from the queue. The next demo will be loaded when the first demo completes, and so on.
            LoadNextDemoInQueue();
        }

        /// <summary>
        /// Loads the first enqueued demo in the demo queue, or does nothing if the queue is empty.
        /// </summary>
        public void LoadNextDemoInQueue()
        {
            if (demosToLoad.Count == 0)
            {
                return;
            }

            Core.Demo demo = demosToLoad.Dequeue();
            demo.OperationErrorEvent += demo_OperationErrorEvent;
            demo.OperationCompleteEvent += demo_OperationCompleteEvent;

            ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
            {
                demo.Load();
            }));
        }

        void demo_OperationErrorEvent(object sender, Core.Demo.OperationErrorEventArgs e)
        {
            navigationService.Invoke(() => 
            {
                Core.Demo demo = (Core.Demo)sender;
                demo.OperationErrorEvent -= demo_OperationErrorEvent;
                demo.OperationCompleteEvent -= demo_OperationCompleteEvent;
                navigationService.OpenModalWindow(new Views.DemoError(), new DemoError(demo.FileName, e.ErrorMessage, e.Exception));
                LoadNextDemoInQueue();
            });
        }

        void demo_OperationCompleteEvent(object sender, EventArgs e)
        {
            navigationService.Invoke(demo =>
            {
                demo.OperationErrorEvent -= demo_OperationErrorEvent;
                demo.OperationCompleteEvent -= demo_OperationCompleteEvent;
                Item item = new Item(demo, iconCache.FindIcon(demo.IconFileNames));
                Items.Add(item);
                IFileSystem fileSystem = ObjectCreator.Get<IFileSystem>();

                if (fileSystem.GetFileName(demo.FileName) == fileSystem.GetFileName(fileNameToSelect))
                {
                    SelectedItem = item;
                    OnPropertyChanged("SelectedItem");
                }

                LoadNextDemoInQueue();
            },
            (Core.Demo)sender);
        }
    }
}