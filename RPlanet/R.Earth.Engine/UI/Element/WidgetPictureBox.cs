using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using R.Earth.OverLayer.Interface;
using R.Earth.OverLayer.Enums;

namespace R.Earth.OverLayer.Element
{
    /// <summary>
    /// Summary description for PictureBox.
    /// </summary>
    public class PictureBox : IWidget, IInteractive
    {
        string m_Text = "";
        byte m_Opacity = 255;
        System.Drawing.Point m_Location = new System.Drawing.Point(0, 0);
        System.Drawing.Size m_Size = new System.Drawing.Size(0, 0);
        bool m_Visible = true;
        bool m_Enabled = true;
        IWidget m_ParentWidget = null;
        object m_Tag = null;
        string m_Name = "";
        string m_SaveFilePath = null;
        System.Drawing.Color m_ForeColor = System.Drawing.Color.White;

        double m_RefreshTime = 0;
        System.Timers.Timer m_RefreshTimer = new System.Timers.Timer(100000);

        string m_ImageUri = null;
        string clickableUrl = null;

        /// <summary>
        /// CountHeight property value
        /// </summary>
        protected bool m_countHeight = true;

        /// <summary>
        /// CountWidth property value
        /// </summary>
        protected bool m_countWidth = true;

        #region IInteractive support variables

        /// <summary>
        /// LeftClickAction value - holds method to call on left mouse click
        /// </summary>
        protected MouseClickAction m_leftClickAction = null;

        /// <summary>
        /// RightClickAction value - holds method to call on right mouse click
        /// </summary>
        protected MouseClickAction m_rightClickAction = null;

        #endregion

        public string ClickableUrl
        {
            get { return clickableUrl; }
            set { clickableUrl = value; }
        }

        public double RefreshTime
        {
            get
            {
                return m_RefreshTime;
            }
            set
            {
                m_RefreshTime = value;
                if (m_RefreshTime > 0)
                    m_RefreshTimer.Interval = value;
            }
        }
        public byte Opacity
        {
            get
            {
                return m_Opacity;
            }
            set
            {
                m_Opacity = value;
            }
        }

        public PictureBox()
        {

        }

        #region Properties

        public string SaveFilePath
        {
            get
            {
                return m_SaveFilePath;
            }
            set
            {
                m_SaveFilePath = value;
            }
        }

        public string ImageUri
        {
            get
            {
                return m_ImageUri;
            }
            set
            {
                m_ImageUri = value;
            }
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }
        public System.Drawing.Color ForeColor
        {
            get
            {
                return m_ForeColor;
            }
            set
            {
                m_ForeColor = value;
            }
        }
        public string Text
        {
            get
            {
                return m_Text;
            }
            set
            {
                m_Text = value;
            }
        }
        #endregion

        #region IWidget Members

        public IWidget ParentWidget
        {
            get
            {
                return m_ParentWidget;
            }
            set
            {
                m_ParentWidget = value;
            }
        }

        public bool Visible
        {
            get
            {
                return m_Visible;
            }
            set
            {
                m_Visible = value;
            }
        }

        public object Tag
        {
            get
            {
                return m_Tag;
            }
            set
            {
                m_Tag = value;
            }
        }

        public IWidgetCollection ChildWidgets
        {
            get
            {
                // TODO:  Add TextLabel.ChildWidgets getter implementation
                return null;
            }
            set
            {
                // TODO:  Add TextLabel.ChildWidgets setter implementation
            }
        }

        public System.Drawing.Size ClientSize
        {
            get
            {
                return m_Size;
            }
            set
            {
                m_Size = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
            }
        }

        public System.Drawing.Point ClientLocation
        {
            get
            {
                return m_Location;
            }
            set
            {
                m_Location = value;
            }
        }


        public System.Drawing.Point AbsoluteLocation
        {
            get
            {
                if (m_ParentWidget != null)
                {
                    return new System.Drawing.Point(
                        m_Location.X + m_ParentWidget.ClientLocation.X,
                        m_Location.Y + m_ParentWidget.ClientLocation.Y);

                }
                else
                {
                    return m_Location;
                }
            }
        }


