using System;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Serialization;
using Microsoft.DirectX.Direct3D;
using R.Earth.OverLayer.Control;

namespace R.Earth.Config
{
    public class WorldSetting : SettingBase
    {
        internal Color _standardAmbientColor = Color.White;
        internal bool m_enableSunShading = true;
        internal bool m_sunSynchedWithTime = true;
        /// <summary>
        /// Vsync on/off (Wait for vertical retrace)
        /// </summary>
        internal bool vSync = true;
        private bool m_AllowPureDevice = false;

        public Color StandardAmbientColor { get { return this._standardAmbientColor; } set { this._standardAmbientColor = value; } }

        internal bool enableAtmosphericScattering = true;

        [Browsable(true), Category("Atmosphere")]
        [Description("Enable Atmospheric Scattering")]
        public bool EnableAtmosphericScattering
        {
            get { return enableAtmosphericScattering; }
            set { enableAtmosphericScattering = value; }
        }

        internal bool forceCpuAtmosphere = false;
        [Browsable(true), Category("Atmosphere")]
        [Description("Forces CPU calculation instead of GPU for Atmospheric Scattering")]
        public bool ForceCpuAtmosphere
        {
            get { return forceCpuAtmosphere; }
            set { forceCpuAtmosphere = value; }
        }


        [Browsable(true), Category("3D settings")]
        [Description("Shade the Earth according to the Sun's position at a certain time.")]
        public bool EnableSunShading { get { return m_enableSunShading; } set { m_enableSunShading = value; } }


        [Browsable(true), Category("3D settings")]
        [Description("Sun position is computed according to time.")]
        public bool SunSynchedWithTime { get { return m_sunSynchedWithTime; } set { m_sunSynchedWithTime = value; } }

        private double m_sunElevation = Math.PI / 4;
        [Browsable(true), Category("3D settings")]
        [Description("Sun elevation when not synched to time.")]
        public double SunElevation { get { return m_sunElevation; } set { m_sunElevation = value; } }

        private double m_sunHeading = -Math.PI / 4;
        [Browsable(true), Category("3D settings")]
        [Description("Sun direction when not synched to time.")]
        public double SunHeading { get { return m_sunHeading; } set { m_sunHeading = value; } }

        private double m_sunDistance = 150000000000;
        [Browsable(true), Category("3D settings")]
        [Description("Sun distance in meter.")]
        public double SunDistance { get { return m_sunDistance; } set { m_sunDistance = value; } }
        private int m_shadingAmbientColor = System.Drawing.Color.White.ToArgb();
        [Browsable(true), Category("3D settings")]
        [Description("The background ambient color when sun shading is enabled.")]
        [XmlIgnore]
        public System.Drawing.Color ShadingAmbientColor { get { return System.Drawing.Color.FromArgb(m_shadingAmbientColor); } set { m_shadingAmbientColor = value.ToArgb(); } }


        [Browsable(true), Category("UI")]
        [Description("Synchronize render buffer swaps with the monitor's refresh rate (vertical retrace). Change active only after program restart.")]
        public bool VSync
        {
            get { return vSync; }
            set { vSync = value; }
        }
        private Microsoft.DirectX.Direct3D.FillMode _fillmode = FillMode.Solid;
        public Microsoft.DirectX.Direct3D.FillMode FillMode
        {
            get { return this._fillmode; }
            set { this._fillmode = value; }
        }
        [Browsable(true), Category("3D settings")]
        [Description("Allows use of the Pure Device, which allows faster rendering, but might break older World Wind plugins that need some updating.  Requires restart of application.")]
        public bool AllowPureDevice
        {
            get
            {
                return m_AllowPureDevice;
            }
            set
            {
                m_AllowPureDevice = value;
            }
        }
        private Format textureFormat = Format.Dxt3;
        [Browsable(true), Category("3D settings")]
        [Description("In-memory texture format.  Also used for converted files on disk when image conversion is enabled.")]
        public Format TextureFormat
        {
            get
            {
                //	return Format.Dxt3;
                return textureFormat;
            }
            set
            {
                textureFormat = value;
            }
        }

        #region Units
        private Units m_displayUnits = Units.Metric;
        [Browsable(true), Category("Units")]
        [Description("The target display units for measurements.")]
        public Units DisplayUnits
        {
            get
            {
                return m_displayUnits;
            }
            set
            {
                m_displayUnits = value;
            }
        }
        #endregion
        #region Camera




        internal Projection _projection = Projection.Perspective;
        internal bool cameraIsPointGoto = true;
        internal bool cameraHasInertia = true;
        internal bool cameraSmooth = true;
        internal bool cameraHasMomentum = false;
        internal bool cameraTwistLock = true;
        internal bool cameraBankLock = false;
        internal float cameraSlerpStandard = 0.35f;
        internal float cameraSlerpInertia = 0.25f;

