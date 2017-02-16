using System;
using System.Globalization;
using System.IO;

namespace R.Earth.QuadTile
{
    /// <summary>
    /// Provides elevation data (BIL format).
    /// </summary>
    public class TerrainTileService : IDisposable
    {
        #region Private Members
        string m_serverUrl;
        string m_dataSet;
        float m_levelZeroTileSizeDegrees;
        int m_samplesPerTile;
        int m_numberLevels;
        string m_fileExtension;
        string m_terrainTileDirectory;
        #endregion

        #region Properties
        public string ServerUrl
        {
            get
            {
                return m_serverUrl;
            }
        }

        public string DataSet
        {
            get
            {
                return m_dataSet;
            }
        }

        public float LevelZeroTileSizeDegrees
        {
            get
            {
                return m_levelZeroTileSizeDegrees;
            }
        }

        public int SamplesPerTile
        {
            get
            {
                return m_samplesPerTile;
            }
        }

        public string FileExtension
        {
            get
            {
                return m_fileExtension;
            }
        }

        public string TerrainTileDirectory
        {
            get
            {
                return m_terrainTileDirectory;
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref= "T:WorldWind.Terrain.TerrainTileService"/> class.
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="dataset"></param>
        /// <param name="levelZeroTileSizeDegrees"></param>
        /// <param name="samplesPerTile"></param>
        /// <param name="fileExtension"></param>
        /// <param name="numberLevels"></param>
        /// <param name="terrainTileDirectory"></param>
        public TerrainTileService(
            string serverUrl,
            string dataset,
            float levelZeroTileSizeDegrees,
            int samplesPerTile,
            string fileExtension,
            int numberLevels,
            string terrainTileDirectory)
        {
            m_serverUrl = serverUrl;
            m_dataSet = dataset;
            m_levelZeroTileSizeDegrees = levelZeroTileSizeDegrees;
            m_samplesPerTile = samplesPerTile;
            m_numberLevels = numberLevels;
            m_fileExtension = fileExtension.Replace(".", "");
            m_terrainTileDirectory = terrainTileDirectory;
            if (!Directory.Exists(m_terrainTileDirectory))
                Directory.CreateDirectory(m_terrainTileDirectory);
        }

        /// <summary>
        /// Builds terrain tile containing the specified coordinates.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees.</param>
        /// <param name="longitude">Longitude in decimal degrees.</param>
        /// <param name="samplesPerDegree"></param>
        /// <returns>Uninitialized terrain tile (no elevation data)</returns>
        public TerrainTile GetTerrainTile(float latitude, float longitude, float samplesPerDegree)
        {
            TerrainTile tile = new TerrainTile(this);

            tile.TargetLevel = m_numberLevels - 1;
            for (int i = 0; i < m_numberLevels; i++)
            {
                if (samplesPerDegree <= m_samplesPerTile / (m_levelZeroTileSizeDegrees * Math.Pow(0.5, i)))
                {
                    tile.TargetLevel = i;
                    break;
                }
            }

            tile.Row = GetRowFromLatitude(latitude, m_levelZeroTileSizeDegrees * Math.Pow(0.5, tile.TargetLevel));
            tile.Col = GetColFromLongitude(longitude, m_levelZeroTileSizeDegrees * Math.Pow(0.5, tile.TargetLevel));
            tile.TerrainTileFilePath = string.Format(CultureInfo.InvariantCulture,
                @"{0}\{4}\{1:D8}\{1:D8}_{2:D8}.{3}",
                m_terrainTileDirectory, tile.Row, tile.Col, m_fileExtension, tile.TargetLevel);
            tile.SamplesPerTile = m_samplesPerTile;
            tile.TileSizeDegrees = m_levelZeroTileSizeDegrees * (float)Math.Pow(0.5f, tile.TargetLevel);
            tile.North = -90.0f + tile.Row * tile.TileSizeDegrees + tile.TileSizeDegrees;
            tile.South = -90.0f + tile.Row * tile.TileSizeDegrees;
            tile.West = -180.0f + tile.Col * tile.TileSizeDegrees;
            tile.East = -180.0f + tile.Col * tile.TileSizeDegrees + tile.TileSizeDegrees;

            return tile;
        }

        // Hack: newer methods in MathEngine class cause problems
        public static int GetColFromLongitude(double longitude, double tileSize)
        {
            return (int)System.Math.Floor((System.Math.Abs(-180.0 - longitude) % 360) / tileSize);
        }

        public static int GetRowFromLatitude(double latitude, double tileSize)
        {
            return (int)System.Math.Floor((System.Math.Abs(-90.0 - latitude) % 180) / tileSize);
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (DrawArgs.DownloadQueue != null)
                DrawArgs.DownloadQueue.Clear(this);
        }

        #endregion
    }
}
