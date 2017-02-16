
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;
using R.Earth.OverLayer.Control;

namespace R.Earth.UI
{
    public partial class WorldViewer : Control
    {
        private DrawArgs m_drawArgs;



        private Device m_Device3d;
        private World m_World;
        private PresentParameters presentParams;
        private Thread m_WorkerThread;
        private bool m_WorkerThreadRunning = false;
        private bool m_isRenderDisabled;

        private System.Drawing.Color backgroundColor = System.Drawing.Color.Black;

        public ControlMenuBar MenuBar
        {
            get { return this._menuBar; }
        }
        private ControlMenuBar _menuBar = new ControlMenuBar(World.Settings.ToolbarAnchor, 170);

        public WorldViewer()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            InitializeComponent();
            try
            {
                if (!Utility.IsInDesignMode())
                {
                    this.InitializeGraphics();
                    this.m_drawArgs = new DrawArgs(this.m_Device3d, this);
                    //Application.Idle += new EventHandler(this.OnApplicationIdle);

                }
            }
            catch (InvalidCallException caught)
            {
                throw new InvalidCallException(
                    "Unable to locate a compatible graphics adapter. Make sure you are running the latest version of DirectX.", caught);
            }
            catch (NotAvailableException caught)
            {
                throw new NotAvailableException(
                    "Unable to locate a compatible graphics adapter. Make sure you are running the latest version of DirectX.", caught);
            }

        }

        private void InitializeGraphics()
        {
            // Set up our presentation parameters
            presentParams = new PresentParameters();

            presentParams.Windowed = true;
            presentParams.SwapEffect = SwapEffect.Discard;
            presentParams.AutoDepthStencilFormat = DepthFormat.D16;
            presentParams.EnableAutoDepthStencil = true;

            if (!World.Settings.VSync)
                // Disable wait for vertical retrace (higher frame rate at the expense of tearing)
                presentParams.PresentationInterval = PresentInterval.Immediate;

            int adapterOrdinal = 0;
            try
            {
                // Store the default adapter
                adapterOrdinal = Manager.Adapters.Default.Adapter;
            }
            catch
            {
                // User probably needs to upgrade DirectX or install a 3D capable graphics adapter
                throw new NotAvailableException();
            }

            DeviceType dType = DeviceType.Hardware;

            foreach (AdapterInformation ai in Manager.Adapters)
            {
                if (ai.Information.Description.IndexOf("NVPerfHUD") >= 0)
                {
                    adapterOrdinal = ai.Adapter;
                    dType = DeviceType.Reference;
                }
            }
            CreateFlags flags = CreateFlags.SoftwareVertexProcessing;

            // Check to see if we can use a pure hardware m_Device3d
            Caps caps = Manager.GetDeviceCaps(adapterOrdinal, DeviceType.Hardware);

            // Do we support hardware vertex processing?
            if (caps.DeviceCaps.SupportsHardwareTransformAndLight)
                //	// Replace the software vertex processing
                flags = CreateFlags.HardwareVertexProcessing;

            // Use multi-threading for now - TODO: See if the code can be changed such that this isn't necessary (Texture Loading for example)
            flags |= CreateFlags.MultiThreaded | CreateFlags.FpuPreserve;

            if (World.Settings.AllowPureDevice && caps.DeviceCaps.SupportsPureDevice)
            {
                flags |= CreateFlags.PureDevice;
            }
            try
            {
                // Create our m_Device3d
                m_Device3d = new Device(adapterOrdinal, dType, this, flags, presentParams);
            }
            catch (Microsoft.DirectX.DirectXException)
            {
                throw new NotSupportedException("Unable to create the Direct3D m_Device3d.");
            }

            // Hook the m_Device3d reset event
            m_Device3d.DeviceReset += new EventHandler(OnDeviceReset);
            m_Device3d.DeviceResizing += new CancelEventHandler(OnDeviceResizing);
            this.OnDeviceReset(m_Device3d, null);
        }

