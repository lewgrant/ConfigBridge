using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ConfigBridge.Application
{
	public class ArgumentParser
	{
		public ParsedArguments Parse(string[] args)
		{
			if (args == null || args.Length < 1)
			{
				throw new ArgumentException("Invalid arguments. Usage: <coreAppPath> <jsonConfigString> [--debug] or --json <path to .json file>");
			}

			var parsedArgs = new ParsedArguments();
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].Equals("--debug", StringComparison.OrdinalIgnoreCase))
				{
					parsedArgs.DebugMode = true;
				}
				else if (args[i].Equals("--json", StringComparison.OrdinalIgnoreCase))
				{
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("The --json flag must be followed by a valid file path.");
					}

					parsedArgs.JsonFilePath = args[++i];
				}
				else if (parsedArgs.CoreAppPath == null)
				{
					parsedArgs.CoreAppPath = args[i];
				}
				else if (parsedArgs.JsonConfig == null)
				{
					parsedArgs.JsonConfig = args[i];
				}
				else
				{
					throw new ArgumentException($"Unexpected argument: {args[i]}");
				}
			}

			if (string.IsNullOrWhiteSpace(parsedArgs.JsonFilePath) &&
					( string.IsNullOrWhiteSpace(parsedArgs.CoreAppPath) || string.IsNullOrWhiteSpace(parsedArgs.JsonConfig) ))
			{
				throw new ArgumentException("Either --json <path to .json file> or <coreAppPath> and <jsonConfigString> are required.");
			}

			return parsedArgs;
		}
	}

	public class ParsedArguments
	{
		public string CoreAppPath { get; set; }
		public string JsonConfig { get; set; }
		public string JsonFilePath { get; set; }
		public bool DebugMode { get; set; }
	}
}

