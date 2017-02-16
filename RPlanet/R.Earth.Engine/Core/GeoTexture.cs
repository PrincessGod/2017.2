using System;
using System.Drawing;

using Microsoft.DirectX.Direct3D;

namespace R.Earth.GeoEntity
{
    public class GeoTexture : IDisposable
    {
        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        public Texture Texture;
        public int Width;
        public int Height;
        public int ReferenceCount = 0;

        public GeoTexture(Device device, string filename)
        {
            this.UpdateTexture(device, filename);
        }

        private void UpdateTexture(Device device, string textureFileName)
        {
            if (ImageHelper.IsGdiSupportedImageFormat(textureFileName))
            {
                // Load without rescaling source bitmap
                using (Image image = ImageHelper.LoadImage(textureFileName))
                    LoadImage(device, image);
            }
            else
            {
                // Only DirectX can read this file, might get upscaled depending on input dimensions.
                Texture = ImageHelper.LoadIconTexture(textureFileName);
                // Read texture level 0 size
                using (Surface s = Texture.GetSurfaceLevel(0))
                {
                    SurfaceDescription desc = s.Description;
                    Width = desc.Width;
                    Height = desc.Height;
                }
            }
        }

        private void LoadImage(Device device, Image image)
        {
            Width = (int)Math.Round(Math.Pow(2, (int)(Math.Ceiling(Math.Log(image.Width) / Math.Log(2)))));
            if (Width > device.DeviceCaps.MaxTextureWidth)
                Width = device.DeviceCaps.MaxTextureWidth;

            Height = (int)Math.Round(Math.Pow(2, (int)(Math.Ceiling(Math.Log(image.Height) / Math.Log(2)))));
            if (Height > device.DeviceCaps.MaxTextureHeight)
                Height = device.DeviceCaps.MaxTextureHeight;

            using (Bitmap textureSource = new Bitmap(Width, Height))
            using (Graphics g = Graphics.FromImage(textureSource))
            {
                g.DrawImage(image, 0, 0, Width, Height);
                if (Texture != null)
                    Texture.Dispose();
                Texture = new Texture(device, textureSource, Usage.None, Pool.Managed);
            }
        }
    }
}
