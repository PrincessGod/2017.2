using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace R.Earth.OverLayer.Interface
{
    public interface IMenu
    {
        void OnKeyUp(KeyEventArgs keyEvent);
        void OnKeyDown(KeyEventArgs keyEvent);
        bool OnMouseUp(MouseEventArgs e);
        bool OnMouseDown(MouseEventArgs e);
        bool OnMouseMove(MouseEventArgs e);
        bool OnMouseWheel(MouseEventArgs e);
        void Render(DrawArgs drawArgs);
        void Dispose();
    }
}
