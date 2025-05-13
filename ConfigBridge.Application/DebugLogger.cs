using ConfigBridge.Library;
using System;
using System.Collections.Generic;

namespace ConfigBridge.Application
{
    public class DebugLogger
    {
        private readonly bool debugMode;

        public DebugLogger(bool debugMode)
        {
            this.debugMode = debugMode;
        }

        public void LogArguments(ParsedArguments args)
        {
            if (!debugMode) return;

            Console.WriteLine("--- Debug Mode Enabled ---");
            Console.WriteLine($"Core App Path: {args.CoreAppPath}");
            Console.WriteLine($"JSON Config: {args.JsonConfig}");
            Console.WriteLine("--------------------------");
        }

        public void LogConfigItems(List<ConfigItem> configItems)
        {
            if (!debugMode) return;

            Console.WriteLine("\n--- Parsed Configuration Items ---");
            foreach (var item in configItems)
            {
                Console.WriteLine($"Type: {item.ConfigType}, Name: \"{item.Name}\", Output: \"--{item.OutputParameter}\"");
            }
            Console.WriteLine("----------------------------------");
        }

        public void LogResolvedValues(Dictionary<string, string> resolvedValues)
        {
            if (!debugMode) return;

            Console.WriteLine("\n--- Resolved Values to be Forwarded ---");
            foreach (var kvp in resolvedValues)
            {
                Console.WriteLine($"--{kvp.Key} \"{kvp.Value}\"");
            }
            Console.WriteLine("---------------------------------------");
        }
    }
}
