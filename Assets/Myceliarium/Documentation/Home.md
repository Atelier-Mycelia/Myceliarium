# Myceliarium

**Myceliarium: Control Panels Made Easy** is an editor-tooling framework for Unity (2022.3 and later, UI Toolkit) for building editor windows similar to RPG Maker's Database window: a sidebar of tabs on the left, each paired with a subwindow on the right.

Here's how you use it in a nutshell:
1. Design your Control Panels, tabs and subwindows through uxml (perhaps best with Unity's built-in UI Builder)
2. Write up some classes to handle the logic of said ControlPanels, tabs and subwindows
3. Let Myceliarium scan for those (it does so on project startup and assembly reload) so that your work gets properly integrated into the Unity Editor

## Getting started

New here? [Getting Started](Getting-Started.md) covers the requirements, installation, core concepts, how entries are found, and the entry lifecycle. Once you're set up, the [Simple Tutorial](Simple-Tutorial.md) walks you through building your own control panel.

## Where to look in the repository

| Path | Contents |
| --- | --- |
| `Assets/Myceliarium/Editor` | Core code (namespace `AtMycelia.Myceliarium`) |
| `Assets/Myceliarium/Editor/OwnEntry` | Myceliarium's own built-in entry (root and About), a small reference implementation |
| `Assets/Myceliarium/Resources/Editor` | UXML (including the `_BaseTab` and `_BaseSubwindow` ones to duplicate for your own), USS and icons |
| `Assets/Myceliarium/Documentation` | These pages, so they can be read from your code editor |
| `Assets/MyceliariumDemos/SimpleExample` | The smallest demo: a Notepad entry. The one the [Simple Tutorial](Simple-Tutorial.md) follows |
| `Assets/MyceliariumDemos/ComplexExample` | A bigger demo: a character bio list and a skills window on one panel, with add/delete and undo |
| `Assets/Tests/Editor` | NUnit EditMode tests |

## Running the tests

Open **Window > General > Test Runner > EditMode**, or run headless:

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## Wiki pages

- [Getting Started](Getting-Started.md)
- [Simple Tutorial](Simple-Tutorial.md)

More pages will be added here as they are written.

## Links

- [Repository](https://github.com/Atelier-Mycelia/Myceliarium)
