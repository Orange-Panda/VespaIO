# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.0] - Unreleased

### Breaking Changes

- The entire asset has been rewritten from the ground up so consider your previous implementations completely broken with this major version.
- The codebase has been restructured as follows:
	- Part of the codebase has been separated into a `LLAPI` which handles a lot of the heavy lifiting of the asset
		- If you do not plan to extend console functionality you should amost never need to interact with `LLAPI`, but it is available if that is of interest to your project.
	- Some code and assets that were related to the default implementation such as `DevConsoleRunner` have been moved to `Samples`
		- If you were using any 1.X version you likely need to import this default console implementation.
	- The rest of the codebase lies within the `HLAPI` which provides the same intuitive console implemntation from 1.X versions
		- Because of this separation of `LLAPI` from `HLAPI` it is possible to import exclusively the `LLAPI` if you want to write your own console implementation.
- `ManualPriority` for `Commands` are now of type `int` instead of `bool` enabling finer tuning of the sorting of the help manual.
	- Native commands will only use the inclusive range of `0-127` so if you want to guarantee your command shows before or after native commands consider this range.
- By default: Assemblies that are incredibly unlikely to contain commands are no longer included in the command search
	- This specifically ignores assemblies that **begin** with `unity`, `system`, `mscorlib`, `mono`, `log4net`, `newtonsoft`, `nunit`, `jetbrains` (Case insensitive).
	- In testing this reduces command search time by an order of magnitude
	- If for some reason this behavior is undesirable you can revert to the old behavior
- Enforcement of key definition rules are now much more strict that before so invalid command or alias keys may register differently to the console.
	- As a reminder: Keys should only use chracters from a-z, 0-9, and the `_` character.
	- Any keys that violate these rules will be converted to a valid key by going to lower case, replacing spaces with the `_`, and removing other invalid characters.
- The text parser for console input has been completely rewritten. It follows very similiar syntax to before but may have slightly different behavior from before including:
	- By default a command is defined by space separated words. Each word will be translated into a method parameter later.
	- If you would like to include a space in a parameter begin a quote using the "" character. 
		- Only the first set of quotations will be removed
		- You can escape the quote character using `\"` to ignore this behavior
	- You can begin a new command definition by using the semicolon character
		- You can escape the semicolon character using `\;` to ignore this behavior
		- Semicolons within quotes will not begin a new command.
- `StaticCommand` is now `VespaCommand` since it now supports non-static methods and more. (see more below)
- As stated above the original `DevConsoleRunner` implementation (including its assets) has been moved into the project samples.
	- If you did not already have your own console implementation imported into your Asset folder you are required to import this sample to use the console again.
	- Some `ConsoleSettingsConfig` values that were only relevant to the `DevConsoleRunner` have been moved onto the `DevConsoleRunner` itself.

### Added

- Added public methods to the `Aliases` class to add support for adding, removing, and viewing alias definitions within your own code.
- Added public methods to the `Commands` class to add support for adding, removing, and viewing command definitions within your own code.
- Added various low level types to more clearly communicate command syntax and invocations.
- Added the JetBrain's `[MeansImplicitUse]` attribute to attributes to automatically supress `Method never used` intellisense warnings for commands.
- Added support for `Word[]` commands which can dynamically handle any amount of parameters.
	- If this parameter is ever present it will *always* be invoked.
- Added support for autofilling subjects and parameters in console input
- Added support for command definitions on Fields and Properties. 
	- Does not require writing any methods to function!
	- Supports autofill for the get value and can set value if not readonly.
- Added support for instanced (non-static) commands on any class that inherits from `UnityEngine.Object`
	- The first parameter will be used to specify the name of the object to be targeted.
	- Only one Object can be the target of a method. The first object found will be used.
	- This works for methods, fields, and properties.
- Added `LogStyling` parameter to the `Console.Log` method which will apply common styling such as warning or error formatting.
- Added `VESPA_DISABLE` define symbol which when present will remove core functionality of the console. Not present by default
- Added `VESPA_DISABLE_NATIVE_COMMANDS` define symbol which when present will remove the native commands that come with the console. Not present by default.

### Removed

- The `LongString` type has been completely removed in favor of using quotation blocks around command input.
	- This means that previous LongString methods now take a string input and you are required to put quotations around your phrase.
	- Alternatively you can use `Word[]` to handle a dynamic amount of commands.
- The default `DevConsoleRunner` and its assets have been moved to `Samples`
	- This requires an import in order to use the console out of the box.

## [1.2.0] - 2022-10-09

### Added

- Added `requireHeldKeyToToggle` and `inputWhileHeldKeycodes` settings which when enabled require a key to be held to open/close the console. This is not enabled by default.
- Semicolons in the input field will now begin a new command, which can be prevented with the `\;` input which will later be interpreted as `;`.
- Added the `Alias` system which allows definitions of command shortcuts at runtime.
- The quotation mark character in the input field will now be used to prevent space argument separation and semicolon command separation.
	- This gives support to WORD having spaces without the implementation of LongString
	- This behavior can be ignored by escaping the character with `\"` which will later be interpreted as `"`.
	- Unescaped Quotes are removed from the final word.
		- Except for Longstring which does not sanitize the input.

### Changed

- Breaking: Split the formerly `closeConsoleKeycodes` setting into two separate settings: `closeAnyConsoleKeycodes` which will close the console regardless of input size
  and `closeEmptyConsoleKeycodes` which will only close the console when it is empty. This is to allow the input of the `, \, and ~ characters except for at the very start of input.
- Backend for parameter interpretation has been improved.
	- This adds support for bool static command parameters
- For multiple method definitions for the same command: Non-string commands are given priority over string commands.
- Autofill will choose aliases from the new alias system and then autofill commands.

### Fixed

- Fixed Tab key autocompletion not functioning correctly with capital letters.

## [1.1.2] - 2022-03-31

### Changed

- Tab autofill is now reset on command submit
- History traversal now uses unscaled delta time instead of delta time

## [1.1.1] - 2022-03-29

### Changed

- Autofilling now adds a trailing space for convenience

### Fixed

- Unavailable or hidden commands no longer appear in autofill

## [1.1.0] - 2022-03-29

### Added

- New preference to scale console canvas
- New preference to change keycodes for open/close console.
- Up/Down Arrow Navigation through recent commands
- Autofill prediction for command name
	- Pressing `Tab` will input the predicted command

### Changed

- Some fields were renamed which may require external scripts to be updated.

## [1.0.4] - 2022-02-20

### Fixed

- Improved handling of event system to fix issue with ui losing focus.

## [1.0.3] - 2022-02-13

### Added

- New config option that, when enabled, automatically enables cheats in the editor.

## [1.0.2] - 2022-01-23

### Changed

- Casing of Longstring has been changed to LongString.

### Fixed

- LongString not properly overriding ToString()

## [1.0.1] - 2022-01-22

### Added

- Support for ConsoleEnabled variable, preventing user access to the development console.
- Default enable state configuration options for editor and standalone builds.
- Console warns user if there is no event system present.

### Changed

- Help manual usage parameters are now shown in a more user friendly syntax.

### Fixed

- Addressed issue with console recognizing a parameterless method with higher priority than a LongString method.

## [1.0.0] - 2022-01-22

### Added

- Vespa IO Package created.