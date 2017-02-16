using System;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace R.Earth
{
    /// <summary>
    /// Interop methods for WorldWindow namespace
    /// </summary>
    public sealed class NativeMethods
    {
        private NativeMethods()
        {
        }


        public const int WM_COPYDATA = 0x004A;
        public const int WM_ACTIVATEAPP = 0x001C;

       

        /// <summary>
        /// Sends string arguments to running instance of EvGlobe.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool SendArgs(IntPtr targetHWnd, string args)
        {
            if (targetHWnd == IntPtr.Zero)
                return false;

            CopyDataStruct cds = new CopyDataStruct();
            try
            {
                cds.cbData = (args.Length + 1) * 2;
                cds.lpData = NativeMethods.LocalAlloc(0x40, cds.cbData);
                Marshal.Copy(args.ToCharArray(), 0, cds.lpData, args.Length);
                cds.dwData = (IntPtr)1;

                return SendMessage(targetHWnd, WM_COPYDATA, /*Handle*/System.IntPtr.Zero, ref cds);
            }
            finally
            {
                cds.Dispose();
            }
        }

        internal struct CopyDataStruct : IDisposable
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;

            public void Dispose()
            {
                if (this.lpData != IntPtr.Zero)
                {
                    LocalFree(this.lpData);
                    this.lpData = IntPtr.Zero;
                }
            }
        }


        /// <summary>
        /// Contains message information from a thread's message queue.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Message
        {
            public IntPtr hWnd;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point p;
        }

        /// <summary>
        /// The PeekMessage function dispatches incoming sent messages, 
        /// checks the thread message queue for a posted message, 
        /// and retrieves the message (if any exist).
        /// </summary>
        [System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);

        [DllImport("user32.dll")]
        internal static extern bool SendMessage(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam,
            ref CopyDataStruct lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LocalAlloc(int flag, int size);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr p);

        /// <summary>
        /// API function to find window based on WindowName and class.
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    }
}
