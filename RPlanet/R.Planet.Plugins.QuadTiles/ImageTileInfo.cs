using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace R.Earth.QuadTile
{
    public class ImageTileInfo
    {
        #region Private Members

        string m_imagePath;
        string m_uri;

        #endregion

        #region Properties

        /// <summary>
        /// Local full path to the image file (cache or data)
        /// </summary>
        public string ImagePath
        {
            get
            {
                return m_imagePath;
            }
            set
            {
                m_imagePath = value;
            }
        }

        /// <summary>
        /// Uri for downloading image from network
        /// </summary>
        public string Uri
        {
            get
            {
                return m_uri;
            }
        }

        #endregion

        public ImageTileInfo(string imagePath)
        {
            m_imagePath = imagePath;
        }

        public ImageTileInfo(string imagePath, string uri)
        {
            m_imagePath = imagePath;
            m_uri = uri;
        }

        /// <summary>
        /// Check if this image tile is available locally.
        /// </summary>
        public bool Exists()
        {
            if (File.Exists(m_imagePath))
                return true;
            return false;
        }
    }
}
