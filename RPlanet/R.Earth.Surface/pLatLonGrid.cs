using System;
using Microsoft.DirectX.Direct3D;

namespace R.Earth.Plugin
{
 
    public class LatLonGrid : GeoLayer
    {

        /// <summary>
        /// Planet radius (constant)
        /// </summary>
        public double WorldRadius;

        /// <summary>
        /// Grid line radius (varies, >= world radius
        /// </summary>
        protected double radius;

        /// <summary>
        /// Current planet == Earth?
        /// </summary>
        public bool IsEarth;

        /// <summary>
        /// Lowest visible longitude
        /// </summary>
        public int MinVisibleLongitude;

        /// <summary>
        /// Highest visible longitude
        /// </summary>
        public int MaxVisibleLongitude;

        /// <summary>
        /// Lowest visible Latitude
        /// </summary>
        public int MinVisibleLatitude;

        /// <summary>
        /// Highest visible Latitude
        /// </summary>
        public int MaxVisibleLatitude;

        /// <summary>
        /// Interval in degrees between visible latitudes
        /// </summary>
        public int LongitudeInterval;

        /// <summary>
        /// Interval in degrees between visible longitudes
        /// </summary>
        public int LatitudeInterval;

        /// <summary>
        /// The number of visible longitude lines
        /// </summary>
        public int LongitudePointCount;

        /// <summary>
        /// The number of visible latitude lines
        /// </summary>
        public int LatitudePointCount;

        /// <summary>
        /// Temporary buffer used for rendering  lines
        /// </summary>
        protected CustomVertex.PositionColored[] lineVertices;

        /// <summary>
        /// Z Buffer enabled (depending on distance)
        /// </summary>
        protected bool useZBuffer;



        public LatLonGrid(string name)
            : base(name)
        {
            this.WorldRadius = World.EquatorialRadius;
            this.IsEarth = true;
        }

        public override void OnFrameMove(DrawArgs drawArgs)
        {
            if (!this.IsInitialized)
            {
                this.OnInitialize(drawArgs);
            }
        }
        public override void OnInitialize(DrawArgs drawArgs)
        {
            base.OnInitialize(drawArgs);
        }
        public override void OnRender(DrawArgs drawArgs)
        {
            if (!World.Settings.ShowLatLongLines)
            {
                return;
            }
            bool light = drawArgs.device.RenderState.Lighting;
            drawArgs.device.RenderState.Lighting = false;
 
            ComputeGridValues(drawArgs);

            float offsetDegrees = (float)drawArgs.WorldCamera.TrueViewRange.Degrees / 6;

            if (!useZBuffer)
                drawArgs.device.RenderState.ZBufferEnable = false;

            drawArgs.device.TextureState[0].ColorOperation = TextureOperation.Disable;
            drawArgs.device.VertexFormat = CustomVertex.PositionColored.Format;

            // Draw longitudes
            for (float longitude = MinVisibleLongitude; longitude < MaxVisibleLongitude; longitude += LongitudeInterval)
            {
                // Draw longitude lines
                int vertexIndex = 0;
                for (float latitude = MinVisibleLatitude; latitude <= MaxVisibleLatitude; latitude += LatitudeInterval)
                {   //TODO JHJ 10 度线做了内插 9个点
                    if (LongitudeInterval == 10)
                    {
                        for(int i = 0; i < 10 ; i++)
                        {
                            Vector3d p = SMath.SphericalToCartesianV3D(latitude+i, longitude, radius);
                            lineVertices[vertexIndex].X = (float)p.X;
                            lineVertices[vertexIndex].Y = (float)p.Y;
                            lineVertices[vertexIndex].Z = (float)p.Z;
                            lineVertices[vertexIndex].Color = World.Settings.LatLonLinesColor.ToArgb();
                            vertexIndex++;
                        }
                        continue;
                    }
                    Vector3d pointXyz = SMath.SphericalToCartesianV3D(latitude, longitude, radius);
                    lineVertices[vertexIndex].X = (float)pointXyz.X;
                    lineVertices[vertexIndex].Y = (float)pointXyz.Y;
                    lineVertices[vertexIndex].Z = (float)pointXyz.Z;
                    lineVertices[vertexIndex].Color = World.Settings.LatLonLinesColor.ToArgb();
                    vertexIndex++;
                }
                if (LongitudeInterval == 10)
                {
                    drawArgs.device.DrawUserPrimitives(PrimitiveType.LineStrip, LatitudePointCount * 10 - 1, lineVertices);
                }
                else
                {
                    drawArgs.device.DrawUserPrimitives(PrimitiveType.LineStrip, LatitudePointCount - 1, lineVertices);
                }

                // Draw longitude label
                float lat = (float)(drawArgs.WorldCamera.Latitude).Degrees;
                if (lat > 70)
                    lat = 70;
                Vector3d v = SMath.SphericalToCartesianV3D(lat, (float)longitude, radius);
                if (drawArgs.WorldCamera.ViewFrustum.ContainsPoint(v))
                {
                    // Make sure longitude is in -180 .. 180 range
                    int longitudeRanged = (int)longitude;
                    if (longitudeRanged <= -180)
                        longitudeRanged += 360;
                    else if (longitudeRanged > 180)
                        longitudeRanged -= 360;

                    string s = Math.Abs(longitudeRanged).ToString();
                    if (longitudeRanged < 0)
                        s += "W";
                    else if (longitudeRanged > 0 && longitudeRanged < 180)
                        s += "E";

                    v = drawArgs.WorldCamera.Project(v);
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)v.X + 2, (int)v.Y, 110, 20);
                    DrawArgs.Instance.DrawText(null, s, rect, DrawTextFormat.Top | DrawTextFormat.Left, World.Settings.latLonLinesColor);
                }
            }

