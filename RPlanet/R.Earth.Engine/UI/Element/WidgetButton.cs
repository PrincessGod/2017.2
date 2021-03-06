using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using R.Earth.OverLayer.Interface;
using R.Earth.OverLayer.Enums;

namespace R.Earth.OverLayer.Element
{

    /// <summary>
    /// ButtonWidget - This class implements a basic button.  Left
    /// and/or right) click actions must be assigned for the button to 
    /// do anything.  Set the ImageName property for the button to have
    /// a texture.
    /// 
    /// TODO Dispose needs to decrement the Texture reference count.
    /// </summary>
    public class WidgetButton : IWidget, IInteractive
    {
        #region IWidget support variables

        /// <summary>
        /// Name property value
        /// </summary>
        protected string m_name = "";

        /// <summary>
        /// Location property value
        /// </summary>
        protected System.Drawing.Point m_location = new System.Drawing.Point(0, 0);

        /// <summary>
        /// WidgetSize property value
        /// </summary>
        protected System.Drawing.Size m_size = new System.Drawing.Size(0, 0);

        /// <summary>
        /// Visible property value
        /// </summary>
        protected bool m_visible = true;

        /// <summary>
        /// Enabled property value
        /// </summary>
        protected bool m_enabled = true;

        /// <summary>
        /// CountHeight property value
        /// </summary>
        protected bool m_countHeight = false;

        /// <summary>
        /// CountWidth property value
        /// </summary>
        protected bool m_countWidth = true;

        /// <summary>
        /// Parent widget property value
        /// </summary>
        protected IWidget m_parentWidget = null;

        /// <summary>
        /// Tag property
        /// </summary>
        protected object m_tag = null;

        /// <summary>
        /// Flag indicating if initialization is required
        /// </summary>
        protected bool m_isInitialized = false;

        #endregion

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

        /// <summary>
        /// Sprite used to draw texture and text
        /// </summary>
        protected Sprite m_sprite;

        /// <summary>
        /// The texture for this widget
        /// </summary>
        protected IconTexture m_iconTexture;

        /// <summary>
        /// Name of texture image
        /// </summary>
        protected string m_imageName = "button_in.png";

        /// <summary>
        /// Normal color (not highlighted/mouseover etc.)
        /// </summary>
        protected static int m_normalColor = System.Drawing.Color.FromArgb(
            192,
            255,
            255,
            255).ToArgb();

        /// <summary>
        /// Background color
        /// </summary>
        protected System.Drawing.Color m_BackgroundColor = System.Drawing.Color.FromArgb(
            170,
            40,
            40,
            40);
        //		96,
        //		32,
        //		32,
        //		32);

        /// <summary>
        /// Foreground color
        /// </summary>
        protected System.Drawing.Color m_ForeColor = System.Drawing.Color.White;

        /// <summary>
        ///  Icon X scaling computed by dividing icon width by texture width
        /// </summary>
        protected float XScale;

        /// <summary>
        ///  Icon Y scaling computed by dividing icon height by texture height 
        /// </summary>
        protected float YScale;

        /// <summary>
        /// Default constructor
        /// </summary>
        public WidgetButton()
        {
            m_location.X = 90;
            m_location.Y = 100;
            m_size.Height = 32;
            m_size.Width = 32;
            m_isInitialized = false;
        }

        #region Properties

        /// <summary>
        /// Filename of button graphic
        /// </summary>
        public string ImageName
        {
            get { return m_imageName; }
            set
            {
                m_imageName = value;
                m_isInitialized = false;
            }
        }

        /// <summary>
        /// Foreground color
        /// </summary>
        public System.Drawing.Color ForeColor
        {
            get { return m_ForeColor; }
            set { m_ForeColor = value; }
        }

        #endregion

        #region IWidget Members

        #region Properties

        /// <summary>
        /// Name of this widget
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }


        /// <summary>
        /// Location of this widget relative to the client area of the parent
        /// </summary>
        public System.Drawing.Point Location
        {
            get { return m_location; }
            set { m_location = value; }
        }


        /// <summary>
        /// Where this widget is on the window
        /// </summary>
        public System.Drawing.Point AbsoluteLocation
        {
            get
            {
                if (m_parentWidget != null)
                {
                    return new System.Drawing.Point(
                        m_location.X + m_parentWidget.ClientLocation.X,
                        m_location.Y + m_parentWidget.ClientLocation.Y);
                }
                else
                {
                    return m_location;
                }
            }
        }


