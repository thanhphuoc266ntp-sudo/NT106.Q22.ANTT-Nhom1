using System;
using System.Runtime.InteropServices;

namespace RemoteMate.Services
{
    public class RemoteInputService
    {
        // Mouse event flags
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        // Keyboard event flags
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, int dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public void Execute(string? command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            try
            {
                var parts = command.Split('|');
                if (parts.Length == 0) return;
                if (parts[0] != "INPUT") return;

                // Normalize parts length
                // Expected: INPUT|TYPE|... 
                var type = parts.Length > 1 ? parts[1] : string.Empty;

                switch (type)
                {
                    case "MOVE":
                        // INPUT|MOVE|{x}|{y}
                        if (parts.Length >= 4 &&
                            int.TryParse(parts[2], out var mx) &&
                            int.TryParse(parts[3], out var my))
                        {
                            SetCursorPos(mx, my);
                        }
                        break;

                    case "DOWN":
                    case "UP":
                        // INPUT|DOWN|{button}|{x}|{y}  (button = Left|Right|Middle)
                        if (parts.Length >= 5 &&
                            Enum.TryParse(parts[2], true, out MouseButton btn) &&
                            int.TryParse(parts[3], out var dx) &&
                            int.TryParse(parts[4], out var dy))
                        {
                            SetCursorPos(dx, dy);
                            uint flag = 0;
                            if (btn == MouseButton.Left) flag = type == "DOWN" ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
                            else if (btn == MouseButton.Right) flag = type == "DOWN" ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
                            else if (btn == MouseButton.Middle) flag = type == "DOWN" ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;

                            mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
                        }
                        break;

                    case "WHEEL":
                        // INPUT|WHEEL|{delta}|{x}|{y}
                        if (parts.Length >= 5 &&
                            int.TryParse(parts[2], out var delta) &&
                            int.TryParse(parts[3], out var wx) &&
                            int.TryParse(parts[4], out var wy))
                        {
                            SetCursorPos(wx, wy);
                            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, delta, UIntPtr.Zero);
                        }
                        break;

                    case "KEYDOWN":
                        // INPUT|KEYDOWN|{virtualKey}
                        if (parts.Length >= 3 && byte.TryParse(parts[2], out var vkDown))
                        {
                            keybd_event(vkDown, 0, 0, UIntPtr.Zero);
                        }
                        break;

                    case "KEYUP":
                        // INPUT|KEYUP|{virtualKey}
                        if (parts.Length >= 3 && byte.TryParse(parts[2], out var vkUp))
                        {
                            keybd_event(vkUp, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        }
                        break;

                    default:
                        // Unknown input type - ignore
                        break;
                }
            }
            catch
            {
                // Swallow exceptions as required to avoid dropping connection
            }
        }

        private enum MouseButton
        {
            Left,
            Right,
            Middle
        }
    }
}