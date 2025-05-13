using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConfigBridge.Library
{
	/// <summary>
	/// Handles parsing of configuration requests and retrieval of values
	/// from the .NET configuration system (machine.config).
	/// </summary>
	public class ConfigurationReader
	{
		/// <summary>
		/// Parses a JSON string into a list of <see cref="ConfigItem"/> objects.
		/// </summary>
		/// <param name="jsonConfig">
		/// The JSON string representing an array of configuration items. Each item must include
		/// a valid <see cref="ConfigType"/>, <see cref="ConfigItem.Name"/>, and <see cref="ConfigItem.OutputParameter"/>.
		/// </param>
		/// <returns>A list of <see cref="ConfigItem"/> objects parsed from the JSON string.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonConfig"/> is null or whitespace.</exception>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="jsonConfig"/> exceeds the size limit, is invalid JSON, or results in no valid items.
		/// </exception>
		public List<ConfigItem> ParseConfigItems(string jsonConfig)
		{
			if (string.IsNullOrWhiteSpace(jsonConfig))
			{
				throw new ArgumentNullException(nameof(jsonConfig), "JSON configuration string cannot be null or empty.");
			}

			if (jsonConfig.Length > 10000) // Size limit
			{
				throw new ArgumentException("JSON configuration string is too large.", nameof(jsonConfig));
			}

			try
			{
				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true, // Allows for "configType" or "ConfigType" in JSON
					Converters = { new JsonStringEnumConverter() } // Handles enum parsing
				};
				List<ConfigItem> configItems = JsonSerializer.Deserialize<List<ConfigItem>>(jsonConfig, options);

				if (configItems == null || configItems.Count == 0)
				{
					throw new ArgumentException("Parsed JSON configuration resulted in no items or was invalid.", nameof(jsonConfig));
				}

				// Basic validation for each item
				foreach (var item in configItems)
				{
					if (string.IsNullOrWhiteSpace(item.Name))
					{
						throw new ArgumentException($"A ConfigItem is missing a 'Name'. JSON: {jsonConfig}", nameof(jsonConfig));
					}
					if (string.IsNullOrWhiteSpace(item.OutputParameter))
					{
						throw new ArgumentException($"A ConfigItem (Name: {item.Name}) is missing an 'OutputParameter'. JSON: {jsonConfig}", nameof(jsonConfig));
					}
				}
				return configItems;
			}
			catch (JsonException ex)
			{
				throw new ArgumentException($"Invalid JSON configuration string: {ex.Message}", nameof(jsonConfig), ex);
			}
		}


		/// <summary>
		/// Retrieves configuration values from the .NET configuration system (e.g., machine.config) based on the provided list of <see cref="ConfigItem"/> objects.
		/// </summary>
		/// <param name="configItems">
		/// The list of configuration items to retrieve. Each item specifies the configuration type (e.g., AppSetting or ConnectionString),
		/// the key to look up, and the output parameter name.
		/// </param>
		/// <param name="debugMode">
		/// If true, prints diagnostic information to the console. Debug mode is automatically disabled if the "APP_ENV" environment variable is set to "Production".
		/// </param>
		/// <returns>
		/// A dictionary where keys are the output parameter names (from <see cref="ConfigItem.OutputParameter"/>) and values are the retrieved configuration values.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configItems"/> is null.</exception>
		/// <remarks>
		/// Unsupported <see cref="ConfigType"/> values are skipped. Missing configuration values are ignored unless debug mode is enabled, in which case warnings are logged.
		/// </remarks>
		public Dictionary<string, string> GetConfigurationValues(List<ConfigItem> configItems, bool debugMode = false)
		{
			if (Environment.GetEnvironmentVariable("APP_ENV") == "Production")
			{
				debugMode = false;
			}

			if (configItems == null)
			{
				throw new ArgumentNullException(nameof(configItems));
			}

			var resolvedValues = new Dictionary<string, string>();

			if (debugMode)
			{
				Console.WriteLine("\n--- Debug Mode: Retrieving Configuration Values ---");
			}

			foreach (var item in configItems)
			{
				string value = null;
				string sourceType = string.Empty;

				try
				{
					switch (item.ConfigType)
					{
						case ConfigType.Sett:
							sourceType = "AppSetting";
							value = ConfigurationManager.AppSettings[item.Name];
							break;

						case ConfigType.Conn:
							sourceType = "ConnectionString";
							ConnectionStringSettings cs = ConfigurationManager.ConnectionStrings[item.Name];
							value = cs?.ConnectionString;
							break;

						default:
							if (debugMode)
							{
								Console.WriteLine($"[SKIPPING] Unsupported ConfigType: {item.ConfigType} for Name: '{item.Name}'");
							}
							continue; // Skip unsupported types
					}

					if (debugMode)
					{
						Console.WriteLine($"[{sourceType}] Request: Name='{item.Name}', OutputParam='--{item.OutputParameter}'");
						Console.WriteLine($"  Value: '{value ?? "NOT FOUND"}'");
					}

					if (value != null)
					{
						resolvedValues[item.OutputParameter] = value;
					}
					else
					{
						// Optionally, decide if a missing value should be an error or just a warning.
						// For now, it's a warning in debug mode, otherwise silently ignored if not found.
						if (debugMode)
						{
							Console.WriteLine($"  WARNING: Value for '{item.Name}' (Type: {item.ConfigType}) not found in configuration.");
						}
					}
				}
				catch (ConfigurationErrorsException ex)
				{
					// This can happen if there's an issue reading the config file itself (e.g., malformed)
					Console.WriteLine($"ERROR reading configuration for '{item.Name}': {ex.Message}");
					if (debugMode)
					{
						Console.WriteLine(ex.ToString());
					}
					// Depending on requirements, you might re-throw or collect errors.
					// For now, we log and continue.
				}
			}

			if (debugMode)
			{
				Console.WriteLine("--- End Configuration Value Retrieval ---");
			}
			return resolvedValues;
		}
	}
}