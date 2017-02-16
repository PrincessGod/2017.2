using System;
using System.Collections.Generic;
using System.IO;

namespace R.Earth.QuadTile
{
    public class TerrainTile : IDisposable
    {
        public string TerrainTileFilePath;
        public float TileSizeDegrees;
        public int SamplesPerTile;
        public float South;
        public float North;
        public float West;
        public float East;
        public int Row;
        public int Col;
        public int TargetLevel;
        public TerrainTileService m_owner;
        public bool IsInitialized;
        public bool IsValid;

        public List<float> ElevationData;
        protected TerrainDownloadRequest request;

        /// <summary>
        /// How long a bad terrain tile should be cached before retrying download.
        /// </summary>
        public static TimeSpan BadTileRetryInterval = TimeSpan.FromMinutes(30);

        public TerrainTile(TerrainTileService owner)
        {
            m_owner = owner;
        }

        public void Initialize()
        {
            if (IsInitialized)
                return;

            if (!File.Exists(TerrainTileFilePath))
            {
                // Download elevation
                if (request == null)
                {
                    using (request = new TerrainDownloadRequest(this, m_owner, Row, Col, TargetLevel))
                    {
                        request.SaveFilePath = TerrainTileFilePath;
                        request.DownloadInForeGround();
                    }
                }
            }

            if (ElevationData == null)
                ElevationData = new List<float>(SamplesPerTile * SamplesPerTile);

            if (File.Exists(TerrainTileFilePath))
            {
                // Load elevation file
                try
                {
                    using (Stream s = File.OpenRead(TerrainTileFilePath))
                    {
                        byte[] tfBuffer = new byte[SamplesPerTile * SamplesPerTile * 2];
                        if (s.Read(tfBuffer, 0, tfBuffer.Length) == tfBuffer.Length)
                        {
                            int offset = 0;

                            for (int y = 0; y < SamplesPerTile; y++)
                                for (int x = 0; x < SamplesPerTile; x++)
                                    ElevationData.Add(tfBuffer[offset++] + (short)(tfBuffer[offset++] << 8));
                            IsInitialized = true;
                            IsValid = true;
                        }
                    }
                }
                catch
                {
                }

                if (!IsValid)
                {
                    try
                    {
                        // Remove corrupt/failed elevation files after preset time.
                        FileInfo badFileInfo = new FileInfo(TerrainTileFilePath);
                        TimeSpan age = DateTime.Now.Subtract(badFileInfo.LastWriteTime);
                        if (age < BadTileRetryInterval)
                        {
                            // This tile is still flagged bad
                            IsInitialized = true;
                            return;
                        }

                        File.Delete(TerrainTileFilePath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public float GetElevationAt(float lat, float lon)
        {
            if (lat >= 90 || ElevationData.Count == 0)
                return 0;
            try
            {
                float tileUpperLeftLatitude = this.North;
                float tileUpperLeftLongitude = this.West;

                float deltaLat = tileUpperLeftLatitude - lat;
                float deltaLon = lon - tileUpperLeftLongitude;

                float lat_pixel = (deltaLat / TileSizeDegrees * SamplesPerTile);
                float lon_pixel = (deltaLon / TileSizeDegrees * SamplesPerTile);

                int lat_pixel_min = (int)lat_pixel;
                int lat_pixel_max = lat_pixel_min + 1;

                if (lat_pixel_min >= SamplesPerTile)
                    lat_pixel_min = SamplesPerTile - 1;

                int lon_pixel_min = (int)lon_pixel;
                int lon_pixel_max = lon_pixel_min + 1;

                if (lat_pixel_max >= SamplesPerTile)
                    lat_pixel_max = SamplesPerTile - 1;

                if (lon_pixel_min >= SamplesPerTile)
                    lon_pixel_min = SamplesPerTile - 1;

                if (lon_pixel_max >= SamplesPerTile)
                    lon_pixel_max = SamplesPerTile - 1;

                float x1y1 = ElevationData[lon_pixel_min + lat_pixel_min * SamplesPerTile];
                float x1y2 = ElevationData[lon_pixel_min + lat_pixel_max * SamplesPerTile];
                float x2y1 = ElevationData[lon_pixel_max + lat_pixel_min * SamplesPerTile];
                float x2y2 = ElevationData[lon_pixel_max + lat_pixel_max * SamplesPerTile];
                float x1_avg = x1y1 * (1 - (lat_pixel - lat_pixel_min)) + x1y2 * (lat_pixel - lat_pixel_min);
                float x2_avg = x2y1 * (1 - (lat_pixel - lat_pixel_min)) + x2y2 * (lat_pixel - lat_pixel_min);
                float avg_h = x1_avg * (1 - (lon_pixel - lon_pixel_min)) + x2_avg * (lon_pixel - lon_pixel_min);

                return avg_h;
            }
            catch
            {
            }
            return 0;
        }
        #region IDisposable Members

        public void Dispose()
        {
            if (request != null)
            {
                request.Dispose();
                request = null;
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
