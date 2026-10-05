namespace GeniaText.Services;

public sealed class PasteService
{
    private readonly AppSettingsStore _settingsStore;
    private readonly SemaphoreSlim _pasteGate = new(1, 1);
    private const string ClipboardMarkerFormat = "GeniaText.InternalPasteToken.v1";
    private const int ClipboardRetryCount = 5;
    private static readonly TimeSpan ClipboardRetryDelay = TimeSpan.FromMilliseconds(50);

    public PasteService(AppSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public async Task<bool> PasteAsync(IntPtr targetWindow, string text)
    {
        if (targetWindow == IntPtr.Zero || string.IsNullOrEmpty(text) || !IsWindow(targetWindow))
            return false;

        await _pasteGate.WaitAsync();
        try
        {

        WpfDataObject? clipboardSnapshot = CaptureClipboard(out bool clipboardCaptured);
        string marker = Guid.NewGuid().ToString("N");
        uint sequenceBefore = GetClipboardSequenceNumber();

        if (!await TrySetClipboardTextAsync(text, marker))
            return false;

        uint ourSequence = GetClipboardSequenceNumber();
        bool pasteSent = false;

        try
        {
            // Preserve the timing of the original 0.6.2 implementation. The picker
            // has just been hidden, so Windows needs a short moment to settle focus.
            await Task.Delay(80);

            // Match the proven 0.6.2 insertion sequence exactly: ask Windows to
            // return focus to the window that was active before the picker opened,
            // wait briefly, and then emit Ctrl+V.  fixed3 added a strict
            // GetForegroundWindow()==targetWindow gate here.  On some WPF/Win32
            // applications the foreground HWND is transient while focus is being
            // restored, so that gate rejected a paste that Windows would otherwise
            // deliver correctly.
            IntPtr foregroundBefore = GetForegroundWindow();
            bool foregroundRequested = SetForegroundWindow(targetWindow);
            await Task.Delay(100);
            IntPtr foregroundAfter = GetForegroundWindow();

            if (_settingsStore.Current.Features.CompatibilityModeEnabled &&
                foregroundAfter != targetWindow && IsWindow(targetWindow))
            {
                // Optional compatibility module only. The default path above stays
                // identical to the proven 0.6.3 behavior. Some applications need a
                // second foreground request after their transient popup loses focus.
                await Task.Delay(60);
                bool retryRequested = SetForegroundWindow(targetWindow);
                await Task.Delay(90);
                foregroundAfter = GetForegroundWindow();
                AppLog.Info($"Compatibility focus retry: requested={retryRequested}, after=0x{foregroundAfter.ToInt64():X}.");
            }

            // Exact HWND equality proved too strict during 0.6.3 recovery, because
            // some applications transiently expose another HWND while focus settles.
            // A process-level guard is a safer compromise: it blocks Ctrl+V if the
            // user has genuinely switched to another application, while allowing
            // child/transient windows owned by the original target process.
            if (!IsSameProcess(targetWindow, foregroundAfter))
            {
                _ = SetForegroundWindow(targetWindow);
                await Task.Delay(50);
                foregroundAfter = GetForegroundWindow();
                if (!IsSameProcess(targetWindow, foregroundAfter))
                {
                    AppLog.Info(
                        $"Автовставка отменена: foreground process отличается от target; target=0x{targetWindow.ToInt64():X}, after=0x{foregroundAfter.ToInt64():X}.");
                    return false;
                }
            }

            AppLog.Info(
                $"Автовставка: target=0x{targetWindow.ToInt64():X}, before=0x{foregroundBefore.ToInt64():X}, " +
                $"SetForegroundWindow={foregroundRequested}, after=0x{foregroundAfter.ToInt64():X}.");

            INPUT[] inputs =
            [
                CreateKeyboardInput(0x11, keyUp: false), // Ctrl down
                CreateKeyboardInput(0x56, keyUp: false), // V down
                CreateKeyboardInput(0x56, keyUp: true),  // V up
                CreateKeyboardInput(0x11, keyUp: true)   // Ctrl up
            ];

            int inputSize = Marshal.SizeOf<INPUT>();
            uint sent = SendInput((uint)inputs.Length, inputs, inputSize);
            if (sent != (uint)inputs.Length)
            {
                int error = Marshal.GetLastWin32Error();
                AppLog.Info($"SendInput отправил {sent} из {inputs.Length} событий; cbSize={inputSize}; Win32={error}.");
                ReleasePasteKeysBestEffort(inputSize);
                return false;
            }

            pasteSent = true;

            // 0.6.2 waited 350 ms before restoring the previous clipboard. This is
            // deliberately longer than a single dispatcher tick because some target
            // applications consume Ctrl+V asynchronously.
            await Task.Delay(350);
            return true;
        }
        finally
        {
            // Restore the user's previous clipboard only after a successful Ctrl+V.
            // If automatic insertion failed, the selected phrase intentionally stays
            // in the clipboard so the user can paste it manually.
            //
            // The marker + clipboard sequence guard prevents us from overwriting a
            // newer clipboard value copied by the user or another application while
            // GeniaText was inserting the phrase.
            if (pasteSent && clipboardCaptured && clipboardSnapshot is not null)
                await TryRestoreClipboardAsync(clipboardSnapshot, marker, ourSequence, sequenceBefore);
        }
        }
        finally
        {
            _pasteGate.Release();
        }
    }

    private static WpfDataObject? CaptureClipboard(out bool captured)
    {
        captured = false;
        try
        {
            WpfDataObject? source = Clipboard.GetDataObject();
            var copy = new DataObject();

            if (source is not null)
            {
                foreach (string format in source.GetFormats(autoConvert: false))
                {
                    try
                    {
                        object? data = source.GetData(format, autoConvert: false);
                        if (data is not null)
                            copy.SetData(format, data, autoConvert: false);
                    }
                    catch (Exception ex) when (ex is ExternalException)
                    {
                        AppLog.Error($"Не удалось скопировать формат буфера {format}", ex);
                    }
                }
            }

            captured = true;
            return copy;
        }
        catch (Exception ex) when (ex is ExternalException)
        {
            AppLog.Error("Не удалось захватить буфер обмена", ex);
            return null;
        }
    }

    private static async Task<bool> TrySetClipboardTextAsync(string text, string marker)
    {
        for (int attempt = 0; attempt < ClipboardRetryCount; attempt++)
        {
            try
            {
                var data = new DataObject();
                data.SetData(DataFormats.UnicodeText, text);
                data.SetData(DataFormats.Text, text);
                data.SetData(ClipboardMarkerFormat, marker);
                Clipboard.SetDataObject(data, copy: true);
                return true;
            }
            catch (Exception ex) when (ex is ExternalException)
            {
                if (attempt == ClipboardRetryCount - 1)
                    AppLog.Error("Не удалось временно записать текст в буфер обмена", ex);
                else
                    await Task.Delay(ClipboardRetryDelay);
            }
        }
        return false;
    }

    private static async Task TryRestoreClipboardAsync(
        WpfDataObject snapshot,
        string marker,
        uint ourSequence,
        uint sequenceBefore)
    {
        for (int attempt = 0; attempt < ClipboardRetryCount; attempt++)
        {
            try
            {
                WpfDataObject? current = Clipboard.GetDataObject();
                bool markerStillPresent =
                    current is not null &&
                    current.GetDataPresent(ClipboardMarkerFormat, autoConvert: false) &&
                    string.Equals(current.GetData(ClipboardMarkerFormat, autoConvert: false) as string,
                        marker, StringComparison.Ordinal);

                uint currentSequence = GetClipboardSequenceNumber();
                if (!markerStillPresent || currentSequence != ourSequence)
                {
                    AppLog.Info(
                        $"Буфер не восстановлен: он изменился после временной записи (before={sequenceBefore}, ours={ourSequence}, now={currentSequence}).");
                    return;
                }

                Clipboard.SetDataObject(snapshot, copy: true);
                return;
            }
            catch (Exception ex) when (ex is ExternalException)
            {
                if (attempt == ClipboardRetryCount - 1)
                    AppLog.Error("Не удалось восстановить буфер обмена", ex);
                else
                    await Task.Delay(ClipboardRetryDelay);
            }
        }
    }

    private static bool IsSameProcess(IntPtr firstWindow, IntPtr secondWindow)
    {
        if (firstWindow == IntPtr.Zero || secondWindow == IntPtr.Zero) return false;
        _ = GetWindowThreadProcessId(firstWindow, out uint firstProcess);
        _ = GetWindowThreadProcessId(secondWindow, out uint secondProcess);
        return firstProcess != 0 && firstProcess == secondProcess;
    }

    private static void ReleasePasteKeysBestEffort(int inputSize)
    {
        try
        {
            INPUT[] releases =
            [
                CreateKeyboardInput(0x56, keyUp: true),
                CreateKeyboardInput(0x11, keyUp: true)
            ];
            _ = SendInput((uint)releases.Length, releases, inputSize);
        }
        catch
        {
            // Never mask the original paste failure. This is only protection from
            // a partially delivered Ctrl+V leaving Ctrl logically pressed.
        }
    }

    private static INPUT CreateKeyboardInput(ushort virtualKey, bool keyUp)
    {
        return new INPUT
        {
            type = 1, // INPUT_KEYBOARD
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = keyUp ? 0x0002u : 0u, // KEYEVENTF_KEYUP
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    // IMPORTANT: INPUT is a Win32 union. Even though GeniaText only sends
    // keyboard input, the union must still be large enough for MOUSEINPUT.
    // On x64 this makes sizeof(INPUT) == 40 bytes (32-byte union + type/padding).
    // If the union contains KEYBDINPUT only, Marshal.SizeOf<INPUT>() becomes
    // 32 bytes and user32!SendInput rejects the call with ERROR_INVALID_PARAMETER.
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern UIntPtr GetMessageExtraInfo();

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
