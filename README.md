# MoonWorksTemplate

Template and build tasks for developing a cross-platform multi-target .NET 7 MoonWorks project in VSCode.

The generated solution file will also work in regular Visual Studio.

## Features

- Project boilerplate code
- VSCode build tasks
- VSCode step debugger integration

## Requirements

- [Git](https://git-scm.com/) or [Git for Windows](https://gitforwindows.org/) on Windows
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- [Visual Studio Code](https://code.visualstudio.com/)
- [VSCode C# Dev Kit Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

## Installation

- Make sure you have Git Bash from Git for Windows if you are on Windows
- Download this repository
- Run `install.sh`
- Move the newly created project directory wherever you want

## Usage

- Open the project directory in VSCode
- Press Ctrl-Shift-B to open the build tasks menu
- Tasks use .NET 7.0 to build and run
- Press F5 to build and run step debugger

## Acknowledgments

Thanks to Andrew Russell and Caleb Cornett's FNA templates for a starting point for this template.
