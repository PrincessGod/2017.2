using System;
using System.Collections.Generic;
using System.Text;

namespace R.Earth.OverLayer.Enums
{
    /// <summary>
    /// Widget Anchor Styles.  Same values as Forms AnchorStyles
    /// </summary>
    [Flags]
    public enum AnchorWidgetStyles
    {
        None = 0x0000,
        Top = 0x0001,
        Bottom = 0x0002,
        Left = 0x0004,
        Right = 0x0008,
    }
}
