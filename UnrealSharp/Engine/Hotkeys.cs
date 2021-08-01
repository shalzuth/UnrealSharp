using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UnrealSharp
{
    public class Hotkeys
    {
        [DllImport("user32")] static extern short GetKeyState(int keyCode);
        [DllImport("user32")] static extern short GetAsyncKeyState(int keyCode);
        static Dictionary<Keys, Boolean> ToggleVals = new Dictionary<Keys, Boolean>();
        static Dictionary<Keys, Boolean> SingleVals = new Dictionary<Keys, Boolean>();
        public static Boolean ToggledKey(Keys keyCode)
        {
            if (!ToggleVals.ContainsKey(keyCode)) ToggleVals.Add(keyCode, false);
            if (SinglePress(keyCode))
                ToggleVals[keyCode] = !ToggleVals[keyCode];
            return ToggleVals[keyCode];
            return (GetAsyncKeyState((int)keyCode) & 0x8000) != 0;
            return (GetAsyncKeyState((int)keyCode) & 0x1) != 0;
            return (GetAsyncKeyState((int)keyCode) == 0);
            return (GetKeyState((int)keyCode) == 0);
        }
        public static Boolean SinglePress(Keys keyCode)
        {
            if (!SingleVals.ContainsKey(keyCode)) SingleVals.Add(keyCode, false);
            if (IsPressed(keyCode))
            {
                if (SingleVals[keyCode]) return false;
                SingleVals[keyCode] = true;
                return true;
            }
            else SingleVals[keyCode] = false;
            return false;
            return (GetAsyncKeyState((int)keyCode) & 0x8000) != 0;
            return (GetAsyncKeyState((int)keyCode) & 0x1) != 0;
            return (GetAsyncKeyState((int)keyCode) == 0);
            return (GetKeyState((int)keyCode) == 0);
        }
        public static Boolean IsPressed(Keys keyCode)
        {
            return (GetAsyncKeyState((int)keyCode) & 0x8000) != 0;
            return (GetKeyState((int)keyCode) & 0x100) != 0;
        }
    }
}
