using MyKaraoke.Service.Logging;
using System.Diagnostics;

namespace MyKaraoke.Service.PythonServer {
    public class PythonScriptRunner {
        public static string ExecutePythonScriptWithCPython(string scriptPath, string[] arguments) {
            Logger.Log("Python script entry:");
            Logger.Log($"script => python {scriptPath} {string.Join(' ', arguments)}");

            // Set up the process start information to run Python with your script
            var startInfo = new ProcessStartInfo {
                FileName = "python", // Ensure Python is in your PATH or specify the full path to the Python executable
                Arguments = $"\"{scriptPath}\" {string.Join(" ", arguments.Select(arg => $"\"{arg}\""))}",
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Capture errors
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            try {
                // Start the process
                using (var process = Process.Start(startInfo)) {
                    // Read the standard output and error
                    using (var reader = process.StandardOutput)
                    using (var errorReader = process.StandardError) {
                        string result = reader.ReadToEnd();
                        string errorResult = errorReader.ReadToEnd();

                        if (!string.IsNullOrEmpty(result)) {
                            Logger.Important($"Python script output => {result}");
                        }

                        if (!string.IsNullOrEmpty(errorResult)) {
                            Logger.Error($"Python script error => {errorResult}");
                        }

                        // Return the result (or error if any)
                        return string.IsNullOrEmpty(result) ? errorResult : result;
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error($"Error executing Python script: {ex.Message}");
                return ex.Message; // Return the exception message if the process fails
            }
        }
    }
}