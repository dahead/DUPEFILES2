# DUPEFILES 2

DUPEFILES 2 is a c# .NET Core application which runs under linux, windows and probably mac. 

It scans your file system for duplicate files and helps you save space by removing the duplicates you want to.

## FEATURES

- Open source .NET Core CLI application
- Runs under linux/mac/windows
- Parallel and async file operations

## COMMANDS

### index-add

    index-add [PATH]
    index-add ~/Downloads --verbose

Adds files to the index.

### index-update

    index-update

Updates the index and removes non existing files.

### index-scan

    index-scan

Scans the index for duplicate files.


## TODO

- Command: index-purge-duplictes...
- Command: export results