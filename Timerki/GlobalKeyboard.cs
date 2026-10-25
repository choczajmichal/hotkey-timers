using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
namespace Timerki;
public sealed class GlobalKeyboard : IDisposable
{
    private const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_SYSKEYDOWN = 0x104, WM_KEYUP = 0x101, WM_SYSKEYUP = 0x105;
    private readonly LowLevelProc _callback;
    private IntPtr _hook;
    private readonly HashSet<int> _pressed = [];
    public event Action<Key>? KeyPressed;
    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] private struct KbdData { public uint vkCode, scanCode, flags, time; public UIntPtr dwExtraInfo; }
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc proc, IntPtr hMod, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string? moduleName);
    public GlobalKeyboard()
    {
        _callback = Hook;
        using var proc = Process.GetCurrentProcess();
        using var module = proc.MainModule;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, GetModuleHandle(module?.ModuleName), 0);
        if (_hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Nie można włączyć skrótów globalnych.");
    }
    private IntPtr Hook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message = unchecked((int)wParam.ToInt64());
            if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN || message == WM_KEYUP || message == WM_SYSKEYUP)
            {
                var data = Marshal.PtrToStructure<KbdData>(lParam);
                int vk = (int)data.vkCode;
                if (message == WM_KEYUP || message == WM_SYSKEYUP) _pressed.Remove(vk);
                else if (_pressed.Add(vk))
                {
                    var key = KeyInterop.KeyFromVirtualKey(vk);
                    try { KeyPressed?.Invoke(key); } catch { /* never break hook chain */ }
                }
            }
        }
        // Do not consume keyboard input. Game/app receives its key as normal.
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }
    public void Dispose()
    {
        if (_hook != IntPtr.Zero) { UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
        _pressed.Clear();
    }
}
