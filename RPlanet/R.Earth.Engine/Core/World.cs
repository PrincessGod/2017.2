using System;
using System.ComponentModel;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using R.Earth.Config;
using R.Earth.OverLayer;
using R.Earth.QuadTile;

namespace R.Earth
{
    public interface IWorld
    {
        string Name { get; set; }

        void Render(DrawArgs drawArgs);

        void Update(DrawArgs drawArgs);

    }
    public interface IWorldSurface
    {
        void RenderSurfaceImages(DrawArgs drawArgs);
        void Update(DrawArgs drawArgs);
        void Dispose();
    }
    public class World : IWorld
    {
        #region IWorld 成员

        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }

        #endregion

      
        private GeoLayerList m_renderLayerList;

        private WidgetRoot m_OverlayList;

        private IWorldSurface m_WorldSurfaceRenderer;
        protected string _name = "Earth";
        private static double m_worldradius;
        /// <summary>
        /// 对象位置
        /// </summary>
        protected Vector3d position;
        /// <summary>
        /// 对象的四元数
        /// </summary>
        protected Quaternion4d orientation;

        private Projection m_currentProjection = Projection.Perspective;

        public const double EarthRadius = 6378137.0;
        public static double MeterPerDegree;

        public World(string name, double radius)
        {
            this._name = name;
            m_worldradius = radius;
            this.RenderLayerList = new GeoLayerList();    
        }

        public World(string name, Vector3d position, Quaternion4d orientation, System.Windows.Forms.Control control, TerrainAccessor terrainAccessor)
        {
            this._name = name;
            m_worldradius = EarthRadius;
            this.position = position;
            this.orientation = orientation;    
            MeterPerDegree = SMath.MeterPerDegree() ;
            this._terrainAccessor = terrainAccessor;
            this.RenderLayerList = new GeoLayerList();

            m_OverlayList = new WidgetRoot(control);            
        }


        public WidgetRoot OverlayList { get { return this.m_OverlayList; } set { this.m_OverlayList = value; } }
        public GeoLayerList RenderLayerList { get { return this.m_renderLayerList; } set { this.m_renderLayerList = value; } }
        public static WorldSetting Settings = new WorldSetting();
        public static double EquatorialRadius { get { return m_worldradius; } }

        #region IWorld 成员

        public void Render(DrawArgs drawArgs)
        {
            if (World.Settings.Project == Projection.Perspective)
            {
                //this.SetupLight(drawArgs);
                drawArgs.device.RenderState.FillMode = World.Settings.FillMode;

              
                if (this.m_renderLayerList != null)
                {
                    this.m_renderLayerList.Render(drawArgs);
                }
                if (m_WorldSurfaceRenderer != null)
                {
                    //m_WorldSurfaceRenderer.RenderSurfaceImages(drawArgs);
                }
            }
            else
            {
			
                if (this.m_renderLayerList != null)
                {
                    this.m_renderLayerList.RenderOrtho(drawArgs);
                }        
            }
            this.m_OverlayList.Render(drawArgs);
            this.RenderPositionInfo(drawArgs);

        }

