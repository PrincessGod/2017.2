using System;
using System.Collections;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using R.Earth.Web;

namespace R.Earth
{
    public class DrawArgs : IDisposable
    {
        #region IDisposable ≥…‘±

        public void Dispose()
        {
           
        }

        #endregion

        private static DrawArgs instance;

        public static DrawArgs Instance       {            get { return instance; }        }

        private CameraBase m_worldCamera;
        private Device m_device;
        private Font m_DefaultFont;

        public System.Windows.Forms.Control ParentControl { get { return this.m_OwnerControl; } }
        public CameraBase WorldCamera { get { return this.m_worldCamera; } set { this.m_worldCamera = value; } }
        public Device device { get { return this.m_device; }  }    
        public Font DefauleFont { get { return this.m_DefaultFont; } }        
      
        public int ScreenWidth;
        public int ScreenHeight;
        public System.Drawing.Point LastMousePosition;

        private bool isPainting;
        private System.Windows.Forms.Control m_OwnerControl;

        public int NumberTilesDrawn = 0;

        public bool Repaint = true;


        public static DownloadQueue DownloadQueue = new DownloadQueue();


        private  Hashtable m_textureTable = new Hashtable();

        public  Hashtable TextureTable { get { return m_textureTable; } }

        private Sprite m_sprite;













        public DrawArgs(Device device, System.Windows.Forms.Control parent)
        {
            this.m_device = device;
            this.m_OwnerControl = parent;
            this.m_DefaultFont = new Font(device, this.m_OwnerControl.Font);
            this.m_sprite = new Sprite(device);
            instance = this;
        }

              

        internal void Present()
        {
            this.m_device.Present();
        }

        internal void BeginRender()
        {
            this.isPainting = true;
        }

        internal void EndRender()
        {
            Debug.Assert(isPainting);
            this.isPainting = false;
        }

        public void DrawText(Sprite sprite, string captionText, System.Drawing.Rectangle textRect, DrawTextFormat dtf, int p_5)
        {
            this.m_DefaultFont.DrawText(sprite, captionText, textRect, dtf, p_5);
        }
        
        public void SpritePreRender()
        {
            this.m_sprite.Begin(SpriteFlags.AlphaBlend);
        }
        public void SpriteEndRender()
        {
            this.m_sprite.End();
        }

        public void SpriteRender(Texture p, Matrix transformmatrix, Vector3 center, int color)
        {
            this.m_sprite.Transform = transformmatrix;
            this.m_sprite.Draw(p, center, Vector3.Empty, color);
            this.m_sprite.Transform = Matrix.Identity;
        }

        public System.Drawing.Rectangle MeasureString(string p,DrawTextFormat fat ,System.Drawing.Color color)
        {
            return this.DefauleFont.MeasureString(this.m_sprite, p, fat, color.ToArgb());
        }
        public  virtual void DrawText(string captionText, System.Drawing.Rectangle textRect, DrawTextFormat dtf, int p_5)
        {
            this.m_DefaultFont.DrawText(this.m_sprite, captionText, textRect, dtf, p_5);
        }

    }
}
