using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AnimLib {
#if Linux
    class FileChooser {
        static private async Task<string?> RunZenity(string arguments) {
            var startInfo = new ProcessStartInfo() {
                FileName = "zenity",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using(var process = new Process())
            {
                process.StartInfo = startInfo;
                try {
                    process.Start();
                } catch (Exception) {
                    Debug.Error($"Failed to run file chooser. On Linux zenity is required, make sure it is installed!");
                }
                await process.WaitForExitAsync();
                var ret = process.StandardOutput.ReadLine();
                if(string.IsNullOrEmpty(ret)) {
                    Debug.Warning($"Zenity output was empty! Exit code: {process.ExitCode}, Error: {process.StandardError.ReadLine()}");
                    Debug.Log($"Zenity command-line was: \"{startInfo.FileName} {startInfo.Arguments}\"");
                    return null;
                }
                return ret;
            }
        }

        static public Task<string?> ChooseFile(string title, string path, string[] filters)
        {
            string arguments = $"--file-selection --title=\"{title}\"";
            if(!string.IsNullOrEmpty(path))
            {
                arguments += " --filename=\"" + path + "\"";
            }
            if (filters != null && filters.Length > 0)
            {
                arguments += " --file-filter=";
                foreach(var filter in filters)
                {
                    arguments += $"\"{filter}\"" + " ";
                }
            }
            return RunZenity(arguments);
        }

        static public Task<string?> ChooseDirectory(string title, string path)
        {
            var args = $"--file-selection --directory --save --title=\"{title}\"";
            if(!string.IsNullOrEmpty(path))
            {
                args += " --filename=\"" + path + "\"";
            }
            return RunZenity(args);
        }
    }
#endif
}
