# Remote Command Tester

English | [简体中文](README_zh.md)

Remote Command Tester is a desktop application for sending remote commands to [Revit-MCP-Plugin](https://github.com/revit-mcp/revit-mcp-plugin). This tool is primarily used for debugging Revit-MCP-Plugin functionality, providing a simple and intuitive way to test and verify the execution results of remote commands.

## Features

- **Direct Execution**: Send commands without relying on AI conversation clients
- **Response Viewing**: View command execution responses directly in the interface
- **Clean Interface**: Intuitive user interface, easy to learn and use

## Installation

1. Download the latest release
2. Extract to a location of your choice
3. Run `RevitRemoteCommandTester.exe` *Note: This application is based on .NET Framework and may require the corresponding runtime environment to be installed.*

## User Guide

### Creating and Managing Collections

1. Click the "Add Collection" button in the top left to create a new command collection
2. Name the collection for easy identification
3. You can rename or delete collections via the options menu (⋮) on the right side of the collection

### Adding and Managing Commands

1. Add a new command via the collection's options menu by selecting "Add Command"
2. You can also add a new command from the information panel after selecting a collection
3. Commands can be renamed or deleted through their options menu (⋮)

### Editing Command Parameters

1. When a command is selected, the JSON parameter editor will display in the right panel
2. Enter parameters in JSON format
3. Use the "Format JSON" button to format JSON parameters
4. Use the "Clear" button to clear parameters

### Sending Commands

1. After editing parameters, click the "Send Command" button at the bottom of the page to send the command
2. Command execution results will display in the response panel at the bottom
3. The response includes the sent command and the server's returned result

### Data Persistence

The application automatically saves all collections and commands to the `data` folder in the application directory. No manual saving is required, and content is automatically loaded when restarting the program.

## Technical Information

- Desktop application developed with C# and WPF
- Uses Newtonsoft.Json for JSON processing
- Uses TCP communication to send JSON-RPC requests

## Troubleshooting

### Common Issues

**Cannot Connect to Server**

- Ensure that Revit-MCP-Plugin is correctly installed and the MCP service is enabled

## Feedback

If you discover any issues or have suggestions for improvement, please submit an Issue.