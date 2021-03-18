namespace DirectoryCleanup
{
    public struct Settings
    {
        public string[] Folders;
        public string[] IgnoreFilter;
        public uint HoldingTime;

        public static Settings Empty
        {
            get
            {
                var _back_ = new Settings
                {
                    Folders = new string[] { },
                    IgnoreFilter = new string[] { },
                    HoldingTime = 2,
                };

                return _back_;
            }
        }
    }

    public class ConfigLoader : LiteDBHelper
    {
        public static Settings Config
        {
            get => new ConfigLoader().DBSettings;
            set => new ConfigLoader().DBSettings = value;
        }

        public const string DBName = "DirectoryCleanup";

        public ConfigLoader() : base(DBName, App.StartDirectory) { }

        private Settings DBSettings
        {
            get => RestoreStruct(Settings.Empty, "Config");
            set => StoreStruct(value, "Config");
        }
    }
}