        private void SetupLight(DrawArgs drawArgs)
        {
            if (World.Settings.EnableSunShading)
            {
                Vector3d sunPosition = SunCalculator.GetGeocentricPosition(TimeKeeper.CurrentTimeUtc);
                Vector3 sunVector = new Vector3(
                    (float)sunPosition.X,
                    (float)sunPosition.Y,
                    (float)sunPosition.Z);

                drawArgs.device.RenderState.Lighting = true;
                Material material = new Material();
                material.Diffuse = System.Drawing.Color.White;
                material.Ambient = System.Drawing.Color.White;

                drawArgs.device.Material = material;
                drawArgs.device.RenderState.AmbientColor = World.Settings.ShadingAmbientColor.ToArgb();
                drawArgs.device.RenderState.NormalizeNormals = true;
                drawArgs.device.RenderState.AlphaBlendEnable = true;

                drawArgs.device.Lights[0].Enabled = true;
                drawArgs.device.Lights[0].Type = LightType.Directional;
                drawArgs.device.Lights[0].Diffuse = System.Drawing.Color.White;
                drawArgs.device.Lights[0].Direction = sunVector;

                drawArgs.device.TextureState[0].ColorOperation = TextureOperation.Modulate;
                drawArgs.device.TextureState[0].ColorArgument1 = TextureArgument.Diffuse;
                drawArgs.device.TextureState[0].ColorArgument2 = TextureArgument.TextureColor;
            }
            else
            {
                drawArgs.device.RenderState.Lighting = false;
                drawArgs.device.RenderState.Ambient = World.Settings.StandardAmbientColor;

                drawArgs.device.TextureState[0].ColorOperation = TextureOperation.SelectArg1;
                drawArgs.device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
            }

            drawArgs.device.RenderState.TextureFactor = System.Drawing.Color.FromArgb(254, 255, 255, 255).ToArgb();
            drawArgs.device.TextureState[0].AlphaOperation = TextureOperation.Modulate;
            drawArgs.device.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
            drawArgs.device.TextureState[0].AlphaArgument2 = TextureArgument.TFactor;
        }

        protected void RenderPositionInfo(DrawArgs drawArgs)
        {
            // Render some Development information to screen
            string captionText = "";

            captionText += String.Format("纬度: {0}\n经度: {1}\n海拔高度:{2}",
                drawArgs.WorldCamera.Latitude,
                 drawArgs.WorldCamera.Longitude,
                 drawArgs.WorldCamera.Altitude) +
                "\n" +
                 drawArgs.WorldCamera;




            captionText = captionText.Trim();
            DrawTextFormat dtf = DrawTextFormat.NoClip | DrawTextFormat.WordBreak | DrawTextFormat.Right | DrawTextFormat.Top;
            int x = 7;
            int y = 7;
            Rectangle textRect = Rectangle.FromLTRB(x, y, drawArgs.device.Viewport.Width - 8,drawArgs.device.Viewport.Height - 8);

            DrawArgs.Instance.DrawText(null, captionText, textRect, dtf, Color.Gray.ToArgb());
            textRect.Offset(-1, -1);
            DrawArgs.Instance.DrawText(null, captionText, textRect, dtf, Color.White.ToArgb());
        }

        #endregion

        #region IWorld Members


        public void Update(DrawArgs drawArgs)
        {
            if (this.m_currentProjection != World.Settings.Project)
            {

                if (this.m_renderLayerList != null)
                {
                    this.m_renderLayerList.UpdateMesh(drawArgs);
                }

                this.m_currentProjection = World.Settings.Project;
            }
            if (this.m_renderLayerList != null)
            {
                this.m_renderLayerList.Update(drawArgs);
            }
            if (this.m_WorldSurfaceRenderer != null)
            {
                this.m_WorldSurfaceRenderer.Update(drawArgs);
            }
        }

        #endregion


        /// <summary>
        /// 对象的位置 (XYZ 世界坐标系)
        /// </summary>
        [Browsable(false)]
        public Vector3d Position        {            get            {                return this.position;            }            set            {                this.position = value;            }        }

        /// <summary>
        ///对象的旋转信息
        /// </summary>
        [Browsable(false)]
        public virtual Quaternion4d Orientation { get { return this.orientation; } set { this.orientation = value; } }

        internal void Dispose()
        {
            if (this.m_renderLayerList != null)
            {
                this.m_renderLayerList.Dispose();
                this.m_renderLayerList = null;
            }
            if (m_WorldSurfaceRenderer != null)
            {
                m_WorldSurfaceRenderer.Dispose();
            }
        }


        public TerrainAccessor TerrainAccessor
        {
            get
            {
                return this._terrainAccessor;
            }
            set
            {
                this._terrainAccessor = value;
            }
        }
        private TerrainAccessor _terrainAccessor;

        public IWorldSurface WorldSurfaceRenderer
        {
            get
            {
                return m_WorldSurfaceRenderer;
            }
            set { this.m_WorldSurfaceRenderer = value; }
        }
    }
}
