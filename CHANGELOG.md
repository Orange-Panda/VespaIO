# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Semicolons in the input field will now begin a new command, which can be prevented with the `\;` input which will later be interpreted as `;`.
- Added `requireHeldKeyToToggle` and `inputWhileHeldKeycodes` settings which when enabled require a key to be held to open/close the console. This is not enabled by default.

### Changed
- Breaking: Split the formerly `closeConsoleKeycodes` setting into two separate settings: `closeAnyConsoleKeycodes` which will close the console regardless of input size and `closeEmptyConsoleKeycodes` which will only close the console when it is empty. This is to allow the input of the `, \, and ~ characters except for at the very start of input.

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