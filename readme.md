Command line program to check if an installed Steam app has an update available.

## Installation

Releases can be found [here](https://github.com/CrystalFerrai/SteamAppUpdateCheck/releases).

This program is released standalone, meaning there is no installer. Simply extract the files to a directory to install it.

You will need to install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if you do not already have it.

## Usage

The purpose of this program is to run it as part of an automated script. The program will not output anything unless there is an error. On a successful run, the return code of the program can be checked to find out if an update is avaialble. Here are the possible return codes.

* 0: No parameters were passed in, so program help was printed.
* 1: An error occurred while attempting to check for an update. Error details will have been printed to `stderr`.
* 2: The check was successful, and no update is available for the app.
* 3: The check was successful, and there is an update available for the app.

### Example

Here is an example bat script that will print whether an app is up to date or not.

```bat
set AppId=%1

:DoCheck

echo Checking for updates for app %AppId%...
SteamAppUpdateCheck.exe %AppId%

rem Retry on failure
if %errorlevel% equ 1 (
  timeout /t 2 /nobreak
  echo Retrying...
  goto DoCheck
)

if %errorlevel% equ 2 (
  echo App %AppId% is up to date.
) else (
  if %errorlevel% equ 3 (
    echo An update is available for app %AppId%.
  )
)

pause
```

Example output for an app that has an avaialble update.
```
Checking for updates for app 1062090...
An update is available for app 1062090.
Press any key to continue . . .
```

### Command Line Parameters

Run the program in a console window with no parmaters to see a list of options like the following.

```
Checks if there is an update available for an installed Steam app.
Returns 2 if no update is available or 3 if an update is available.

Usage: SteamAppUpdateCheck [[options]] [app id]

  [app id]  The Steam App ID of the app to check for an update

Options

  --appsdir  Path to a directory containing the steamapps directory with information
             about the app. If not specified, will check Steam library directories.

  --branch   The branch of the app to check for an update. If not specified, will
             check the branch the installed app is currently using.
```

## Building
Clone the repository, including submodules.
```
git clone --recursive https://github.com/CrystalFerrai/SteamAppUpdateCheck.git
```

You can then open and build SteamAppUpdateCheck.sln.

To publish a build for release, run this command from the directory containing the SLN.
```
dotnet publish -p:DebugType=None -r win-x64 -c Release --self-contained false
```

The resulting build can be located at `SteamAppUpdateCheck\bin\x64\Release\net8.0\win-x64\publish`.
