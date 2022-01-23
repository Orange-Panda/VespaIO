# VespaIO - A Unity Developer Console
A runtime developer console that allows the user to execute commands enabling quick debugging.

## Download and Installation
Options for installation:
- Option A: Package Manager with Git (Recommended)
	1. Ensure you have Git installed and your Unity Version supports Git package manager imports (2019+)
	2. In Unity go to Window -> Package Manager
	3. Press the + icon in the top left
	4. Click "Add package from Git URL"
	5. Enter the following into the field and press enter: 
```
https://github.com/Orange-Panda/VespaIO.git
```
- Option B: Package Manager from Disk
	1. Download the repository and note down where the repository is saved.
	2. In Unity go to Window -> Package Manager
	3. Press the + icon in the top left
	4. Click "Add package from disk"
	5. Select the file from Step 1
- Option C: Import Package Manually
	1. Download the repository
	2. In Unity's project window drag the folder into the "Packages" folder on the left hand side beside the "Assets" folder

## Quick Start Guide
1. After importing the package into your Unity project create a settings asset by going to `Tools > Vespa IO > Select Console Settings`
	1. This file defines all the options you can configure for the Vespa IO Console. If this file is not present the defaults will be used instead.
2. In one of your scripts add the [StaticCommand] attribute to a static method and provide it with a key. This should look something like the following:
	```c#
	[StaticCommand("keyhere")]
	private static void MethodName()
	{
		// DO SOMETHING
	}
	```
3. Enter play mode and press the `~` key to open the console.
	1. If prompted to, import the TextMeshPro essentials. The default console runner requires these assets.
4. Enter the command key defined for the method in step 2.
5. Your method will now be called any time your enter the command.
6. Vespa IO comes with some built in commands, use the `help` command to see a list of all commands (built-in and user defined commands)

### Customizing Command Properties
More information about your commands can optionally be added to the attribute to document it or extend its functionality. The customizations are as follows:
- Name: The title of the command shown in the help manual. If not provided will automatically assign the method name to this property.
- Description: The description of the command shown in the help manual. If not provided will be empty.
- Cheat: Determines if this command requires cheats to be enabled in the console for its usage.
- Hidden: Determines if this command should be shown in the manual pages or searches.

A fully customized attribute will look like the following:
```c#
[StaticCommand("quit", Name = "Quit Application", Description = "Closes the application", Cheat = false, Hidden = false)]
public static void Quit()
{
	Application.Quit();
}
```

### Using Commands With Parameters
The static command attribute supports the following parameters from the user input: `string`, `int`, `float`, `bool`, and `LongString` (more info on LongString later).

Default values are respected by static commands and will be utilized if the user does not provide one in the console.

Consider the following example:
```c#
[StaticCommand("key")]
public static void MethodName(int integerParameter, float floatParameter = 2.2f)
{
	Log($"{integerParameter} - {floatParameter}");
}
```

With this command the console will output the following:

| INPUT                               | OUTPUT                   | NOTES                                                                                                                     |
| ----------------------------------- | ------------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| `key`                               | key USAGE: [int] [float] | Prints out the manual since no parameters were provided (a method without parameters will execute without this occurring) |
| `key 5`                             | 5 - 2.2                  | The first parameter is accepted and interpreted while the float uses its default value                                    |
| `key 5 3`                           | 5 - 3.0                  | Despite the second parameter being an integer it is cast to a float and sent to the method                                |
| `key some random input by the user` | **ERROR**                | The user gave parameters but they did not match and the user is informed about this mistake then prints out the manual.   |

### String Parameter Handling and the LongString Parameter
When assigning the StaticCommand attribute to a method with a string parameter it will only receive a single word in that index from the user input. 

Consider the following example:
```c#
[StaticCommand("key")]
public static void MethodName(string arg1, string arg2, string arg3)
{
	Log($"1: {arg1} 2: {arg2} 3: {arg3}");
}
```

With this command the console will output the following:
| INPUT                           | OUTPUT                      | NOTES                                                                                                                           |
| ------------------------------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `key first second`              | **ERROR**                   | Error occurs since there are too few parameters input                                                                           |
| `key first second third`        | 1: first 2: second 3: third | Prints out as expected                                                                                                          |
| `key first second third fourth` | **ERROR**                   | Error occurs since the method does not accept 4 strings                                                                         |
| `key 1 2 3`                     | **ERROR**                   | Despite the fact these could be cast to strings the console will interpret these as integers or floats, thus no method matches. |

As you can see this rule of the console can dramatically restrict the usage of string parameters. There is a way around this however, the LongString parameter. 

Now consider the following example:
```c#
[StaticCommand("key")]
public static void MethodName(LongString input)
{
	Log($"{input}");
}
```

This command will instead output the following:
| INPUT                           | OUTPUT                    |
| ------------------------------- | ------------------------- |
| `key first second`              | first second              |
| `key first second third`        | first second third        |
| `key first second third fourth` | first second third fourth |
| `key 1 2 3`                     | 1 2 3                     |

Thus LongString is very useful if you want to receive the full message the user input regardless of how many spaces it contains or if it is parsed to a non-string object. 

**NOTE:** LongString requires that it be the only parameter present.

## Limitations
- The collection of the commands is an expensive action when it is done and can take up to a few seconds to generate. This is only done once per play session but can seemingly freeze the application briefly when it is done. You can configure when this occurs in the console configuration file.