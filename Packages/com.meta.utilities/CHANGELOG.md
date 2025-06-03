# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2023-04-17

### Added

- "Tools/Identify Missing References (via git)" menu item. This uses the git history of the repo to try to identify the sources of missing references.
- "Tools/Fix Incorrect Asset Names" menu item. This finds all assets that have Object names that differ from their file names.
- "Assets/Resave" menu item. This will fully reimport and reserialize an asset.
- New `CameraFollowing`, `HoverAbove`, and `ResetTransform` components.
- New extension method `IEnumerator.CatchExceptions`. This wraps a coroutine method so that its exceptions are logged.

### Fixed

- Fixed issue where `AutoSetPostProcessor` would fail to set inherited fields in child classes.

### Changed

- `NetcodeHashFixer` has been simplified.
