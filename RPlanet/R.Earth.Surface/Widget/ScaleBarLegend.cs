using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using R.Earth.OverLayer;
using R.Earth.OverLayer.Element;
using R.Earth.OverLayer.Interface;

namespace R.Earth.Plugin.Widget
{
    public class ScaleBarWidget : IWidget
    {
        private System.Drawing.Point m_Location = new System.Drawing.Point(0, 0);
        private System.Drawing.Size m_Size = new System.Drawing.Size(0, 20);
        private bool m_Visible = true;
        private bool m_Enabled = true;
        private IWidget m_ParentWidget = null;
        private object m_Tag = null;
        private System.Drawing.Color m_ForeColor = System.Drawing.Color.White;
        private string m_Name = "";
        private System.Drawing.Font m_localFont = null;
        private Font m_drawingFont = null;

        /// <summary>
        /// CountHeight property value
        /// </summary>
        protected bool m_countHeight = true;

        /// <summary>
        /// CountWidth property value
        /// </summary>
        protected bool m_countWidth = true;

        public ScaleBarWidget()
        {

        }

        #region Properties
        public System.Drawing.Font Font
        {
            get { return m_localFont; }
            set
            {
                m_localFont = value;
                if (m_drawingFont != null)
                {
                    m_drawingFont.Dispose();
                    m_drawingFont = new Font(DrawArgs.Instance.device, m_localFont);
                }
            }
        }
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }
        public System.Drawing.Color ForeColor
        {
            get
            {
                return m_ForeColor;
            }
            set
            {
                m_ForeColor = value;
            }
        }
        #endregion

        #region IWidget Members

        public IWidget ParentWidget
        {
            get
            {
                return m_ParentWidget;
            }
            set
            {
                m_ParentWidget = value;
            }
        }

        public bool Visible
        {
            get
            {
                return m_Visible;
            }
            set
            {
                m_Visible = value;
            }
        }

        public object Tag
        {
            get
            {
                return m_Tag;
            }
            set
            {
                m_Tag = value;
            }
        }

        public IWidgetCollection ChildWidgets
        {
            get
            {
                return null;
            }
            set
            {

            }
        }

        public System.Drawing.Size ClientSize
        {
            get
            {
                return m_Size;
            }
            set
            {
                m_Size = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
            }
        }

        public System.Drawing.Point ClientLocation
        {
            get
            {
                return m_Location;
            }
            set
            {
                m_Location = value;
            }
        }

        public System.Drawing.Point AbsoluteLocation
        {
            get
            {
                if (m_ParentWidget != null)
                {
                    return new System.Drawing.Point(
                        m_Location.X + m_ParentWidget.ClientLocation.X,
                        m_Location.Y + m_ParentWidget.ClientLocation.Y);

                }
                else
                {
                    return m_Location;
                }
            }
        }


        /// New IWidget properties

        /// <summary>
        /// Location of this widget relative to the client area of the parent
        /// </summary>
        public System.Drawing.Point Location
        {
            get { return m_Location; }
            set { m_Location = value; }
        }

        /// <summary>
        /// Size of widget in pixels
        /// </summary>
        public System.Drawing.Size WidgetSize
        {
            get { return m_Size; }
            set { m_Size = value; }
        }


        /// <summary>
        /// Whether this widget should count for height calculations - HACK until we do real layout
        /// </summary>
        public bool CountHeight
        {
            get { return m_countHeight; }
            set { m_countHeight = value; }
        }


        /// <summary>
        /// Whether this widget should count for width calculations - HACK until we do real layout
        /// </summary>
        public bool CountWidth
        {
            get { return m_countWidth; }
            set { m_countWidth = value; }
        }

        public void Initialize(DrawArgs drawArgs)
        {
        }


