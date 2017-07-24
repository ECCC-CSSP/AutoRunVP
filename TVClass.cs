using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CSSPAppModel;

namespace AutoRunVP
{
    public class TVClass
    {
        public TVClass()
        {
            TVItem tvi = new TVItem();
            tvi.csspItem = new CSSPItem();
            tvi.csspItemLanguage = new CSSPItemLanguage();
            tvi.csspTypeItem = new CSSPTypeItem();
            tvi.csspTypeItemLanguage = new CSSPTypeItemLanguage();
        }
        public class TVItem
        {
            public CSSPItem csspItem { get; set; }
            public CSSPItemLanguage csspItemLanguage { get; set; }
            public CSSPTypeItem csspTypeItem { get; set; }
            public CSSPTypeItemLanguage csspTypeItemLanguage { get; set; }
        }
    }

    public partial class AutoRunVP
    {
        public class VPAndCormixResValues
        {
            public double Conc { get; set; }
            public double Dilu { get; set; }
            public double FarfieldWidth { get; set; }
            public double Distance { get; set; }
            public double TheTime { get; set; }
            public double Decay { get; set; }
        }
        public class APIFunc
        {

            private const uint GW_CHILD = 5;
            private const uint GW_HWNDNEXT = 2;
            private const uint WM_LBUTTONDOWN = 0x201;
            private const uint WM_LBUTTONUP = 0x202;
            private const uint WM_CHAR = 0x102;
            private const uint WM_KEYDOWN = 0x100;
            private const uint WM_KEYUP = 0x101;
            private const uint WM_SYSKEYDOWN = 0x104;
            private const uint WM_SYSKEYUP = 0x105;
            private const uint VK_SHIFT = 0x10;
            private const uint VK_CONTROL = 0x11;

            private static List<WndHandleAndTitle> wht;

            public APIFunc()
            {
                wht = new List<WndHandleAndTitle>();
            }
            private void MegaDoEvents()
            {
                for (int i = 0; i < 20000; i++)
                {
                    Application.DoEvents();
                }
            }
            public void APISendMouseClick(IntPtr hWnd, int x, int y)
            {
                APISendMessage(hWnd, WM_LBUTTONDOWN, (int)0, (uint)(Convert.ToUInt16(x) + (Convert.ToUInt16(y) << 16)));
                APISendMessage(hWnd, WM_LBUTTONUP, (int)0, (uint)(Convert.ToUInt16(x) + (Convert.ToUInt16(y) << 16)));
                MegaDoEvents();
            }
            public void APIPostMouseClick(IntPtr hWnd, int x, int y)
            {
                APIPostMessage(hWnd, (uint)WM_LBUTTONDOWN, (uint)1, (uint)(Convert.ToUInt16(x) + (Convert.ToUInt16(y) << 16)));
                APIPostMessage(hWnd, (uint)WM_LBUTTONUP, (uint)0, (uint)(Convert.ToUInt16(x) + (Convert.ToUInt16(y) << 16)));
                MegaDoEvents();
            }
            public IntPtr APIFindWinow(string lpClassName, string lpWindowName)
            {
                return FindWindow(lpClassName, lpWindowName);
            }
            public IntPtr APIFindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName)
            {
                return FindWindowEx(hwndParent, hwndChildAfter, lpClassName, lpWindowName);
            }
            public bool APISetForegroundWindow(IntPtr hWnd)
            {
                return SetForegroundWindow(hWnd);
            }
            public IntPtr APIGetForegroundWindow()
            {
                return GetForegroundWindow();
            }
            public uint APISendMessage(IntPtr hWnd, uint Msg, int wParam, uint lParam)
            {
                return SendMessage(hWnd, Msg, wParam, lParam);
            }
            public uint APIPostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam)
            {
                return PostMessage(hWnd, Msg, wParam, lParam);
            }
            public IntPtr APIGetDesktopWindow()
            {
                return GetDesktopWindow();
            }
            public IntPtr APIGetWindow(IntPtr hWnd, uint uCmd)
            {
                return GetWindow(hWnd, uCmd);
            }
            public bool APIIsWindowVisible(IntPtr hWnd)
            {
                return IsWindowVisible(hWnd);
            }
            public int APIGetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount)
            {
                return GetWindowText(hWnd, lpString, nMaxCount);
            }
            public int APIInternalGetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount)
            {
                return InternalGetWindowText(hWnd, lpString, nMaxCount);
            }
            public int APIGetWindowTextLength(IntPtr hWnd)
            {
                return GetWindowTextLength(hWnd);
            }
            public IntPtr APISetFocus(IntPtr hWnd)
            {
                return SetFocus(hWnd);
            }
            public IntPtr APIGetFocus()
            {
                return GetFocus();
            }
            public bool APISetWindowText(IntPtr hWnd, StringBuilder WindowText)
            {
                return SetWindowText(hWnd, WindowText);
            }
            public bool APICloseWindow(IntPtr hWnd)
            {
                return CloseWindow(hWnd);
            }
            public bool APIDestroyWindow(IntPtr hWnd)
            {
                return DestroyWindow(hWnd);
            }
            public List<WndHandleAndTitle> GetChildrenWindowsHandleAndTitle(IntPtr hWnd)
            {
                wht.Clear();
                FillWindowHandleAndTitle(hWnd);
                return wht;
            }

            #region DllImport static functions
            // Get a handle to an application window.
            [DllImport("USER32.DLL")]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("USER32.DLL")]
            public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

            // Activate an application window.
            [DllImport("USER32.DLL")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            // Get the forground window.
            [DllImport("USER32.DLL")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            public static extern uint SendMessage(IntPtr hWnd, uint Msg, int wParam, uint lParam);

            [DllImport("user32.dll")]
            public static extern uint PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

            [DllImport("user32.dll")]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern int InternalGetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("USER32.DLL")]
            public static extern IntPtr SetFocus(IntPtr hWnd);

            [DllImport("USER32.DLL")]
            public static extern IntPtr GetFocus();

            [DllImport("USER32.DLL")]
            public static extern bool SetWindowText(IntPtr hWnd, StringBuilder WindowText);

            [DllImport("user32")]
            public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

            [DllImport("user32")]
            public static extern bool CloseWindow(IntPtr window);

            [DllImport("user32")]
            public static extern bool DestroyWindow(IntPtr window);

            public void FillWindowHandleAndTitle(IntPtr hWnd)
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(hWnd, childProc, IntPtr.Zero);
                return;
            }
            private static bool EnumWindow(IntPtr hWnd, IntPtr pointer)
            {
                // get the text from the window
                StringBuilder bld = new StringBuilder(256);
                GetWindowText(hWnd, bld, 256);
                string text = bld.ToString();

                WndHandleAndTitle Tempwht = new WndHandleAndTitle()
                {
                    Handle = hWnd,
                    Title = text
                };
                wht.Add(Tempwht);
                return true;
            }
            public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);
            #endregion DllImport static functions
        }
        public class WndHandleAndTitle
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; }
        }
        public class CloseCaptionAndCommand
        {
            public string Caption { get; set; }
            public string Command { get; set; }
        }
        public class TVI
        {
            public int ItemID { get; set; }
            public string ItemText { get; set; }
        }
    }
}
