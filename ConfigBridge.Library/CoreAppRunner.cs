using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ConfigBridge.Library
{
    public interface IProcess
    {
        ProcessStartInfo StartInfo { get; set; }
        int ExitCode { get; }
        void Start();
        void WaitForExit();
        void BeginOutputReadLine();
        void BeginErrorReadLine();
        event DataReceivedEventHandler OutputDataReceived;
        event DataReceivedEventHandler ErrorDataReceived;
    }

    public class ProcessWrapper : IProcess
    {
        private readonly Process process;

        public ProcessWrapper()
        {
            process = new Process();
        }

        public ProcessStartInfo StartInfo
        {
            get => process.StartInfo;
            set => process.StartInfo = value;
        }

        public int ExitCode => process.ExitCode;

        public void Start() => process.Start();

        public void WaitForExit() => process.WaitForExit();

        public void BeginOutputReadLine() => process.BeginOutputReadLine();

        public void BeginErrorReadLine() => process.BeginErrorReadLine();

        public event DataReceivedEventHandler OutputDataReceived
        {
            add => process.OutputDataReceived += value;
            remove => process.OutputDataReceived -= value;
        }

        public event DataReceivedEventHandler ErrorDataReceived
        {
            add => process.ErrorDataReceived += value;
            remove => process.ErrorDataReceived -= value;
        }
    }

    public interface IProcessFactory
    {
        IProcess CreateProcess();
    }

    public class ProcessFactory : IProcessFactory
    {
        public IProcess CreateProcess() => new ProcessWrapper();
    }

    /// <summary>
    /// Handles the execution of the .NET application (exe or dll).
    /// </summary>
    public class CoreAppRunner
    {
        private static readonly string _dotnetExecutableName = "dotnet.exe";
        private readonly IFileSystem fileSystem;
        private readonly IProcessFactory processFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreAppRunner"/> class.
        /// </summary>
        /// <param name="fileSystem">An abstraction for file system operations.</param>
        /// <param name="processFactory">A factory for creating process instances.</param>
        public CoreAppRunner(IFileSystem fileSystem, IProcessFactory processFactory)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.processFactory = processFactory ?? throw new ArgumentNullException(nameof(processFactory));
        }

        /// <summary>
        /// Runs the specified .NET application with the given arguments.
        /// The application can be a direct executable (.exe) or a .dll to be run with 'dotnet'.
        /// </summary>
        /// <param name="coreAppPath">The full path to the .NET executable (.exe) or library (.dll).</param>
        /// <param name="arguments">A dictionary of arguments to pass to the application.
        /// Keys will be prefixed with '--' and values will be quoted.</param>
        /// <param name="debugMode">If true, prints the command to be executed.</param>
        /// <exception cref="ArgumentNullException">Thrown if coreAppPath is null or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the coreAppPath does not exist.</exception>
        /// <exception cref="ApplicationException">Thrown if the .NET application fails to start or exits with an error.</exception>
        public void RunCoreApp(string coreAppPath, Dictionary<string, string> arguments, bool debugMode = false, bool useEnvironmentVariables = false)
        {
            if (Environment.GetEnvironmentVariable("APP_ENV") == "Production")
            {
                debugMode = false;
            }

            if (string.IsNullOrWhiteSpace(coreAppPath))
            {
                throw new ArgumentNullException(nameof(coreAppPath), ".NET application path cannot be null or empty.");
            }

            if (!fileSystem.Exists(coreAppPath))
            {
                throw new FileNotFoundException($".NET application not found at path: {coreAppPath}", coreAppPath);
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments), "Arguments cannot be null.");
            }

            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            bool isDll = coreAppPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

            if (isDll)
            {
                processStartInfo.FileName = _dotnetExecutableName;
                processStartInfo.Arguments = $"\"{EscapeArgument(coreAppPath)}\"";
            }
            else
            {
                processStartInfo.FileName = coreAppPath;
                processStartInfo.Arguments = string.Empty;
            }

            if (useEnvironmentVariables)
            {
                foreach (var arg in arguments)
                {
                    string envVarName = arg.Key;
                    processStartInfo.EnvironmentVariables[envVarName] = arg.Value ?? string.Empty;
                }
            }
            else
            {
                var argsBuilder = new StringBuilder(processStartInfo.Arguments);
                if (arguments != null)
                {
                    if (argsBuilder.Length > 0)
                        argsBuilder.Append(" ");
                    foreach (var arg in arguments)
                    {
                        argsBuilder.Append($"--{arg.Key} \"{EscapeArgument(arg.Value)}\" ");
                    }
                }
                processStartInfo.Arguments = argsBuilder.ToString().TrimEnd();
            }

            if (debugMode)
            {
                Console.WriteLine("\n--- Debug Mode: Executing .NET Application ---");
                Console.WriteLine($"Executable/Command: {processStartInfo.FileName}");
                Console.WriteLine($"Arguments: {processStartInfo.Arguments}");
                if (useEnvironmentVariables)
                {
                    Console.WriteLine("Environment Variables:");
                    foreach (var arg in arguments)
                    {
                        string envVarName = $"CB_{arg.Key}";
                        Console.WriteLine($"  {envVarName} = {arg.Value}");
                    }
                }
                Console.WriteLine("----------------------------------------------");
            }

            try
            {
                var process = processFactory.CreateProcess();
                process.StartInfo = processStartInfo;

                process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new ApplicationException(
                            $".NET application '{Path.GetFileName(coreAppPath)}' (executed via '{processStartInfo.FileName}') exited with code {process.ExitCode}. " +
                            $"Check console output for errors from the application.");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (isDll && ex.Message.Contains("The system cannot find the file specified"))
                {
                    throw new ApplicationException($"Failed to start .NET application. Ensure '{_dotnetExecutableName}' is installed and in your system PATH. Original error: {ex.Message}", ex);
                }
                throw new ApplicationException($"Failed to start .NET application '{coreAppPath}'. Error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to run .NET application '{coreAppPath}'. Error: {ex.Message}", ex);
            }
        }


        private static string EscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return string.Empty;
            return arg.Replace("\"", "\\\"");
        }
    }
}
