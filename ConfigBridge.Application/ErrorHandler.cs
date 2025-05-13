using System;
using System.Collections.Generic;

namespace ConfigBridge.Application
{
	public class ErrorHandler
	{
		public int HandleException(Exception ex, string appName, List<string> usage)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Error.WriteLine($"\nError: {ex.Message}");
			Console.ResetColor();

			if (ex is ArgumentException)
			{
				PrintUsage(appName, usage);
				return 1; // Invalid arguments
			}
			else if (ex is System.IO.FileNotFoundException)
			{
				return 2; // File not found
			}
			else if (ex is ApplicationException)
			{
				return 3; // Application error
			}
			else
			{
				return 4; // Unexpected error
			}
		}

		private void PrintUsage(string appName, List<string> usage)
		{
			foreach (var line in usage)
			{
				Console.WriteLine(line);
			}
		}
	}
}
