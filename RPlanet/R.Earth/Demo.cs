using System;
using System.Drawing;
using System.IO;
using System.Security.Permissions;
using System.Windows.Forms;
using Microsoft.DirectX;
using R.Earth.Config;
using R.Earth.OverLayer.Element;
using R.Earth.Plugin;
using R.Earth.QuadTile;
using R.Earth.UI; 

namespace R.Earth
{
    public partial class Demo : Form
    {
        private double startlatitude = 37.1245;
        private double startlongitude = 117.43232;
        private double startAltitude = 100000.0;
        
        public Demo()
        {
            InitializeComponent();
        }

        private void InitEvent()
        {
            this.worldViewer1.MouseWheel += new MouseEventHandler(worldViewer1_MouseWheel);
            this.worldViewer1.MouseMove += new MouseEventHandler(worldViewer1_MouseMove);
            this.worldViewer1.MouseUp += new MouseEventHandler(worldViewer1_MouseUp);
            this.worldViewer1.MouseDown += new MouseEventHandler(worldViewer1_MouseDown);
            this.worldViewer1.KeyDown += new KeyEventHandler(worldViewer1_KeyDown);
        }

        private void  InitWorld()
        {
            Vector3 v = SMath.SphericalToCartesian(startlatitude, startlongitude , World.EarthRadius);
            v.Z = (float)startAltitude * 1.0f;
            Quaternion4d q = Quaternion4d.EulerToQuaternion(SMath.DegreesToRadians(startlongitude),SMath.DegreesToRadians(startlatitude), 0);
            Quaternion qz = Quaternion.RotationAxis(new Vector3(0,0,1), (float)SMath.DegreesToRadians(startlatitude));
            //q.W = qz.W;
            //q.X = qz.X;
            //q.Y = qz.Y;
            //q.Z = qz.Z;
            //TerrainTileService terrainTileService = new TerrainTileService("http://worldwind25.arc.nasa.gov/tile/tile.aspx", "100", 20, 150, "bil", 8, Path.Combine(EarthSetting.CachePath, "Earth\\TerrainAccessor\\SRTM"));

            TerrainTileService terrainTileService = new TerrainTileService("http://worldwind25.arc.nasa.gov/tile/tile.aspx", "100", 1, 150, "bil", 6, @"D:\空间数据\重庆H48\bil29107");
            TerrainAccessor terrainAccessor = new NltTerrainAccessor("Earth", -180, -90, 180, 90, terrainTileService, null);


            World _world = new World("Earth",new Vector3d(0, 0, 0), q,this.worldViewer1,terrainAccessor);
            this.worldViewer1.CurrentWorld = _world;

            this.worldViewer1.ResetSize();
        }

        public WorldViewer WorldViewer { get { return this.worldViewer1; } }

        private void Demo_Load(object sender, EventArgs e)
        {
            InitWorld();
            InitializePluginCompiler();
            InitEvent();
            
            Application.Idle += new EventHandler(this.WorldViewer.OnApplicationIdle);
            
        }



        private void m_form_OnResizeEvent(WidgetForm m_form, Size size)
        {
            this.worldViewer1.ResetSize();
        }
        /// <summary>
        /// All messages are sent to the WndProc method after getting filtered through 
        /// the PreProcessMessage method.  The WndProc method corresponds exactly to 
        /// the Windows WindowProc function.
        /// </summary>
        /// <param name="m">The Windows Message to process.</param>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true), SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_ACTIVATEAPP:
                    if (this.worldViewer1 != null)
                        worldViewer1.IsRenderDisabled = m.WParam.ToInt32() == 0;
                    break;

