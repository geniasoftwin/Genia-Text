namespace GeniaText.Services;

public sealed class GlobalHotkey : IDisposable
{
    private const int HotkeyId = 0x5148;
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;

    private readonly IntPtr _windowHandle;
    private readonly HwndSource _source;
    private bool _registered;
    private bool _disposed;
    private uint _modifiers;
    private uint _virtualKey;

    public GlobalHotkey(Window owner, uint modifiers, uint virtualKey)
    {
        _windowHandle = new WindowInteropHelper(owner).EnsureHandle();
        _source = HwndSource.FromHwnd(_windowHandle)
            ?? throw new InvalidOperationException("Не удалось подключить оконный обработчик горячей клавиши.");
        _source.AddHook(WindowMessageHook);
        _modifiers = modifiers;
        _virtualKey = virtualKey;
    }

    public event EventHandler? Pressed;

    public uint Modifiers => _modifiers;
    public uint VirtualKey => _virtualKey;

    public bool Register()
    {
        ThrowIfDisposed();
        if (_registered) return true;

        _registered = RegisterHotKey(_windowHandle, HotkeyId, _modifiers | ModNoRepeat, _virtualKey);
        return _registered;
    }

    public bool TryChange(uint modifiers, uint virtualKey)
    {
        ThrowIfDisposed();
        if (_modifiers == modifiers && _virtualKey == virtualKey)
            return _registered || Register();

        uint oldModifiers = _modifiers;
        uint oldVirtualKey = _virtualKey;
        bool wasRegistered = _registered;

        if (_registered)
        {
            UnregisterHotKey(_windowHandle, HotkeyId);
            _registered = false;
        }

        _modifiers = modifiers;
        _virtualKey = virtualKey;
        if (Register()) return true;

        // Roll back to the last known configuration. If rollback also fails,
        // leave _registered=false so callers can warn the user accurately.
        _modifiers = oldModifiers;
        _virtualKey = oldVirtualKey;
        _registered = wasRegistered && RegisterHotKey(_windowHandle, HotkeyId, oldModifiers | ModNoRepeat, oldVirtualKey);
        return false;
    }

    public static string Format(uint modifiers, uint virtualKey)
    {
        var parts = new List<string>();
        if ((modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((modifiers & 0x0004) != 0) parts.Add("Shift");

        parts.Add(virtualKey switch
        {
            0x20 => "Space",
            >= 0x70 and <= 0x7B => $"F{virtualKey - 0x6F}",
            0x2D => "Insert",
            _ => $"VK 0x{virtualKey:X2}"
        });
        return string.Join("+", parts);
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_registered)
        {
            UnregisterHotKey(_windowHandle, HotkeyId);
            _registered = false;
        }
        _source.RemoveHook(WindowMessageHook);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
