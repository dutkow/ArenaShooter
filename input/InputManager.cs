using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text.Json;
using static InputActions;


public enum InputMode
{
    GAME,
    UI,
    CONSOLE
}

public enum InputBindingType
{
    PRIMARY,
    SECONDARY,
    EXTRA,
}

public partial class InputManager : Node
{
    public static InputManager Instance { get; private set; }

    public static HashSet<Key> DisallowedKeys = new()
    {
        Key.Meta,
    };

    public static HashSet<MouseButton> DisallowedMouseButtons = new()
    {
        MouseButton.Left,
        MouseButton.Right,
    };

    // Input mappings
    [Export] Json _defaultInputMappingsJson;
    Json _userInputMappingsJson;


    // Click events
    public event Action PrimaryClickPressed;
    public event Action SecondaryClickPressed;

    // Camera movement events
    public event Action MoveUpPressed;
    public event Action MoveUpReleased;

    public event Action MoveDownPressed;
    public event Action MoveDownReleased;

    public event Action MoveLeftPressed;
    public event Action MoveLeftReleased;

    public event Action MoveRightPressed;
    public event Action MoveRightReleased;

    public event Action<InputEventMouseMotion> DragMove;

    // Camera zoom events
    public event Action ZoomInToCursorPressed;
    public event Action ZoomOutAwayFromCursorPressed;
    public event Action ZoomInPressed;
    public event Action ZoomOutPressed;
    public event Action MaxZoomOutPressed;


    // Time scale events
    public event Action PauseGamePressed;
    public event Action UnpauseGamePressed;
    public event Action SetTimeScale1Pressed;
    public event Action SetTimeScale2Pressed;
    public event Action SetTimeScale3Pressed;
    public event Action SetTimeScale4Pressed;
    public event Action SetTimeScale5Pressed;
    public event Action IncreaseTimeScalePressed;
    public event Action DecreaseTimeScalePressed;

    // Pause menu events
    public event Action OpenPauseMenuPressed;
    public event Action ClosePauseMenuPressed;

    // Main panels
    public event Action DiplomacyPressed;

    // Generic navigation events
    public event Action NavigateBackPressed;

    // State management flags
    public bool IsDragMoving;

    public event Action<InputMappingProfile> DefaultInputMappingsRestored;

    public bool _wasDirty;
    public event Action<bool> DirtyStateChanged;

    public bool IsHoveringUI => GetViewport().GuiGetHoveredControl() != null;

    public InputMappingProfile DefaultInputMappingProfile;

    public InputMappingProfile UserInputMappingProfile;

    public System.Collections.Generic.Dictionary<string, InputEventWithModifiers> DefaultPrimaryInputEvents = new();
    public System.Collections.Generic.Dictionary<string, InputEventWithModifiers> DefaultSecondaryInputEvents = new();

    public System.Collections.Generic.Dictionary<string, InputEventWithModifiers> PrimaryInputEvents = new();
    public System.Collections.Generic.Dictionary<string, InputEventWithModifiers> SecondaryInputEvents = new();

    public System.Collections.Generic.Dictionary<string, InputEventWithModifiers> PrimaryInputEventsPendingChange = new();
    public System.Collections.Generic.Dictionary<string, InputEventWithModifiers> SecondaryInputEventsPendingChange = new();

    public HashSet<string> PrimaryInputEventsPendingClear = new();
    public HashSet<string> SecondaryInputEventsPendingClear = new();

    // Input binding events
    public bool InputBindingsDirty => PrimaryInputEventsPendingChange.Count > 0 || SecondaryInputEventsPendingChange.Count > 0 || PrimaryInputEventsPendingClear.Count > 0 || SecondaryInputEventsPendingClear.Count > 0;

    public bool IsListeningForInputBinding = false;
    public string ListeningAction = string.Empty;
    public InputBindingType ListeningBindingType;


    // action changed events

    public readonly Dictionary<string, Action<InputEventWithModifiers?>> PrimaryInputChangedEvents = new();

    public readonly Dictionary<string, Action<InputEventWithModifiers?>> SecondaryInputChangedEvents = new();

    public HashSet<string> _rebindableInputActions = new();

    bool _inputsInitialized;

    public InputEventWithModifiers InputEventPendingChange;


