using MyKaraoke.Service.Logging;

namespace MyKaraoke.Service.EnvironmentSetup{
    public static class Constants {
        public static string BaseAppDataPath => ConfigLoader.BaseAppDataPath;
        public static string LogsPath => ConfigLoader.LogsPath;
        public static string DatabasePath => ConfigLoader.DatabasePath;
        public static string FilesPath => ConfigLoader.FilesPath;
        public static string SolutionRoot => GetSolutionDirectoryInfo().FullName;
        
        public static DirectoryInfo GetSolutionDirectoryInfo(string currentPath = null) {
            return ConfigLoader.TryGetSolutionDirectoryInfo(currentPath);
        } 
    }   
}