using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IronAHK.Rusty
{
    partial class Core
    {
        // TODO: organise Mouse.cs

        /// <summary>
        /// Clicks a mouse button at the specified coordinates. It can also hold down a mouse button, turn the mouse wheel, or move the mouse.
        /// </summary>
        /// <param name="Options"></param>
        public static void Click(object[] Options)
        {
            // only supporting Windows for now
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;

            string ParamLine = "";
            Windows.INPUT[] aInput = new Windows.INPUT[2];
            Point MousePos = new Point(0, 0);
            int ClickCount = 1;

            Regex RE_Coord = new Regex(@"(\d*?)\s*,\s*(\d*)[\s,\,]*", RegexOptions.IgnoreCase);
            Regex RE_Num = new Regex(@"\d+", RegexOptions.IgnoreCase);
            CaptureCollection Out;
            Match Match;

            //rebuild Argument string, as we have to parse this in a special way 
            foreach (object option in Options)
            {
                if (option is string)
                    ParamLine += (string)option + ",";
                else if (option is double)
                    ParamLine += ((int)(double)option).ToString() + ",";
            }
            ParamLine = ParamLine.ToLower().Substring(0, ParamLine.Length - 1);

            //search coordinates, move mouse, remove them
            if (RE_Coord.IsMatch(ParamLine))
            {
                Match = RE_Coord.Match(ParamLine);
                MousePos.X = Convert.ToInt32(Match.Groups[1].Value);
                MousePos.Y = Convert.ToInt32(Match.Groups[2].Value);
                ParamLine = RE_Coord.Replace(ParamLine, ""); //remove coord

                Cursor CurrentCursor = new Cursor(Cursor.Current.Handle);
                Cursor.Position = MousePos;
            }
            //click count
            if (RE_Num.IsMatch(ParamLine))
            {
                Out = RE_Num.Match(ParamLine).Captures;
                ClickCount = Convert.ToInt32(Out[0].Value);
                if (ClickCount <= 0)
                    return;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //right or left mouse
                if (ParamLine.Contains("right"))
                {
                    aInput[0].i.m.dwFlags = (uint)Windows.MOUSEEVENTF.RIGHTDOWN;
                    aInput[1].i.m.dwFlags = (uint)Windows.MOUSEEVENTF.RIGHTUP;
                }
                else
                {
                    aInput[0].i.m.dwFlags = (uint)Windows.MOUSEEVENTF.LEFTDOWN;
                    aInput[1].i.m.dwFlags = (uint)Windows.MOUSEEVENTF.LEFTUP;
                }
                //down event
                aInput[0].type = Windows.INPUT_MOUSE;
                aInput[0].i.m.dwExtraInfo = IntPtr.Zero;
                aInput[0].i.m.mouseData = 0;
                aInput[0].i.m.time = 0;
                aInput[0].i.m.dx = MousePos.X;
                aInput[0].i.m.dy = MousePos.Y;
                //up event
                aInput[1].type = Windows.INPUT_MOUSE;
                aInput[1].i.m.dwExtraInfo = IntPtr.Zero;
                aInput[1].i.m.mouseData = 0;
                aInput[1].i.m.time = 0;
                aInput[1].i.m.dx = MousePos.X;
                aInput[1].i.m.dy = MousePos.Y;

                if (ParamLine.Contains("up"))
                { //just send the up event:
                    aInput[0] = aInput[1];
                    for (int i = 1; ClickCount >= i; i++)
                        Windows.SendInput(1, aInput, Marshal.SizeOf(typeof(Windows.INPUT)));
                }
                else if (ParamLine.Contains("down"))
                {
                    //just send the down event:
                    for (int i = 1; ClickCount >= i; i++)
                        Windows.SendInput(1, aInput, Marshal.SizeOf(typeof(Windows.INPUT)));
                }
                else //send both events:
                    for (int i = 1; ClickCount >= i; i++)
                        Windows.SendInput((uint)aInput.Length, aInput, Marshal.SizeOf(typeof(Windows.INPUT)));
            }
        }
        
        
        /// <summary>
        /// Sends a mouse button or mouse wheel event to a control.
        /// </summary>
        /// <param name="Target">
        /// <para>If this parameter is blank, the target window's topmost control will be clicked (or the target window itself if it has no controls). Otherwise, one of the two modes below will be used.</para>
        /// <para>Mode 1 (Position): Specify the X and Y coordinates relative to the target window's upper left corner. The X coordinate must precede the Y coordinate and there must be at least one space or tab between them. For example: X55 Y33. If there is a control at the specified coordinates, it will be sent the click-event at those exact coordinates. If there is no control, the target window itself will be sent the event (which might have no effect depending on the nature of the window). Note: In this mode, the X and Y option letters of the Options parameter are ignored.</para>
        /// <para>Mode 2 (ClassNN or Text): Specify either ClassNN (the classname and instance number of the control) or the name/text of the control, both of which can be determined via Window Spy. When using name/text, the matching behavior is determined by SetTitleMatchMode.</para>
        /// <para>By default, mode 2 takes precedence over mode 1. For example, in the unlikely event that there is a control whose text or ClassNN has the format "Xnnn Ynnn", it would be acted upon by Mode 2. To override this and use mode 1 unconditionally, specify the word Pos in Options as in the following example: ControlClick, x255 y152, WinTitle,,,, Pos</para>
        /// <para>To operate upon a control's HWND (window handle), leave this parameter blank and specify ahk_id %ControlHwnd% for the WinTitle parameter (this also works on hidden controls even when DetectHiddenWindows is Off) . The HWND of a control is typically retrieved via ControlGet Hwnd, MouseGetPos, or DllCall.</para></param>
        /// <param name="WinTitle">The title or partial title of the target window (the matching behavior is determined by SetTitleMatchMode). If this and the other 3 window parameters are omitted, the Last Found Window will be used. If this is the letter A and the other 3 window parameters are omitted, the active window will be used. To use a window class, specify ahk_class ExactClassName (shown by Window Spy). To use a process identifier (PID), specify ahk_pid %VarContainingPID%. To use a window group, specify ahk_group GroupName. To use a window's unique ID number, specify ahk_id %VarContainingID%. The search can be narrowed by specifying multiple criteria. For example: My File.txt ahk_class Notepad</param>
        /// <param name="WinText">If present, this parameter must be a substring from a single text element of the target window (as revealed by the included Window Spy utility). Hidden text elements are detected if DetectHiddenText is ON.</param>
        /// <param name="WhichButton">
        /// <para>The button to click: LEFT, RIGHT, MIDDLE (or just the first letter of each of these). If omitted or blank, the LEFT button will be used.</para>
        /// <para>WheelUp (or WU) and WheelDown (or WD) are also supported on Windows NT/2000/XP or later. In this case, ClickCount is the number of notches to turn the wheel.</para>
        /// <para>X1 (XButton1, the 4th mouse button) and X2 (XButton2, the 5th mouse button) are also supported on Windows 2000/XP or later.</para>
        /// </param>
        /// <param name="ClickCount">The number of clicks to send. If omitted or blank, 1 click is sent.</param>
        /// <param name="Options">
        /// <para>A series of zero or more of the following option letters. For example: d x50 y25</para>
        /// <para>NA: Avoids activating the window, which might also improve reliability in cases where the user is physically moving the mouse during the ControlClick. However, this mode might not work properly for all types of windows and controls.</para>
        /// <para>D: Press the mouse button down but do not release it (i.e. generate a down-event). If both the D and U options are absent, a complete click (down and up) will be sent.</para>
        /// <para>U: Release the mouse button (i.e. generate an up-event). This option should not be present if the D option is already present (and vice versa).</para>
        /// <para>Pos: Specify the word Pos anywhere in Options to unconditionally use the X/Y positioning mode as described in the Control-or-Pos parameter above. </para>
        /// <para>Xn: Specify for n the X position to click at, relative to the control's upper left corner. If unspecified, the click will occur at the horizontal-center of the control.</para>
        /// <para>Yn: Specify for n the Y position to click at, relative to the control's upper left corner. If unspecified, the click will occur at the vertical-center of the control.</para>
        /// <para>Use decimal (not hexadecimal) numbers for the X and Y options.</para>
        /// </param>
        /// <param name="ExcludeTitle">Windows whose titles include this value will not be considered.</param>
        /// <param name="ExcludeText">Windows whose text include this value will not be considered.</param>
        public static void ControlClick(string Target, string WinTitle, string WinText, string WhichButton, string ClickCount, string Options, string ExcludeTitle, string ExcludeText)
        {

        }

        /// <summary>
        /// Sets coordinate mode for various commands to be relative to either the active window or the screen.
        /// </summary>
        /// <param name="Item">
        /// <list type="">
        /// <item>ToolTip: Affects ToolTip.</item>
        /// <item>Pixel: Affects PixelGetColor, PixelSearch, and ImageSearch.</item>
        /// <item>Mouse: Affects MouseGetPos, Click, and MouseMove/Click/Drag.</item>
        /// <item>Caret: Affects the built-in variables A_CaretX and A_CaretY.</item>
        /// <item>Menu: Affects the "Menu Show" command when coordinates are specified for it.</item>
        /// </list>
        /// </param>
        /// <param name="Mode">
        /// <para>If Param2 is omitted, it defaults to Screen.</para>
        /// <list type="">
        /// <item>Screen: Coordinates are relative to the desktop (entire screen).</item>
        /// <item>Relative: Coordinates are relative to the active window.</item>
        /// </list>
        /// </param>
        public static void CoordMode(string Item, string Mode)
        {

        }

        /// <summary>
        /// Retrieves the current position of the mouse cursor, and optionally which window and control it is hovering over.
        /// </summary>
        /// <param name="OutputVarX">The names of the variables in which to store the X and Y coordinates. The retrieved coordinates are relative to the active window unless CoordMode was used to change to screen coordinates.</param>
        /// <param name="OutputVarY">See <paramref name="OutputVarX"/>.</param>
        /// <param name="OutputVarWin">
        /// <para>This optional parameter is the name of the variable in which to store the unique ID number of the window under the mouse cursor. If the window cannot be determined, this variable will be made blank.</para>
        /// <para>The window does not have to be active to be detected. Hidden windows cannot be detected.</para>
        ///</param>
        /// <param name="OutputVarControl">
        /// <para>This optional parameter is the name of the variable in which to store the name (ClassNN) of the control under the mouse cursor. If the control cannot be determined, this variable will be made blank.</para>
        /// <para>The names of controls should always match those shown by the version of Window Spy distributed with v1.0.14+ (but not necessarily older versions of Window Spy). However, unlike Window Spy, the window under the mouse cursor does not have to be active for a control to be detected.</para>
        ///</param>
        /// <param name="Mode">
        /// <para>If omitted, it defaults to 0. Otherwise, specify one of the following digits:</para>
        /// <para>1: Uses a simpler method to determine OutputVarControl. This method correctly retrieves the active/topmost child window of an Multiple Document Interface (MDI) application such as SysEdit or TextPad. However, it is less accurate for other purposes such as detecting controls inside a GroupBox control.</para>
        /// <para>2: Stores the control's HWND in OutputVarControl rather than the control's ClassNN.</para>
        /// <para>3: A combination of 1 and 2 above.</para>
        /// </param>
        public static void MouseGetPos(out int OutputVarX, out int OutputVarY, out int OutputVarWin, out string OutputVarControl, int Mode)
        {
            Point pos;
            OutputVarWin = 0;
            OutputVarControl = null;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Windows.GetCursorPos(out pos);
                IntPtr hwnd = Windows.WindowFromPoint(pos);

                OutputVarWin = hwnd.ToInt32();

                var rect = new Windows.RECT();
                Windows.GetWindowRect(hwnd, out rect);
                IntPtr chwnd = Windows.RealChildWindowFromPoint(hwnd, new Point(pos.X - rect.Left, pos.Y - rect.Top));

                OutputVarControl = Mode == 1 ?
                    Windows.GetWindowText(chwnd) : chwnd.ToInt32().ToString();
            }
            else pos = System.Windows.Forms.Control.MousePosition;

            OutputVarX = pos.X;
            OutputVarY = pos.Y;
            if (/*_CoordMode.Mouse*/ true)
            {
                Windows.RECT rect;
                Windows.GetWindowRect(Windows.GetForegroundWindow(), out rect);
                OutputVarX -= rect.Left;
                OutputVarY -= rect.Top;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTAPI
        {
            public int x;
            public int y;
        }
    }
}