        // Set to either Inertia or Standard slerp value
        internal float cameraSlerpPercentage = 0.25f;

        internal Angle cameraFov = Angle.FromRadians(Math.PI * 0.25f);
        internal Angle cameraFovMin = Angle.FromDegrees(5);
        internal Angle cameraFovMax = Angle.FromDegrees(150);
        internal float cameraZoomStepFactor = 0.015f;
        internal float cameraZoomAcceleration = 10f;
        internal float cameraZoomAnalogFactor = 1f;
        internal float cameraZoomStepKeyboard = 0.15f;
        internal float cameraRotationSpeed = 3.5f;


        public Projection Project
        {
            get { return this._projection; }
            set { this._projection = value; }
        }

        [Browsable(true), Category("Camera")]
        public bool CameraIsPointGoto
        {
            get { return cameraIsPointGoto; }
            set { cameraIsPointGoto = value; }
        }

        [Browsable(true), Category("Camera")]
        [Description("Smooth camera movement.")]
        public bool CameraSmooth
        {
            get { return cameraSmooth; }
            set { cameraSmooth = value; }
        }

        [Browsable(true), Category("Camera")]
        [Description("See CameraSlerp settings for responsiveness adjustment.")]
        public bool CameraHasInertia
        {
            get { return cameraHasInertia; }
            set
            {
                cameraHasInertia = value;
                cameraSlerpPercentage = cameraHasInertia ? cameraSlerpInertia : cameraSlerpStandard;
            }
        }

        [Browsable(true), Category("Camera")]
        public bool CameraHasMomentum
        {
            get { return cameraHasMomentum; }
            set { cameraHasMomentum = value; }
        }

        [Browsable(true), Category("Camera")]
        public bool CameraTwistLock
        {
            get { return cameraTwistLock; }
            set { cameraTwistLock = value; }
        }

        [Browsable(true), Category("Camera")]
        public bool CameraBankLock
        {
            get { return cameraBankLock; }
            set { cameraBankLock = value; }
        }

        [Browsable(true), Category("Camera")]
        [Description("Responsiveness of movement when inertia is enabled.")]
        public float CameraSlerpInertia
        {
            get { return cameraSlerpInertia; }
            set
            {
                cameraSlerpInertia = value;
                if (cameraHasInertia)
                    cameraSlerpPercentage = cameraSlerpInertia;
            }
        }

        [Browsable(true), Category("Camera")]
        [Description("Responsiveness of movement when inertia is disabled.")]
        public float CameraSlerpStandard
        {
            get { return cameraSlerpStandard; }
            set
            {
                cameraSlerpStandard = value;
                if (!cameraHasInertia)
                    cameraSlerpPercentage = cameraSlerpStandard;
            }
        }

        [Browsable(true), Category("Camera")]
        public Angle CameraFov
        {
            get { return cameraFov; }
            set { cameraFov = value; }
        }

        [Browsable(true), Category("Camera")]
        public Angle CameraFovMin
        {
            get { return cameraFovMin; }
            set { cameraFovMin = value; }
        }

        [Browsable(true), Category("Camera")]
        public Angle CameraFovMax
        {
            get { return cameraFovMax; }
            set { cameraFovMax = value; }
        }

        [Browsable(true), Category("Camera")]
        public float CameraZoomStepFactor
        {
            get { return cameraZoomStepFactor; }
            set
            {
                const float maxValue = 0.3f;
                const float minValue = 1e-4f;

                if (value >= maxValue)
                    value = maxValue;
                if (value <= minValue)
                    value = minValue;
                cameraZoomStepFactor = value;
            }
        }

        [Browsable(true), Category("Camera")]
        public float CameraZoomAcceleration
        {
            get { return cameraZoomAcceleration; }
            set
            {
                const float maxValue = 50f;
                const float minValue = 1f;

                if (value >= maxValue)
                    value = maxValue;
                if (value <= minValue)
                    value = minValue;

                cameraZoomAcceleration = value;
            }
        }

        [Browsable(true), Category("Camera")]
        [Description("Analog zoom factor (Mouse LMB+RMB)")]
        public float CameraZoomAnalogFactor
        {
            get { return cameraZoomAnalogFactor; }
            set { cameraZoomAnalogFactor = value; }
        }

        [Browsable(true), Category("Camera")]
        public float CameraZoomStepKeyboard
        {
            get { return cameraZoomStepKeyboard; }
            set
            {
                const float maxValue = 0.3f;
                const float minValue = 1e-4f;

                if (value >= maxValue)
                    value = maxValue;
                if (value <= minValue)
                    value = minValue;

                cameraZoomStepKeyboard = value;
            }
        }

