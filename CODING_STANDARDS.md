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

We also adopted EditorConfig standards and created our very own [`.editorconfig` file on the root of the repository](.editorconfig) to maintain our coding standards.

Developers do not have to install any tools or plugins for EditorConfig support because `dotnet-format` + `standards.py` + Yamato CI/CD will be consuming rules defined in `.editorconfig` file already.

However, it is possible to apply these rules within IDEs while authoring source code, please see the [IDE Plugins](#ide-plugins) section for details.

#### dotnet-format

#### standards.py

### Usage

#### Check & Fix

#### Git Hooks

#### IDE Plugins

### Yamato

## Ruleset

### Spacing

### Formatting

### Naming