        /// New IWidget properties

        /// <summary>
        /// Location of this widget relative to the client area of the parent
        /// </summary>
        public System.Drawing.Point Location
        {
            get { return m_Location; }
            set { m_Location = value; }
        }

        /// <summary>
        /// Size of widget in pixels
        /// </summary>
        public System.Drawing.Size WidgetSize
        {
            get { return m_Size; }
            set { m_Size = value; }
        }


        /// <summary>
        /// Whether this widget should count for height calculations - HACK until we do real layout
        /// </summary>
        public bool CountHeight
        {
            get { return m_countHeight; }
            set { m_countHeight = value; }
        }


        /// <summary>
        /// Whether this widget should count for width calculations - HACK until we do real layout
        /// </summary>
        public bool CountWidth
        {
            get { return m_countWidth; }
            set { m_countWidth = value; }
        }


        Texture m_ImageTexture = null;
        string displayText = null;

        bool isLoading = false;
        Sprite m_sprite = null;
        SurfaceDescription m_surfaceDescription;
        public bool IsLoaded = false;
        string m_currentImageUri = null;
        bool m_isMouseInside = false;

        public event System.EventHandler OnMouseEnterEvent;
        public event System.EventHandler OnMouseLeaveEvent;
        public event System.Windows.Forms.MouseEventHandler OnMouseUpEvent;
        public event System.Windows.Forms.MouseEventHandler OnMouseDownEvent;

        public void Initialize(DrawArgs drawArgs)
        {
        }

        public void Render(DrawArgs drawArgs)
        {
            if (m_Visible)
            {
                if (m_ImageTexture == null)
                {
                    if (!m_RefreshTimer.Enabled)
                    {
                        displayText = "Loading Image...";
                        if (m_RefreshTime > 0)
                        {
                            m_RefreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(m_RefreshTimer_Elapsed);
                            m_RefreshTimer.Start();
                        }
                        else
                        {
                            m_RefreshTimer_Elapsed(null, null);
                        }
                    }
                }

                if (DrawArgs.Instance.LastMousePosition.X > AbsoluteLocation.X + clickBuffer &&
                    DrawArgs.Instance.LastMousePosition.X < AbsoluteLocation.X + ClientSize.Width - clickBuffer &&
                        DrawArgs.Instance.LastMousePosition.Y > AbsoluteLocation.Y + clickBuffer &&
                        DrawArgs.Instance.LastMousePosition.Y < AbsoluteLocation.Y + ClientSize.Height - clickBuffer)
                {
                    if (!m_isMouseInside)
                    {
                        m_isMouseInside = true;
                        if (OnMouseEnterEvent != null)
                        {
                            OnMouseEnterEvent(this, null);
                        }
                    }
                }
                else
                {
                    if (m_isMouseInside)
                    {
                        m_isMouseInside = false;
                        if (OnMouseLeaveEvent != null)
                        {
                            OnMouseLeaveEvent(this, null);
                        }
                    }
                }

                if (m_ImageTexture != null && m_currentImageUri != m_ImageUri)
                {
                    m_RefreshTimer_Elapsed(null, null);
                }

                if (displayText != null)
                {
                    drawArgs.DrawText(
                        null,
                        displayText,
                        new System.Drawing.Rectangle(AbsoluteLocation.X, AbsoluteLocation.Y, m_Size.Width, m_Size.Height),
                        DrawTextFormat.None,
                        m_ForeColor.ToArgb());
                }

                if (m_ImageTexture != null && !isLoading)
                {
                    drawArgs.device.SetTexture(0, m_ImageTexture);

                    drawArgs.device.RenderState.ZBufferEnable = false;

                    System.Drawing.Point ul = new System.Drawing.Point(AbsoluteLocation.X, AbsoluteLocation.Y);
                    System.Drawing.Point ur = new System.Drawing.Point(AbsoluteLocation.X + m_Size.Width, AbsoluteLocation.Y);
                    System.Drawing.Point ll = new System.Drawing.Point(AbsoluteLocation.X, AbsoluteLocation.Y + m_Size.Height);
                    System.Drawing.Point lr = new System.Drawing.Point(AbsoluteLocation.X + m_Size.Width, AbsoluteLocation.Y + m_Size.Height);

                    if (m_sprite == null)
                        m_sprite = new Sprite(drawArgs.device);

                    m_sprite.Begin(SpriteFlags.AlphaBlend);

                    float xscale = (float)(ur.X - ul.X) / (float)m_surfaceDescription.Width;
                    float yscale = (float)(lr.Y - ur.Y) / (float)m_surfaceDescription.Height;
                    m_sprite.Transform = Matrix.Scaling(xscale, yscale, 0);
                    m_sprite.Transform *= Matrix.Translation(0.5f * (ul.X + ur.X), 0.5f * (ur.Y + lr.Y), 0);
                    m_sprite.Draw(m_ImageTexture,
                        new Vector3(m_surfaceDescription.Width / 2, m_surfaceDescription.Height / 2, 0),
                        Vector3.Empty,
                        System.Drawing.Color.FromArgb(m_Opacity, 255, 255, 255).ToArgb()
                        );

                    // Reset transform to prepare for text rendering later
                    m_sprite.Transform = Matrix.Identity;
                    m_sprite.End();
                }
            }
        }
        #endregion