        [Browsable(true), Category("Camera")]
        public float CameraRotationSpeed
        {
            get { return cameraRotationSpeed; }
            set { cameraRotationSpeed = value; }
        }

        #endregion

        #region UI
        internal int widgetBackgroundColor = Color.FromArgb(0, 0, 0, 255).ToArgb();
        [Browsable(true), Category("UI")]
        [Description("Widget background color.")]
        public Color WidgetBackgroundColor
        {
            get { return Color.FromArgb(widgetBackgroundColor); }
            set { widgetBackgroundColor = value.ToArgb(); }
        }

        /// <summary>
        /// The color of the latitude/longitude grid
        /// </summary>
        public int latLonLinesColor = System.Drawing.Color.FromArgb(100, Color.White).ToArgb();

        /// <summary>
        /// The color of the equator latitude line
        /// </summary>
        public int equatorLineColor = System.Drawing.Color.Red.ToArgb();

        /// <summary>
        /// Display the tropic of capricorn/cancer lines
        /// </summary>
        internal bool showTropicLines = true;

        /// <summary>
        /// The color of the latitude/longitude grid
        /// </summary>
        public int tropicLinesColor = System.Drawing.Color.Yellow.ToArgb();
        /// <summary>
        /// 经纬网格的显示颜色
        /// </summary>
        /// The color of the latitude/longitude grid.
        [XmlIgnore]
        [Browsable(true), Category("Grid Lines")]
        [Description("经纬网格的显示颜色")]
        public Color LatLonLinesColor
        {
            get { return Color.FromArgb(latLonLinesColor); }
            set { latLonLinesColor = value.ToArgb(); }
        }
        /// <summary>
        /// 赤道线的显示颜色
        /// </summary>
        /// The color of the equator latitude line.
        [XmlIgnore]
        [Browsable(true), Category("Grid Lines")]
        [Description("赤道线的显示颜色 ")]
        public Color EquatorLineColor
        {
            get { return Color.FromArgb(equatorLineColor); }
            set { equatorLineColor = value.ToArgb(); }
        }
        /// <summary>
        /// 显示回归线
        /// </summary>
        /// Display the tropic latitude lines.
        [Browsable(true), Category("Grid Lines")]
        [Description("显示回归线 ")]
        public bool ShowTropicLines
        {
            get { return showTropicLines; }
            set { showTropicLines = value; }
        }
        /// <summary>
        /// 回归线的显示颜色
        /// </summary>
        /// The color of the latitude/longitude grid
        [XmlIgnore]
        [Browsable(true), Category("Grid Lines")]
        [Description("回归线的显示颜色 ")]
        public Color TropicLinesColor
        {
            get { return Color.FromArgb(tropicLinesColor); }
            set { tropicLinesColor = value.ToArgb(); }
        }

        private bool isshowLatlongLines = true;

        /// <summary>
        /// 显示
        /// </summary>
        /// Display the tropic latitude lines.
        [Browsable(true), Category("Grid Lines")]
        [Description("显示 ")]
        public bool ShowLatLongLines
        {
            get { return isshowLatlongLines; }
            set { isshowLatlongLines = value; }
        }
        #endregion

        #region Toolbar
        private Color toolBarBackColor = Color.FromArgb(100, Color.Black);

        public Color ToolBarBackColor
        {
            get { return this.toolBarBackColor; }
            set { this.toolBarBackColor = value; }
        }

        private bool showToolbar = true;

        public bool ShowToolBar { get { return this.showToolbar; } set { this.showToolbar = value; } }

        [Browsable(true), Category("UI")]
        [Description("Where the toolbar is anchored.")]
        public MenuAnchor ToolbarAnchor
        {
            get { return toolbarAnchor; }
            set { toolbarAnchor = value; }
        }

        /// <summary>
        /// Where the tool bar should be anchored
        /// </summary>
        internal MenuAnchor toolbarAnchor = MenuAnchor.Bottom;
        #endregion

        #region DownLoad
        private bool showDownloadRectangles = true;

        public bool ShowDownloadRectangles { get { return this.showDownloadRectangles; } set { this.showDownloadRectangles = value; } }

        private Color downloadProgressColor = Color.Green;
        public Color DownloadProgressColor { get { return this.downloadProgressColor; } set { this.downloadProgressColor = value; } }


        private Color downloadTerrainRectangleColor = Color.Green;
        public Color DownloadTerrainRectangleColor { get { return this.downloadTerrainRectangleColor; } set { this.downloadTerrainRectangleColor = value; } }

        #endregion
        public override void Load(string path)
        {
            base.Load(path);
        }
        public override void Save(string path)
        {
            base.Save(path);
        }
    }
}