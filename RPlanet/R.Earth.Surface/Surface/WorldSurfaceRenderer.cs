using System;
using System.Collections;
using Microsoft.DirectX.Direct3D;
using R.Earth.Plugin;
namespace R.Earth.WorldSurface
{
    public class pWorldSurface : TechDemoPlugin
    {
        string surfacepath = Config.EarthSetting.RSourcePath + @"clouds_20081026-1533.png";
        string cosspath = Config.EarthSetting.RSourcePath + @"TM0541.jpg";
        string matrix = Config.EarthSetting.RSourcePath + @"GreenMatrixCode.jpg";


        public override void Load()
        {
            return;
            Texture tex = TextureLoader.FromFile(this.Viewer.DrawArgs.device, surfacepath);
            Texture tex2 = TextureLoader.FromFile(this.Viewer.DrawArgs.device, cosspath);
            Texture tex3 = TextureLoader.FromFile(this.Viewer.DrawArgs.device, matrix);

            WorldSurfaceRenderer sur = new WorldSurfaceRenderer(32, 0, this.Viewer.CurrentWorld);



            //SurfaceImage image = new SurfaceImage(surfacepath,90,-90,-180,180,tex ,(R.Earth.GeoLayer) this.Viewer.CurrentWorld.RenderLayerList[0]);
            //sur.AddSurfaceImage(image);
            //SurfaceImage image2 = new SurfaceImage(cosspath, 90, -90, -180, 180, tex2, (R.Earth.GeoLayer)this.Viewer.CurrentWorld.RenderLayerList[0]);
            //sur.AddSurfaceImage(image2);
            //SurfaceImage image3 = new SurfaceImage(matrix, 50, -30, -20, 80, tex3, (R.Earth.GeoLayer)this.Viewer.CurrentWorld.RenderLayerList[0]);
            //sur.AddSurfaceImage(image3);

            this.Viewer.CurrentWorld.WorldSurfaceRenderer = sur;
        }
        public override void UnLoad()
        {
            base.UnLoad();
        }
    }
    public class WorldSurfaceRenderer : IWorldSurface
    {
        public const int RenderSurfaceSize = 256;

        #region Private Members

        private const int m_NumberRootTilesHigh = 2;

        private World m_ParentWorld;
        private RenderToSurface m_Rts = null;

        private uint m_SamplesPerTile;

        private SurfaceTile[] m_RootSurfaceTiles;
        private double m_DistanceAboveSeaLevel = 0;
        private bool m_Initialized = false;
        private ArrayList m_SurfaceImages = new System.Collections.ArrayList();
        private Queue m_TextureLoadQueue = new System.Collections.Queue();

        private Device m_Device = null;
        #endregion

        public System.DateTime LastChange = System.DateTime.Now;
        public int NumberTilesUpdated = 0;


        public static short[] m_IndicesElevated;



        public WorldSurfaceRenderer(uint samplesPerTile, double distanceAboveSeaLevel, World parentWorld)
        {
            m_SamplesPerTile = samplesPerTile;
            m_ParentWorld = parentWorld;
            m_DistanceAboveSeaLevel = distanceAboveSeaLevel;

            double tileSize = 180.0f / m_NumberRootTilesHigh;

            m_RootSurfaceTiles = new SurfaceTile[m_NumberRootTilesHigh * (m_NumberRootTilesHigh * 2)];
            for (int i = 0; i < m_NumberRootTilesHigh; i++)
            {
                for (int j = 0; j < m_NumberRootTilesHigh * 2; j++)
                {
                    m_RootSurfaceTiles[i * m_NumberRootTilesHigh * 2 + j] = new SurfaceTile(
                        (i + 1) * tileSize - 90.0f,
                        i * tileSize - 90.0f,
                        j * tileSize - 180.0f,
                        (j + 1) * tileSize - 180.0f,
                        0,
                        this);
                }
            }
        }


