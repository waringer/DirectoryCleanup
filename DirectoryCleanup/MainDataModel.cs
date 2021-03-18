using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace DirectoryCleanup
{
    public class MainDataModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _Errors = new Dictionary<string, List<string>>();

        private Settings _Config;
        private bool _IsDirty = false;
        private string _AppTitle = "Directory Cleanup";
        private Visibility _WaitVisibility;
        private string _StatusTextMessage;
        private string _StatusTextToolTip;
        private string _NewException;
        private bool _IsExpanded;
        private string _Messages;

        public MainDataModel()
        {
            IsLoaded = false;
            _Errors = new Dictionary<string, List<string>>();

            if (!(bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue)
                Load();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(bool IgnoreDirtyState = false, [CallerMemberName] string PropertyName = "")
        {
            try
            {
                if (PropertyChanged != null)
                    foreach (PropertyChangedEventHandler _singleCast_ in PropertyChanged.GetInvocationList())
                        try
                        {
                            if ((_singleCast_.Target is ISynchronizeInvoke _syncInvoke_) && _syncInvoke_.InvokeRequired)
                                _syncInvoke_.Invoke(_singleCast_, new object[] { this, new PropertyChangedEventArgs(PropertyName) });
                            else
                                _singleCast_(this, new PropertyChangedEventArgs(PropertyName));
                        }
                        catch { }
            }
            catch { }

            IsDirty |= !IgnoreDirtyState;
        }
        #endregion

        #region INotifyDataErrorInfo
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected virtual void OnErrorsChanged([CallerMemberName] string PropertyName = "")
        {
            try
            {
                if (ErrorsChanged != null)
                    foreach (EventHandler<DataErrorsChangedEventArgs> _singleCast_ in ErrorsChanged.GetInvocationList())
                        try
                        {
                            if ((_singleCast_.Target is ISynchronizeInvoke _syncInvoke_) && _syncInvoke_.InvokeRequired)
                                _syncInvoke_.Invoke(_singleCast_, new object[] { this, new DataErrorsChangedEventArgs(PropertyName) });
                            else
                                _singleCast_(this, new DataErrorsChangedEventArgs(PropertyName));
                        }
                        catch { }
            }
            catch { }
        }

        public IEnumerable GetErrors(string PropertyName) => string.IsNullOrEmpty(PropertyName) ? null : (IEnumerable)(_Errors.ContainsKey(PropertyName) ? _Errors[PropertyName] : null);

        public bool HasErrors => _Errors.Count != 0;

        private void AddError(string Message, [CallerMemberName] string PropertyName = "")
        {
            var _ErrorMessages_ = new List<string>();

            if (_Errors.ContainsKey(PropertyName))
            {
                _ErrorMessages_ = _Errors[PropertyName];
                _ErrorMessages_.Add(Message);
                _Errors[PropertyName] = _ErrorMessages_;
            }
            else
            {
                _ErrorMessages_.Add(Message);
                _Errors.Add(PropertyName, _ErrorMessages_);
            }

            OnErrorsChanged(PropertyName);
        }

        private void ClearErrors([CallerMemberName] string PropertyName = "")
        {
            if (_Errors.ContainsKey(PropertyName))
            {
                _Errors.Remove(PropertyName);
                OnErrorsChanged(PropertyName);
            }
        }
        #endregion

        #region Load / Save
        public async Task Load()
        {
            try
            {
                var _Config_ = ConfigLoader.Config;

                // assign every single property to trigger gui update!
                _Config = _Config_;
                FolderNames = _Config_.Folders;
                IgnoreFilter = _Config_.IgnoreFilter;
                HoldingTime = _Config_.HoldingTime;

                IsDirty = false;
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while loading config values\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task Save()
        {
            await Task.Run(() =>
            {
                try
                {
                    ConfigLoader.Config = _Config;
                    IsDirty = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while saving config values\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }
        #endregion

        public bool IsDirty
        {
            get => _IsDirty;
            private set
            {
                if (_IsDirty != value)
                {
                    _IsDirty = value;
                    OnPropertyChanged(true);
                }
            }
        }

        public bool IsLoaded { get; private set; }

        public Settings Config => _Config;

        public string StatusTextMessage
        {
            get => _StatusTextMessage;
            set
            {
                if (_StatusTextMessage != value)
                {
                    _StatusTextMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusTextToolTip
        {
            get => _StatusTextToolTip;
            set
            {
                if (_StatusTextToolTip != value)
                {
                    _StatusTextToolTip = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AppTitle
        {
            get => _AppTitle;
            set
            {
                _AppTitle = value;
                OnPropertyChanged();
            }
        }

        public bool UIEnabled
        {
            get => WaitVisibility == Visibility.Collapsed;
            set
            {
                if (value)
                    WaitVisibility = Visibility.Collapsed;
                else
                    WaitVisibility = Visibility.Visible;

                OnPropertyChanged();
            }
        }

        public Visibility WaitVisibility
        {
            get => _WaitVisibility;
            set
            {
                if (_WaitVisibility != value)
                {
                    _WaitVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsExpanded
        {
            get => _IsExpanded;
            set
            {
                if (_IsExpanded != value)
                {
                    _IsExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Messages
        {
            get => _Messages;
            set
            {
                if (_Messages != value)
                {
                    _Messages = value;
                    OnPropertyChanged();
                }
            }
        }

        public string[] FolderNames
        {
            get => _Config.Folders;
            set
            {
                if (_Config.Folders != value)
                {
                    _Config.Folders = value;
                    OnPropertyChanged();
                }
            }
        }

        public string[] IgnoreFilter
        {
            get => _Config.IgnoreFilter;
            set
            {
                if (_Config.IgnoreFilter != value)
                {
                    _Config.IgnoreFilter = value;
                    OnPropertyChanged();
                }
            }
        }

        public uint HoldingTime
        {
            get => _Config.HoldingTime;
            set
            {
                if (_Config.HoldingTime != value)
                {
                    _Config.HoldingTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewException
        {
            get => _NewException;
            set
            {
                if (_NewException != value)
                {
                    _NewException = value;
                    OnPropertyChanged();

                    ClearErrors();
                    try
                    {
                        var _RegEx_ = new Regex(_NewException, RegexOptions.IgnoreCase);
                    }
                    catch (Exception ex)
                    {
                        AddError($"Invalid RegEx => {ex.Message}");
                    }
                }
            }
        }
    }
}
