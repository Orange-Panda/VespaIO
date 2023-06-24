# VespaIO

![VespaIO Logo](.github/images/VespaIO%20Readme.png)

A runtime developer console for Unity that runs commands for rapid debugging and testing.

## Features

- Supports commands on fields, properties, and methods.
- Use commands in a static context and Unity Object instances.
- Quick setup and easy command definitions using the `[VespaCommand]` attribute.
- Set aliases for commands to shortcut frequently used commands
- Preview values and command names in console with autofill values
- Execute multiple commands in a single input using `;` symbol

## Quick Start Guide

1. Install the package via Git in the Package Manager
    1. Ensure you have Git installed and your Unity Version supports Git package manager imports (2019+)
    2. In Unity go to `Window -> Package Manager`
    3. Press the + icon at the top left of the Package Manager window
    4. Choose "Add package from Git URL"
    5. Enter the following into the field and press enter:
        1. Tip: You can append a version to the end of the Git URL to lock it to a specific version such as `https://github.com/Orange-Panda/VespaIO.git#v2.0.0`
   ```
   https://github.com/Orange-Panda/VespaIO.git
   ```
2. Create a Settings file using `Tools -> VespaIO -> Select Console Settings`
3. Import the `Developer Console` sample from this package in the Package Manager.
    1. This imports a default implementation of the developer console for the Unity UI system using TextMeshPro.
4. Run your application and press the `` ` `` key to view the console.
    1. Try out some of the native commands such as `help`, `scene`, `quit`.
5. Add a command to your code by adding the `[VespaCommand]` attribute to it.
    1. Example: `[VespaCommand("get_position", "Get the position of this object")]` - Defines a non-cheat command that prints the player position.
    2. Example: `[VespaCommand("item_grant", "Grant and item to the player", Cheat = true)]` - Defines a cheat command that grants an item to the player. Cheat commands require permanently enabling cheats for a session.
    3. Example: `[VespaCommand("big_secret", "Don't tell anyone...", Hidden = true)]` - Define a secret command that is hidden from the help manual and autofill. Hidden commands are not inherently cheats.
6. Enter play mode and try out your commands!
    1. You are ready to go with VespaIO in your project!

## Getting Help

- Use the [Issues](https://github.com/Orange-Panda/VespaIO/issues) or [Discussions](https://github.com/Orange-Panda/VespaIO/discussions) of this GitHub repository for support.

## Credits

This package is developed by [Luke Mirman](https://lukemirman.com/).

- Hive icon used in the logo is provided by Google Fonts under the Appache 2.0 license.
- [Lato font](https://fonts.google.com/specimen/Lato/about) in the logo is provided by Google Fonts under the Open Font License.
- [JetBrains Mono Font](https://github.com/JetBrains/JetBrainsMono) included in the default developer console is provided by JetBrains under Open Font License.