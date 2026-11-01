# Before We Start
You may want to clone Myceliarium's repo so you can take a look at certain not-built-in-classes we refer to in this tutorial. They all live in `Assets/MyceliariumDemos/SimpleExample`: a control panel with a single Notepad entry. Its text can be edited, undone, saved and loaded.

If you haven't yet, give [Getting Started](Getting-Started.md) a read first. This tutorial leans on the concepts it introduces.

# The Big Picture
A Control Panel is built from a handful of small classes, each with one job:

| Class | Job |
| --- | --- |
| `ControlPanel` | The window itself |
| `ControlPanelTab` | The button in the left sidebar |
| `ControlPanelSubwindow` | The content area shown when the tab is selected. Only deals with UI |
| `ControlPanelEntryLogic` | The state the user edits, plus the undo history and the unsaved-changes tracking that go with it. Knows nothing about UI |
| `ControlPanelEntryLoader` | Gets the saved data from wherever it lives |
| `ControlPanelEntrySaver` | Writes the state back to where it lives |
| `ControlPanelEntry` | Ties the above together: it owns one tab, one subwindow, one loader and one logic |

You never construct the tab, subwindow, loader or logic yourself. Your entry just names their types and Myceliarium makes them (each needs a public parameterless constructor).

That's the idea behind how entries are written: you **declare** what your entry is made of (by overriding properties such as `SidebarTabType`) instead of **writing the code** that creates and initializes each piece. The result is far less boilerplate. A typical entry is a handful of one-line overrides plus whatever is specific to your data. If a module can't be created (say, a type without a public parameterless constructor), Myceliarium logs an error to tell you.

# Setting Up The Visuals
Your custom `ControlPanel`s, tabs and subwindows will need their own uxml files. Rather than building them from scratch, best do this:
1. In the Unity editor, go to Packages/Myceliarium: Control Panels Made Easy/Resources/Editor/Uxml 
2. Duplicate the `ControlPanel`, `_BaseTab`, and `_BaseSubwindow` uxml files in there
3. Move the dupes somewhere under a Resources folder in your project
4. Rename the dupes
5. Alter their layouts and styling as you wish

Whatever you name the elements you'll want to reach from code (the note's text field, say), keep those names in mind. You'll look them up later.

## Prepping the ControlPanel for Tabs and Subwindows
Create two interfaces: one that extends `IControlPanelEntry` and another that extends `IControlPanelEntrySaver`. They act as marker interfaces: only entries and savers that implement them get attached to your panel, which is how several panels can live side by side.

Now create a class that inherits from `ControlPanel`. Make sure it overrides the following:
- `string DisplayName`
- `string PathToUxml`
  - This is so that the system knows exactly where to find your CP's own uxml
- `Type EntrySuperType`
  - This is so that the system knows which entries are compatible with this panel
- `Type SaverSuperType`
  - Same idea, but for savers

Now add a public static function that lets the user bring up your panel (a `[MenuItem]` that calls `GetWindow<YourPanel>()` will do). If you're unsure if you understand how to do all this, please take a look at the repo's `SimpleControlPanel` class. It can serve well as reference.

Now if you try to open your panel, you'll notice that the left sidebar and subwindow area are empty. That's because we didn't properly set up code for those yet. And thus...

## Custom Tab Code
Create a class that inherits from `ControlPanelTab`, making sure it overrides the following:
- `string DisplayName`
- `string PathToUxml`

Optionally, you can also override `StyleLength OverrideSubtabHeight`. You can probably guess what it does.

That's it. Easy peasy. If you want a reference, best take a look at the repo's `NotepadTab` class.

## Custom Subwindow Code
Not-so-easy-peasy is this... Create a class that inherits from `ControlPanelSubwindow`. Make sure it overrides the following:
- `string PathToUxml`
- `void RegisterVisualElements`
  - Since naturally, you'll want to be able to access them
- `void ConfigureVisualElements`
  - For when you want to make sure some VEs have certain settings applied (like `isDelayed` or `multiline`)