        bool isUpdating = false;
        private void m_RefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
        }

        public bool SizeParentToImage = false;

        #region IInteractive Members

        public bool OnKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            // TODO:  Add PictureBox.OnKeyDown implementation
            return false;
        }

        public bool OnKeyUp(System.Windows.Forms.KeyEventArgs e)
        {
            // TODO:  Add PictureBox.OnKeyUp implementation
            return false;
        }

        public bool OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
            // TODO:  Add PictureBox.OnKeyPress implementation
            return false;
        }

        public bool OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            if (!Visible)
                return false;

            bool handled = false;

            if (e.X > AbsoluteLocation.X + clickBuffer && e.X < AbsoluteLocation.X + ClientSize.Width - clickBuffer &&
                        e.Y > AbsoluteLocation.Y + clickBuffer && e.Y < AbsoluteLocation.Y + ClientSize.Height - clickBuffer)
            {
                if (OnMouseDownEvent != null)
                {
                    OnMouseDownEvent(this, e);
                    handled = true;
                }
            }

            // TODO:  Add the rest of PictureBox.OnMouseDown implementation
            return handled;
        }

        public bool OnMouseEnter(EventArgs e)
        {
            // TODO:  Add PictureBox.OnMouseEnter implementation
            return false;
        }

        public bool OnMouseLeave(EventArgs e)
        {
            // TODO:  Add PictureBox.OnMouseLeave implementation
            return false;
        }

        public bool OnMouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            // TODO:  Add PictureBox.OnMouseMove implementation
            return false;
        }

        private int clickBuffer = 5;

        public bool OnMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            if (!Visible)
                return false;

            bool handled = false;

            if (e.X > AbsoluteLocation.X + clickBuffer && e.X < AbsoluteLocation.X + ClientSize.Width - clickBuffer &&
                        e.Y > AbsoluteLocation.Y + clickBuffer && e.Y < AbsoluteLocation.Y + ClientSize.Height - clickBuffer)
            {
                if (OnMouseUpEvent != null)
                {
                    OnMouseUpEvent(this, e);
                }

                handled = true;
            }

            
            return handled;
        }

        public bool OnMouseWheel(System.Windows.Forms.MouseEventArgs e)
        {
            // TODO:  Add PictureBox.OnMouseWheel implementation
            return false;
        }


        /// TODO: Refactor so that URL click is done on LeftClickAction
        /// and OnMouseUp (or Down) the relevant Left and Right click actions
        /// are called.

        /// <summary>
        /// Action to perform when the left mouse button is clicked
        /// </summary>
        public MouseClickAction LeftClickAction
        {
            get { return m_leftClickAction; }
            set { m_leftClickAction = value; }
        }


        /// <summary>
        /// Action to perform when the right mouse button is clicked
        /// </summary>
        public MouseClickAction RightClickAction
        {
            get { return m_rightClickAction; }
            set { m_rightClickAction = value; }
        }

        #endregion
    }
}
