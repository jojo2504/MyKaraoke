namespace MyKaraoke.Service.EnvironmentSetup{
    public static class Constants {
        public static string BaseAppDataPath => ConfigLoader.BaseAppDataPath;
        public static string LogsPath => ConfigLoader.LogsPath;
        public static string DatabasePath => ConfigLoader.DatabasePath;
        public static string FilesPath => ConfigLoader.FilesPath;
        public static string SolutionRoot => Helper.GetSolutionDirectoryInfo().FullName;
    }   
}