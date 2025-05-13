using ConfigBridge.Library;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ConfigBridge.Tests
{
	[TestClass]
	public class CoreAppRunnerTests
	{
		private Mock<IFileSystem> fileSystemMock;
		private Mock<IProcessFactory> processFactoryMock;
		private Mock<IProcess> processMock;
		private CoreAppRunner coreAppRunner;

		[TestInitialize]
		public void SetUp()
		{
			fileSystemMock = new Mock<IFileSystem>();
			processFactoryMock = new Mock<IProcessFactory>();
			processMock = new Mock<IProcess>();

			processFactoryMock.Setup(factory => factory.CreateProcess()).Returns(processMock.Object);

			coreAppRunner = new CoreAppRunner(fileSystemMock.Object, processFactoryMock.Object);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RunCoreApp_ThrowsArgumentNullException_WhenCoreAppPathIsNull()
		{
			// Arrange
			string coreAppPath = null;
			var arguments = new Dictionary<string, string>();

			// Act
			coreAppRunner.RunCoreApp(coreAppPath, arguments);
		}

		[TestMethod]
		[ExpectedException(typeof(FileNotFoundException))]
		public void RunCoreApp_ThrowsFileNotFoundException_WhenCoreAppPathDoesNotExist()
		{
			// Arrange
			string coreAppPath = "nonexistent.exe";
			var arguments = new Dictionary<string, string>();
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(false);

			// Act
			coreAppRunner.RunCoreApp(coreAppPath, arguments);
		}

		[TestMethod]
		public void RunCoreApp_ExecutesDotnet_WhenCoreAppPathIsDll()
		{
			// Arrange
			string coreAppPath = "test.dll";
			var arguments = new Dictionary<string, string> { { "key", "value" } };
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			// Act & Assert
			try
			{
				coreAppRunner.RunCoreApp(coreAppPath, arguments);
			}
			catch (Exception ex)
			{
				Assert.Fail($"Expected no exception, but got: {ex.Message}");
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RunCoreApp_ThrowsArgumentNullException_WhenArgumentsAreNull()
		{
			// Arrange
			string coreAppPath = "test.exe";
			Dictionary<string, string> arguments = null;

			// Simulate that the file exists
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			// Act
			coreAppRunner.RunCoreApp(coreAppPath, arguments);
		}


		[TestMethod]
		public void RunCoreApp_HandlesEmptyArguments()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string>();
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			processMock.Setup(p => p.Start()).Verifiable();
			processMock.Setup(p => p.WaitForExit()).Verifiable();
			processMock.Setup(p => p.ExitCode).Returns(0);

			// Act
			coreAppRunner.RunCoreApp(coreAppPath, arguments);

			// Assert
			processMock.Verify(p => p.Start(), Times.Once);
			processMock.Verify(p => p.WaitForExit(), Times.Once);
		}


		[TestMethod]
		public void RunCoreApp_ThrowsApplicationException_WhenDotnetNotFound()
		{
			// Arrange
			string coreAppPath = "test.dll";
			var arguments = new Dictionary<string, string>();
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			processMock.Setup(p => p.Start()).Throws(new System.ComponentModel.Win32Exception("The system cannot find the file specified"));

			// Act & Assert
			Assert.ThrowsException<ApplicationException>(() => coreAppRunner.RunCoreApp(coreAppPath, arguments));
		}


		[TestMethod]
		public void RunCoreApp_PrintsDebugInfoInNonProduction()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string> { { "key", "value" } };

			// Simulate that the file exists
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			// Ensure the environment variable is not set to "Production"
			Environment.SetEnvironmentVariable("APP_ENV", "Development");

			using (var consoleOutput = new StringWriter())
			{
				Console.SetOut(consoleOutput);

				// Act
				coreAppRunner.RunCoreApp(coreAppPath, arguments, debugMode: true);

				// Assert
				string output = consoleOutput.ToString();
				Assert.IsTrue(output.Contains("Debug Mode: Executing .NET Application"));
				Assert.IsTrue(output.Contains("Executable/Command: test.exe"));
				Assert.IsTrue(output.Contains("Arguments: --key \"value\""));
			}

			// Clean up the environment variable
			Environment.SetEnvironmentVariable("APP_ENV", null);
		}


		[TestMethod]
		public void RunCoreApp_DisablesDebugModeInProduction()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string> { { "key", "value" } };

			// Simulate that the file exists
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			// Set the environment variable to "Production"
			Environment.SetEnvironmentVariable("APP_ENV", "Production");

			using (var consoleOutput = new StringWriter())
			{
				Console.SetOut(consoleOutput);

				// Act
				coreAppRunner.RunCoreApp(coreAppPath, arguments, debugMode: true);

				// Assert
				string output = consoleOutput.ToString();
				Assert.IsFalse(output.Contains("Debug Mode: Executing .NET Application"));
			}

			// Clean up the environment variable
			Environment.SetEnvironmentVariable("APP_ENV", null);
		}


		[TestMethod]
		public void RunCoreApp_ExecutesExeDirectly_WhenCoreAppPathIsExe()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string> { { "key", "value" } };

			// Simulate that the file exists
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			// Act & Assert
			try
			{
				coreAppRunner.RunCoreApp(coreAppPath, arguments);
			}
			catch (Exception ex)
			{
				Assert.Fail($"Expected no exception, but got: {ex.Message}");
			}
		}


		[TestMethod]
		public void RunCoreApp_ThrowsApplicationException_WhenProcessFailsToStart()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string>();
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			// Simulate a failure to start the process
			processMock.Setup(p => p.Start()).Throws(new InvalidOperationException("Failed to start process"));

			// Act & Assert
			try
			{
				coreAppRunner.RunCoreApp(coreAppPath, arguments);
				Assert.Fail("Expected ApplicationException was not thrown.");
			}
			catch (ApplicationException ex)
			{
				Assert.IsTrue(ex.Message.Contains("Failed to run .NET application"));
			}
		}


		[TestMethod]
		public void RunCoreApp_ThrowsApplicationException_WhenProcessExitsWithNonZeroCode()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string>();
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			processMock.Setup(p => p.Start()).Verifiable();
			processMock.Setup(p => p.WaitForExit()).Verifiable();
			processMock.Setup(p => p.ExitCode).Returns(1);

			// Act & Assert
			Assert.ThrowsException<ApplicationException>(() => coreAppRunner.RunCoreApp(coreAppPath, arguments));
		}

		[TestMethod]
		public void RunCoreApp_EscapesArgumentsCorrectly()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string>
						{
								{ "key", "value with spaces" },
								{ "anotherKey", "value\"with\"quotes" }
						};
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			// Act & Assert
			try
			{
				coreAppRunner.RunCoreApp(coreAppPath, arguments);
			}
			catch (Exception ex)
			{
				Assert.Fail($"Expected no exception, but got: {ex.Message}");
			}
		}

		[TestMethod]
		public void RunCoreApp_ExecutesProcess_WhenValidInputsAreProvided()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string> { { "key", "value" } };
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			processMock.Setup(p => p.Start()).Verifiable();
			processMock.Setup(p => p.WaitForExit()).Verifiable();
			processMock.Setup(p => p.ExitCode).Returns(0);

			// Act
			coreAppRunner.RunCoreApp(coreAppPath, arguments);

			// Assert
			processMock.Verify(p => p.Start(), Times.Once);
			processMock.Verify(p => p.WaitForExit(), Times.Once);
		}

		[TestMethod]
		public void RunCoreApp_PrintsDebugInfo_WhenDebugModeIsEnabled()
		{
			// Arrange
			string coreAppPath = "test.exe";
			var arguments = new Dictionary<string, string> { { "key", "value" } };
			bool debugMode = true;

			// Simulate that the file exists
			fileSystemMock.Setup(fs => fs.Exists(coreAppPath)).Returns(true);

			using (var consoleOutput = new StringWriter())
			{
				Console.SetOut(consoleOutput);

				// Act
				coreAppRunner.RunCoreApp(coreAppPath, arguments, debugMode);

				// Assert
				string output = consoleOutput.ToString();
				Assert.IsTrue(output.Contains("Debug Mode: Executing .NET Application"));
				Assert.IsTrue(output.Contains("Executable/Command: test.exe"));
				Assert.IsTrue(output.Contains("Arguments: --key \"value\""));
			}
		}

	}
}