        public void Render(DrawArgs drawArgs)
        {
            try
            {
                if (m_Visible)
                {
                    if (m_localFont != null && m_drawingFont == null)
                    {
                        m_drawingFont = new Font(drawArgs.device, m_localFont);
                    }

                    DrawTextFormat drawTextFormat = DrawTextFormat.Center;

                    Angle startLatitude = Angle.NaN;
                    Angle startLongitude = Angle.NaN;

                    Angle endLatitude = Angle.NaN;
                    Angle endLongitude = Angle.NaN;

                    string displayString = "";

                    drawArgs.WorldCamera.PickingRayIntersection(
                        AbsoluteLocation.X,
                        AbsoluteLocation.Y + ClientSize.Height,
                        out startLatitude,
                        out startLongitude);

                    drawArgs.WorldCamera.PickingRayIntersection(
                        AbsoluteLocation.X + ClientSize.Width,
                        AbsoluteLocation.Y + ClientSize.Height,
                        out endLatitude,
                        out endLongitude);

                    if (startLatitude == Angle.NaN ||
                        startLongitude == Angle.NaN ||
                        endLatitude == Angle.NaN ||
                        endLongitude == Angle.NaN)
                    {

                        //displayString = "Out of Range";

                    }
                    else
                    {
                        double distance = getDistance(startLatitude, startLongitude, endLatitude, endLongitude, World.EquatorialRadius);
                        if (distance > double.MinValue && distance < double.MaxValue)
                        {
                            displayString = GetDisplayString(distance);
                        }
                    }

                    drawArgs.device.TextureState[0].ColorOperation = TextureOperation.SelectArg1;
                    drawArgs.device.TextureState[0].ColorArgument1 = TextureArgument.Diffuse;
                    drawArgs.device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1;
                    drawArgs.device.TextureState[0].AlphaArgument1 = TextureArgument.Diffuse;


                    renderBackbone(drawArgs);

                    if (m_drawingFont == null)
                    {
                        drawArgs.DefauleFont.DrawText(
                            null,
                            displayString,
                            new System.Drawing.Rectangle(AbsoluteLocation.X, AbsoluteLocation.Y, m_Size.Width, m_Size.Height),
                            drawTextFormat,
                            m_ForeColor);
                    }
                    else
                    {
                        drawArgs.DefauleFont.DrawText(
                            null,
                            displayString,
                            new System.Drawing.Rectangle(AbsoluteLocation.X, AbsoluteLocation.Y, m_Size.Width, m_Size.Height),
                            drawTextFormat,
                            m_ForeColor);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        Vector2[] m_vectors = new Vector2[2];

        private double getDistance(Angle startLatitude, Angle startLongitude, Angle endLatitude, Angle endLongitude, double radius)
        {

            Angle angularDistance = SMath.ApproxAngularDistance(startLatitude, startLongitude, endLatitude, endLongitude);
            int steps = 4;
            double distance = 0;
            Vector3d d1 = SMath.SphericalToCartesianV3D(startLatitude.Degrees, startLongitude.Degrees, radius);

            for (int i = 1; i < steps; i++)
            {
                Angle curLat;
                Angle curLon;

                SMath.IntermediateGCPoint((float)i / (float)(steps - 1), startLatitude, startLongitude, endLatitude, endLongitude, angularDistance, out curLat, out curLon);

                Vector3d d2 = SMath.SphericalToCartesianV3D(curLat.Degrees, curLon.Degrees, radius);
                Vector3d segment = d2 - d1;
                distance += segment.Length;
                d1 = d2;
            }

            return distance;
        }

        private void renderBackbone(DrawArgs drawArgs)
        {
            int backboneWidth = 3;
            int centerHeight = ClientSize.Height / 2;
            int quarterHeight = ClientSize.Height / 3;
            int eighthHeight = ClientSize.Height / 4;

            WidgetUtilities.DrawBox(AbsoluteLocation.X, AbsoluteLocation.Y - backboneWidth + ClientSize.Height, ClientSize.Width, backboneWidth, 0.0f, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - centerHeight;
            m_vectors[1].X = AbsoluteLocation.X;
            m_vectors[1].Y = AbsoluteLocation.Y + ClientSize.Height;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X + ClientSize.Width / 2;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - centerHeight;
            m_vectors[1].X = AbsoluteLocation.X + ClientSize.Width / 2;
            m_vectors[1].Y = AbsoluteLocation.Y + ClientSize.Height;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X + ClientSize.Width;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - centerHeight;
            m_vectors[1].X = AbsoluteLocation.X + ClientSize.Width;
            m_vectors[1].Y = AbsoluteLocation.Y + ClientSize.Height;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X + ClientSize.Width / 4;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - quarterHeight;
            m_vectors[1].X = AbsoluteLocation.X + ClientSize.Width / 4;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X + 3 * ClientSize.Width / 4;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - quarterHeight;
            m_vectors[1].X = AbsoluteLocation.X + 3 * ClientSize.Width / 4;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X + 1 * ClientSize.Width / 8;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - eighthHeight;
            m_vectors[1].X = AbsoluteLocation.X + 1 * ClientSize.Width / 8;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X + 3 * ClientSize.Width / 8;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - eighthHeight;
            m_vectors[1].X = AbsoluteLocation.X + 3 * ClientSize.Width / 8;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X + 5 * ClientSize.Width / 8;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - eighthHeight;
            m_vectors[1].X = AbsoluteLocation.X + 5 * ClientSize.Width / 8;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);

            m_vectors[0].X = AbsoluteLocation.X + 7 * ClientSize.Width / 8;
            m_vectors[0].Y = AbsoluteLocation.Y + ClientSize.Height - eighthHeight;
            m_vectors[1].X = AbsoluteLocation.X + 7 * ClientSize.Width / 8;
            WidgetUtilities.DrawLine(m_vectors, m_ForeColor.ToArgb(), drawArgs.device);
        }

        private string GetDisplayString(double distance)
        {
            if (World.Settings.DisplayUnits == Units.Metric)
            {
                if (distance >= 1000)
                {
                    return string.Format("{0:,.0} km", distance / 1000);
                }
                else
                {
                    return string.Format("{0:f0} m", distance);
                }
            }
            else
            {
                double feetPerMeter = 3.2808399;
                double feetPerMile = 5280;

                distance *= feetPerMeter;

                if (distance >= feetPerMile)
                {
                    return string.Format("{0:,.0} miles", distance / feetPerMile);
                }
                else
                {
                    return string.Format("{0:f0} ft", distance);
                }
            }
        }

        #endregion
    }
}
