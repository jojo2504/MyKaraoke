namespace MyKaraoke.Service.EnvironmentSetup {
    public static class Helper {
        public static DirectoryInfo GetSolutionDirectoryInfo(string currentPath = null) {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any()) {
                directory = directory.Parent;
            }
            return directory;
        }

        public static void ResetFileDirectory(){
            if (Directory.Exists(ConfigLoader.FilesPath)){
                Directory.Delete(ConfigLoader.FilesPath, true);
            }
        }
    }
}
