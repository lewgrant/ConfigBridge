using System;
using System.Collections.Generic;
using System.Text;

namespace ConfigBridge.Library
{
	/// <summary>
	/// Specifies the type of configuration to retrieve.
	/// </summary>
	public enum ConfigType
	{
		/// <summary>
		/// Represents an application setting from the <appSettings> section.
		/// </summary>
		Sett,

		/// <summary>
		/// Represents a connection string from the <connectionStrings> section.
		/// </summary>
		Conn
	}

	/// <summary>
	/// Represents a configuration item to be retrieved from machine.config
	/// and passed to the .NET Core application.
	/// </summary>
	public class ConfigItem
	{
		/// <summary>
		/// Gets or sets the type of the configuration item (AppSetting or ConnectionString).
		/// </summary>
		public ConfigType ConfigType { get; set; }

		/// <summary>
		/// Gets or sets the name (key) of the configuration item as it appears in machine.config.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the name of the output command-line parameter
		/// for the .NET Core application (e.g., "apiEndpoint" will become "--apiEndpoint").
		/// </summary>
		public string OutputParameter { get; set; }
	}
}