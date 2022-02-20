# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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