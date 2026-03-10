using Godot;
using System;


public partial class InputBindingSettingsEntry : SettingsEntry
{
    [Export] string _pendingInputText = "...";
    [Export] string _noBindingString = " ";

    [Export] Button _primaryInputButton;
    [Export] Button _secondaryInputButton;
    [Export] Button _clearPrimaryInputButton;
    [Export] Button _clearSecondaryInputButton;
    [Export] Button _resetToDefaultsButton;

    private string _action;

    private InputBindingType _inputBindingListeningType;

    public void Init(InputAction inputAction)
    {
        _action = inputAction.Action;

        SettingNameLabel.Text = _action.ToUpper();

        _primaryInputButton.Text = GetInputBindingDisplayString(inputAction.PrimaryInputBinding);
        _secondaryInputButton.Text = GetInputBindingDisplayString(inputAction.SecondaryInputBinding);

        _clearPrimaryInputButton.Visible = _primaryInputButton.Text != _noBindingString;
        _clearSecondaryInputButton.Visible = _secondaryInputButton.Text != _noBindingString;

        _primaryInputButton.Pressed += OnPrimaryInputButtonPressed;
        _secondaryInputButton.Pressed += OnSecondaryInputButtonPressed;

        _clearPrimaryInputButton.Pressed += OnClearPrimaryInputButtonPressed;
        _clearSecondaryInputButton.Pressed += OnClearSecondaryInputButtonPressed;
        _resetToDefaultsButton.Pressed += OnResetToDefaultsButtonPressed;


        var inputMgr = InputManager.Instance;
        _resetToDefaultsButton.Visible = !inputMgr.IsInputActionDefault(_action);

        if(!inputMgr.PrimaryInputChangedEvents.ContainsKey(_action))
        {
            inputMgr.PrimaryInputChangedEvents[_action] = null;
        }
        inputMgr.PrimaryInputChangedEvents[_action] += OnPrimaryInputChanged;

        if (!inputMgr.SecondaryInputChangedEvents.ContainsKey(_action))
        {
            inputMgr.SecondaryInputChangedEvents[_action] = null;
        }
        inputMgr.SecondaryInputChangedEvents[_action] += OnSecondaryInputChanged;
    }

    public string GetInputBindingDisplayString(InputBinding inputBinding)
    {
        if (inputBinding == null || inputBinding.Key == string.Empty)
        {
            return _noBindingString;
        }

        string displayString = string.Empty;

        foreach (var modifier in inputBinding.Modifiers)
        {
            displayString += $"{Tr(modifier.ToUpper())} + ";
        }

        displayString += Tr(inputBinding.Key.ToUpper());

        return displayString;
    }

    public void OnPrimaryInputButtonPressed()
    {
        _primaryInputButton.Text = _pendingInputText;
        _inputBindingListeningType = InputBindingType.PRIMARY;

        InputManager.Instance.BeginListeningForInput(_action, InputBindingType.PRIMARY);
    }

    public void OnSecondaryInputButtonPressed()
    {
        _secondaryInputButton.Text = _pendingInputText;
        _inputBindingListeningType = InputBindingType.SECONDARY;

        InputManager.Instance.BeginListeningForInput(_action, InputBindingType.SECONDARY);
    }

    public void OnClearPrimaryInputButtonPressed()
    {
        SetPrimaryButtonText(_noBindingString);
        InputManager.Instance.AddPrimaryInputBindingPendingClear(_action);

        _resetToDefaultsButton.Visible = !InputManager.Instance.IsInputActionDefault(_action);
    }

    public void OnClearSecondaryInputButtonPressed()
    {
        SetSecondaryButtonText(_noBindingString);
        InputManager.Instance.AddSecondaryInputBindingPendingClear(_action);
        _resetToDefaultsButton.Visible = !InputManager.Instance.IsInputActionDefault(_action);
    }

    public void SetPrimaryButtonText(string text)
    {
        _primaryInputButton.Text = text;
        _clearPrimaryInputButton.Visible = text != _noBindingString;
    }

    public void SetSecondaryButtonText(string text)
    {
        _secondaryInputButton.Text = text; 
        _clearSecondaryInputButton.Visible = text != _noBindingString;
    }


    public void OnResetToDefaultsButtonPressed()
    {
        InputManager.Instance.ResetInputsToDefault(_action);
    }

    public void OnPrimaryInputChanged(InputEventWithModifiers inputEvent)
    {
        if (inputEvent == null)
        {
            _primaryInputButton.Text = _noBindingString;
            _clearPrimaryInputButton.Visible = false;
        }
        else
        {
            _primaryInputButton.Text = TextUtils.GetInputEventDisplayString(inputEvent);
            _clearPrimaryInputButton.Visible = true;
        }
        _resetToDefaultsButton.Visible = !InputManager.Instance.IsInputActionDefault(_action);
    }

    public void OnSecondaryInputChanged(InputEventWithModifiers inputEvent)
    {
        if (inputEvent == null)
        {
            _secondaryInputButton.Text = _noBindingString;
            _clearSecondaryInputButton.Visible = false;
        }
        else
        {
            _secondaryInputButton.Text = TextUtils.GetInputEventDisplayString(inputEvent);
            _clearSecondaryInputButton.Visible = true;
        }

        _resetToDefaultsButton.Visible = !InputManager.Instance.IsInputActionDefault(_action);

    }
}