        private void OnDeviceReset(object sender, EventArgs e)
        {
            Device dev = (Device)sender;

            m_Device = dev;

            m_Rts = new RenderToSurface(
                dev,
                RenderSurfaceSize,
                RenderSurfaceSize,
                Format.X8R8G8B8,
                true,
                DepthFormat.D16);
        }

        public void AddSurfaceImage(SurfaceImage surfaceImage)
        {
            if (surfaceImage.ImageTexture != null)
            {
                lock (m_SurfaceImages.SyncRoot)
                {
                    m_SurfaceImages.Add(surfaceImage);
                    m_SurfaceImages.Sort();
                }

                LastChange = System.DateTime.Now;
            }
            else
            {
                lock (m_TextureLoadQueue.SyncRoot)
                {
                    m_TextureLoadQueue.Enqueue(surfaceImage);
                }
            }
        }
        public void RemoveSurfaceImage(string imageResource)
        {
            try
            {
                lock (m_SurfaceImages.SyncRoot)
                {
                    for (int i = 0; i < m_SurfaceImages.Count; i++)
                    {
                        SurfaceImage current = m_SurfaceImages[i] as SurfaceImage;
                        if (current != null && current.ImageFilePath == imageResource)
                        {
                            m_SurfaceImages.RemoveAt(i);
                            break;
                        }
                    }

                    m_SurfaceImages.Sort();
                }
                LastChange = System.DateTime.Now;
            }
            catch (System.Threading.ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                Log.DebugWrite(ex);
            }
        }


        public void Initialize(DrawArgs drawArgs)
        {
            this.SetStaticValue();
            foreach (SurfaceTile st in m_RootSurfaceTiles)
            {
                st.Initialize (drawArgs);
            }
            m_Initialized = true;
        }

        private void SetStaticValue()
        {
            int thisVertexDensityElevatedPlus2 = ((int)SamplesPerTile / 2 + 2);
            m_IndicesElevated = new short[2 * thisVertexDensityElevatedPlus2 * thisVertexDensityElevatedPlus2 * 3];

            for (int i = 0; i < thisVertexDensityElevatedPlus2; i++)
            {
                int elevated_idx = (2 * 3 * i * thisVertexDensityElevatedPlus2);
                for (int j = 0; j < thisVertexDensityElevatedPlus2; j++)
                {
                    m_IndicesElevated[elevated_idx] = (short)(i * (thisVertexDensityElevatedPlus2 + 1) + j);
                    m_IndicesElevated[elevated_idx + 1] = (short)((i + 1) * (thisVertexDensityElevatedPlus2 + 1) + j);
                    m_IndicesElevated[elevated_idx + 2] = (short)(i * (thisVertexDensityElevatedPlus2 + 1) + j + 1);

                    m_IndicesElevated[elevated_idx + 3] = (short)(i * (thisVertexDensityElevatedPlus2 + 1) + j + 1);
                    m_IndicesElevated[elevated_idx + 4] = (short)((i + 1) * (thisVertexDensityElevatedPlus2 + 1) + j);
                    m_IndicesElevated[elevated_idx + 5] = (short)((i + 1) * (thisVertexDensityElevatedPlus2 + 1) + j + 1);

                    elevated_idx += 6;
                }
            }
        }
        private short[] CreateTriangleIndicesRegular(CustomVertex.PositionTextured[] ElevatedVertices, int VertexDensity, int Margin, double LayerRadius)
        {
            short[] indices;
            int thisVertexDensityElevatedPlus2 = (VertexDensity + (Margin * 2));
            indices = new short[2 * thisVertexDensityElevatedPlus2 * thisVertexDensityElevatedPlus2 * 3];

            for (int i = 0; i < thisVertexDensityElevatedPlus2; i++)
            {
                int elevated_idx = (2 * 3 * i * thisVertexDensityElevatedPlus2);
                for (int j = 0; j < thisVertexDensityElevatedPlus2; j++)
                {
                    indices[elevated_idx] = (short)(i * (thisVertexDensityElevatedPlus2 + 1) + j);
                    indices[elevated_idx + 1] = (short)((i + 1) * (thisVertexDensityElevatedPlus2 + 1) + j);
                    indices[elevated_idx + 2] = (short)(i * (thisVertexDensityElevatedPlus2 + 1) + j + 1);

                    indices[elevated_idx + 3] = (short)(i * (thisVertexDensityElevatedPlus2 + 1) + j + 1);
                    indices[elevated_idx + 4] = (short)((i + 1) * (thisVertexDensityElevatedPlus2 + 1) + j);
                    indices[elevated_idx + 5] = (short)((i + 1) * (thisVertexDensityElevatedPlus2 + 1) + j + 1);

                    elevated_idx += 6;
                }
            }
            return indices;
        }