                case 0x0112:
                    if ((int)m.WParam == 0xF060)
                    {
                        this.Close();
                    }
                    break;
            }
            base.WndProc(ref m);
        }


        private bool isMouseDragging;
        private Point mouseDownStartPosition = Point.Empty;
        private Angle cLat, cLon;

        void worldViewer1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (this.WorldViewer.CurrentWorld.OverlayList.OnMouseWheel(e)) return;
            try
            {
                this.WorldViewer.DrawArgs.WorldCamera.ZoomStepped(e.Delta / 120.0f);
            }
            finally
            {
                base.OnMouseWheel(e);
            }
        }
        
        void worldViewer1_MouseDown(object sender, MouseEventArgs e)
        {
            //TODO JHJ 修改事件路由
            if (this.WorldViewer.CurrentWorld.OverlayList.OnMouseDown(e)) return;

            DrawArgs.Instance.LastMousePosition.X = e.X;
            DrawArgs.Instance.LastMousePosition.Y = e.Y;

            mouseDownStartPosition.X = e.X;
            mouseDownStartPosition.Y = e.Y;
        }

        void worldViewer1_MouseUp(object sender, MouseEventArgs e)
        {
            //TODO JHJ 修改事件路由
            if (this.WorldViewer.CurrentWorld.OverlayList.OnMouseUp(e)) return;
            DrawArgs.Instance.LastMousePosition.X = e.X;
            DrawArgs.Instance.LastMousePosition.Y = e.Y;
        }

        void worldViewer1_MouseMove(object sender, MouseEventArgs e)
        {
            
            try
            {
                //TODO JHJ 修改事件路由
                if (this.WorldViewer.CurrentWorld.OverlayList.OnMouseMove(e)) return;

                int deltaX = e.X - DrawArgs.Instance.LastMousePosition.X;
                int deltaY = e.Y - DrawArgs.Instance.LastMousePosition.Y;
                float deltaXNormalized = (float)deltaX / DrawArgs.Instance.ScreenWidth;
                float deltaYNormalized = (float)deltaY / DrawArgs.Instance.ScreenHeight;
                
                if (mouseDownStartPosition == Point.Empty)
                    return;

                bool isMouseLeftButtonDown = ((int)e.Button & (int)MouseButtons.Left) != 0;
                bool isMouseRightButtonDown = ((int)e.Button & (int)MouseButtons.Right) != 0;
                if (isMouseLeftButtonDown || isMouseRightButtonDown)
                {
                    int dx = this.mouseDownStartPosition.X - e.X;
                    int dy = this.mouseDownStartPosition.Y - e.Y;
                    int distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared > 3 * 3)
                    {
                        this.isMouseDragging = true;
                    }
                }
               
                if (isMouseLeftButtonDown && !isMouseRightButtonDown)
                {

                    Angle prevLat, prevLon;
                    this.WorldViewer.DrawArgs.WorldCamera.PickingRayIntersection(
                       DrawArgs.Instance.LastMousePosition.X,
                       DrawArgs.Instance.LastMousePosition.Y,
                       out prevLat,
                       out prevLon);

                    Angle curLat, curLon;
                    this.WorldViewer.DrawArgs.WorldCamera.PickingRayIntersection(
                       e.X,
                       e.Y,
                       out curLat,
                       out curLon);

                    if (World.Settings.CameraTwistLock)
                    {
                        this.WorldViewer.DrawArgs.WorldCamera.SlerpPercentage = 1.0;
                        if (Angle.IsNaN(curLat) || Angle.IsNaN(prevLat))
                        {   return;
                            // Old style pan
                            Angle deltaLat = Angle.FromRadians((double)deltaY * (this.WorldViewer.DrawArgs.WorldCamera.Altitude) / (800 * World.EquatorialRadius));
                            Angle deltaLon = Angle.FromRadians((double)-deltaX * (this.WorldViewer.DrawArgs.WorldCamera.Altitude) / (800 * World.EquatorialRadius));
                            this.WorldViewer.DrawArgs.WorldCamera.Pan(deltaLat, deltaLon);
                        }
                        else
                        {
                            //Picking ray pan
                            Angle lat = prevLat - curLat;
                            Angle lon = prevLon - curLon;
                            this.WorldViewer.DrawArgs.WorldCamera.Pan(lat, lon);
                        }
                    }
                    else
                    {
                        double factor = (this.WorldViewer.DrawArgs.WorldCamera.Altitude) / (1500 * World.EquatorialRadius);
                        this.WorldViewer.DrawArgs.WorldCamera.SlerpPercentage = 1.0;
                        this.WorldViewer.DrawArgs.WorldCamera.RotationYawPitchRoll(
                           Angle.FromRadians(DrawArgs.Instance.LastMousePosition.X - e.X) * factor,
                           Angle.FromRadians(e.Y - DrawArgs.Instance.LastMousePosition.Y) * factor,
                           Angle.Zero);
                    }
                }
                else if (!isMouseLeftButtonDown && isMouseRightButtonDown)
                {
                    //Right mouse button
                    //TODO JHJ 修改镜头控制  
                    //JHJ 2017.2.9 delete "-" make heading more reasonable
                    // Heading
                    Angle deltaEyeDirection = Angle.FromRadians(deltaXNormalized * World.Settings.CameraRotationSpeed);
                    this.WorldViewer.DrawArgs.WorldCamera.SlerpPercentage = 1.0;
                    this.WorldViewer.DrawArgs.WorldCamera.RotationYawPitchRoll(Angle.Zero, Angle.Zero, deltaEyeDirection);

                    //JHJ 2017.2.9 add "-" make tilt more reasonable 
                    // tilt
                    this.WorldViewer.DrawArgs.WorldCamera.Tilt += Angle.FromRadians(-deltaYNormalized * World.Settings.CameraRotationSpeed);
                }
                else if (isMouseLeftButtonDown && isMouseRightButtonDown)
                {
                    // Both buttons (zoom)
                    this.WorldViewer.DrawArgs.WorldCamera.SlerpPercentage = 1.0;
                    if (Math.Abs(deltaYNormalized) > float.Epsilon)
                        this.WorldViewer.DrawArgs.WorldCamera.Zoom(-deltaYNormalized * World.Settings.CameraZoomAnalogFactor);

                    if (!World.Settings.CameraBankLock)
                        this.WorldViewer.DrawArgs.WorldCamera.Bank -= Angle.FromRadians(deltaXNormalized * World.Settings.CameraRotationSpeed);
                }
            }
            catch
            {
            }
            finally
            {

                this.WorldViewer.DrawArgs.WorldCamera.PickingRayIntersection(
                   e.X,
                   e.Y,
                   out cLat,
                   out cLon);

                DrawArgs.Instance.LastMousePosition.X = e.X;
                DrawArgs.Instance.LastMousePosition.Y = e.Y;
                base.OnMouseMove(e);
            }
        }
        
        void worldViewer1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V)
            {
                //this.ve.IsVisible = !this.ve.IsVisible;
                //if ((int)World.Settings.Project == 1)
                //{
                //    World.Settings.Project = 0;
                //}
                //else
                //{
                //    World.Settings.Project++;
                //}
            }
            else if (e.KeyCode == Keys.W)
            {
                World.Settings.FillMode = Microsoft.DirectX.Direct3D.FillMode.WireFrame;
            }
            else if (e.KeyCode == Keys.S)
            {
                World.Settings.FillMode = Microsoft.DirectX.Direct3D.FillMode.Solid;
            }
            else if (e.KeyCode == Keys.P)
            {
                World.Settings.FillMode = Microsoft.DirectX.Direct3D.FillMode.Point;
            }
            else if (e.KeyCode == Keys.L)
            {
                World.Settings.ShowLatLongLines = !World.Settings.ShowLatLongLines;
            }
            else if (e.KeyCode == Keys.Space)
            {
                this.WorldViewer.DrawArgs.WorldCamera.Reset();
            }
        }

     
        
        
        private PluginCompiler m_compiler;
        private void InitializePluginCompiler()
        {
            this.m_compiler = new PluginCompiler(this.worldViewer1);
            this.m_compiler.LoadPlugin();
            this.m_compiler.LoadStartUpPlugin();         
        }
        
    }
}