            // Draw latitudes
            for (float latitude = MinVisibleLatitude; latitude <= MaxVisibleLatitude; latitude += LatitudeInterval)
            {
                // Draw latitude label
                float longitude = (float)(drawArgs.WorldCamera.Longitude).Degrees + offsetDegrees;

                Vector3d v = SMath.SphericalToCartesianV3D(latitude, longitude, radius);
                if (drawArgs.WorldCamera.ViewFrustum.ContainsPoint(v))
                {
                    v = drawArgs.WorldCamera.Project(v);
                    float latLabel = latitude;
                    if (latLabel > 90)
                        latLabel = 180 - latLabel;
                    else if (latLabel < -90)
                        latLabel = -180 - latLabel;
                    string s = ((int)Math.Abs(latLabel)).ToString();
                    if (latLabel > 0)
                        s += "N";
                    else if (latLabel < 0)
                        s += "S";
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)v.X, (int)v.Y, 100, 100);
                    DrawArgs.Instance.DrawText(null, s, rect, DrawTextFormat.Left | DrawTextFormat.Top, World.Settings.latLonLinesColor);
                }

                // Draw latitude line
                int vertexIndex = 0;
                for (longitude = MinVisibleLongitude; longitude <= MaxVisibleLongitude; longitude += LongitudeInterval)
                {
                    if (LongitudeInterval == 10)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector3d p = SMath.SphericalToCartesianV3D(latitude, longitude + i, radius);
                            lineVertices[vertexIndex].X = (float)p.X;
                            lineVertices[vertexIndex].Y = (float)p.Y;
                            lineVertices[vertexIndex].Z = (float)p.Z;

                            if (latitude == 0)
                                lineVertices[vertexIndex].Color = World.Settings.EquatorLineColor.ToArgb();
                            else
                                lineVertices[vertexIndex].Color = World.Settings.LatLonLinesColor.ToArgb();

                            vertexIndex++;
                        }
                        continue;
                    }
                    Vector3d pointXyz = SMath.SphericalToCartesianV3D(latitude, longitude, radius);
                    lineVertices[vertexIndex].X = (float)pointXyz.X;
                    lineVertices[vertexIndex].Y = (float)pointXyz.Y;
                    lineVertices[vertexIndex].Z = (float)pointXyz.Z;

                    if (latitude == 0)
                        lineVertices[vertexIndex].Color = World.Settings.EquatorLineColor.ToArgb();
                    else
                        lineVertices[vertexIndex].Color = World.Settings.LatLonLinesColor.ToArgb();

                    vertexIndex++;
                }
                if (LongitudeInterval == 10)
                {
                    drawArgs.device.DrawUserPrimitives(PrimitiveType.LineStrip, LongitudePointCount * 10 - 1, lineVertices);
                }
                else
                {
                    drawArgs.device.DrawUserPrimitives(PrimitiveType.LineStrip, LongitudePointCount - 1, lineVertices);
                }
                
            }

            if (World.Settings.ShowTropicLines && IsEarth)
                RenderTropicLines(drawArgs);

            // Restore state
            if (!useZBuffer)
                // Reset Z buffer setting
                drawArgs.device.RenderState.ZBufferEnable = true;

            drawArgs.device.RenderState.Lighting = light;
            
        }

        /// <summary>
        /// Draw Tropic of Cancer, Tropic of Capricorn, Arctic and Antarctic lines 
        /// </summary>
        void RenderTropicLines(DrawArgs drawArgs)
        {
            //RenderTropicLine(drawArgs, 23.439444f, "Tropic Of Cancer");
            //RenderTropicLine(drawArgs, -23.439444f, "Tropic Of Capricorn");
            //RenderTropicLine(drawArgs, 66.560556f, "Arctic Circle");
            //RenderTropicLine(drawArgs, -66.560556f, "Antarctic Circle");

            RenderTropicLine(drawArgs, 23.439444f, "北回归线");
            RenderTropicLine(drawArgs, -23.439444f, "南回归线");
            RenderTropicLine(drawArgs, 66.560556f, "北极圈");
            RenderTropicLine(drawArgs, -66.560556f, "南极圈");
        }

        /// <summary>
        /// Draws a tropic line at specified latitude with specified label
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        void RenderTropicLine(DrawArgs drawArgs, float latitude, string label)
        {
            int vertexIndex = 0;
            for (float longitude = MinVisibleLongitude; longitude <= MaxVisibleLongitude; longitude = longitude + LongitudeInterval)
            {
                if (LongitudeInterval == 10)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector3d p = SMath.SphericalToCartesianV3D(latitude, longitude + i, radius);
                        lineVertices[vertexIndex].X = (float)p.X;
                        lineVertices[vertexIndex].Y = (float)p.Y;
                        lineVertices[vertexIndex].Z = (float)p.Z;

                        lineVertices[vertexIndex].Color = World.Settings.TropicLinesColor.ToArgb();

                        vertexIndex++;
                    }
                    continue;
                }
                Vector3d pointXyz = SMath.SphericalToCartesianV3D(latitude, longitude, radius);

                lineVertices[vertexIndex].X = (float)pointXyz.X;
                lineVertices[vertexIndex].Y = (float)pointXyz.Y;
                lineVertices[vertexIndex].Z = (float)pointXyz.Z;
                lineVertices[vertexIndex].Color = World.Settings.TropicLinesColor.ToArgb();
                vertexIndex++;
            }
            //TODO JHJ 10度线做了差值  9点
            if (LongitudeInterval == 10)
            {
                drawArgs.device.DrawUserPrimitives(PrimitiveType.LineStrip, LongitudePointCount * 10 - 1, lineVertices);
            }
            else
            {
                drawArgs.device.DrawUserPrimitives(PrimitiveType.LineStrip, LongitudePointCount - 1, lineVertices);
            }
            

            Vector3d t1 = SMath.SphericalToCartesianV3D(latitude,
                drawArgs.WorldCamera.Longitude.Degrees - drawArgs.WorldCamera.TrueViewRange.Degrees * 0.3 * 0.5, radius);
            if (drawArgs.WorldCamera.ViewFrustum.ContainsPoint(t1))
            {
                t1 = drawArgs.WorldCamera.Project(t1);
                DrawArgs.Instance.DrawText(null, label, new System.Drawing.Rectangle((int)t1.X, (int)t1.Y, 640, 480), DrawTextFormat.Left, World.Settings.tropicLinesColor);
            }
        }

        /// <summary>
        /// Recalculates the grid bounds + interval values
        /// </summary>
        public void ComputeGridValues(DrawArgs drawArgs)
        {
            double vr = drawArgs.WorldCamera.TrueViewRange.Radians;

            // Compensate for closer grid towards poles
            vr *= 1 + Math.Abs(Math.Sin(drawArgs.WorldCamera.Latitude.Radians));

            if (vr < 0.17)
                LatitudeInterval = 1;
            else if (vr < 0.6)
                LatitudeInterval = 2;
            else if (vr < 1.0)
                LatitudeInterval = 5;
            else
                LatitudeInterval = 10;

            LongitudeInterval = LatitudeInterval;

            if (drawArgs.WorldCamera.ViewFrustum.ContainsPoint(SMath.SphericalToCartesianV3D(90, 0, radius)) ||
                drawArgs.WorldCamera.ViewFrustum.ContainsPoint(SMath.SphericalToCartesianV3D(-90, 0, radius)))
            {
                // Pole visible, 10 degree longitude spacing forced
                LongitudeInterval = 10;
            }

            MinVisibleLongitude = LongitudeInterval >= 10 ? -180 : (int)drawArgs.WorldCamera.Longitude.Degrees / LongitudeInterval * LongitudeInterval - 18 * LongitudeInterval;
            MaxVisibleLongitude = LongitudeInterval >= 10 ? 180 : (int)drawArgs.WorldCamera.Longitude.Degrees / LongitudeInterval * LongitudeInterval + 18 * LongitudeInterval;
            MinVisibleLatitude = (int)drawArgs.WorldCamera.Latitude.Degrees / LatitudeInterval * LatitudeInterval - 9 * LatitudeInterval;
            MaxVisibleLatitude = (int)drawArgs.WorldCamera.Latitude.Degrees / LatitudeInterval * LatitudeInterval + 9 * LatitudeInterval;

            if (MaxVisibleLatitude - MinVisibleLatitude >= 180 || LongitudeInterval == 10)
            {
                MinVisibleLatitude = -90;
                MaxVisibleLatitude = 90;
            }
            LongitudePointCount = (MaxVisibleLongitude - MinVisibleLongitude) / LongitudeInterval + 1;
            LatitudePointCount = (MaxVisibleLatitude - MinVisibleLatitude) / LatitudeInterval + 1;
            int vertexPointCount = Math.Max(LatitudePointCount, LongitudePointCount);
            if (lineVertices == null || vertexPointCount > lineVertices.Length)
            {
                if (LongitudeInterval == 10)
                {
                    lineVertices = new CustomVertex.PositionColored[Math.Max(LatitudePointCount, LongitudePointCount) * 10];
                }
                else
                {
                    lineVertices = new CustomVertex.PositionColored[Math.Max(LatitudePointCount, LongitudePointCount)];
                }
            }
            

            radius = WorldRadius;
            if (drawArgs.WorldCamera.Altitude < 0.10f * WorldRadius)
                useZBuffer = false;
            else
            {
                useZBuffer = true;
                double bRadius = WorldRadius * 1.01f;
                double nRadius = WorldRadius + 0.015f * drawArgs.WorldCamera.Altitude;

                radius = Math.Min(nRadius, bRadius);
            }
        }

        public override void UpdateMesh(DrawArgs drawArgs)
        {

        }

        public override void OnRenderOrtho(DrawArgs drawArgs)
        {
            if (!World.Settings.ShowLatLongLines)
            {
                return;
            }
            //bool light = drawArgs.device.RenderState.Lighting;
            //drawArgs.device.RenderState.Lighting = false;

            ComputeGridValues(drawArgs);

            float offsetDegrees = (float)drawArgs.WorldCamera.TrueViewRange.Degrees / 6;

            if (!useZBuffer)
                drawArgs.device.RenderState.ZBufferEnable = false;

            drawArgs.device.TextureState[0].ColorOperation = TextureOperation.Disable;
            drawArgs.device.VertexFormat = CustomVertex.PositionColored.Format;

            // Draw longitudes
            for (float longitude = MinVisibleLongitude; longitude < MaxVisibleLongitude; longitude += LongitudeInterval)
            {
                // Draw longitude lines
                int vertexIndex = 0;
                for (float latitude = MinVisibleLatitude; latitude <= MaxVisibleLatitude; latitude += LatitudeInterval)
                {
                    Vector3d pointXyz = SMath.SphericalToCartesianV3D(latitude, longitude, radius);
                    lineVertices[vertexIndex].X = (float)pointXyz.X;
                    lineVertices[vertexIndex].Y = (float)pointXyz.Y;
                    lineVertices[vertexIndex].Z = (float)pointXyz.Z;
                    lineVertices[vertexIndex].Color = World.Settings.latLonLinesColor;
                    vertexIndex++;
                }
                drawArgs.device.DrawUserPrimitives(PrimitiveType.LineStrip, LatitudePointCount - 1, lineVertices);

                // Draw longitude label
                float lat = (float)(drawArgs.WorldCamera.Latitude).Degrees;
                if (lat > 70)
                    lat = 70;
                Vector3d v = SMath.SphericalToCartesianV3D(lat, (float)longitude, radius);
                if (drawArgs.WorldCamera.ViewFrustum.ContainsPoint(v))
                {
                    // Make sure longitude is in -180 .. 180 range
                    int longitudeRanged = (int)longitude;
                    if (longitudeRanged <= -180)
                        longitudeRanged += 360;
                    else if (longitudeRanged > 180)
                        longitudeRanged -= 360;

                    string s = Math.Abs(longitudeRanged).ToString();
                    if (longitudeRanged < 0)
                        s += "W";
                    else if (longitudeRanged > 0 && longitudeRanged < 180)
                        s += "E";

                    v = drawArgs.WorldCamera.Project(v);
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)v.X + 2, (int)v.Y, 110, 20);
                    DrawArgs.Instance.DrawText(null, s, rect, DrawTextFormat.Top | DrawTextFormat.Left, World.Settings.latLonLinesColor);
                }
            }

            // Draw latitudes
            for (float latitude = MinVisibleLatitude; latitude <= MaxVisibleLatitude; latitude += LatitudeInterval)
            {
                // Draw latitude label
                float longitude = (float)(drawArgs.WorldCamera.Longitude).Degrees + offsetDegrees;

                Vector3d v = SMath.SphericalToCartesianV3D(latitude, longitude, radius);
                if (drawArgs.WorldCamera.ViewFrustum.ContainsPoint(v))
                {
                    v = drawArgs.WorldCamera.Project(v);
                    float latLabel = latitude;
                    if (latLabel > 90)
                        latLabel = 180 - latLabel;
                    else if (latLabel < -90)
                        latLabel = -180 - latLabel;
                    string s = ((int)Math.Abs(latLabel)).ToString();
                    if (latLabel > 0)
                        s += "N";
                    else if (latLabel < 0)
                        s += "S";
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)v.X, (int)v.Y, 100, 100);
                    DrawArgs.Instance.DrawText(null, s, rect, DrawTextFormat.Left | DrawTextFormat.Top, World.Settings.latLonLinesColor);
                }

                // Draw latitude line
                int vertexIndex = 0;
                for (longitude = MinVisibleLongitude; longitude <= MaxVisibleLongitude; longitude += LongitudeInterval)
                {
                    Vector3d pointXyz = SMath.SphericalToCartesianV3D(latitude, longitude, radius);
                    lineVertices[vertexIndex].X = (float)pointXyz.X;
                    lineVertices[vertexIndex].Y = (float)pointXyz.Y;
                    lineVertices[vertexIndex].Z = (float)pointXyz.Z;

                    if (latitude == 0)
                        lineVertices[vertexIndex].Color = World.Settings.equatorLineColor;
                    else
                        lineVertices[vertexIndex].Color = World.Settings.latLonLinesColor;

                    vertexIndex++;
                }
                drawArgs.device.DrawUserPrimitives(PrimitiveType.LineStrip, LongitudePointCount - 1, lineVertices);
            }

            if (World.Settings.ShowTropicLines && IsEarth)
                RenderTropicLines(drawArgs);

            // Restore state
            if (!useZBuffer)
                // Reset Z buffer setting
                drawArgs.device.RenderState.ZBufferEnable = true;

            //drawArgs.device.RenderState.Lighting = light;
        }


        public override void Dispose()
        {

        }
    }
}