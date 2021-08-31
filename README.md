# DUPEFILES 2

DUPEFILES 2 scans your file system for duplicate files.
DUPEFILES 2 is a csharp dotnet core application which runs under windows, linux and probably osx.

DUPEFILES 2 checks files for for file size, hash and finally binary.

## COMMANDS

### index-add

    index-add [PATH]
    index-add ~/Downloads

Adds files to the index.

### index-update

    index-update

Updates the index and removes non existing files.

### index-scan

    index-scan

Scans the index for duplicate files.