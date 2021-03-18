using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DirectoryCleanup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Type MyType = typeof(MainWindow);

        public static readonly RoutedCommand AboutCommand = new RoutedCommand("AboutCommand", MyType);
        public static readonly RoutedCommand CloseCommand = new RoutedCommand("CloseCommand", MyType);
        public static readonly RoutedCommand AddFolderCommand = new RoutedCommand("AddFolderCommand", MyType);
        public static readonly RoutedCommand DelFolderCommand = new RoutedCommand("DelFolderCommand", MyType);
        public static readonly RoutedCommand AddExceptionCommand = new RoutedCommand("AddExceptionCommand", MyType);
        public static readonly RoutedCommand DelExceptionCommand = new RoutedCommand("DelExceptionCommand", MyType);
        public static readonly RoutedCommand CleanUpCommand = new RoutedCommand("CleanUpCommand", MyType);

        private const string About = 
            "Directory Cleanup writen by Holger Wolff\r\n" +
            "Published under the BSD License.\r\n\r\n" +
            "Program icon from Ravindra Kalkani (https://www.iconfinder.com/UN-icon)\r\n" +
            "https://www.iconfinder.com/icons/3876395/extension_file_system_tmp_icon";

        private bool _Init = false;
        private bool _IsCeanupRunning = false;
        private MainDataModel _Model;
        private ConcurrentQueue<string> _Messages = new ConcurrentQueue<string>();

        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            DataContextChanged += (sender, e) => { _Model = DataContext as MainDataModel; };

            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        public MainDataModel Model
        {
            get => _Model;
            private set
            {
                if ((_Model != value) && (value != null))
                    _Model = value;
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_Init)
                    return;

                _Init = true;

                Model.StatusTextMessage = "Loading ...";

                await Task.Delay(50);
                await Init();

                Model.StatusTextMessage = string.Empty;

                Closing += async (sender, e) => await Model.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while loading...\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Dispatcher.InvokeShutdown();
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Error...\n\n{e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private async Task Init()
        {
            try
            {
                _ = new WindowPosSizeStorrage(this) { Database = new ConfigLoader() };

                #region command
                CommandBindings.Add(new CommandBinding(AboutCommand, (sender, e) => MessageBox.Show(About, "About", MessageBoxButton.OK)));
                CommandBindings.Add(new CommandBinding(CloseCommand, (sender, e) => Application.Current.Shutdown()));

                CommandBindings.Add(new CommandBinding(AddFolderCommand, AddFolder_Executed, (sender, e) => { e.CanExecute = !_IsCeanupRunning; }));
                CommandBindings.Add(new CommandBinding(DelFolderCommand, DelFolder_Executed, (sender, e) => { e.CanExecute = !_IsCeanupRunning; }));

                CommandBindings.Add(new CommandBinding(AddExceptionCommand, AddException_Executed, (sender, e) => { e.CanExecute = !_IsCeanupRunning && !Model.HasErrors && !string.IsNullOrEmpty(Model.NewException); }));
                CommandBindings.Add(new CommandBinding(DelExceptionCommand, DelException_Executed, (sender, e) => { e.CanExecute = !_IsCeanupRunning; }));

                CommandBindings.Add(new CommandBinding(CleanUpCommand, CleanUp_Executed, (sender, e) => { e.CanExecute = !_IsCeanupRunning; }));
                #endregion

                while (!_Model.IsLoaded)
                    await Task.Delay(250);
            }
            finally
            {
                Model.UIEnabled = true;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void AddFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var _OFD_ = new CommonOpenFileDialog { IsFolderPicker = true };
            if (_OFD_.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var _List_ = new List<string>(Model.FolderNames ?? new string[0]);
                if (!_List_.Contains(_OFD_.FileName))
                {
                    _List_.Add(_OFD_.FileName);
                    Model.FolderNames = _List_.ToArray();
                }
            }
        }

        private void DelFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var _List_ = new List<string>(Model.FolderNames ?? new string[0]);
            if (_List_.Contains(e.Parameter))
            {
                _List_.Remove((string)e.Parameter);
                Model.FolderNames = _List_.ToArray();
            }
        }

        private void DelException_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var _List_ = new List<string>(Model.IgnoreFilter ?? new string[0]);
            if (_List_.Contains(e.Parameter))
            {
                _List_.Remove((string)e.Parameter);
                Model.IgnoreFilter = _List_.ToArray();
            }
        }

        private void AddException_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var _List_ = new List<string>(Model.IgnoreFilter ?? new string[0]);
            if (!string.IsNullOrEmpty(Model.NewException) && !_List_.Contains(Model.NewException))
            {
                _List_.Add(Model.NewException);
                Model.NewException = string.Empty;
                Model.IgnoreFilter = _List_.ToArray();
            }
        }

        private void CleanUp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _IsCeanupRunning = true;
            _Messages.Clear();
            _Messages.Enqueue($"[{DateTime.Now:u}] Started clean up");
            try
            {
                var _RegExArray_ = new Regex[(Model.IgnoreFilter ?? new string[0]).Length];
                for (int _i_ = 0; _i_ < _RegExArray_.Length; _i_++)
                    _RegExArray_[_i_] = new Regex(Model.IgnoreFilter[_i_], RegexOptions.IgnoreCase);

                var _Folders_ = Model.FolderNames ?? new string[0];
                foreach (var _Folder_ in _Folders_)
                {
                    DeleteFiles(_RegExArray_, _Folder_);
                    DeleteFolders(_RegExArray_, _Folder_);
                }
            }
            catch (Exception ex)
            {
                _Messages.Enqueue($"[{DateTime.Now:u}] Errow while processing => {ex.Message}");
            }
            finally
            {
                _IsCeanupRunning = false;
                _Messages.Enqueue($"[{DateTime.Now:u}] Finished clean up");
                Model.Messages = string.Join(Environment.NewLine, _Messages.ToArray());
                Model.IsExpanded = true;
            }
        }

        private void DeleteFolders(Regex[] RegExList, string Folder)
        {
            var _Directories_ = System.IO.Directory.GetDirectories(Folder);
            foreach (var _Dir_ in _Directories_)
            {
                var _DirName_ = System.IO.Path.GetFileName(_Dir_);
                var _LastWrite_ = System.IO.File.GetLastWriteTimeUtc(_Dir_);


                if (_LastWrite_.AddDays(Model.HoldingTime) < DateTime.Now)
                {
                    var _Found_ = false;
                    foreach (var _RegEx_ in RegExList)
                        if (_RegEx_.IsMatch(_DirName_))
                        {
                            _Found_ = true;
                            break;
                        }

                    if (!_Found_)
                        DeleteDir(_Dir_);
                }
            }
        }

        private void DeleteFiles(Regex[] RegExList, string Folder)
        {
            var _Files_ = System.IO.Directory.GetFiles(Folder);
            foreach (var _File_ in _Files_)
            {
                var _FileName_ = System.IO.Path.GetFileName(_File_);
                var _LastWrite_ = System.IO.File.GetLastWriteTimeUtc(_File_);

                if (_LastWrite_.AddDays(Model.HoldingTime) < DateTime.Now)
                {
                    var _Found_ = false;
                    foreach (var _RegEx_ in RegExList)
                        if (_RegEx_.IsMatch(_FileName_))
                        {
                            _Found_ = true;
                            break;
                        }

                    if (!_Found_)
                        DeleteFile(_File_);
                }
            }
        }

        private void DeleteFile(string FileName)
        {
            try
            {
                System.IO.File.Delete(FileName);
                _Messages.Enqueue($"[{DateTime.Now:u}] Deleted file {FileName}");
            }
            catch (Exception ex)
            {
                _Messages.Enqueue($"[{DateTime.Now:u}] Can't delete file {FileName} => {ex.Message}");
            }
        }


        private void DeleteDir(string DirectoryName)
        {
            try
            {
                System.IO.Directory.Delete(DirectoryName, true);
                _Messages.Enqueue($"[{DateTime.Now:u}] Deleted dir {DirectoryName}");
            }
            catch (Exception ex)
            {
                _Messages.Enqueue($"[{DateTime.Now:u}] Can't delete dir {DirectoryName} => {ex.Message}");
            }
        }
    }
}
