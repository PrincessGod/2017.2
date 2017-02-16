using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Microsoft.DirectX.Direct3D;
using System.Windows.Forms;
using System.Drawing;
using R.Earth.OverLayer.Interface;

namespace R.Earth.OverLayer.Control
{
    public enum MenuAnchor
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public class ControlMenuBar : IMenu
    {
        #region Private Members
        protected ArrayList m_toolsMenuButtons = new ArrayList();
        protected ArrayList m_layersMenuButtons = new ArrayList();
        protected VisibleState _visibleState = VisibleState.Visible;
        protected DateTime _lastVisibleChange = DateTime.Now;
        protected float _outerPadding = 5;
        protected int x;
        protected int y;
        protected int hideTimeMilliseconds = 100;
        protected bool _isHideable;
        protected const float padRatio = 1 / 9.0f;
        //protected CursorType mouseCursor;
        protected int chevronColor = Color.Black.ToArgb();
        protected CustomVertex.TransformedColored[] enabledChevron = new CustomVertex.TransformedColored[3];
        protected Sprite m_sprite;


        #endregion

        #region Properties

        /// <summary>
        /// Where the menubar is anchored.
        /// </summary>
        public MenuAnchor Anchor
        {
            get { return m_anchor; }
            set { m_anchor = value; }
        }
        private MenuAnchor m_anchor = MenuAnchor.Top;
      

        /// <summary>
        /// Indicates whether the menu is "open". (user activity)
        /// </summary>
        public bool IsActive
        {
            get
            {
                return (this._curSelection >= 0);
            }
        }

        public System.Collections.ArrayList LayersMenuButtons
        {
            get
            {
                return m_layersMenuButtons;
            }
            set
            {
                m_layersMenuButtons = value;
            }
        }

        public System.Collections.ArrayList ToolsMenuButtons
        {
            get
            {
                return m_toolsMenuButtons;
            }
            set
            {
                m_toolsMenuButtons = value;
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref= "T:WorldWind.Menu.MenuBar"/> class.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="iconSize"></param>
        public ControlMenuBar(MenuAnchor anchor, int iconSize)
        {
            m_anchor = anchor;
            ControlMenuButton.SelectedSize = iconSize;
        }

        /// <summary>
        /// Adds a tool button to the bar
        /// </summary>
        public void AddToolsMenuButton(ControlMenuButton button)
        {
            lock (m_toolsMenuButtons.SyncRoot)
            {
                m_toolsMenuButtons.Add(button);
            }
        }

        /// <summary>
        /// Adds a tool button to the bar
        /// </summary>
        public void AddToolsMenuButton(ControlMenuButton button, int index)
        {
            lock (m_toolsMenuButtons.SyncRoot)
            {
                if (index < 0)
                    m_toolsMenuButtons.Insert(0, button);
                else if (index >= m_toolsMenuButtons.Count)
                    m_toolsMenuButtons.Add(button);
                else
                    m_toolsMenuButtons.Insert(index, button);
            }
        }

        /// <summary>
        /// Removes a layer button from the bar if it is found.
        /// </summary>
        public void RemoveToolsMenuButton(ControlMenuButton button)
        {
            lock (m_toolsMenuButtons.SyncRoot)
            {
                m_toolsMenuButtons.Remove(button);
            }
        }

        /// <summary>
        /// Adds a layer button to the bar
        /// </summary>
        public void AddLayersMenuButton(ControlMenuButton button)
        {
            lock (m_layersMenuButtons.SyncRoot)
            {
                m_layersMenuButtons.Add(button);
            }
        }

        /// <summary>
        /// Adds a layer button to the bar
        /// </summary>
        public void AddLayersMenuButton(ControlMenuButton button, int index)
        {
            lock (m_layersMenuButtons.SyncRoot)
            {
                if (index < m_layersMenuButtons.Count)
                    m_layersMenuButtons.Insert(0, button);
                else if (index >= m_layersMenuButtons.Count)
                    m_layersMenuButtons.Add(button);
                else
                    m_layersMenuButtons.Insert(index, button);
            }
        }

        /// <summary>
        /// Removes a layer button from the bar if it is found.
        /// </summary>
        public void RemoveLayersMenuButton(ControlMenuButton button)
        {
            lock (m_layersMenuButtons.SyncRoot)
            {
                m_layersMenuButtons.Remove(button);
            }
        }

        #region IMenu Members

        public void OnKeyUp(KeyEventArgs keyEvent)
        {
            // TODO:  Add ToolsMenuBar.OnKeyUp implementation
        }

        public void OnKeyDown(KeyEventArgs keyEvent)
        {
            // TODO:  Add ToolsMenuBar.OnKeyDown implementation
        }

        public bool OnMouseUp(MouseEventArgs e)
        {
            if (World.Settings.ShowToolBar)
            {
                if (this._curSelection != -1 && e.Button == MouseButtons.Left)
                {
                    if (this._curSelection < m_toolsMenuButtons.Count)
                    {
                        ControlMenuButton button = (ControlMenuButton)m_toolsMenuButtons[this._curSelection];
                        button.SetPushed(!button.IsPushed());
                    }
                    else
                    {
                        ControlMenuButton button = (ControlMenuButton)m_layersMenuButtons[this._curSelection - m_toolsMenuButtons.Count];
                        button.SetPushed(!button.IsPushed());
                    }

                    return true;
                }
            }

            // Pass message on to the "tools"
            foreach (ControlMenuButton button in m_toolsMenuButtons)
                if (button.IsPushed())
                    if (button.OnMouseUp(e))
                        return true;

            return false;
        }

        public bool OnMouseDown(MouseEventArgs e)
        {
            if (World.Settings.ShowToolBar)
            {
                if (this._curSelection != -1 && e.Button == MouseButtons.Left)
                {
                    if (this._curSelection < m_toolsMenuButtons.Count)
                    {
                        return true;
                    }
                }
            }
            //// Trigger "tool" update
            //foreach (ControlMenuButton button in m_toolsMenuButtons)
            //    if (button.IsPushed())
            //        if (button.OnMouseDown(e))
            //            return true;

            return false;
        }

        int _curSelection = -1;

        public bool OnMouseMove(MouseEventArgs e)
        {
            // Default to arrow cursor every time mouse moves
            //mouseCursor = CursorType.Arrow;

            // Trigger "tools" update
            //foreach (ControlMenuButton button in m_toolsMenuButtons)
            //    if (button.IsPushed())
            //        if (button.OnMouseMove(e))
            //            return true;

            if (!World.Settings.ShowToolBar)
                return false;

            if (this._visibleState == VisibleState.Visible)
            {
                float width, height;

                int buttonCount;

                int sel = -1;

                switch (m_anchor)
                {
                    case MenuAnchor.Top:
                        buttonCount = m_toolsMenuButtons.Count + m_layersMenuButtons.Count;
                        width = buttonCount * (_outerPadding + ControlMenuButton.NormalSize) + _outerPadding;
                        height = _outerPadding * 2 + ControlMenuButton.NormalSize;

                        if (e.Y >= y && e.Y <= y + height + 2 * _outerPadding)
                        {
                            sel = (int)((e.X - _outerPadding) / (ControlMenuButton.NormalSize + _outerPadding));
                            if (sel < buttonCount) { }
                            //mouseCursor = CursorType.Hand;
                            else
                                sel = -1;
                        }
                        _curSelection = sel;

                        break;

                    case MenuAnchor.Bottom:
                        buttonCount = m_toolsMenuButtons.Count + m_layersMenuButtons.Count;
                        width = buttonCount * (_outerPadding + ControlMenuButton.NormalSize) + _outerPadding;
                        height = _outerPadding * 2 + ControlMenuButton.NormalSize;

                        if (e.Y >= y && e.Y <= y + (height + 2 * _outerPadding))
                        {
                            sel = (int)((e.X - _outerPadding) / (ControlMenuButton.NormalSize + _outerPadding));
                            if (sel < buttonCount){}
                                //mouseCursor = CursorType.Hand;
                            else
                                sel = -1;
                        }
                        _curSelection = sel;

                        break;

                    case MenuAnchor.Right:
                        width = _outerPadding * 2 + ControlMenuButton.SelectedSize;
                        height = _outerPadding * 2 + (m_toolsMenuButtons.Count * m_layersMenuButtons.Count) * ControlMenuButton.SelectedSize;

                        if (e.X >= x + _outerPadding && e.X <= x + width + _outerPadding &&
                            e.Y >= y + _outerPadding && e.Y <= y + height + _outerPadding)
                        {
                            int dx = (int)(e.Y - (y + _outerPadding));
                            _curSelection = (int)(dx / ControlMenuButton.SelectedSize);
                        }
                        else
                        {
                            _curSelection = -1;
                        }
                        break;
                }
            }
            if (World.Settings.ShowToolBar)
            {
                if (this._curSelection != -1 && e.Button == MouseButtons.Left)
                {
                    if (this._curSelection < m_toolsMenuButtons.Count)
                    {
                        return true;
                    }
                }
            }
           
            return false;
        }

        public bool OnMouseWheel(MouseEventArgs e)
        {
            // Trigger "tool" update
            foreach (ControlMenuButton button in m_toolsMenuButtons)
                if (button.IsPushed())
                    if (button.OnMouseWheel(e))
                        return true;

            return false;
        }

        public void Render(DrawArgs drawArgs)
        {
            if (m_sprite == null)
                m_sprite = new Sprite(drawArgs.device);

            //if (mouseCursor != CursorType.Arrow)
            //    DrawArgs.MouseCursor = mouseCursor;


            foreach (ControlMenuButton button in m_toolsMenuButtons)
                if (button.IsPushed())
                    // Does not render the button, but the functionality behind the button
                    button.Render(drawArgs);

            foreach (ControlMenuButton button in m_toolsMenuButtons)
                button.Update(drawArgs);

            foreach (ControlMenuButton button in m_layersMenuButtons)
                button.Update(drawArgs);

            if (!World.Settings.ShowToolBar)
                return;

            if (this._isHideable)
            {
                if (this._visibleState == VisibleState.NotVisible)
                {
                    if (                        (m_anchor == MenuAnchor.Top && DrawArgs.Instance.LastMousePosition.Y < ControlMenuButton.NormalSize) ||
                        (m_anchor == MenuAnchor.Bottom && DrawArgs.Instance.LastMousePosition.Y > drawArgs.ScreenHeight - ControlMenuButton.NormalSize) ||
                        (m_anchor == MenuAnchor.Right && DrawArgs.Instance.LastMousePosition.X > drawArgs.ScreenWidth - ControlMenuButton.NormalSize)                        )
                    {
                        this._visibleState = VisibleState.Ascending;
                        this._lastVisibleChange = System.DateTime.Now;
                    }
                }
                else if (                    (m_anchor == MenuAnchor.Top && DrawArgs.Instance.LastMousePosition.Y > 2 * this._outerPadding + ControlMenuButton.NormalSize) ||
                    (m_anchor == MenuAnchor.Bottom && DrawArgs.Instance.LastMousePosition.Y < drawArgs.ScreenHeight - 2 * this._outerPadding - ControlMenuButton.NormalSize) ||
                    (m_anchor == MenuAnchor.Right && DrawArgs.Instance.LastMousePosition.X < drawArgs.ScreenWidth - ControlMenuButton.NormalSize)
                    )
                {
                    if (this._visibleState == VisibleState.Visible)
                    {
                        this._visibleState = VisibleState.Descending;
                        this._lastVisibleChange = System.DateTime.Now;
                    }
                    else if (this._visibleState == VisibleState.Descending)
                    {
                        if (System.DateTime.Now.Subtract(this._lastVisibleChange) > System.TimeSpan.FromMilliseconds(hideTimeMilliseconds))
                        {
                            this._visibleState = VisibleState.NotVisible;
                            this._lastVisibleChange = System.DateTime.Now;
                        }
                    }
                }
                else if (this._visibleState == VisibleState.Ascending)
                {
                    if (System.DateTime.Now.Subtract(this._lastVisibleChange) > System.TimeSpan.FromMilliseconds(hideTimeMilliseconds))
                    {
                        this._visibleState = VisibleState.Visible;
                        this._lastVisibleChange = System.DateTime.Now;
                    }
                }
                else if (this._visibleState == VisibleState.Descending)
                {
                    if (System.DateTime.Now.Subtract(this._lastVisibleChange) > System.TimeSpan.FromMilliseconds(hideTimeMilliseconds))
                    {
                        this._visibleState = VisibleState.NotVisible;
                        this._lastVisibleChange = System.DateTime.Now;
                    }
                }
            }
            else
            {
                this._visibleState = VisibleState.Visible;
            }

            int totalNumberButtons = m_toolsMenuButtons.Count + m_layersMenuButtons.Count;
            ControlMenuButton.NormalSize = ControlMenuButton.SelectedSize / 2;
            _outerPadding = ControlMenuButton.NormalSize * padRatio;

            float menuWidth = (ControlMenuButton.NormalSize + _outerPadding) * totalNumberButtons + _outerPadding;
            if (menuWidth > drawArgs.ScreenWidth)
            {
                ControlMenuButton.NormalSize = (drawArgs.ScreenWidth) / ((padRatio + 1) * totalNumberButtons + padRatio);
                _outerPadding = ControlMenuButton.NormalSize * padRatio;

                // recalc menuWidth if we want to center the toolbar
                menuWidth = (ControlMenuButton.NormalSize + _outerPadding) * totalNumberButtons + _outerPadding;
            }

            if (m_anchor == MenuAnchor.Left)
            {
                x = 0;
                y = (int)ControlMenuButton.NormalSize;
            }
            else if (m_anchor == MenuAnchor.Right)
            {
                x = (int)(drawArgs.ScreenWidth - 2 * _outerPadding - ControlMenuButton.NormalSize);
                y = (int)ControlMenuButton.NormalSize;
            }
            else if (m_anchor == MenuAnchor.Top)
            {
                x = (int)(drawArgs.ScreenWidth / 2 - totalNumberButtons * ControlMenuButton.NormalSize / 2 - _outerPadding);
                y = 0;
            }
            else if (m_anchor == MenuAnchor.Bottom)
            {
                x = (int)(drawArgs.ScreenWidth / 2 - totalNumberButtons * ControlMenuButton.NormalSize / 2 - _outerPadding);
                y = (int)(drawArgs.ScreenHeight - 2 * _outerPadding - ControlMenuButton.NormalSize);
            }

            if (this._visibleState == VisibleState.Ascending)
            {
                TimeSpan t = System.DateTime.Now.Subtract(this._lastVisibleChange);
                if (t.Milliseconds < hideTimeMilliseconds)
                {
                    double percent = (double)t.Milliseconds / hideTimeMilliseconds;
                    int dx = (int)((ControlMenuButton.NormalSize + 5) - (percent * (ControlMenuButton.NormalSize + 5)));

                    if (m_anchor == MenuAnchor.Left)
                    {
                        x -= dx;
                    }
                    else if (m_anchor == MenuAnchor.Right)
                    {
                        x += dx;
                    }
                    else if (m_anchor == MenuAnchor.Top)
                    {
                        y -= dx;

                    }
                    else if (m_anchor == MenuAnchor.Bottom)
                    {
                        y += dx;
                    }
                }
            }
            else if (this._visibleState == VisibleState.Descending)
            {
                TimeSpan t = System.DateTime.Now.Subtract(this._lastVisibleChange);
                if (t.Milliseconds < hideTimeMilliseconds)
                {
                    double percent = (double)t.Milliseconds / hideTimeMilliseconds;
                    int dx = (int)((percent * (ControlMenuButton.NormalSize + 5)));

                    if (m_anchor == MenuAnchor.Left)
                    {
                        x -= dx;
                    }
                    else if (m_anchor == MenuAnchor.Right)
                    {
                        x += dx;
                    }
                    else if (m_anchor == MenuAnchor.Top)
                    {
                        y -= dx;
                    }
                    else if (m_anchor == MenuAnchor.Bottom)
                    {
                        y += dx;
                    }
                }
            }

            lock (m_toolsMenuButtons.SyncRoot)
            {
                ControlMenuButton selectedButton = null;
                if (_curSelection >= 0 & _curSelection < totalNumberButtons)
                {
                    if (_curSelection < m_toolsMenuButtons.Count)
                        selectedButton = (ControlMenuButton)m_toolsMenuButtons[_curSelection];
                    else
                        selectedButton = (ControlMenuButton)m_layersMenuButtons[_curSelection - m_toolsMenuButtons.Count];
                }

                //_outerPadding = MenuButton.NormalSize*padRatio;
                //float menuWidth = (MenuButton.NormalSize+_outerPadding)*totalNumberButtons+_outerPadding;
                //if(menuWidth>drawArgs.screenWidth)
                //{
                //    //MessageBox.Show(drawArgs.screenWidth.ToString());
                //    MenuButton.NormalSize = (drawArgs.screenWidth)/((padRatio+1)*totalNumberButtons+padRatio);
                //    //MessageBox.Show(MenuButton.NormalSize.ToString());
                //    _outerPadding = MenuButton.NormalSize*padRatio;
                //}

                if (this._visibleState != VisibleState.NotVisible)
                {
                    if (m_anchor == MenuAnchor.Top)
                    {
                        WidgetUtilities.DrawBox(0, 0, drawArgs.ScreenWidth, (int)(ControlMenuButton.NormalSize + 2 * _outerPadding), 0.0f,
                            World.Settings.ToolBarBackColor.ToArgb(), drawArgs.device);
                    }
                    else if (m_anchor == MenuAnchor.Bottom)
                    {
                        WidgetUtilities.DrawBox(0, (int)(y - _outerPadding), drawArgs.ScreenWidth, (int)(ControlMenuButton.NormalSize + 4 * _outerPadding), 0.0f,
                            World.Settings.ToolBarBackColor.ToArgb(), drawArgs.device);
                    }
                }

                float total = 0;
                float extra = 0;
                for (int i = 0; i < totalNumberButtons; i++)
                {
                    ControlMenuButton button;
                    if (i < m_toolsMenuButtons.Count)
                        button = (ControlMenuButton)m_toolsMenuButtons[i];
                    else
                        button = (ControlMenuButton)m_layersMenuButtons[i - m_toolsMenuButtons.Count];
                    total += button.CurrentSize;
                    extra += button.CurrentSize - ControlMenuButton.NormalSize;
                }

                float pad = ((float)_outerPadding * (totalNumberButtons + 1) - extra) / (totalNumberButtons + 1);
                float buttonX = pad;

                // TODO - to center the menubar set the buttonX to center-half toolbar width
                // float buttonX = (drawArgs.screenWidth - menuWidth) / 2; 

                m_sprite.Begin(SpriteFlags.AlphaBlend);
                for (int i = 0; i < totalNumberButtons; i++)
                {
                    ControlMenuButton button;
                    if (i < m_toolsMenuButtons.Count)
                        button = (ControlMenuButton)m_toolsMenuButtons[i];
                    else
                        button = (ControlMenuButton)m_layersMenuButtons[i - m_toolsMenuButtons.Count];

                    if (button.IconTexture == null)
                        button.InitializeTexture(drawArgs.device);

                    if (this._visibleState != VisibleState.NotVisible)
                    {
                        int centerX = (int)(buttonX + button.CurrentSize * 0.5f);
                        buttonX += button.CurrentSize + pad;
                        float buttonTopY = y + _outerPadding;

                        if (m_anchor == MenuAnchor.Bottom)
                            buttonTopY = (int)(drawArgs.ScreenHeight - _outerPadding - button.CurrentSize);

                        if (button.IsPushed())
                        {
                            // Draw the chevron
                            float chevronSize = button.CurrentSize * padRatio;

                            enabledChevron[0].Color = chevronColor;
                            enabledChevron[1].Color = chevronColor;
                            enabledChevron[2].Color = chevronColor;

                            if (m_anchor == MenuAnchor.Bottom)
                            {
                                enabledChevron[2].X = centerX - chevronSize;
                                enabledChevron[2].Y = y - 2;
                                enabledChevron[2].Z = 0.0f;

                                enabledChevron[0].X = centerX;
                                enabledChevron[0].Y = y - 2 + chevronSize;
                                enabledChevron[0].Z = 0.0f;

                                enabledChevron[1].X = centerX + chevronSize;
                                enabledChevron[1].Y = y - 2;
                                enabledChevron[1].Z = 0.0f;
                            }
                            else
                            {
                                enabledChevron[2].X = centerX - chevronSize;
                                enabledChevron[2].Y = y + 2;
                                enabledChevron[2].Z = 0.0f;

                                enabledChevron[0].X = centerX;
                                enabledChevron[0].Y = y + 2 + chevronSize;
                                enabledChevron[0].Z = 0.0f;

                                enabledChevron[1].X = centerX + chevronSize;
                                enabledChevron[1].Y = y + 2;
                                enabledChevron[1].Z = 0.0f;
                            }

                            drawArgs.device.VertexFormat = CustomVertex.TransformedColored.Format;
                            drawArgs.device.TextureState[0].ColorOperation = TextureOperation.Disable;
                            drawArgs.device.DrawUserPrimitives(PrimitiveType.TriangleList, 1, enabledChevron);
                            drawArgs.device.TextureState[0].ColorOperation = TextureOperation.SelectArg1;
                        }

                        button.RenderEnabledIcon(
                            m_sprite,
                            drawArgs,
                            centerX,
                            buttonTopY,
                            i == this._curSelection,
                            m_anchor);
                    }
                }
                m_sprite.End();

            }
        }

        public void Dispose()
        {
            foreach (ControlMenuButton button in m_toolsMenuButtons)
                button.Dispose();

            if (m_sprite != null)
            {
                m_sprite.Dispose();
                m_sprite = null;
            }
        }

        #endregion

        protected enum VisibleState
        {
            NotVisible,
            Descending,
            Ascending,
            Visible
        }
    }
}