        /// <summary>
        /// The top left corner of this widget's client area
        /// </summary>
        public System.Drawing.Point ClientLocation
        {
            get { return this.AbsoluteLocation; }
        }


        /// <summary>
        /// Size of widget in pixels
        /// </summary>
        public System.Drawing.Size WidgetSize
        {
            get { return m_size; }
            set { m_size = value; }
        }


        /// <summary>
        /// Size of the client area in pixels - Same as widget size.
        /// </summary>
        public System.Drawing.Size ClientSize
        {
            get { return m_size; }
            set { m_size = value; }
        }


        /// <summary>
        /// Whether this widget is enabled
        /// </summary>
        public bool Enabled
        {
            get { return m_enabled; }
            set { m_enabled = value; }
        }


        /// <summary>
        /// Whether this widget is visible
        /// </summary>
        public bool Visible
        {
            get { return m_visible; }
            set { m_visible = value; }
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


        /// <summary>
        /// The parent widget of this widget.
        /// </summary>
        public IWidget ParentWidget
        {
            get { return m_parentWidget; }
            set { m_parentWidget = value; }
        }


        /// <summary>
        /// List of children widgets - None in the case of button widgets.
        /// </summary>
        public IWidgetCollection ChildWidgets
        {
            get { return null; }
            set { }
        }


        /// <summary>
        /// A link to an object.
        /// </summary>
        public object Tag
        {
            get { return m_tag; }
            set { m_tag = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the button by loading the texture, creating the sprite and figure out the scaling.
        /// 
        /// Called on the GUI thread.
        /// </summary>
        /// <param name="drawArgs">The drawing arguments passed from the WW GUI thread.</param>
        public void Initialize(DrawArgs drawArgs)
        {
            object key = null;

            // TODO - Make this work the same way as Icons including fetch from URL

            // Icon image from file
         
            if (m_iconTexture == null)
            {
                key = m_imageName;
                m_iconTexture = new IconTexture(drawArgs.device, m_imageName);
            }

            if (m_iconTexture != null)
            {
                m_iconTexture.ReferenceCount++;


                if (m_size.Width == 0)
                    m_size.Width = m_iconTexture.Width;

                if (m_size.Height == 0)
                    m_size.Height = m_iconTexture.Height;

                this.XScale = (float)m_size.Width / m_iconTexture.Width;
                this.YScale = (float)m_size.Height / m_iconTexture.Height;
            }

            if (m_sprite == null)
                m_sprite = new Sprite(drawArgs.device);

            m_isInitialized = true;
        }


        /// <summary>
        /// The render method to draw this widget on the screen.
        /// 
        /// Called on the GUI thread.
        /// </summary>
        /// <param name="drawArgs">The drawing arguments passed from the WW GUI thread.</param>
        public void Render(DrawArgs drawArgs)
        {

            if (!this.m_isInitialized)
                this.Initialize(drawArgs);

            if ((m_visible) && (m_enabled))
            {
                m_sprite.Begin(SpriteFlags.AlphaBlend);
                m_sprite.Transform = Matrix.Scaling(this.XScale, this.YScale, 0);
                m_sprite.Transform *= Matrix.Translation(AbsoluteLocation.X + m_size.Width / 2, AbsoluteLocation.Y + m_size.Height / 2, 0);
                m_sprite.Draw(m_iconTexture.Texture,
                    new Vector3(m_iconTexture.Width >> 1, m_iconTexture.Height >> 1, 0),
                    Vector3.Empty,
                    m_normalColor);
                m_sprite.Transform = Matrix.Identity;
                m_sprite.End();
            }

        }


        #endregion

        #endregion

        #region IInteractive Members

        #region Properties

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

        #region Methods

        /// <summary>
        /// Mouse down event handler.
        /// </summary>
        /// <param name="e">Event args</param>
        /// <returns>If this widget handled this event</returns>
        public bool OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            if ((m_visible) && (m_enabled))
            {
                // if we're in the client area and lmb
                if (e.X >= this.AbsoluteLocation.X &&
                    e.X <= this.AbsoluteLocation.X + m_size.Width &&
                    e.Y >= this.AbsoluteLocation.Y &&
                    e.Y <= this.AbsoluteLocation.Y + m_size.Height)
                {
                    // Handle left button clicks
                    if (e.Button == System.Windows.Forms.MouseButtons.Left &&
                        m_leftClickAction != null)
                    {
                        m_leftClickAction(e);
                        return true;
                    }

                    // Handle right button clicks
                    if (e.Button == System.Windows.Forms.MouseButtons.Right &&
                        m_rightClickAction != null)
                    {
                        m_rightClickAction(e);
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Mouse up event handler.
        /// </summary>
        /// <param name="e">Event args</param>
        /// <returns>If this widget handled this event</returns>
        public bool OnMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            return false;
        }


        /// <summary>
        /// Mouse move event handler.
        /// </summary>
        /// <param name="e">Event args</param>
        /// <returns>If this widget handled this event</returns>
        public bool OnMouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            return false;
        }


        /// <summary>
        /// Mouse wheel event handler.
        /// </summary>
        /// <param name="e">Event args</param>
        /// <returns>If this widget handled this event</returns>
        public bool OnMouseWheel(System.Windows.Forms.MouseEventArgs e)
        {
            return false;
        }


        /// <summary>
        /// Mouse entered this widget event handler.
        /// </summary>
        /// <param name="e">Event args</param>
        /// <returns>If this widget handled this event</returns>
        public bool OnMouseEnter(EventArgs e)
        {
            return false;
        }


        /// <summary>
        /// Mouse left this widget event handler.
        /// </summary>
        /// <param name="e">Event args</param>
        /// <returns>If this widget handled this event</returns>
        public bool OnMouseLeave(EventArgs e)
        {
            return false;
        }


        /// <summary>
        /// Key down event handler.
        /// </summary>
        /// <param name="e">Event args</param>
        /// <returns>If this widget handled this event</returns>
        public bool OnKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            return false;
        }


        /// <summary>
        /// Key up event handler.
        /// </summary>
        /// <param name="e">Event args</param>
        /// <returns>If this widget handled this event</returns>
        public bool OnKeyUp(System.Windows.Forms.KeyEventArgs e)
        {
            return false;
        }

        /// <summary>
        /// Key press event handler.
        /// This widget does nothing with key presses.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
            return false;
        }

        #endregion

        #endregion

    }

    /// <summary>
    /// 包含地标纹理图片的类
    /// Contains one texture for our icon texture cache
    /// </summary>
    public class IconTexture : IDisposable
    {
        /// <summary>
        /// 图标纹理
        /// </summary>
        public Texture Texture;
        /// <summary>
        /// 图标的宽度
        /// </summary>
        public int Width;
        /// <summary>
        /// 图标的高度
        /// </summary>
        public int Height;
        //----		
        /// <summary>
        /// 相关的个数
        ///  yu06.12.23
        /// </summary>
        public int ReferenceCount;
        //---
        /// <summary>
        /// 通过指定硬盘中纹理图片的路径构造一个新的 IconTexture 实例对象
        /// Construct an icon texture from a texture file on disk.
        /// </summary>
        public IconTexture(Device device, string textureFileName)
        {
            Texture = ImageHelper.LoadIconTexture(textureFileName);

            UpdateDimensions();
        }

        /// <summary>
        /// 从位图中构造一个新的 IconTexture 实例对象
        /// Construct an icon texture from a bitmap.
        /// </summary>
        public IconTexture(Device device, Bitmap image)
        {
            Texture = new Texture(device, image, Usage.None, Pool.Managed);

            UpdateDimensions();
        }

        /// <summary>
        /// 根据纹理的大小重新设置图标的宽高
        /// Retrieve width/height from the texture
        /// </summary>
        private void UpdateDimensions()
        {
            // Read texture level 0 size
            using (Surface s = Texture.GetSurfaceLevel(0))
            {
                SurfaceDescription desc = s.Description;
                Width = desc.Width;
                Height = desc.Height;
            }
        }
        #region IDisposable Members

        /// <summary>
        /// 释放系统托管资源
        /// </summary>
        public void Dispose()
        {
            if (Texture != null)
            {
                Texture.Dispose();
                Texture = null;
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
