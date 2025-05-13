using ConfigBridge.Library;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ConfigBridge.Tests
{
	[TestClass]
	public class ConfigurationReaderTests
	{
		private ConfigurationReader configurationReader;

		[TestInitialize]
		public void SetUp()
		{
			configurationReader = new ConfigurationReader();
		}

		[TestMethod]
		public void ParseConfigItems_ParsesValidJson()
		{
			// Arrange
			string json = "[{\"ConfigType\":\"Sett\",\"Name\":\"TestSetting\",\"OutputParameter\":\"testParam\"}]";

			// Act
			var result = configurationReader.ParseConfigItems(json);

			// Assert
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("TestSetting", result[0].Name);
			Assert.AreEqual("testParam", result[0].OutputParameter);
			Assert.AreEqual(ConfigType.Sett, result[0].ConfigType);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ParseConfigItems_ThrowsArgumentException_WhenConfigItemIsMissingOutputParameter()
		{
			// Arrange
			string json = "[{\"ConfigType\":\"Sett\",\"Name\":\"TestSetting\"}]";

			// Act
			configurationReader.ParseConfigItems(json);
		}

		[TestMethod]
		public void GetConfigurationValues_HandlesNullValuesInConfiguration()
		{
			// Arrange
			var configItems = new List<ConfigItem>
			 {
					 new ConfigItem { ConfigType = ConfigType.Sett, Name = "MissingSetting", OutputParameter = "missingParam" }
			 };

			// Act
			var result = configurationReader.GetConfigurationValues(configItems);

			// Assert
			Assert.AreEqual(0, result.Count); // No values should be added to the dictionary
		}


		[TestMethod]
		public void GetConfigurationValues_ReturnsEmptyDictionary_WhenConfigItemsIsEmpty()
		{
			// Arrange
			var configItems = new List<ConfigItem>();

			// Act
			var result = configurationReader.GetConfigurationValues(configItems);

			// Assert
			Assert.AreEqual(0, result.Count);
		}


		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ParseConfigItems_ThrowsArgumentNullException_WhenJsonIsNull()
		{
			// Act
			configurationReader.ParseConfigItems(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ParseConfigItems_ThrowsArgumentException_WhenConfigItemIsMissingName()
		{
			// Arrange
			string json = "[{\"ConfigType\":\"Sett\",\"OutputParameter\":\"testParam\"}]";

			// Act
			configurationReader.ParseConfigItems(json);
		}


		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ParseConfigItems_ThrowsArgumentException_WhenJsonExceedsSizeLimit()
		{
			// Arrange
			string oversizedJson = new string('a', 10001); // Create a string with 10,001 characters

			// Act
			configurationReader.ParseConfigItems(oversizedJson);
		}


		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ParseConfigItems_ThrowsArgumentException_WhenJsonIsInvalid()
		{
			// Arrange
			string json = "invalid json";

			// Act
			configurationReader.ParseConfigItems(json);
		}

		[TestMethod]
		public void GetConfigurationValues_RetrievesAppSetting()
		{
			// Arrange
			var configItems = new List<ConfigItem>
						{
								new ConfigItem { ConfigType = ConfigType.Sett, Name = "TestSetting", OutputParameter = "testParam" }
						};

			ConfigurationManager.AppSettings["TestSetting"] = "TestValue";

			// Act
			var result = configurationReader.GetConfigurationValues(configItems);

			// Assert
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("TestValue", result["testParam"]);
		}

		[TestMethod]
		public void GetConfigurationValues_RetrievesConnectionString()
		{
			// Arrange
			var configItems = new List<ConfigItem>
		{
				new ConfigItem { ConfigType = ConfigType.Conn, Name = "TestConnection", OutputParameter = "testConn" }
		};

			// Create a custom configuration file
			var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			config.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings("TestConnection", "Server=myServer;Database=myDb;"));
			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("connectionStrings");

			var configurationReader = new ConfigurationReader();

			// Act
			var result = configurationReader.GetConfigurationValues(configItems);

			// Assert
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("Server=myServer;Database=myDb;", result["testConn"]);
		}

		[TestMethod]
		public void GetConfigurationValues_DisablesDebugModeInProduction()
		{
			// Arrange
			var configItems = new List<ConfigItem>
		{
				new ConfigItem { ConfigType = ConfigType.Sett, Name = "TestSetting", OutputParameter = "testParam" }
		};

			// Set the environment variable to "Production"
			Environment.SetEnvironmentVariable("APP_ENV", "Production");

			using (var consoleOutput = new StringWriter())
			{
				Console.SetOut(consoleOutput);

				// Act
				var result = configurationReader.GetConfigurationValues(configItems, debugMode: true);

				// Assert
				string output = consoleOutput.ToString();
				Assert.IsFalse(output.Contains("Debug Mode: Retrieving Configuration Values"));
			}

			// Clean up the environment variable
			Environment.SetEnvironmentVariable("APP_ENV", null);
		}



		[TestMethod]
		public void GetConfigurationValues_SkipsUnsupportedConfigType()
		{
			// Arrange
			var configItems = new List<ConfigItem>
						{
								new ConfigItem { ConfigType = (ConfigType)999, Name = "Unsupported", OutputParameter = "unsupported" }
						};

			// Act
			var result = configurationReader.GetConfigurationValues(configItems);

			// Assert
			Assert.AreEqual(0, result.Count);
		}
	}
}
