# Coding Standards

## Index

- [Tooling](#tooling)
  - [Setup](#setup)
    - [EditorConfig](#editorconfig)
    - [dotnet-format](#dotnet-format)
    - [standards.py](#standardspy)
  - [Usage](#usage)
    - [Check & Fix](#check--fix)
    - [Git Hooks](#git-hooks)
    - [IDE Plugins](#ide-plugins)
  - [Yamato](#yamato)
- [Ruleset](#ruleset)
  - [Spacing](#spacing)
  - [Formatting](#formatting)
  - [Naming](#naming)

## Tooling

We are leveraging a couple of tools to help us better enforce our coding standards during the local development and code review process.

### Setup

#### EditorConfig

[EditorConfig](https://editorconfig.org/) helps maintain consistent coding styles for multiple developers working on the same project across various editors and IDEs.

We also adopted EditorConfig and created our very own [`.editorconfig` file on the root of the repository](.editorconfig) to maintain our coding standards.

Developers do not have to install any tools or plugins for EditorConfig support because `dotnet-format` + `standards.py` + Yamato CI/CD will be consuming rules defined in `.editorconfig` file already.

However, it is possible to apply these rules within IDEs while authoring source code, please see the [IDE Plugins](#ide-plugins) section for details.

#### dotnet-format

[`dotnet-format`](https://github.com/dotnet/format) is a code formatter for `dotnet` that applies style preferences to a project or solution. Preferences will be read from an `.editorconfig` file, if present, otherwise a default set of preferences will be used.

- [Download and install latest .NET SDK from one of the official Microsoft sources](https://dotnet.microsoft.com/download)
- Verify `dotnet` installation by executing `dotnet --version` command
- Install `dotnet-format` tool by executing `dotnet tool install -g dotnet-format` command
- Verify `dotnet-format` installation by executing `dotnet-format --version` command
- Done!

#### standards.py

_// todo_

### Usage

_// todo_

#### Check & Fix

_// todo_

#### Git Hooks

_// todo_

#### IDE Plugins

_// todo_

### Yamato

_// todo_

## Ruleset

_// todo_

### Spacing

_// todo_

### Formatting

_// todo_

### Naming

_// todo_
