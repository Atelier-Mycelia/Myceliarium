This page covers what you need to install Myceliarium and the concepts behind it. For an overview, see [Home](Home.md).

## Requirements

### For All Users
- Unity Version: 2022.3 LTS or later
- TextMeshPro 3.0.6 or later installed in Package Manager

### For Designers
A passable foundation with setting up layouts and styling in uss, uxml, and UI Builder.

### For Coders
A solid foundation on using Unity with C#. If you are not sure if you've got this, we recommend completing at least one of the following:
- GameDevTV's Complete Unity 3D Developer Course
  - The 2D version works just as well
- Zenva Academy's Unity Associate Programmer Certification Pathway

Doing so should put you in a comfortable position to get a lot of value out of Myceliarium. 

## Installation

First, make sure to install the Atelier Mycelia Common Lib through the instructions here: <br> https://github.com/Atelier-Mycelia/CommonLib
<br><br>Now do the same as with the Common Lib, except **this** is the link you use with Add Package From Git Url: <br> https://github.com/Atelier-Mycelia/Myceliarium.git?path=/Assets/Myceliarium

Note: Outside its own repo, Myceliarium is **not** meant to be installed as something copy-pasted to your Assets folder. It's UPM only. 

## Connecting To Your Codebase
For context: Myceliarium is set up with an Assembly Definition. This means that if you want some part of your codebase to be able to use that plugin, you'll need said part set up under an Assembly Definition too. If you don't know how to do that, take a look [here](https://embrace.io/blog/getting-started-with-assembly-definitions-in-unity/).

After you've made your AD, make sure it references the following:
- AtMycelia.Common.Core
- AtMycelia.Common.Editor
- AtMycelia.Myceliarium.Editor

Why the common lib? It has a fair bit of utility code you might want to use. Now... the sections below might make your head spin. Be not afraid. Just give them a read and then go over the [Simple Tutorial](Simple-Tutorial.md). It should help stuff stick.

## Core Concepts

| Concept | What it is |
| --- | --- |
| **Control panel** | An editor window (derived from `ControlPanel`) that hosts the sidebar, subwindows and footer. Myceliarium ships `AtMyceliaControlPanel`; you can define your own. |
| **Entry** | A `ControlPanelEntry` ties together one tab, one subwindow, one loader and one logic, plus (optionally) a tree of subentries. You don't build those yourself; the entry names their types (`SidebarTabType`, `SubwindowType`, `LoaderType`, `LogicType`) and Myceliarium creates them. |
| **Tab / Subwindow** | The sidebar button and the content area shown when said button is selected. The subwindow only deals with UI. |
| **Logic** | A `ControlPanelEntryLogic` owns the state the user edits, along with its undo history and unsaved-changes tracking. It knows nothing about UI. |
| **Loader / Saver** | Classes you write to get an entry's data when it's selected (`ControlPanelEntryLoader`) and to write it back when a save is requested (`ControlPanelEntrySaver`). The base classes take care of the plumbing: you only write the part that touches your data. |
| **Signals** | `ControlPanelSignals` is a static event bus (save requests, close requests, tab clicks) that decouples entries from panels. |

What Myceliarium does for you: keeps an undo/redo history per entry, runs the savers when the user saves (reporting failures) and makes sure the panel plays nice with assembly reloads and Play Mode. What's yours: the data itself, meaning where it comes from (the loader), how it's written back (the saver) and how edits change it (the logic).

## Entry-Discovery

No need to get them found by hand. On editor load (and after every assembly reload) Myceliarium scans for all `IControlPanelEntry` types and attaches each one to every control panel it is compatible with. A panel declares which entries and savers belong to it through marker interfaces (for example `IAtMyceliaControlPanelEntry` and `IAtMyceliaControlPanelEntrySaver`). That is how several panels can coexist without stepping on each others' toes. Each saver then says which entries it handles through `IsCompatibleWith`.

## Entry Lifecycle

1. `Init()` builds the entry's modules, in this order: the tab, the subentries, the subwindow, the loader and finally the logic (it comes last because it talks to the others). If `ShouldPreloadOnInit` is on, loading (steps 3 and 4 below) also happens here.
2. `Select()` runs when the entry's tab is clicked (or when the panel restores its last selection).
3. `HandleLoading()` has the loader get the saved data. It's skipped if the entry's logic has unsaved changes, so selecting an entry never throws away the user's edits.
4. `OnLoadingDone()` hands the loader's result to the logic (`Load`), refreshes the subwindow and shows it.
5. `Dispose()` cleans up, logic first, then the tab and subwindow.

Subentries are declared by overriding `CreateSubentries()`, which runs once. The instances are reused for the entry's lifetime, even when the panel is reinitialized. Each concrete entry type may appear only once across the whole tree.

Note that an entry can be initted without being loaded. `IsInitted` says the entry was built; `IsLoaded` says its state was loaded. Anything that works with the entry's state (a saver, for one) should check the latter.

## Saving And Loading

When the user presses Save, every saver gets to save each entry it's compatible with:
- Savers only run for entries that are loaded. An entry nobody opened can't have been edited, so it's skipped without fuss
- A saver reports back, one way or another, for every entry it handles. A throwing saver counts as a failed save: the exception is logged, the panel warns that the entry's changes are still unsaved, and the rest of the savers carry on
- When the Play Mode unsaved-changes dialog is answered with Save, Play Mode only starts if every save succeeded

## Undo And Redo

Each entry has its own undo history, independent of Unity's global one. Ctrl+Z and Ctrl+Shift+Z (and Ctrl+Y on Windows) work while a Control Panel is focused. Edits to the common field types (text, number, object, toggle, enum and dropdown fields) are picked up automatically; other kinds of changes need explicit commands. The [Simple Tutorial](Simple-Tutorial.md#undo-and-redo) goes into the details.

## Reloads And Play Mode

- **Assembly reloads:** open Control Panels close themselves right before the reload and reopen afterwards (unsaved changes are discarded, since a prompt would block the reload). This keeps panels from coming back as empty "ghost" windows.
- **Play Mode:** entering Play Mode with unsaved changes stops it and asks whether to save, discard or keep editing. Saving or discarding starts Play Mode afterwards; keeping editing leaves you in Edit Mode.
- **Reopening a panel:** if a panel has at least 2 entries with subwindows, it reopens on the entry that was selected last. This is remembered per machine rather than per project.