    public InputMode InputMode;
    public event Action<InputMode> InputModeChanged;


    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        EnsureInputMappingsExist();

        _userInputMappingsJson = ResourceLoader.Load<Json>(FilePaths.USER_INPUT_MAPPINGS);

        DefaultInputMappingProfile = LoadInputMappings(_defaultInputMappingsJson, true, DefaultPrimaryInputEvents, DefaultSecondaryInputEvents);
        UserInputMappingProfile = LoadInputMappings(_userInputMappingsJson, false, PrimaryInputEvents, SecondaryInputEvents);

        _inputsInitialized = true;
    }

    private void EnsureInputMappingsExist()
    {
        var dir = DirAccess.Open(FilePaths.USER_SETTINGS);
        if (dir == null)
        {
            dir = DirAccess.Open("user://");
            if (dir == null)
            {
                GD.PrintErr("Could not open base user:// folder!");
                return;
            }

            var err = dir.MakeDirRecursive(FilePaths.USER_SETTINGS);
            if (err != Error.Ok)
            {
                GD.PrintErr($"Could not create USER_SETTINGS folder! Error: {err}");
                return;
            }
        }

        if (!Godot.FileAccess.FileExists(FilePaths.USER_INPUT_MAPPINGS))
        {
            GD.Print("User input mappings not found. Creating from default.");

            string defaultJsonText = Godot.FileAccess.GetFileAsString(_defaultInputMappingsJson.ResourcePath);

            using var file = Godot.FileAccess.Open(FilePaths.USER_INPUT_MAPPINGS, Godot.FileAccess.ModeFlags.Write);
            file.StoreString(defaultJsonText);

            GD.Print("Default input mappings copied to user directory successfully.");
        }
    }

    public InputMappingProfile LoadInputMappings(Json inputMappingsJson, bool isDefault, Dictionary<string, InputEventWithModifiers> PrimaryInputDictionary, Dictionary<string, InputEventWithModifiers> SecondaryInputDictionary)
    {
        var inputMappingProfile = JsonUtils.LoadJson<InputMappingProfile>(inputMappingsJson);

        if(inputMappingProfile == null)
        {
            GD.PushError("KeyBinding profile is null!");
            return null;
        }

        foreach(var category in inputMappingProfile.Categories)
        {
            foreach(var inputAction in category.InputActions)
            {
                if(isDefault)
                {
                    InputMap.AddAction(inputAction.Action);
                }

                if(!_inputsInitialized)
                {
                    _rebindableInputActions.Add(inputAction.Action);
                }

                for (int i = 0; i < inputAction.InputBindings.Count; i++)
                {
                    var inputBinding = inputAction.InputBindings[i];

                    if (i == 0)
                    {
                        AddInputBinding(inputAction.Action, inputBinding, isDefault, InputBindingType.PRIMARY);
                    }
                    else if(i == 1)
                    {
                        AddInputBinding(inputAction.Action, inputBinding, isDefault, InputBindingType.SECONDARY);
                    }
                    AddInputBinding(inputAction.Action, inputBinding, isDefault, InputBindingType.EXTRA);
                }
            }
        }
        return inputMappingProfile;
    }

    public void ClearGameInput()
    {
        PrimaryClickPressed = null;
        SecondaryClickPressed = null;
        MoveUpPressed = null;
        MoveUpReleased = null;
        MoveDownPressed = null;
        MoveDownReleased = null;
        MoveLeftPressed = null;
        MoveLeftReleased = null;
        MoveRightPressed = null;
        MoveRightReleased = null;
        DragMove = null;
        ZoomInToCursorPressed = null;
        ZoomOutAwayFromCursorPressed = null;
        ZoomInPressed = null;
        ZoomOutPressed = null;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (!IsListeningForInputBinding)
        {
            return;
        }


        if (@event is InputEventKey inputEventKey)
        {
            if (DisallowedKeys.Contains(inputEventKey.Keycode))
            {
                return;
            }

            if (inputEventKey.Keycode == Key.Shift ||
                inputEventKey.Keycode == Key.Ctrl ||
                inputEventKey.Keycode == Key.Alt ||
                inputEventKey.Keycode == Key.Meta)
            {
                return;
            }

            FinishListeningForInput(inputEventKey);
        }
        else if (@event is InputEventMouseButton eventMouseButton)
        {
            if (DisallowedMouseButtons.Contains(eventMouseButton.ButtonIndex))
            {
                return;
            }

            FinishListeningForInput(eventMouseButton);
        }
    }

    public void SetInputMode(InputMode inputMode)
    {
        if(inputMode == InputMode)
        {
            return;
        }

        GD.Print($"setting input mode to {InputMode}");
        InputMode = inputMode;

        InputModeChanged?.Invoke(InputMode);
    }

    public bool IsPressed(string action)
    {
        return Input.IsActionPressed(action, true);
    }

    public bool IsJustPressed(string action)
    {
        return Input.IsActionJustPressed(action, true);
    }

    public bool IsJustReleased(string action)
    {
        return Input.IsActionJustReleased(action, true);
    }

    public void RemovePrimaryInputBinding(string action)
    {
        if(PrimaryInputEvents.TryGetValue(action, out var inputEvent))
        {
            InputMap.ActionEraseEvent(action, inputEvent);
            PrimaryInputEvents.Remove(action);
        }
    }

    public void RemoveSecondaryInputBinding(string action)
    {
        if (SecondaryInputEvents.TryGetValue(action, out var inputEvent))
        {
            InputMap.ActionEraseEvent(action, inputEvent);
            SecondaryInputEvents.Remove(action);
        }
    }

    public void AcceptPendingInputChanges()
    {
        // Apply primary input binding changes
        foreach (var kvp in PrimaryInputEventsPendingChange)
        {
            var actionName = kvp.Key;
            var newEvent = kvp.Value;

            if (PrimaryInputEvents.TryGetValue(actionName, out var existingEvent))
            {
                InputMap.ActionEraseEvent(actionName, existingEvent);
            }

            if (newEvent != null)
            {
                InputMap.ActionAddEvent(actionName, newEvent);
            }

            UpdateProfileBinding(actionName, newEvent, isPrimary: true);
        }

        // Apply secondary input binding changes
        foreach (var kvp in SecondaryInputEventsPendingChange)
        {
            var actionName = kvp.Key;
            var newEvent = kvp.Value;

            if (SecondaryInputEvents.TryGetValue(actionName, out var existingEvent))
            {
                InputMap.ActionEraseEvent(actionName, existingEvent);
            }

            if (newEvent != null)
            {
                InputMap.ActionAddEvent(actionName, newEvent);
            }

            UpdateProfileBinding(actionName, newEvent, isPrimary: false);
        }

        // Apply primary input binding clears
        foreach (var actionName in PrimaryInputEventsPendingClear)
        {
            if (PrimaryInputEvents.TryGetValue(actionName, out var existingEvent))
            {
                InputMap.ActionEraseEvent(actionName, existingEvent);
            }

            UpdateProfileBinding(actionName, null, isPrimary: true);
        }

        // Apply secondary input binding clears
        foreach (var actionName in SecondaryInputEventsPendingClear)
        {
            if (SecondaryInputEvents.TryGetValue(actionName, out var existingEvent))
            {
                InputMap.ActionEraseEvent(actionName, existingEvent);
            }

            UpdateProfileBinding(actionName, null, isPrimary: false);
        }

        // Clear all pending changes
        PrimaryInputEventsPendingChange.Clear();
        SecondaryInputEventsPendingChange.Clear();
        PrimaryInputEventsPendingClear.Clear();
        SecondaryInputEventsPendingClear.Clear();

        // Save input mapping profile
        SaveUserInputMappingProfile();
    }
    private void SaveUserInputMappingProfile()
    {
        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(
                UserInputMappingProfile,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            Godot.FileAccess file = Godot.FileAccess.Open(FilePaths.USER_INPUT_MAPPINGS, Godot.FileAccess.ModeFlags.Write);
            file.StoreString(json);
            file.Close();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save input mapping profile: {ex.Message}");
        }
    }


    private void UpdateProfileBinding(string actionName, InputEvent newEvent, bool isPrimary)
    {
        foreach (var category in UserInputMappingProfile.Categories)
        {
            foreach (var action in category.InputActions)
            {
                if (action.Action == actionName)
                {
                    int index = isPrimary ? 0 : 1;

                    // Ensure the list is long enough
                    while (action.InputBindings.Count <= index)
                    {
                        // Instead of null, add an empty InputBinding
                        action.InputBindings.Add(new InputBinding { Key = string.Empty, Modifiers = new List<string>() });
                    }

                    if (newEvent != null)
                    {
                        var asModifiers = newEvent as InputEventWithModifiers;
                        if (asModifiers == null)
                        {
                            GD.PrintErr($"InputEvent for {actionName} is not an InputEventWithModifiers.");
                            return;
                        }

                        action.InputBindings[index] = InputBinding.FromInputEvent(asModifiers);
                    }
                    else
                    {
                        // Assign an empty binding instead of null
                        action.InputBindings[index] = new InputBinding { Key = "", Modifiers = new List<string>() };
                    }

                    return;
                }
            }
        }
    }

    public void DiscardPendingInputChanges()
    {
        HashSet<string> primaryActionsToRevert = new();
        HashSet<string> secondaryActionsToRevert = new();


        foreach (var key in PrimaryInputEventsPendingChange.Keys)
        {
            primaryActionsToRevert.Add(key);
        }

        foreach(var key in SecondaryInputEventsPendingChange.Keys)
        {
            secondaryActionsToRevert.Add(key);
        }

        foreach (var key in PrimaryInputEventsPendingClear)
        {
            primaryActionsToRevert.Add(key);
        }

        foreach (var key in SecondaryInputEventsPendingClear)
        {
            secondaryActionsToRevert.Add(key);
        }


        PrimaryInputEventsPendingChange.Clear();
        SecondaryInputEventsPendingChange.Clear();
        PrimaryInputEventsPendingClear.Clear();
        SecondaryInputEventsPendingClear.Clear();

        foreach(var action in primaryActionsToRevert)
        {
            NotifyPrimaryChanged(action);
        }

        foreach(var action in secondaryActionsToRevert)
        {
            NotifySecondaryChanged(action);
        }

        EvaluateDirtyState();
    }

    private void AddInputBinding(string action, InputBinding binding, bool isDefault, InputBindingType inputBindingType)
    {
        var inputEvent = binding != null ? binding.GetInputEvent() : null;

        if (!isDefault && inputEvent != null)
        {
            InputMap.ActionAddEvent(action, inputEvent);
        }

        if (inputBindingType == InputBindingType.PRIMARY)
        {
            PrimaryInputEvents[action] = inputEvent;

            if (isDefault)
            {
                DefaultPrimaryInputEvents[action] = inputEvent;
            }
            else
            {
                PrimaryInputEvents[action] = inputEvent;
            }
        }
        else if (inputBindingType == InputBindingType.SECONDARY)
        {
            SecondaryInputEvents[action] = inputEvent;

            if (isDefault)
            {
                DefaultSecondaryInputEvents[action] = inputEvent;
            }
            else
            {
                SecondaryInputEvents[action] = inputEvent;
            }
        }
    }

    private void AddUserInputBinding(string action, InputBinding binding, InputBindingType inputBindingType)
    {
        var inputEvent = binding.GetInputEvent();
        if (inputEvent != null)
        {
            InputMap.ActionAddEvent(action, inputEvent);

            if(inputBindingType == InputBindingType.PRIMARY)
            {
                PrimaryInputEvents[action] = inputEvent;
            }
            else if(inputBindingType == InputBindingType.SECONDARY)
            {
                SecondaryInputEvents[action] = inputEvent;
            }
        }
    }

    public void AddPrimaryInputBindingPendingClear(string action)
    {
        PrimaryInputEventsPendingChange.Remove(action);
        PrimaryInputEventsPendingClear.Add(action);
        NotifyPrimaryChanged(action);
        EvaluateDirtyState();
    }

    public void AddSecondaryInputBindingPendingClear(string action)
    {
        SecondaryInputEventsPendingChange.Remove(action);
        SecondaryInputEventsPendingClear.Add(action);
        NotifySecondaryChanged(action);
        EvaluateDirtyState();
    }

    public void AddPrimaryInputPendingChange(string action, InputEventWithModifiers inputEvent)
    {
        PrimaryInputEventsPendingChange[action] = inputEvent;
        PrimaryInputEventsPendingClear.Remove(action);
        NotifyPrimaryChanged(action);
        EvaluateDirtyState();
    }

    public void AddSecondaryInputPendingChange(string action, InputEventWithModifiers inputEvent)
    {
        SecondaryInputEventsPendingChange[action] = inputEvent;
        SecondaryInputEventsPendingClear.Remove(action);
        NotifySecondaryChanged(action);
        EvaluateDirtyState();
    }
    public void AddPrimaryInputBinding(string action, InputBinding binding)
    {
        AddUserInputBinding(action, binding, InputBindingType.PRIMARY);
    }

    public void AddSecondaryInputBinding(string action, InputBinding binding)
    {
        AddUserInputBinding(action, binding, InputBindingType.SECONDARY);
    }

    public void BeginListeningForInput(string action, InputBindingType bindingType)
    {
        if(bindingType == InputBindingType.PRIMARY)
        {
            if(PrimaryInputEvents.TryGetValue(action, out var foundEvent))
            {
                InputEventPendingChange = foundEvent;
            }
        }
        else if(bindingType == InputBindingType.SECONDARY)
        {
            if (SecondaryInputEvents.TryGetValue(action, out var foundEvent))
            {
                InputEventPendingChange = foundEvent;
            }
        }

        ListeningAction = action;
        ListeningBindingType = bindingType;
        IsListeningForInputBinding = true;
    }

    public void FinishListeningForInput(InputEventWithModifiers inputEvent)
    {
        IsListeningForInputBinding = false;
        inputEvent.MetaPressed = false;

        if(ListeningBindingType == InputBindingType.PRIMARY) // IS PRIMARY INPUT
        {
            if (PrimaryInputEventsPendingChange.TryGetValue(ListeningAction, out var foundEvent)) // IS THIS EVENT ALREADY PENDING CHANGE
            {
                if(!InputUtils.AreInputEventsEqual(inputEvent, foundEvent)) // IS THE PROPOSED CHANGED DIFFERENT
                {
                    PrimaryInputEventsPendingChange[ListeningAction] = inputEvent;
                }
            }
            else // THIS IS NOT ALREADY PENDING CHANGE
            {
                if(PrimaryInputEvents.TryGetValue(ListeningAction, out var foundExistingEvent)) // DOES THIS EVENT EXIST
                {
                    if (!InputUtils.AreInputEventsEqual(inputEvent, foundExistingEvent)) // IS THE PROPOSED CHANGE NOT EQUAL TO THE CURRENT INPUT EVENT
                    {
                        PrimaryInputEventsPendingChange[ListeningAction] = inputEvent;
                    }
                }
                else // EVENT DOESN'T EXIST, ASSIGN IT DIRECTLY
                {
                    PrimaryInputEventsPendingChange[ListeningAction] = inputEvent;
                }
            }

            if(PrimaryInputEvents.TryGetValue(ListeningAction, out var currentInputEvent))
            {
                if(InputUtils.AreInputEventsEqual(inputEvent, currentInputEvent))
                {
                    PrimaryInputEventsPendingChange.Remove(ListeningAction);
                }
            }
        }
        else if(ListeningBindingType == InputBindingType.SECONDARY) // IS SECONDARY INPUT
        {
            if (SecondaryInputEventsPendingChange.TryGetValue(ListeningAction, out var foundEvent)) // IS THE EVENT ALREADY PENDING CHANGE
            {
                if (!InputUtils.AreInputEventsEqual(inputEvent, foundEvent)) // IS THE PROPOSED CHANGE DIFFERENT
                {
                    SecondaryInputEventsPendingChange[ListeningAction] = inputEvent;
                }
            }
            else // THIS IS NOT ALREADY PENDING CHANGE
            {
                if (SecondaryInputEvents.TryGetValue(ListeningAction, out var foundExistingEvent)) // DOES THIS EVENT EXIST
                {
                    if (!InputUtils.AreInputEventsEqual(inputEvent, foundEvent)) // IS THE PROPOSED CHANGE NOT EQUAL TO THE CURRENT INPUT EVENT
                    {
                        SecondaryInputEventsPendingChange[ListeningAction] = inputEvent;
                    }
                }
                else // EVENT DOESN'T EXIST, ASSIGN IT DIRECTLY
                {
                    SecondaryInputEventsPendingChange[ListeningAction] = inputEvent;
                }
            }

            if (SecondaryInputEvents.TryGetValue(ListeningAction, out var currentInputEvent))
            {
                if (InputUtils.AreInputEventsEqual(inputEvent, currentInputEvent))
                {
                    SecondaryInputEventsPendingChange.Remove(ListeningAction);
                }
            }
        }

        InputEventPendingChange = null;


        EvaluateDirtyState();

        if (ListeningBindingType == InputBindingType.PRIMARY)
        {
            NotifyPrimaryChanged(ListeningAction);
        }
        else
        {
            NotifySecondaryChanged(ListeningAction);
        }
    }

    public void RestoreAllInputsToDefaults()
    {
        foreach (var kvp in PrimaryInputEvents)
        {
            InputMap.ActionEraseEvent(kvp.Key, kvp.Value);
        }
        foreach (var kvp in SecondaryInputEvents)
        {
            InputMap.ActionEraseEvent(kvp.Key, kvp.Value);
        }

        PrimaryInputEvents.Clear();
        SecondaryInputEvents.Clear();
        PrimaryInputEventsPendingChange.Clear();
        SecondaryInputEventsPendingChange.Clear();
        PrimaryInputEventsPendingClear.Clear();
        SecondaryInputEventsPendingClear.Clear();

        EvaluateDirtyState();

        UserInputMappingProfile = LoadInputMappings(
            _defaultInputMappingsJson,
            false,
            PrimaryInputEvents,
            SecondaryInputEvents
        );

        foreach(var key in _rebindableInputActions)
        {
            NotifyPrimaryChanged(key);
        }

        foreach(var key in _rebindableInputActions)
        {
            GD.Print($"notifying secondary input reset for key: {key}");
            NotifySecondaryChanged(key);
        }

        SaveUserInputMappingProfile();


        GD.Print("All inputs restored to default mappings.");
    }

    public bool IsInputActionDefault(string action)
    {
        // Determine what is effectively the "current" primary input
        InputEventWithModifiers primaryEffectiveEvent = null;
        if (PrimaryInputEventsPendingChange.TryGetValue(action, out var pendingPrimary))
        {
            primaryEffectiveEvent = pendingPrimary;
        }
        else if (PrimaryInputEventsPendingClear.Contains(action))
        {
            primaryEffectiveEvent = null;
        }
        else
        {
            PrimaryInputEvents.TryGetValue(action, out primaryEffectiveEvent);
        }

        // Determine what is effectively the "current" secondary input
        InputEventWithModifiers secondaryEffectiveEvent = null;
        if (SecondaryInputEventsPendingChange.TryGetValue(action, out var pendingSecondary))
        {
            secondaryEffectiveEvent = pendingSecondary;
        }
        else if (SecondaryInputEventsPendingClear.Contains(action))
        {
            secondaryEffectiveEvent = null;
        }
        else
        {
            SecondaryInputEvents.TryGetValue(action, out secondaryEffectiveEvent);
        }

        // Get default inputs
        DefaultPrimaryInputEvents.TryGetValue(action, out var defaultPrimaryInputEvent);
        DefaultSecondaryInputEvents.TryGetValue(action, out var defaultSecondaryInputEvent);

        bool primaryMatches = InputEventEquals(primaryEffectiveEvent, defaultPrimaryInputEvent);
        bool secondaryMatches = InputEventEquals(secondaryEffectiveEvent, defaultSecondaryInputEvent);

        return primaryMatches && secondaryMatches;
    }

    private bool InputEventEquals(InputEvent? current, InputEvent? defaultEvent)
    {
        if (current == null && defaultEvent == null) return true;
        if (current == null && defaultEvent == null) return true;
        if (current == null && defaultEvent != null) return false;
        if (current != null && defaultEvent == null) return false;

        if (current.GetType() != defaultEvent.GetType()) return false;

        if (current is InputEventKey currentKey && defaultEvent is InputEventKey defaultKey)
        {
            return currentKey.Keycode == defaultKey.Keycode
                   && currentKey.ShiftPressed == defaultKey.ShiftPressed
                   && currentKey.CtrlPressed == defaultKey.CtrlPressed
                   && currentKey.AltPressed == defaultKey.AltPressed
                   && currentKey.MetaPressed == defaultKey.MetaPressed;
        }

        if (current is InputEventMouseButton currentMouse && defaultEvent is InputEventMouseButton defaultMouse)
        {
            return currentMouse.ButtonIndex == defaultMouse.ButtonIndex
                   && currentMouse.ShiftPressed == defaultMouse.ShiftPressed
                   && currentMouse.CtrlPressed == defaultMouse.CtrlPressed
                   && currentMouse.AltPressed == defaultMouse.AltPressed
                   && currentMouse.MetaPressed == defaultMouse.MetaPressed;
        }

        return false;
    }

    public void ResetInputsToDefault(string action)
    {
        ResetPrimaryInputToDefault(action);
        ResetSecondaryInputToDefault(action);

    }
    public void ResetPrimaryInputToDefault(string action)
    {
        DefaultPrimaryInputEvents.TryGetValue(action, out var defaultEvent);

        if (defaultEvent == null)
        {
            PrimaryInputEventsPendingClear.Add(action);
            PrimaryInputEventsPendingChange.Remove(action);
        }
        else
        {
            if (PrimaryInputEventsPendingChange.TryGetValue(action, out var eventPendingChange))
            {
                PrimaryInputEventsPendingChange.Remove(action);
            }
            else if (PrimaryInputEventsPendingClear.TryGetValue(action, out var eventPendingClear))
            {
                PrimaryInputEventsPendingClear.Remove(action);
            }
            else
            {
                if (PrimaryInputEventsPendingChange.ContainsKey(action))
                {
                    PrimaryInputEventsPendingChange[action] = defaultEvent;
                }
            }
        }


        NotifySecondaryChanged(action);

        EvaluateDirtyState();

        NotifyPrimaryChanged(action);

        EvaluateDirtyState();
    }

    public void ResetSecondaryInputToDefault(string action)
    {
        DefaultSecondaryInputEvents.TryGetValue(action, out var defaultEvent);

        if (defaultEvent == null)
        {
            SecondaryInputEventsPendingClear.Add(action);
            SecondaryInputEventsPendingChange.Remove(action);
        }
        else
        {
            if(SecondaryInputEventsPendingChange.TryGetValue(action, out var eventPendingChange))
            {
                SecondaryInputEventsPendingChange.Remove(action);
            }
            else if (SecondaryInputEventsPendingClear.TryGetValue(action, out var eventPendingClear))
            {
                SecondaryInputEventsPendingClear.Remove(action);
            }
            else
            {
                if(SecondaryInputEventsPendingChange.ContainsKey(action))
                {
                    SecondaryInputEventsPendingChange[action] = defaultEvent;
                }
            }
        }


        int count1 = PrimaryInputEventsPendingChange.Count;
        int count2 = PrimaryInputEventsPendingClear.Count;
        int count3 = SecondaryInputEventsPendingChange.Count;
        int count4 = SecondaryInputEventsPendingClear.Count;


        NotifySecondaryChanged(action);

        EvaluateDirtyState();
    }

    private void NotifyPrimaryChanged(string action)
    {
        if (PrimaryInputChangedEvents.TryGetValue(action, out var callback))
        {
            callback?.Invoke(GetEffectivePrimary(action));
        }
    }

    private void NotifySecondaryChanged(string action)
    {
        if (SecondaryInputChangedEvents.TryGetValue(action, out var callback))
        {
            callback?.Invoke(GetEffectiveSecondary(action));
        }
    }

    private InputEventWithModifiers? GetEffectivePrimary(string action)
    {
        if (PrimaryInputEventsPendingChange.TryGetValue(action, out var pending))
        {
            return pending;
        }

        if (PrimaryInputEventsPendingClear.Contains(action))
        {
            return null;
        }


        if (PrimaryInputEvents.TryGetValue(action, out var current))
        {
            return current as InputEventWithModifiers;
        }

        return null;
    }

    private InputEventWithModifiers? GetEffectiveSecondary(string action)
    {
        if (SecondaryInputEventsPendingChange.TryGetValue(action, out var pending))
        {
            return pending;
        }

        if (SecondaryInputEventsPendingClear.Contains(action))
        {
            return null;
        }

        if (SecondaryInputEvents.TryGetValue(action, out var current))
        {
            return current as InputEventWithModifiers;
        }

        return null;
    }

    public void EvaluateDirtyState()
    {
        bool newState = InputBindingsDirty;

        if (newState != _wasDirty)
        {
            _wasDirty = newState;
            DirtyStateChanged?.Invoke(newState);
        }
    }
}
