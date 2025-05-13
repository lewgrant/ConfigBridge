using ConfigBridge.Application;
using ConfigBridge.Library;

using System;
using System.Collections.Generic;

namespace ConfigBridge.ConsoleApp
{
	class Program
	{
		private static readonly string _appName = AppDomain.CurrentDomain.FriendlyName;

		static int Main(string[] args)
		{
			try
			{
				// Parse arguments
				var argumentParser = new ArgumentParser();
				var parsedArgs = argumentParser.Parse(args);
				// Handle --json flag
				if (!string.IsNullOrWhiteSpace(parsedArgs.JsonFilePath))
				{
					var configurationProcessor = new ConfigurationProcessor();
					string formattedJson = configurationProcessor.FormatJsonFile(parsedArgs.JsonFilePath);

					Console.WriteLine("\nFormatted JSON for Command-Line Usage:");
					Console.WriteLine(formattedJson);
					return 0; // Success
				}
				// Process configuration
				var configurationProcessorForConfig = new ConfigurationProcessor();
				var configItems = configurationProcessorForConfig.ParseConfigItems(parsedArgs.JsonConfig);

				// Debug logging
				var debugLogger = new DebugLogger(parsedArgs.DebugMode);
				debugLogger.LogArguments(parsedArgs);
				debugLogger.LogConfigItems(configItems);

				// Resolve configuration values
				var configurationReader = new ConfigurationReader();
				var resolvedValues = configurationReader.GetConfigurationValues(configItems, parsedArgs.DebugMode);
				debugLogger.LogResolvedValues(resolvedValues);

				// Execute the .NET Core application
				var coreAppRunner = new CoreAppRunner(new FileSystem(), new ProcessFactory());
				coreAppRunner.RunCoreApp(parsedArgs.CoreAppPath, resolvedValues, parsedArgs.DebugMode);

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("\n.NET Core application execution completed successfully.");
				Console.ResetColor();
				return 0; // Success
			}
			catch (Exception ex)
			{
				List<string> usage = new List<string>() {
					$"\nUsage: {_appName} <appPath> <jsonConfigString> [--debug] or {_appName} --json <path to .json file>",
					"\nArguments:",
					"  <appPath>            : (Required) Full path to the .NET CORE executable (.exe) or .NET DLL to run.",
					"  <jsonConfigString>   : (Required) JSON string detailing configurations to retrieve and forward.",
					"                       Example JSON: '[{\"ConfigType\":\"Sett\",\"Name\":\"MySetting\",\"OutputParameter\":\"setting1\"},{\"ConfigType\":\"Conn\",\"Name\":\"MyDb\",\"OutputParameter\":\"dbConnection\"}]'",
					"  --json <path>        : (Optional) Path to a JSON file to format for command-line usage.",
					"  --debug              : (Optional) Enables detailed diagnostic output during execution.",
					"\nJSON Object Structure for <jsonConfigString> items:",
					"  {",
					"    \"ConfigType\": \"Sett\" | \"Conn\",      \t// Type of configuration",
					"    \"Name\": \"<name_in_machine.config>\",   \t// Key in machine.config",
					"    \"OutputParameter\": \"<cli_param_name>\" \t// Name for .NET app's CLI arg (e.g., 'apiUrl')",
					"  }",
					"\nExample Command (CMD for .exe):",
					$"  {_appName} \"C:\\path\\to\\core_app.exe\" \"[{{\\\"ConfigType\\\":\\\"Sett\\\",\\\"Name\\\":\\\"SomeKey\\\",\\\"OutputParameter\\\":\\\"someOutput\\\"}}]\" --debug",
					"\nExample Command(PowerShell for .dll):",
					$"  .\\{_appName} 'C:\\path\\to\\framework_dependent_app.dll' '[{{\\\"ConfigType\\\":\\\"Sett\\\",\\\"Name\\\":\\\"SomeKey\\\",\\\"OutputParameter\\\":\\\"someOutput\\\"}}]' --debug",
					"\nNote: When running a .dll, 'dotnet' must be in your system PATH.",
					};
				var errorHandler = new ErrorHandler();
				return errorHandler.HandleException(ex, _appName, usage);
			}
		}
	}
}