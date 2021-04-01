using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UnrealSharp
{
    public class Hotkeys
    {
        [DllImport("user32")] static extern short GetKeyState(int keyCode);
        public static Boolean ToggledKey(Keys keyCode)
        {
            return (GetKeyState((int)keyCode) == 0);
        }
        public static Boolean IsPressed(Keys keyCode)
        {
            return (GetKeyState((int)keyCode) & 0x100) != 0;
        }
    }
}