Optionally, you can override:
- `void SetSubs`
  - This is for setting up responses to things that happen with your VEs, like getting clicked or focused
  - Call the base first, then add or remove your listeners
- `IEnumerable<IFieldEditTracker> CreateEditTrackers` and `object GetEditContext(VisualElement field)`
  - These are for the undo system. More on them in [Undo And Redo](#undo-and-redo) below. For plain fields like the Notepad's, you won't need either

Custom Subwindows are mainly there to hook up the VEs and give clients the access needed for changing and responding to them. See how the `NotepadSubwindow` class lets others set the note text and respond to its changes?

Note that a subwindow **doesn't** hold the user's data. That's the logic's job (see below). The subwindow **shows** the user's data and reports what the user did.

## Custom Logic Code
This is where the state the user edits lives. Create a class that inherits from `ControlPanelEntryLogic` and override:
- `void AdoptState(object loadResult)`
  - Receives whatever the loader produced. Copy what you need out of it (rather than holding on to it), so that edits don't touch the saved data until the user saves
- `IUndoableCommand CreateEditCommand(FieldEdit edit)`
  - Turns an edit the user made in the subwindow into a command, or returns null if it isn't yours. See [Undo And Redo](#undo-and-redo)

The base class gives you the rest for free:
- `History`: the undo stack
- `HasUnsavedChanges`: true while the state differs from the last loaded or saved one. Undoing back to the saved state makes it false again
- `IsLoaded`: whether `Load` has run
- `CanUndo`, `CanRedo`, `Undo()` and `Redo()`

Logics raise their own events for anyone who needs to react to a change (like the entry, which routes it to the subwindow). Keep UI types out of them. See `NotepadLogic` for the minimal version.

## Custom Loader Code
Note: If your entry has no data to load from disk, you can skip this section and the default loader will be used.

Create a class that inherits from `ControlPanelEntryLoader`. Override `LoadProcess` and make sure that the result is stored in the `_loadResult` field inherited from `ControlPanelEntryLoader`, then call `onComplete`. The entry hands that result to your logic's `AdoptState`. For reference, you may want to see the `NotepadLoader` class.

## Custom Saver Code
Note: If the subwindow you want to code has no state you want to save to disk, you can skip this section.

Create a class that inherits from `ControlPanelEntrySaver` and implements your custom saver interface (see the Prepping the ControlPanel section above). Override the following:
- `bool IsCompatibleWith`
  - Tells which entries this saver handles
- `IEnumerator SaveProcess(EntrySaveRequest request)`
  - The saving itself. `request.Entry` is the entry being saved

That's the whole job. The base class takes care of the rest:
- It gives the UI controls a couple of frames to finish (a text field that's still focused, say) before your code runs
- It only runs `SaveProcess` for entries that are loaded. One that was never loaded (its tab wasn't clicked yet) can't have been edited, so there's nothing in it to save
- It reports the outcome afterwards and tells the entry's logic that its state is now the saved one. **Don't** do any of that yourself
- If `SaveProcess` throws, the save counts as failed: the exception is logged, the Control Panel warns that the entry's changes are still unsaved, and Play Mode won't start from the unsaved-changes dialog

Keep in mind that `SaveProcess` is a coroutine. If you have no need to wait on anything, just do your work and end with `yield break`.

For reference, you may want to look at the `NotepadSaver` class.

## Custom Entry Code
Now the part that ties everything together. Create a new class that inherits from `ControlPanelEntry` and implements your custom entry interface. Make sure it overrides the following:
- `string SortingName`
- `Type SidebarTabType`
- `Type SubwindowType`
- `Type LoaderType`
- `Type LogicType`

You just name the types. Myceliarium creates the instances and calls their `Init` for you, in the right order. There are no `Prepare...` methods you have to write. If you skip `LoaderType` or `LogicType`, defaults are used (a loader with nothing to load, and a logic with no state of its own).

The `PrepareMainSidebarTab`, `PrepareSubwindow`, `PrepareLoader` and `PrepareLogic` methods still exist (they're virtual), but they're for special cases: say, a tab that needs constructor arguments, or a test double. Most entries never touch them. Likewise, subentries are declared by overriding `CreateSubentries()` (shocker!), which returns the instances; the base plumbing takes care of initializing, registering and disposing them.

If your entry is NOT meant to have a subwindow (say it only exists to have others nested under it), leave `SubwindowType` alone. 

If your entry is NOT meant to be nested under another, make sure to override `IsTopLevel` so it returns true.

Optionally, override these:
- `void SetSubs`
  - Here, you'll probably want to listen for your logic's events (so you can push changes to the subwindow) and your subwindow's events
  - Call the base first
- `void OnLoadingDone`
  - Here, the base implementation hands the result to your logic's `Load`, refreshes the subwindow and shows it. So if you override it, call the base first, then push the loaded state into the subwindow
- `bool ShouldPreloadOnInit`
  - By default, an entry loads when it's selected. Return true if yours needs to be loaded as soon as the panel is built. Code that works with the loaded state should check `IsLoaded` rather than `IsInitted`. Why? The latter returning true doesn't necessarily mean the former will, too.

Reading your entry's state from the outside works through the logic (`Logic`), so expose what clients need in a property, as `NotepadEntry.WorkingText` does. For reference, you may want to see the `NotepadEntry` class.

# Undo And Redo
Every entry gets its own undo history, independent of Unity's global one. While a Control Panel is focused, these shortcuts do the following instead of Unity's built-in behavior:
- Ctrl+Z undoes the last change done in the panel's currently-diaplayed subwindow
- Ctrl+Shift+Z (or Ctrl+Y on Windows) redoes

You get most of this by doing very little. When the user edits a field, the subwindow's edit trackers report it as a `FieldEdit` (which field, old value, new value). Your logic's `CreateEditCommand` turns that into a command:

```csharp
public override IUndoableCommand CreateEditCommand(FieldEdit edit)
{
    if (edit.Field.name != "NoteTextField")
    {
        return null; // Not one of ours
    }

    Text = (string)edit.NewValue; // The user already typed it, so the state follows
    return edit.ToCommand(ApplyText);
}

private void ApplyText(FieldEdit edit, object value)
{
    Text = (string)value;
    TextChangedByHistory(Text); // So the entry can show it in the subwindow
}
```

The command is **recorded**, not executed, since the UI already applied the edit. When undoing or redoing, `ApplyText` runs with the old or new value. When you show that value in the subwindow, use `SetValueWithoutNotify`. Why? A notifying change would be recorded as a brand new edit.

Things worth knowing:
- Typing controls (`TextField`, `IntegerField`, `LongField`, `FloatField`, `DoubleField`) count everything between focus-in and focus-out as one edit
- `ObjectField`, `Toggle`, `EnumField` and `DropdownField` count every user action as one edit
- Not covered yet: sliders, vector/rect/bounds fields, color/curve/gradient fields, and structural changes like reordering a list or toggling a foldout. Those need explicit commands, pushed through `History.Push` (it executes the command) or `History.Record` (it doesn't, for when the UI already did the change). `CharaBioLogic` in the ComplexExample shows adding and deleting items this way
- To add trackers for other controls, override `CreateEditTrackers` in your subwindow (and call the base)
- If one subwindow edits different objects at different times, override `GetEditContext` so that the undo reaches the right object even after the selection changes

# Finally...
Now open your panel and give it a try! Type something, press Ctrl+Z, and hit Save. If you reload assemblies while the panel is open, it closes itself and reopens on its own. And if you try entering Play Mode with unsaved changes, you'll be asked what to do about them.

When you're ready for something bigger, look at `Assets/MyceliariumDemos/ComplexExample`: a character bio list (with add, delete and undo) and an RPG Maker-esque Skills window. That's two entries on one panel.
