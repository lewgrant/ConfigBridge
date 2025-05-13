# ConfigBridge

**ConfigBridge** is a .NET Framework 4.8 console utility that bridges configuration values from a machine.config (or other sources) to .NET Core/5+ applications at runtime. It parses a JSON configuration, retrieves the requested values, and launches the target .NET application with those values as command-line arguments or environment variables.


---

## Features

- Retrieve settings and connection strings from machine.config or other sources.
- Pass configuration values as command-line arguments or environment variables to .NET Core/5+ apps.
- Supports both .exe and .dll (framework-dependent) .NET applications.
- Format JSON files for command-line usage with the `--json` flag.
- Optional debug output for troubleshooting.

---

## Usage


```
ConfigBridge.ConsoleApp.exe <appPath> <jsonConfigString> [--debug] [--ev] 
ConfigBridge.ConsoleApp.exe --json <path to .json file>

```

### Arguments

| Argument                | Required | Description                                                                                      |
|-------------------------|----------|--------------------------------------------------------------------------------------------------|
| `<appPath>`             | Yes      | Full path to the .NET Core executable (.exe) or .NET DLL to run.                                 |
| `<jsonConfigString>`    | Yes      | JSON string detailing configurations to retrieve and forward.                                    |
| `--json <path>`         | No       | Path to a JSON file to format for command-line usage.                                            |
| `--debug`               | No       | Enables detailed diagnostic output during execution.                                             |
| `--ev`                  | No       | Pass configuration values as environment variables instead of command-line arguments.            |

---

### Passing Configuration as Environment Variables

If you specify the `--ev` flag, each configuration value will be passed to the child process as an environment variable.  
**The environment variable name will be exactly as specified in the `OutputParameter` field of your JSON config.**

**Example:**  
If your config contains:
```json
[ { "ConfigType": "Sett", "Name": "ApiUrl", "OutputParameter": "API_URL" } ]
```

Then the child process will receive an environment variable named `API_URL`.

------

### JSON Object Structure for `<jsonConfigString>`

Each item in the JSON array should have the following structure:


```json
{
  "ConfigType": "Sett" | "Conn",      // Type of configuration: "Sett" for AppSetting, "Conn" for ConnectionString
  "Name": "<name_in_machine.config>", // Key in machine.config
  "OutputParameter": "<cli_param_name>" // Name for .NET app's CLI arg or environment variable (e.g., "apiUrl")
}

```

---

### Example JSON


```json
[
  {
    "ConfigType": "Sett",
    "Name": "MySetting",
    "OutputParameter": "setting1"
  },
  {
    "ConfigType": "Conn",
    "Name": "MyDb",
    "OutputParameter": "dbConnection"
  }
]

```

---

### Example Commands

#### Run a .NET Core EXE (CMD) with command-line arguments


```
ConfigBridge.exe "C:\path\to\core_app.exe" "[{\"ConfigType\":\"Sett\",\"Name\":\"SomeKey\",\"OutputParameter\":\"someOutput\"}]" --debug

```

#### Run a .NET Core EXE (CMD) with environment variables

```
ConfigBridge.exe "C:\path\to\core_app.exe" "[{"ConfigType":"Sett","Name":"SomeKey","OutputParameter":"MY_ENV_VAR"}]" --ev --debug
```

#### Run a .NET Core DLL (PowerShell)


```powershell
.\ConfigBridge.exe 'C:\path\to\framework_dependent_app.dll' '[{\"ConfigType\":\"Sett\",\"Name\":\"SomeKey\",\"OutputParameter\":\"someOutput\"}]' --debug

```

#### Run a .NET Core DLL (PowerShell) with environment variables

```powershell
.\ConfigBridge.exe 'C:\path\to\framework_dependent_app.dll' '[{"ConfigType":"Sett","Name":"SomeKey","OutputParameter":"MY_ENV_VAR"}]' --ev --debug
```


> **Note:** When running a .dll, `dotnet` must be in your system `PATH`.

---

### Format a JSON File for Command-Line Usage


```
ConfigBridge.ConsoleApp.exe --json C:\path\to\config.json

```

This will output a single-line JSON string suitable for passing as a command-line argument.

---

## Debug Mode

Add `--debug` to any command to enable detailed diagnostic output, including parsed arguments, configuration items, and resolved values.

---