        private void OnDeviceReset(object sender, EventArgs e)
        {
            // Can we use anisotropic texture minify filter?
            if (m_Device3d.DeviceCaps.TextureFilterCaps.SupportsMinifyAnisotropic)
            {
                m_Device3d.SamplerState[0].MinFilter = TextureFilter.Anisotropic;
            }
            else if (m_Device3d.DeviceCaps.TextureFilterCaps.SupportsMinifyLinear)
            {
                m_Device3d.SamplerState[0].MinFilter = TextureFilter.Linear;
            }

            // What about magnify filter?
            if (m_Device3d.DeviceCaps.TextureFilterCaps.SupportsMagnifyAnisotropic)
            {
                m_Device3d.SamplerState[0].MagFilter = TextureFilter.Anisotropic;
            }
            else if (m_Device3d.DeviceCaps.TextureFilterCaps.SupportsMagnifyLinear)
            {
                m_Device3d.SamplerState[0].MagFilter = TextureFilter.Linear;
            }

            m_Device3d.SamplerState[0].AddressU = TextureAddress.Clamp;
            m_Device3d.SamplerState[0].AddressV = TextureAddress.Clamp;

            m_Device3d.RenderState.Clipping = true;
            m_Device3d.RenderState.CullMode = Cull.Clockwise;
            m_Device3d.RenderState.Lighting = false;
            m_Device3d.RenderState.Ambient = World.Settings.StandardAmbientColor;

            m_Device3d.RenderState.ZBufferEnable = true;
            m_Device3d.RenderState.AntiAliasedLineEnable = true;
            m_Device3d.RenderState.AlphaBlendEnable = true;
            m_Device3d.RenderState.SourceBlend = Blend.SourceAlpha;
            m_Device3d.RenderState.DestinationBlend = Blend.InvSourceAlpha;


        }
        private void OnDeviceResizing(object sender, CancelEventArgs e)
        {
            if (!m_Device3d.CheckCooperativeLevel() || this.Size.Width == 0 || this.Size.Height == 0)
            {
                e.Cancel = true;
                return;
            }
            this.ResetSize();
        }

        public void ResetSize()
        {
            this.m_drawArgs.ScreenHeight = this.Height;
            this.m_drawArgs.ScreenWidth = this.Width;
        }
        /// <summary>
        /// The world render loop.  
        /// Borrowed from FlightGear and Tom Miller's blog
        /// </summary>
        public void OnApplicationIdle(object sender, EventArgs e)
        {
            try
            {
                if (Parent.Focused && !Focused)
                {
                    this.Focus();
                }

                while (Utility.IsAppStillIdle)
                {
                    if (m_isRenderDisabled && !World.Settings.CameraHasMomentum)
                    {
                        return;
                    }
                    this.Render();

                    this.m_drawArgs.Present();
                }
            }
            catch (DeviceLostException)
            {
                AttemptRecovery();
            }
            catch (Exception caught)
            {
                Log.Write(caught);
            }
        }

        private void AttemptRecovery()
        {
            try
            {
                m_Device3d.TestCooperativeLevel();
            }
            catch (DeviceLostException)
            {
            }
            catch (DeviceNotResetException)
            {
                try
                {
                    m_Device3d.Reset(presentParams);
                }
                catch (DeviceLostException)
                {
                    // If it's still lost or lost again, just do
                    // nothing
                }
            }
        }

        private void Render()
        {

            try
            {
                this.m_drawArgs.BeginRender();

                m_Device3d.Clear(ClearFlags.Target | ClearFlags.ZBuffer, backgroundColor, 1.0f, 0);

                if (m_World == null)
                {
                    m_Device3d.BeginScene();
                    m_Device3d.EndScene();
                    m_Device3d.Present();
                    Thread.Sleep(25);
                    return;
                }

                if (m_WorkerThread == null)
                {
                    m_WorkerThreadRunning = true;
                    m_WorkerThread = new Thread(new ThreadStart(WorkerThreadFunc));
                    m_WorkerThread.Name = "WorldWindow.WorkerThreadFunc";
                    m_WorkerThread.IsBackground = true;

                    m_WorkerThread.Priority = ThreadPriority.Normal;
                    m_WorkerThread.Start();
                }
                this.m_drawArgs.WorldCamera.Update(this.m_Device3d);
                m_Device3d.BeginScene();
                m_Device3d.RenderState.FillMode = FillMode.Solid;
                // Render the current planet

                m_World.Render(this.m_drawArgs);

                _menuBar.Render(m_drawArgs);

                m_Device3d.EndScene();
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                this.m_drawArgs.EndRender();
            }
        }

        private void WorkerThreadFunc()
        {

            while (m_WorkerThreadRunning)
            {
                try
                {
                    if (this.m_World != null)
                    {
                        this.m_World.Update(this.m_drawArgs);
                    }
                }
                catch (ThreadAbortException caught)
                {
                    Log.Write(caught);
                }
                catch (Exception caught)
                {
                    Log.Write(caught);
                }
            }
        }


        public DrawArgs DrawArgs { get { return this.m_drawArgs; } }
        public World CurrentWorld
        {
            get { return this.m_World; }
            set
            {
                this.m_World = value;
                if (m_World != null)
                {
                    MomentumCamera camera = new MomentumCamera(value.Position, World.EquatorialRadius, m_World.Orientation);
                    this.m_drawArgs.WorldCamera = camera;
                }
            }
        }
        /// <summary>
        /// Disables rendering (CPU tick saver)
        /// </summary>
        public bool IsRenderDisabled { get { return m_isRenderDisabled; } set { m_isRenderDisabled = value; } }

        //TODO JHJ 修改事件路由
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if(this._menuBar.OnMouseDown(e)) return;

            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if(this._menuBar.OnMouseMove(e)) return;

            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if(this._menuBar.OnMouseUp(e)) return;

            base.OnMouseUp(e);
        }
    }
}