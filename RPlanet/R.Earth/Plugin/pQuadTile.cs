using System;
using System.IO;
using System.Globalization;

using System.Collections.Generic;
using System.Text;
using R.Earth.QuadTile;
using R.Earth;

namespace R.Earth.Plugin
{
    public class pQuadTile : TechDemoPlugin
    {

        //Image Tile Service



        //Image Accessor

        protected byte m_bOpacity = 255;
        string m_strTextureDirectory;
        string m_strCacheRoot;
        public override void Load()
        {
            string name = "Geocover 1990";
            string description = "NASA derived global 30 meters per pixel satellite image mosaic";

            //////////////////////////---------------
            double north = 90.0;
            double south = -90.0;
            double west = -180.0;
            double east = 180.0;
            GeographicBoundingBox box = new GeographicBoundingBox(north, south, west, east);
            ///////////////////////////---------------------            
            string m_strDataSetName = "bmng.topo.bathy.200403";
            string m_strServerUrl = "http://worldwind25.arc.nasa.gov/tile/tile.aspx";
            ImageTileService m_oImageTileService = new ImageTileService(m_strDataSetName, m_strServerUrl);
            //////////////////////////-------------------------------

            decimal m_decLevelZeroTileSizeDegrees = 36.0m;
            int m_intNumberLevels = 20;
            int m_intTextureSizePixels = 256;
            string m_strImageFileExtension = "jpg";

            string strCachePath =@"E:\项目\智岩切片工具\分块数据\DomTile";// Config.EarthSetting.CachePath + "\\bmng\\";

            ImageAccessor m_oImageAccessor = new ImageAccessor(strCachePath,
                m_intTextureSizePixels,
                m_decLevelZeroTileSizeDegrees,
                m_intNumberLevels,
                m_strImageFileExtension,
                strCachePath,
                m_oImageTileService);

            QuadTileSet tile = new QuadTileSet(name, box, this.Viewer.CurrentWorld, 0,
                this.Viewer.CurrentWorld.TerrainAccessor, m_oImageAccessor, m_bOpacity, true);
            tile.IsVisible = true;
            this.Viewer.CurrentWorld.RenderLayerList.Add(tile);

        }
        public override void UnLoad()
        {
            base.UnLoad();
        }

    }
    public class pQuadTile3 : TechDemoPlugin
    {
        QuadTileSet m_oQuadTileSet = null;
        ImageAccessor m_oImageAccessor = null;
        ImageTileService m_oImageTileService = null;


        //Image Tile Service
        string m_strServerUrl = string.Empty;
        string m_strDataSetName = string.Empty;

        //Image Accessor
        private decimal m_decLevelZeroTileSizeDegrees = 30;
        private int m_intNumberLevels = 1;
        private int m_intTextureSizePixels = 256;
        string m_strImageFileExtension = ".png";

        protected byte m_bOpacity = 255;
        protected string m_strName;
        GeographicBoundingBox m_hBoundary = new GeographicBoundingBox(0, 0, 0, 0);
        protected World m_oWorld;
        //QuadTileLayer
        int distAboveSurface = 0;
        bool terrainMapped = false;


        string m_strTextureDirectory;
        string m_strCacheRoot;


        public static readonly string URLProtocolName = "gxtile://";

        public static readonly string CacheSubDir = "Tile Server Cache";

        public override void Load()
        {
            return;

            string strCachePath = GetCachePath();
            if (m_oImageTileService == null && m_strDataSetName != string.Empty && m_strServerUrl != string.Empty)
            {
                m_oImageTileService = new ImageTileService(m_strDataSetName, m_strServerUrl);
            }
            m_oImageAccessor = new ImageAccessor(strCachePath,
                m_intTextureSizePixels,
                m_decLevelZeroTileSizeDegrees,
                m_intNumberLevels,
                m_strImageFileExtension,
                strCachePath,
                m_oImageTileService);

            m_oQuadTileSet = new QuadTileSet(m_strName,
                m_hBoundary,
                m_oWorld,
                distAboveSurface,
                (terrainMapped ? m_oWorld.TerrainAccessor : null),
                m_oImageAccessor, m_bOpacity, false);

            base.Load();
        }
        public override void UnLoad()
        {
            base.UnLoad();
        }


        public string GetCachePath()
        {
            return Path.Combine(Path.Combine(Path.Combine(Path.Combine(m_strCacheRoot, CacheSubDir), GetServerFileNameFromUrl(m_strServerUrl)), StringHash.GetBase64HashForPath(m_strDataSetName)), m_decLevelZeroTileSizeDegrees.GetHashCode().ToString());
        }

        public static string GetServerFileNameFromUrl(string strurl)
        {
            string serverfile = strurl;
            int iUrl = strurl.IndexOf("//") + 2;
            if (iUrl == -1)
                iUrl = strurl.IndexOf("\\") + 2;
            if (iUrl != -1)
                serverfile = serverfile.Substring(iUrl);
            foreach (Char ch in Path.GetInvalidFileNameChars())
                serverfile = serverfile.Replace(ch.ToString(), "_");
            return serverfile;
        }
    }
}