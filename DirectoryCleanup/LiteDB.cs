using LiteDB;
using System.IO;
using System.Windows;

namespace DirectoryCleanup
{
    public class LiteDBHelper
    {
        private readonly string _ConnectionString;

        public LiteDBHelper(string inDBName, string inPath = null)
        {
            if (string.IsNullOrEmpty(inPath)) inPath = $@"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}";

            var _DBFile_ = $@"{inPath}\{inDBName}.dbl";
            _ConnectionString = $"Filename={_DBFile_};Upgrade=true;";

            using (var _DB_ = new LiteDatabase(_ConnectionString))
                _DB_.Mapper.IncludeFields = true;
        }

        public void StoreStruct<T>(T DataStruct, string Collection) where T : struct
        {
            using (var _DB_ = new LiteDatabase(_ConnectionString))
            {
                _DB_.Mapper.IncludeFields = true;

                var _doc_ = _DB_.Mapper.ToDocument(DataStruct);
                _doc_["_id"] = DataStruct.ToString();

                _DB_.GetCollection(Collection).Upsert(_doc_);
            }
        }

        public T RestoreStruct<T>(T DefaultDataStruct, string Collection) where T : struct
        {
            using (var _DB_ = new LiteDatabase(_ConnectionString))
            {
                _DB_.Mapper.IncludeFields = true;
                var _doc_ = _DB_.GetCollection(Collection).FindById(DefaultDataStruct.ToString());

                if (_doc_ != null)
                    return _DB_.Mapper.ToObject<T>(_doc_);

                return DefaultDataStruct;
            }
        }
    }

    public class WindowPosSizeStorrage
    {
        private struct StorageStruct
        {
            public bool Restore;
            public double Left;
            public double Top;
            public double Width;
            public double Height;
            public WindowState State;
        }

        private readonly Window _Window;
        private LiteDBHelper _Database;
        private bool _DontRestoreSize = false;

        public WindowPosSizeStorrage(Window InWindow)
        {
            _Window = InWindow;

            _Window.Closing += (sender, e) => { try { SaveWindow(); } catch { } };
            _Window.Unloaded += (sender, e) => { try { SaveWindow(); } catch { } };
            _Window.Loaded += (sender, e) => { try { RestoreWindow(); } catch { } };
        }

        public LiteDBHelper Database
        {
            set
            {
                _Database = value;
                RestoreWindow();
            }
        }

        public bool DontRestoreSize
        {
            get => _Window.ResizeMode == ResizeMode.NoResize ? true : _DontRestoreSize;
            set => _DontRestoreSize = value;
        }

        private void RestoreWindow()
        {
            var _Struct_ = new StorageStruct
            {
                Restore = false,
                Left = _Window.Left,
                Top = _Window.Top,
                Width = _Window.Width,
                Height = _Window.Height,
                State = _Window.WindowState,
            };

            _Struct_ = _Database.RestoreStruct(_Struct_, "WindowPosSizeStorrage");

            if (_Struct_.Restore)
            {
                _Window.Left = _Struct_.Left;
                if (_Window.Left < SystemParameters.VirtualScreenLeft)
                    _Window.Left = SystemParameters.VirtualScreenLeft;
                if (_Window.Left > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth)
                    _Window.Left = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100;

                _Window.Top = _Struct_.Top;
                if (_Window.Top < SystemParameters.VirtualScreenTop)
                    _Window.Top = SystemParameters.VirtualScreenTop;
                if (_Window.Top > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
                    _Window.Top = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100;

                if (!DontRestoreSize)
                {
                    _Window.SizeToContent = SizeToContent.Manual;
                    _Window.Width = _Struct_.Width;
                    _Window.Height = _Struct_.Height;
                    _Window.WindowState = _Struct_.State;
                }
            }
        }

        private void SaveWindow() => _Database.StoreStruct(new StorageStruct
        {
            Restore = true,
            Left = _Window.Left,
            Top = _Window.Top,
            Width = _Window.Width,
            Height = _Window.Height,
            State = _Window.WindowState,
        }, "WindowPosSizeStorrage");
    }
}