        public void Dispose()
        {
            m_Initialized = false;
            if (m_Device != null)
            {
                m_Device.DeviceReset -= new EventHandler(OnDeviceReset);
            }

            if (m_Rts != null && !m_Rts.Disposed)
            {
                m_Rts.Dispose();
            }
            lock (m_RootSurfaceTiles.SyncRoot)
            {
                foreach (SurfaceTile st in m_RootSurfaceTiles)
                    st.Dispose();
            }
        }

        public void Update(DrawArgs drawArgs)
        {
            try
            {
                if (!m_Initialized)
                {
                    Initialize(drawArgs);
                }

                foreach (SurfaceTile tile in m_RootSurfaceTiles)
                {
                    tile.Update(drawArgs);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        public void RenderSurfaceImages(DrawArgs drawArgs)
        {
            if (this.m_Rts == null)
            {
                drawArgs.device.DeviceReset += new EventHandler(OnDeviceReset);
                OnDeviceReset(drawArgs.device, null);
            }
            if (!m_Initialized)
                return;

            if (m_TextureLoadQueue.Count > 0)
            {
                SurfaceImage si = m_TextureLoadQueue.Dequeue() as SurfaceImage;
                if (si != null)
                {
                    si.ImageTexture = ImageHelper.LoadTexture(si.ImageFilePath);

                    lock (this.m_SurfaceImages.SyncRoot)
                    {
                        m_SurfaceImages.Add(si);
                        m_SurfaceImages.Sort();
                    }
                }
                //drawArgs.TexturesLoadedThisFrame++;
            }

            if (drawArgs.device.RenderState.Lighting)
            {
                drawArgs.device.RenderState.Lighting = false;
            }
            NumberTilesUpdated = 0;

           

            foreach (SurfaceTile tile in m_RootSurfaceTiles)
            {
                if (tile != null)
                    tile.Render(drawArgs);
            }


            drawArgs.device.RenderState.Lighting = true;
        }


        #region Properties
        /// <summary>
        /// Gets the surface images.
        /// </summary>
        /// <value></value>
        public ArrayList SurfaceImages
        {
            get
            {
                return m_SurfaceImages;
            }
        }

        /// <summary>
        /// Gets the distance above sea level in meters.
        /// </summary>
        /// <value></value>
        public double DistanceAboveSeaLevel
        {
            get
            {
                return m_DistanceAboveSeaLevel;
            }
        }

        /// <summary>
        /// Gets the samples per tile.  Also can be considered the Vertex Density or Mesh Density of each SurfaceTile
        /// </summary>
        /// <value></value>
        public uint SamplesPerTile
        {
            get
            {
                return m_SamplesPerTile;
            }
        }

        /// <summary>
        /// Gets the parent world.
        /// </summary>
        /// <value></value>
        public World ParentWorld
        {
            get
            {
                return m_ParentWorld;
            }
        }


        public RenderToSurface RenderToSurface
        {
            get
            {
                return m_Rts;
            }
        }

        #endregion
    }
}