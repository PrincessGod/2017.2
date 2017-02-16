using System;
using System.Globalization;
using System.Windows.Forms;

namespace R.Earth
{
    public sealed class Utility
    {
        internal static bool IsInDesignMode()
        {
            return Application.ExecutablePath.ToUpper(CultureInfo.InvariantCulture).EndsWith("DEVENV.EXE");
        }
        /// <summary>
        /// Determine whether any window messages is queued.
        /// </summary>
        internal static bool IsAppStillIdle
        {
            get
            {
                NativeMethods.Message msg;
                return !NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
            }
        }
    }
}
