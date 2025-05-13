using ConfigBridge.Library;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConfigBridge.Application
{
	public class ConfigurationProcessor
	{
        private readonly IFileSystem fileSystem;

        public ConfigurationProcessor(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public List<ConfigItem> ParseConfigItems(string jsonConfig)
		{
			var configurationReader = new ConfigurationReader();
			return configurationReader.ParseConfigItems(jsonConfig);
		}
		public string FormatJsonFile(string jsonFilePath)
		{
            if (!fileSystem.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"The specified JSON file was not found: {jsonFilePath}");
            }

            string jsonContent = fileSystem.ReadAllText(jsonFilePath);

            // Validate and minify JSON
            string minifiedJson;
            try
            {
                using (var document = JsonDocument.Parse(jsonContent))
                {
                    minifiedJson = JsonSerializer.Serialize(document.RootElement);
                }
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON in file: {jsonFilePath}. Error: {ex.Message}");
            }

            // Escape quotes for command-line usage
            string formattedJson = minifiedJson.Replace("\"", "\\\"");

            return formattedJson;
        }
	}
}
