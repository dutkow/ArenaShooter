using Godot;
using System;
using System.Collections.Generic;

public enum CursorMode
{
    DEFAULT,
    DRAG_MOVE,
    HOVERING_HINT,
}

public partial class CursorManager : Node
{
    public static CursorManager Instance { get; private set; }

    public event Action<CursorLockMode> CursorLockModeChanged;

    [Export] Json _cursorsJson;

    public static readonly float MIN_CURSOR_WIDTH = 8.0f;
    public static readonly float MAX_CURSOR_WIDTH = 64.0f;
    public int CursorWidth = 32;
    private int _previousCursorWidth;


    private Dictionary<CursorMode, Texture2D> _baseCursors = new();
    private Dictionary<CursorMode, Texture2D> _scaledCursors = new();
    private float CursorScale => CursorWidth / MAX_CURSOR_WIDTH;
    public CursorMode CurrentCursorMode { get; private set; }
    private Texture2D currentPointerTexture;

    private CursorLockMode _cursorLockMode;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;
        _previousCursorWidth = CursorWidth;

        LoadAllBaseCursors();
        UpdateAllScaledCursors();

        SetCursorMode(CursorMode.DEFAULT, true);

        SetCursorLockMode(SettingsManager.Instance.Settings.Controls.CursorLock.Value);
        SettingsManager.Instance.Settings.Controls.CursorLock.Changed += SetCursorLockMode;
    }
    private void LoadAllBaseCursors()
    {
        var CursorData = JsonUtils.LoadJson<List<CursorData>>(_cursorsJson);
        if(CursorData == null)
        {
            GD.PushError("Cursor data is null");
            return;
        }

        for(int i = 0; i < CursorData.Count; ++i)
        {
            _baseCursors[CursorData[i].CursorMode] = LoadCursorFromPath(CursorData[i].TexturePath);
        }
    }

    private Texture2D LoadCursorFromPath(string path)
    {
        var tex = GD.Load<Texture2D>(path);
        if (tex == null)
        {
            GD.PrintErr($"Failed to load cursor texture at path: {path}");
        }
        return tex;
    }

    public void UpdatePointerScaledCursor()
    {
        var baseTex = _baseCursors[CursorMode.DEFAULT];
        if (currentPointerTexture != null)
        {
            currentPointerTexture.Dispose();
            currentPointerTexture = null;
        }

        currentPointerTexture = ScaleTexture(baseTex, CursorScale);
        _scaledCursors[CursorMode.DEFAULT] = currentPointerTexture;
        Input.SetCustomMouseCursor(currentPointerTexture);
    }

    public void UpdateAllScaledCursors()
    {
        foreach (var kvp in _baseCursors)
        {
            UpdateScaledCursor(kvp.Key);
        }
        UpdateCursorTexture();
    }

    public void UpdateScaledCursor(CursorMode cursorMode)
    {
        Texture2D newTexture = _baseCursors[cursorMode];
        _scaledCursors[cursorMode] = ScaleTexture(newTexture, CursorScale);
    }

    public void RestoreDefaultCursor()
    {
        SetCursorMode(CursorMode.DEFAULT);
    }

    public void SetCursorMode(CursorMode cursorMode, bool force = false)
    {
        if (CurrentCursorMode == cursorMode && !force)
        {
            return;
        }

        CurrentCursorMode = cursorMode;
        UpdateCursorTexture();
    }

    private void UpdateCursorTexture()
    {
        if (_scaledCursors.TryGetValue(CurrentCursorMode, out var cursorTexture))
        {
            Input.SetCustomMouseCursor(cursorTexture);
        }
    }

    public void SetCursorWidth(float width)
    {
        width = Mathf.Clamp(width, MIN_CURSOR_WIDTH, MAX_CURSOR_WIDTH);
        int newWidth = Mathf.RoundToInt(width);

        if (newWidth != CursorWidth)
        {
            CursorWidth = newWidth;
            UpdatePointerScaledCursor();
            UpdateCursorTexture();
        }
    }

    private Texture2D ScaleTexture(Texture2D texture, float scale)
    {
        if (texture == null) return null;

        Image image = texture.GetImage();
        if (image == null) return null;

        int newSize = (int)(MAX_CURSOR_WIDTH * scale);
        image.Resize(newSize, newSize, Image.Interpolation.Bilinear);

        return ImageTexture.CreateFromImage(image);
    }

    public void OnLockCursorToWindowChanged(bool value)
    {
        if(value)
        {
            SetMouseMode(Input.MouseModeEnum.Confined);
        }
        else
        {
            SetMouseMode(Input.MouseModeEnum.Visible);
        }
    }

    public void SetCursorLockMode(CursorLockMode mode)
    {
        _cursorLockMode = mode;

        CursorLockModeChanged?.Invoke(_cursorLockMode);

        UpdateCursorLock();
    }

    public void UpdateCursorLock()
    {
        var windowMode = DisplayServer.WindowGetMode();
        if(windowMode == DisplayServer.WindowMode.ExclusiveFullscreen)
        {
            if(_cursorLockMode == CursorLockMode.FULL_SCREEN_ONLY || _cursorLockMode == CursorLockMode.BOTH)
            {
                SetMouseMode(Input.MouseModeEnum.Confined);
            }
            else
            {
                SetMouseMode(Input.MouseModeEnum.Visible);
            }
        }
        else if(windowMode == DisplayServer.WindowMode.Windowed || windowMode == DisplayServer.WindowMode.Maximized)
        {
            if (_cursorLockMode == CursorLockMode.WINDOWED_ONLY || _cursorLockMode == CursorLockMode.BOTH)
            {
                SetMouseMode(Input.MouseModeEnum.Confined);
            }
            else
            {
                SetMouseMode(Input.MouseModeEnum.Visible);
            }
        }
    }

    public void SetMouseMode(Input.MouseModeEnum mouseMode)
    {
        Input.MouseMode = mouseMode;
    }
}
