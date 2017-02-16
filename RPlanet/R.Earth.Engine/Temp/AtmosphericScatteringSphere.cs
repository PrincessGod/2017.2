using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using R.Earth;

namespace R.Earth.Scene
{
    public class AtmosphericScatteringSphere
    {
        public float m_radius;
        protected int m_numberSlices;
        protected int m_numberSections;

        public static float m_fInnerRadius = 0;
        public static float m_fOuterRadius = 0;
        public static int TilesHigh = 3;
        public static int TilesWide = 6;

        public void Init(float radius, int slices, int sections)
        {
            try
            {
                m_radius = radius;
                m_numberSlices = slices;
                m_numberSections = sections;

                Vector3d sunPosition = SunCalculator.GetGeocentricPosition(TimeKeeper.CurrentTimeUtc);
                Vector3 sunVector = new Vector3(
                    (float)-sunPosition.X,
                    (float)-sunPosition.Y,
                    (float)-sunPosition.Z);

                m_vLight = sunVector * 100000000f;
                m_vLightDirection = new Vector3(
                    m_vLight.X / m_vLight.Length(),
                    m_vLight.Y / m_vLight.Length(),
                    m_vLight.Z / m_vLight.Length()
                    );

                m_fScale = 1 / (m_fOuterRadius - m_fInnerRadius);

                m_meshList.Clear();

                double latRange = 180.0 / (double)TilesHigh;
                double lonRange = 360.0 / (double)TilesWide;

                int meshDensity = m_numberSlices / TilesHigh;

                for (int y = 0; y < TilesHigh; y++)
                {
                    for (int x = 0; x < TilesWide; x++)
                    {
                        MeshSubset mesh = new MeshSubset();
                        double north = y * latRange + latRange - 90;
                        double south = y * latRange - 90;

                        double west = x * lonRange - 180;
                        double east = x * lonRange + lonRange - 180;

                        mesh.Vertices = CreateMesh(south, north, west, east, meshDensity);
                        mesh.HigherResolutionVertices = CreateMesh(south, north, west, east, 2 * meshDensity);
                        mesh.BoundingBox = new BoundingBox((float)south, (float)north, (float)west, (float)east, (float)radius, (float)radius);
                        m_meshList.Add(mesh);
                    }
                }

                m_indices = computeIndices(meshDensity);
                m_indicesHighResolution = computeIndices(2 * meshDensity);

                m_nSamples = 4;		// Number of sample rays to use in integral equation
                m_Kr = 0.0025f;		// Rayleigh scattering constant
                m_Kr4PI = m_Kr * 4.0f * (float)Math.PI;
                m_Km = 0.0015f;		// Mie scattering constant
                m_Km4PI = m_Km * 4.0f * (float)Math.PI;
                m_ESun = 15.0f;		// Sun brightness constant
                m_g = -0.85f;		// The Mie phase asymmetry factor

                m_fWavelength[0] = 0.650f;		// 650 nm for red
                m_fWavelength[1] = 0.570f;		// 570 nm for green
                m_fWavelength[2] = 0.475f;		// 475 nm for blue
                m_fWavelength4[0] = (float)Math.Pow(m_fWavelength[0], 4.0f);
                m_fWavelength4[1] = (float)Math.Pow(m_fWavelength[1], 4.0f);
                m_fWavelength4[2] = (float)Math.Pow(m_fWavelength[2], 4.0f);

                m_fRayleighScaleDepth = 0.25f;
                m_fMieScaleDepth = 0.1f;

            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        public void Dispose()
        {
            active = false;
        }

        class MeshSubset
        {
            public CustomVertex.PositionColored[] Vertices = null;
            public CustomVertex.PositionColored[] HigherResolutionVertices = null;
            public BoundingBox BoundingBox = null;
        }
        System.Collections.Generic.List<MeshSubset> m_meshList = new System.Collections.Generic.List<MeshSubset>();

        Vector3 m_lastSunPosition = Vector3.Empty;

        System.Threading.Thread m_backgroundThread = null;
        bool active = false;
        System.DateTime m_lastOpticalUpdate = System.DateTime.MinValue;
        bool m_canDoShaders = false;

        private void Updater()
        {
            try
            {
                while (active)
                {
                    if (World.Settings.EnableAtmosphericScattering && m_meshList.Count > 0)
                    {
                        System.DateTime currentTime = TimeKeeper.CurrentTimeUtc;
                        // Update Sun
                        UpdateLightVector();
                        if (World.Settings.ForceCpuAtmosphere)
                        {
                            m_nSamples = 4;		// Number of sample rays to use in integral equation
                            m_Kr = 0.0025f;		// Rayleigh scattering constant
                            m_Kr4PI = m_Kr * 4.0f * (float)Math.PI;
                            m_Km = 0.0015f;		// Mie scattering constant
                            m_Km4PI = m_Km * 4.0f * (float)Math.PI;
                            m_ESun = 15.0f;		// Sun brightness constant
                            m_g = -0.85f;		// The Mie phase asymmetry factor

                            m_fWavelength[0] = 0.650f;		// 650 nm for red
                            m_fWavelength[1] = 0.570f;		// 570 nm for green
                            m_fWavelength[2] = 0.475f;		// 475 nm for blue
                            m_fWavelength4[0] = (float)Math.Pow(m_fWavelength[0], 4.0f);
                            m_fWavelength4[1] = (float)Math.Pow(m_fWavelength[1], 4.0f);
                            m_fWavelength4[2] = (float)Math.Pow(m_fWavelength[2], 4.0f);

                            m_fRayleighScaleDepth = 0.25f;
                            m_fMieScaleDepth = 0.1f;

                            if (currentTime.Subtract(m_lastOpticalUpdate) > TimeSpan.FromSeconds(100))
                            {
                                MakeOpticalDepthBuffer(m_fInnerRadius, m_fOuterRadius, m_fRayleighScaleDepth, m_fMieScaleDepth);
                                m_lastOpticalUpdate = currentTime;
                            }
                        }
                        else
                        {
                            //m_nSamples = 2;		// Number of sample rays to use in integral equation
                            //m_Kr = 0.0025f;		// Rayleigh scattering constant
                            //m_Kr4PI = m_Kr * 4.0f * (float)Math.PI;
                            //m_Km = 0.0015f;		// Mie scattering constant
                            //m_Km4PI = m_Km * 4.0f * (float)Math.PI;
                            //m_ESun = 15.0f;		// Sun brightness constant
                            //m_g = -0.95f;		// The Mie phase asymmetry factor
                            //m_fScale = 1 / (m_fOuterRadius - m_fInnerRadius);

                            //m_fWavelength[0] = 0.650f;		// 650 nm for red
                            //m_fWavelength[1] = 0.570f;		// 570 nm for green
                            //m_fWavelength[2] = 0.475f;		// 475 nm for blue
                            //m_fWavelength4[0] = (float)Math.Pow(m_fWavelength[0], 4.0f);
                            //m_fWavelength4[1] = (float)Math.Pow(m_fWavelength[1], 4.0f);
                            //m_fWavelength4[2] = (float)Math.Pow(m_fWavelength[2], 4.0f);

                            //m_fRayleighScaleDepth = 0.25f;
                            //m_fMieScaleDepth = 0.1f;


                            m_nSamples = 4;		// Number of sample rays to use in integral equation
                            m_Kr = 0.0025f;		// Rayleigh scattering constant
                            m_Kr4PI = m_Kr * 4.0f * (float)Math.PI;
                            m_Km = 0.0015f;		// Mie scattering constant
                            m_Km4PI = m_Km * 4.0f * (float)Math.PI;
                            m_ESun = 15.0f;		// Sun brightness constant
                            m_g = -0.75f;		// The Mie phase asymmetry factor
                            m_fScale = 1 / (m_fOuterRadius - m_fInnerRadius);

                            m_fWavelength[0] = 0.650f;		// 650 nm for red
                            m_fWavelength[1] = 0.570f;		// 570 nm for green
                            m_fWavelength[2] = 0.475f;		// 475 nm for blue
                            m_fWavelength4[0] = (float)Math.Pow(m_fWavelength[0], 4.0f);
                            m_fWavelength4[1] = (float)Math.Pow(m_fWavelength[1], 4.0f);
                            m_fWavelength4[2] = (float)Math.Pow(m_fWavelength[2], 4.0f);

                            m_fRayleighScaleDepth = 0.25f;
                            m_fMieScaleDepth = 0.1f;
                        }
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch
            { }
        }
        public void Update(DrawArgs drawArgs)
        {


            if (m_backgroundThread == null)
            {
                if (drawArgs.device.DeviceCaps.PixelShaderVersion.Major >= 2)
                {
                    m_canDoShaders = true;
                }
                else
                {
                    m_canDoShaders = false;
                }

                active = true;
                m_backgroundThread = new System.Threading.Thread(new System.Threading.ThreadStart(Updater));
                m_backgroundThread.Name = "AtmosphericScatteringSphere Updater";
                m_backgroundThread.Priority = System.Threading.ThreadPriority.Lowest;
                m_backgroundThread.IsBackground = true;
                m_backgroundThread.Start();
            }
        }

        CustomVertex.PositionColored[] CreateMesh(double minLat, double maxLat, double minLon, double maxLon, int meshPointCount)
        {
            int upperBound = meshPointCount - 1;
            float scaleFactor = (float)1 / upperBound;
            double latrange = Math.Abs(maxLat - minLat);
            double lonrange;
            if (minLon < maxLon)
                lonrange = maxLon - minLon;
            else
                lonrange = 360.0f + maxLon - minLon;

            CustomVertex.PositionColored[] vertices = new CustomVertex.PositionColored[meshPointCount * meshPointCount];

            for (int i = 0; i < meshPointCount; i++)
            {
                for (int j = 0; j < meshPointCount; j++)
                {
                    Vector3 pos = SMath.SphericalToCartesian(
                        maxLat - scaleFactor * latrange * i,
                        minLon + scaleFactor * lonrange * j,
                        m_radius);

                    vertices[i * meshPointCount + j].X = pos.X;
                    vertices[i * meshPointCount + j].Y = pos.Y;
                    vertices[i * meshPointCount + j].Z = pos.Z;
                }
            }

            return vertices;
        }

        short[] computeIndices(int meshPointCount)
        {
            int upperBound = meshPointCount - 1;
            short[] indices = new short[2 * upperBound * upperBound * 3];
            for (int i = 0; i < upperBound; i++)
            {
                for (int j = 0; j < upperBound; j++)
                {
                    indices[(2 * 3 * i * upperBound) + 6 * j] = (short)(i * meshPointCount + j);
                    indices[(2 * 3 * i * upperBound) + 6 * j + 1] = (short)((i + 1) * meshPointCount + j);
                    indices[(2 * 3 * i * upperBound) + 6 * j + 2] = (short)(i * meshPointCount + j + 1);

                    indices[(2 * 3 * i * upperBound) + 6 * j + 3] = (short)(i * meshPointCount + j + 1);
                    indices[(2 * 3 * i * upperBound) + 6 * j + 4] = (short)((i + 1) * meshPointCount + j);
                    indices[(2 * 3 * i * upperBound) + 6 * j + 5] = (short)((i + 1) * meshPointCount + j + 1);
                }
            }

            return indices;
        }

        short[] m_indices = null;
        short[] m_indicesHighResolution = null;
        float[] fCameraDepth = new float[4] { 0, 0, 0, 0 };
        float[] fLightDepth = new float[4];
        float[] fSampleDepth = new float[4];
        float[] fRayleighSum = new float[] { 0, 0, 0 };
        float[] fMieSum = new float[] { 0, 0, 0 };
        Vector3 vPos = new Vector3();
        float[] fAttenuation = new float[3];
        Vector3 vCamera = new Vector3();

        public void SetColor(ref CustomVertex.PositionColored pVertex, DrawArgs drawArgs)
        {
            vPos.X = pVertex.X;
            vPos.Y = pVertex.Y;
            vPos.Z = pVertex.Z;

            // Get the ray from the camera to the vertex, and its length (which is the far point of the ray passing through the atmosphere)
            vCamera.X = (float)drawArgs.WorldCamera.Position.X;
            vCamera.Y = (float)drawArgs.WorldCamera.Position.Y;
            vCamera.Z = (float)drawArgs.WorldCamera.Position.Z;

            Vector3 vRay = vPos - vCamera;
            float fFar = vRay.Length();

            vRay.Normalize();

            // Calculate the closest intersection of the ray with the outer atmosphere (which is the near point of the ray passing through the atmosphere)
            float B = 2.0f * Vector3.Dot(vCamera, vRay);
            float C = Vector3.Dot(vCamera, vCamera) - m_fOuterRadius * m_fOuterRadius;
            float fDet = (float)Math.Max(0.0f, B * B - 4.0f * C);
            float fNear = 0.5f * (-B - (float)Math.Sqrt(fDet));

            bool bCameraAbove = true;

            for (int i = 0; i < fCameraDepth.Length; i++)
                fCameraDepth[i] = 0;

            for (int i = 0; i < fLightDepth.Length; i++)
                fLightDepth[i] = 0;

            for (int i = 0; i < fSampleDepth.Length; i++)
                fSampleDepth[i] = 0;

            if (fNear <= 0)
            {
                // If the near point is behind the camera, it means the camera is inside the atmosphere
                fNear = 0;
                float fCameraHeight = vCamera.Length();
                float fCameraAltitude = (fCameraHeight - m_fInnerRadius) * m_fScale;
                bCameraAbove = fCameraHeight >= vPos.Length();
                float fCameraAngle = Vector3.Dot((bCameraAbove ? -vRay : vRay), vCamera) / fCameraHeight;
                Interpolate(ref fCameraDepth, fCameraAltitude, 0.5f - fCameraAngle * 0.5f);
            }
            else
            {
                // Otherwise, move the camera up to the near intersection point
                vCamera += vRay * fNear;
                fFar -= fNear;
                fNear = 0;
            }

            // If the distance between the points on the ray is negligible, don't bother to calculate anything
            if (fFar <= DELTA)
            {
                pVertex.Color = System.Drawing.Color.FromArgb(255, 0, 0, 0).ToArgb();
                return;
            }

            // Initialize a few variables to use inside the loop
            for (int i = 0; i < fRayleighSum.Length; i++)
                fRayleighSum[i] = 0;
            for (int i = 0; i < fMieSum.Length; i++)
                fMieSum[i] = 0;

            float fSampleLength = fFar / m_nSamples;
            float fScaledLength = fSampleLength * m_fScale;
            Vector3 vSampleRay = vRay * fSampleLength;

            // Start at the center of the first sample ray, and loop through each of the others
            vPos = vCamera + vSampleRay * 0.5f;
            for (int i = 0; i < m_nSamples; i++)
            {
                float fHeight = vPos.Length();

                // Start by looking up the optical depth coming from the light source to this point
                float fLightAngle = Vector3.Dot(m_vLightDirection, vPos) / fHeight;
                float fAltitude = (fHeight - m_fInnerRadius) * m_fScale;
                Interpolate(ref fLightDepth, fAltitude, 0.5f - fLightAngle * 0.5f);

                // If no light light reaches this part of the atmosphere, no light is scattered in at this point
                if (fLightDepth[0] < DELTA)
                    continue;

                // Get the density at this point, along with the optical depth from the light source to this point
                float fRayleighDensity = fScaledLength * fLightDepth[0];
                float fRayleighDepth = fLightDepth[1];
                float fMieDensity = fScaledLength * fLightDepth[2];
                float fMieDepth = fLightDepth[3];

                // If the camera is above the point we're shading, we calculate the optical depth from the sample point to the camera
                // Otherwise, we calculate the optical depth from the camera to the sample point
                if (bCameraAbove)
                {
                    float fSampleAngle = Vector3.Dot(-vRay, vPos) / fHeight;
                    Interpolate(ref fSampleDepth, fAltitude, 0.5f - fSampleAngle * 0.5f);
                    fRayleighDepth += fSampleDepth[1] - fCameraDepth[1];
                    fMieDepth += fSampleDepth[3] - fCameraDepth[3];
                }
                else
                {
                    float fSampleAngle = Vector3.Dot(vRay, vPos) / fHeight;
                    Interpolate(ref fSampleDepth, fAltitude, 0.5f - fSampleAngle * 0.5f);
                    fRayleighDepth += fCameraDepth[1] - fSampleDepth[1];
                    fMieDepth += fCameraDepth[3] - fSampleDepth[3];
                }

                // Now multiply the optical depth by the attenuation factor for the sample ray
                fRayleighDepth *= m_Kr4PI;
                fMieDepth *= m_Km4PI;

                // Calculate the attenuation factor for the sample ray
                fAttenuation[0] = (float)Math.Exp(-fRayleighDepth / m_fWavelength4[0] - fMieDepth);
                fAttenuation[1] = (float)Math.Exp(-fRayleighDepth / m_fWavelength4[1] - fMieDepth);
                fAttenuation[2] = (float)Math.Exp(-fRayleighDepth / m_fWavelength4[2] - fMieDepth);

                fRayleighSum[0] += fRayleighDensity * fAttenuation[0];
                fRayleighSum[1] += fRayleighDensity * fAttenuation[1];
                fRayleighSum[2] += fRayleighDensity * fAttenuation[2];

                fMieSum[0] += fMieDensity * fAttenuation[0];
                fMieSum[1] += fMieDensity * fAttenuation[1];
                fMieSum[2] += fMieDensity * fAttenuation[2];

                // Move the position to the center of the next sample ray
                vPos += vSampleRay;
            }

            // Calculate the angle and phase values (this block of code could be handled by a small 1D lookup table, or a 1D texture lookup in a pixel shader)
            float fAngle = (float)Vector3.Dot(-vRay, m_vLightDirection);
            float[] fPhase = new float[2];
            float fAngle2 = fAngle * fAngle;
            float g2 = m_g * m_g;
            fPhase[0] = 0.75f * (1.0f + fAngle2);
            fPhase[1] = 1.5f * ((1 - g2) / (2 + g2)) * (1.0f + fAngle2) / (float)Math.Pow(1 + g2 - 2 * m_g * fAngle, 1.5f);
            fPhase[0] *= m_Kr * m_ESun;
            fPhase[1] *= m_Km * m_ESun;
            // Calculate the in-scattering color and clamp it to the max color value
            float[] fColor = new float[3] { 0, 0, 0 };
            fColor[0] = fRayleighSum[0] * fPhase[0] / m_fWavelength4[0] + fMieSum[0] * fPhase[1];
            fColor[1] = fRayleighSum[1] * fPhase[0] / m_fWavelength4[1] + fMieSum[1] * fPhase[1];
            fColor[2] = fRayleighSum[2] * fPhase[0] / m_fWavelength4[2] + fMieSum[2] * fPhase[1];
            fColor[0] = (float)Math.Min(fColor[0], 1.0f);
            fColor[1] = (float)Math.Min(fColor[1], 1.0f);
            fColor[2] = (float)Math.Min(fColor[2], 1.0f);

            // Compute alpha transparency (PM 2006-11-19)
            float alpha = (fColor[0] + fColor[1] + fColor[2]) / 3;  // Average luminosity
            alpha = (float)Math.Min(alpha + 0.50, 1f);			  // increase opacity

            // Last but not least, set the color
            pVertex.Color = System.Drawing.Color.FromArgb((byte)(alpha * 255), (byte)(fColor[0] * 255), (byte)(fColor[1] * 255), (byte)(fColor[2] * 255)).ToArgb();

        }

        void Interpolate(ref float[] p, float x, float y)
        {
            float fX = x * (m_nWidth - 1);
            float fY = y * (m_nHeight - 1);
            int nX = Math.Min(m_nWidth - 2, Math.Max(0, (int)fX));
            int nY = Math.Min(m_nHeight - 2, Math.Max(0, (int)fY));
            float fRatioX = fX - nX;
            float fRatioY = fY - nY;

            //float *pValue = (float *)((unsigned long)m_pBuffer + m_nElementSize * (m_nWidth * nY + nX));
            //float pValue = m_opticalDepthBuffer[m_nWidth * nY + nX];
            int pValueOffset = (m_nWidth * nY + nX) * 4;

            for (int i = 0; i < m_nChannels; i++)
            {
                if (m_currentOpticalBuffer == 1)
                {
                    p[i] = m_opticalDepthBuffer1[pValueOffset] * (1 - fRatioX) * (1 - fRatioY) +
                        m_opticalDepthBuffer1[pValueOffset + m_nChannels * 1] * (fRatioX) * (1 - fRatioY) +
                        m_opticalDepthBuffer1[pValueOffset + m_nChannels * m_nWidth] * (1 - fRatioX) * (fRatioY) +
                        m_opticalDepthBuffer1[pValueOffset + m_nChannels * (m_nWidth + 1)] * (fRatioX) * (fRatioY);
                }
                else
                {
                    p[i] = m_opticalDepthBuffer2[pValueOffset] * (1 - fRatioX) * (1 - fRatioY) +
                        m_opticalDepthBuffer2[pValueOffset + m_nChannels * 1] * (fRatioX) * (1 - fRatioY) +
                        m_opticalDepthBuffer2[pValueOffset + m_nChannels * m_nWidth] * (1 - fRatioX) * (fRatioY) +
                        m_opticalDepthBuffer2[pValueOffset + m_nChannels * (m_nWidth + 1)] * (fRatioX) * (fRatioY);
                }
                pValueOffset++;
            }
        }

        float DELTA = 1e-6f;
        //static float[] m_opticalDepthBuffer = null;
        static int m_nChannels = 4;
        static int m_nWidth;				// The width of the buffer (x axis)
        static int m_nHeight;				// The height of the buffer (y axis)
        //static int m_nDepth = 0;				// The depth of the buffer (z axis)
        //static int m_nDataType = 0;			// The data type stored in the buffer (i.e. GL_UNSIGNED_BYTE, GL_FLOAT)
        static int m_nElementSize;			// The size of one element in the buffer
        float m_fScale = 0;
        float[] m_fWavelength = new float[3];
        float[] m_fWavelength4 = new float[3];
        float m_fRayleighScaleDepth = 0;
        float m_fMieScaleDepth = 0;

        int m_nSamples;
        float m_Kr, m_Kr4PI;
        float m_Km, m_Km4PI;
        float m_ESun;
        float m_g;

        Vector3 m_vLight;
        Vector3 m_vLightDirection;

        private void UpdateColor(DrawArgs drawArgs, MeshSubset meshSubset, bool doHighResolution)
        {
            int blank = System.Drawing.Color.FromArgb(255, 0, 0, 0).ToArgb();

            if (doHighResolution)
            {
                for (int i = 0; i < meshSubset.HigherResolutionVertices.Length; i++)
                {
                    if (Vector3.Dot(drawArgs.WorldCamera.Position.Vector(), new Vector3(meshSubset.HigherResolutionVertices[i].X, meshSubset.HigherResolutionVertices[i].Y, meshSubset.HigherResolutionVertices[i].Z)) > 0)
                        SetColor(ref meshSubset.HigherResolutionVertices[i], drawArgs);
                    else
                        meshSubset.HigherResolutionVertices[i].Color = blank;
                }
            }
            else
            {
                for (int i = 0; i < meshSubset.Vertices.Length; i++)
                {
                    if (Vector3.Dot(drawArgs.WorldCamera.Position.Vector(), new Vector3(meshSubset.Vertices[i].X, meshSubset.Vertices[i].Y, meshSubset.Vertices[i].Z)) > 0)
                        SetColor(ref meshSubset.Vertices[i], drawArgs);
                    else
                        meshSubset.Vertices[i].Color = blank;
                }
            }
        }

        // -- Addition from Sky Gradient geometry (PM 2006-11-27)
        // Rebuild sky mesh with updated colors
        double thickness;
        Mesh skyMesh;
        private void UpdateSkyMesh(DrawArgs drawArgs, double horizonSpan)
        {
            CameraBase camera = drawArgs.WorldCamera;
            Device device = drawArgs.device;

            // Use world atmospheric scattering sphere radius for thickness
            thickness = m_radius - camera.WorldRadius;

            double distToCenterOfPlanet = (camera.Altitude + camera.WorldRadius);
            // Compute distance to horizon and dome radius
            double tangentalDistance = Math.Sqrt(distToCenterOfPlanet * distToCenterOfPlanet - camera.WorldRadius * camera.WorldRadius);
            double domeRadius = tangentalDistance;

            // horizon latitude
            double horizonLat = (-Math.PI / 2 + Math.Acos(tangentalDistance / distToCenterOfPlanet)) * 180 / Math.PI;

            // zenith latitude
            double zenithLat = 90;
            if (camera.Altitude >= thickness)
            {
                double tangentalDistanceZenith = Math.Sqrt(distToCenterOfPlanet * distToCenterOfPlanet - (camera.WorldRadius + thickness) * (camera.WorldRadius + thickness));
                zenithLat = (-Math.PI / 2 + Math.Acos(tangentalDistanceZenith / distToCenterOfPlanet)) * 180 / Math.PI;
            }
            if (camera.Altitude < thickness && camera.Altitude > thickness * 0.8)
            {
                zenithLat = (thickness - camera.Altitude) / (thickness - thickness * 0.8) * 90;
            }
            // new mesh
            if (skyMesh != null) skyMesh.Dispose();
            int res = horizonSpan > 180 ? 64 : 128;
            skyMesh = ColoredSpherePartial(drawArgs, (float)domeRadius, horizonLat, zenithLat, res, res / 2, horizonSpan, camera.Heading.Degrees);

        }

        /// <summary>
        /// Creates a partial PositionColored sphere pre-transformed to the camera position
        /// modified to provide a sky/atmosphere gradient dome
        /// </summary>
        /// <param name="drawArgs">Where to get the current direct3D drawing device.</param>
        /// <param name="radius">The sphere's radius</param>
        /// <param name="slices">Number of slices (Horizontal resolution).</param>
        /// <param name="stacks">Number of stacks. (Vertical resolution)</param>
        /// <returns></returns>
        /// <remarks>
        /// Number of vertices in the sphere will be (slices+1)*(stacks+1)<br/>
        /// Number of faces :slices*stacks*2
        /// Number of Indexes   : Number of faces * 3;
        /// </remarks>
        private Mesh ColoredSpherePartial(DrawArgs drawArgs, float radius, double startLat, double endLat, int slices, int stacks, double lonSpan, double heading)
        {
            slices = (int)((double)slices * lonSpan / 360);

            int numVertices = (slices + 1) * (stacks + 1);
            int numFaces = slices * stacks * 2;
            int indexCount = numFaces * 3;
            Device device = drawArgs.device;

            // SkyGradient dome transform
            CameraBase camera = drawArgs.WorldCamera;
            Vector3 cameraPos = camera.Position.Vector();
            double distToCenterOfPlanet = (camera.Altitude + camera.WorldRadius);
            Vector3 cameraCoord = SMath.CartesianToSpherical(cameraPos.X, cameraPos.Y, cameraPos.Z);
            float camLat = cameraCoord.Y;
            float camLon = cameraCoord.Z;
            Matrix SkyGradientTrans = Matrix.Identity;
            SkyGradientTrans *= Matrix.Translation(0, 0, (float)distToCenterOfPlanet);
            SkyGradientTrans *= Matrix.RotationY(-camLat + (float)Math.PI / 2);
            SkyGradientTrans *= Matrix.RotationZ(camLon);

            // Find camera heading from the camera lat/lon to the target
            double d = SMath.SphericalDistance(Angle.FromRadians(camLat), Angle.FromRadians(camLon), camera.Latitude, camera.Longitude).Radians;
            double camHead = Math.Acos((Math.Sin(camera.Latitude.Radians) - Math.Sin(camLat) * Math.Cos(d)) / (Math.Sin(d) * Math.Cos(camLat)));
            if (Math.Sign(camera.Longitude.Radians - camLon) < 0) camHead = Math.PI * 2 - camHead;
            if (double.IsNaN(camHead)) camHead = 0;
            camHead = SMath.RadiansToDegrees(camHead);
            double startLon = -camHead - 180 + (lonSpan / 2);

            Mesh mesh = new Mesh(numFaces, numVertices, MeshFlags.Managed, CustomVertex.PositionColored.Format, device);

            // Get the original sphere's vertex buffer.
            int[] ranks = new int[1];
            ranks[0] = mesh.NumberVertices;
            System.Array arr = mesh.VertexBuffer.Lock(0, typeof(CustomVertex.PositionColored), LockFlags.None, ranks);

            // Set the vertex buffer
            int vertIndex = 0;
            CustomVertex.PositionColored pnt;
            Vector3 v;

            // bottom fade
            double latitude = startLat - ((endLat - startLat) / 10);
            if (latitude < startLat - 1) latitude = startLat - 1;
            for (int slice = 0; slice <= slices; slice++)
            {
                pnt = new CustomVertex.PositionColored();
                double longitude = startLon - ((float)slice / slices * lonSpan);
                v = SMath.SphericalToCartesian(latitude, longitude, radius);
                v.TransformCoordinate(SkyGradientTrans);
                pnt.X = v.X;
                pnt.Y = v.Y;
                pnt.Z = v.Z;
                pnt.Color = System.Drawing.Color.FromArgb(0, 0, 0, 0).ToArgb();
                arr.SetValue(pnt, vertIndex++);
            }
            // stacks and slices
            for (int stack = 1; stack < stacks; stack++)
            {
                //latitude = startLat + ((float)(stack-1)/(stacks-1f)*(float)(endLat - startLat));
                double linear = (float)(stack - 1) / (stacks - 1f);
                double k = 1 - Math.Cos((float)(stack - 1) / (stacks - 1f) * Math.PI / 2);
                latitude = startLat + (k * k * k * (float)(endLat - startLat));
                //double colorFactorZ = (float)(stack-1)/(stacks-1f); 	// coef zenith color
                double colorFactorZ = linear; 				// coef zenith color
                double colorFactorH = 1 - colorFactorZ;			// coef horizon color
                double alphaFactor = 1 - (linear * linear * linear);	// coef alpha transparency
                if (alphaFactor > .8) alphaFactor = .8f;
                for (int slice = 0; slice <= slices; slice++)
                {
                    pnt = new CustomVertex.PositionColored();
                    double longitude = startLon - ((float)slice / slices * lonSpan);
                    v = SMath.SphericalToCartesian(latitude, longitude, radius);
                    v.TransformCoordinate(SkyGradientTrans);
                    pnt.X = v.X;
                    pnt.Y = v.Y;
                    pnt.Z = v.Z;
                    pnt.Color = getAtmosphereColor(drawArgs, pnt);
                    arr.SetValue(pnt, vertIndex++);
                }
            }
            // top fade
            latitude = endLat + ((endLat - startLat) / 10);
            for (int slice = 0; slice <= slices; slice++)
            {
                pnt = new CustomVertex.PositionColored();
                double longitude = startLon - ((float)slice / slices * lonSpan);
                v = SMath.SphericalToCartesian(latitude, longitude, radius);
                v.TransformCoordinate(SkyGradientTrans);
                pnt.X = v.X;
                pnt.Y = v.Y;
                pnt.Z = v.Z;
                pnt.Color = System.Drawing.Color.FromArgb(0, 0, 0, 0).ToArgb();
                arr.SetValue(pnt, vertIndex++);
            }

            mesh.VertexBuffer.Unlock();
            ranks[0] = indexCount;
            arr = mesh.LockIndexBuffer(typeof(short), LockFlags.None, ranks);
            int i = 0;
            short bottomVertex = 0;
            short topVertex = 0;

            // stacks and slices
            for (short x = 0; x < stacks; x++)
            {
                bottomVertex = (short)((slices + 1) * x);
                topVertex = (short)(bottomVertex + slices + 1);
                for (int y = 0; y < slices; y++)
                {
                    arr.SetValue(bottomVertex, i++);
                    arr.SetValue((short)(topVertex + 1), i++);
                    arr.SetValue(topVertex, i++);
                    arr.SetValue(bottomVertex, i++);
                    arr.SetValue((short)(bottomVertex + 1), i++);
                    arr.SetValue((short)(topVertex + 1), i++);
                    bottomVertex++;
                    topVertex++;
                }
            }

            mesh.UnlockIndexBuffer();
            mesh.IndexBuffer.SetData(arr, 0, LockFlags.None);

            return mesh;
        }

        CustomVertex.PositionColored p2 = new CustomVertex.PositionColored();
        /// <summary>
        /// Compute sky vertex color using atmospheric scattering
        /// </summary>
        int getAtmosphereColor(DrawArgs drawArgs, CustomVertex.PositionColored pVertex)
        {
            // Find out intersection point on world scattering sphere
            //Vector3 vPos = new Vector3();
            vPos.X = pVertex.X;
            vPos.Y = pVertex.Y;
            vPos.Z = pVertex.Z;

            // Get the ray from the camera to the vertex
            //Vector3 vCamera = new Vector3();
            vCamera = drawArgs.WorldCamera.Position.Vector();
           

            Vector3 vRay = vPos - vCamera;
            vRay.Normalize();

            // Calculate the closest intersection of the ray with the outer atmosphere 
            float B = 2.0f * Vector3.Dot(vCamera, vRay);
            float C = Vector3.Dot(vCamera, vCamera) - m_radius * m_radius;
            float fDet = B * B - 4.0f * C;

            p2.Color = System.Drawing.Color.FromArgb(0, 0, 0, 0).ToArgb();
            if (fDet >= 0)
            {
                // Camera ray intersect atmosphere
                float fNear1 = 0.5f * (-B - (float)Math.Sqrt(fDet));
                float fNear2 = 0.5f * (-B + (float)Math.Sqrt(fDet));
                if (fNear1 >= 0 || fNear2 >= 0)
                {
                    // biggest distance - not sure why...
                    float fNear = (float)Math.Max(fNear1, fNear2);
                    vPos = vCamera + vRay * fNear;
                    p2.X = vPos.X;
                    p2.Y = vPos.Y;
                    p2.Z = vPos.Z;
                    SetColor(ref p2, drawArgs);
                }
            }
            return p2.Color;
        }
        // -- End SkyGradiant geometry addition

        static Effect skyFromSpaceEffect = null;
        static Effect skyFromAtmosphere = null;

        public void Render(DrawArgs drawArgs)
        {
            try
            {
                if (m_meshList.Count > 0 && ((!World.Settings.ForceCpuAtmosphere && m_canDoShaders) || m_opticalDepthBuffer1 != null))
                {
                    double horizonSpan = HorizonSpan(drawArgs);
                    if (horizonSpan == 0) return;   // Check if horizon visible (PM 2006-11-28)

                    if (skyFromSpaceEffect == null)
                    {
                        drawArgs.device.DeviceReset += new EventHandler(device_DeviceReset);
                        device_DeviceReset(drawArgs.device, null);
                    }

                    vCamera = drawArgs.WorldCamera.Position.Vector();
                 

                    drawArgs.device.VertexFormat = CustomVertex.PositionColored.Format;
                    drawArgs.device.TextureState[0].ColorOperation = TextureOperation.Disable;
                

                    
                    bool doHighResolution = (drawArgs.WorldCamera.Altitude < 30000);

                    Frustum frustum = new Frustum();

                    frustum.Update(ConvertDX.ToMatrix4d(  Matrix.Multiply(drawArgs.device.Transform.World, Matrix.Multiply(drawArgs.device.Transform.View, drawArgs.device.Transform.Projection))));

                    if (!World.Settings.ForceCpuAtmosphere && m_canDoShaders)
                    {
                        Effect shader = null;
                        // Update Sun
                        UpdateLightVector();
                        if (vCamera.Length() >= m_fOuterRadius)
                            shader = skyFromSpaceEffect;
                        else
                            shader = skyFromAtmosphere;

                        shader.Technique = "Sky";
                        shader.SetValue("v3CameraPos", new Vector4(vCamera.X, vCamera.Y, vCamera.Z, 0));
                        shader.SetValue("v3LightPos", Vector4.Normalize(new Vector4(m_vLightDirection.X, m_vLightDirection.Y, m_vLightDirection.Z, 0)));
                        shader.SetValue("WorldViewProj", Matrix.Multiply(drawArgs.device.Transform.World, Matrix.Multiply(drawArgs.device.Transform.View, drawArgs.device.Transform.Projection)));
                        shader.SetValue("v3InvWavelength", new Vector4(1.0f / m_fWavelength4[0], 1.0f / m_fWavelength4[1], 1.0f / m_fWavelength4[2], 0));
                        shader.SetValue("fCameraHeight", vCamera.Length());
                        shader.SetValue("fCameraHeight2", vCamera.LengthSq());
                        shader.SetValue("fInnerRadius", m_fInnerRadius);
                        shader.SetValue("fInnerRadius2", m_fInnerRadius * m_fInnerRadius);
                        shader.SetValue("fOuterRadius", m_fOuterRadius);
                        shader.SetValue("fOuterRadius2", m_fOuterRadius * m_fOuterRadius);
                        shader.SetValue("fKrESun", m_Kr * m_ESun);
                        shader.SetValue("fKmESun", m_Km * m_ESun);
                        shader.SetValue("fKr4PI", m_Kr4PI);
                        shader.SetValue("fKm4PI", m_Km4PI);
                        shader.SetValue("fScale", 1.0f / (m_fOuterRadius - m_fInnerRadius));
                        shader.SetValue("fScaleDepth", m_fRayleighScaleDepth);
                        shader.SetValue("fScaleOverScaleDepth", (1.0f / (m_fOuterRadius - m_fInnerRadius)) / m_fRayleighScaleDepth);
                        shader.SetValue("g", m_g);
                        shader.SetValue("g2", m_g * m_g);
                        shader.SetValue("nSamples", m_nSamples);
                        shader.SetValue("fSamples", m_nSamples);

                        for (int i = 0; i < m_meshList.Count; i++)
                        {
                            if (!frustum.Intersects(m_meshList[i].BoundingBox))
                                continue;

                            int numPasses = shader.Begin(0);
                            for (int j = 0; j < numPasses; j++)
                            {
                                shader.BeginPass(j);
                                if (doHighResolution)
                                    drawArgs.device.DrawIndexedUserPrimitives(PrimitiveType.TriangleList, 0, m_meshList[i].HigherResolutionVertices.Length, m_indicesHighResolution.Length / 3, m_indicesHighResolution, true, m_meshList[i].HigherResolutionVertices);
                                else
                                    drawArgs.device.DrawIndexedUserPrimitives(PrimitiveType.TriangleList, 0, m_meshList[i].Vertices.Length, m_indices.Length / 3, m_indices, true, m_meshList[i].Vertices);
                                shader.EndPass();
                            }
                            shader.End();
                        }
                    }
                    else
                    {
                        /*for (int i = 0; i < m_meshList.Count; i++)
                        {
                            if (!frustum.Intersects(m_meshList[i].BoundingBox))
                                continue;

                            UpdateColor(drawArgs, m_meshList[i], doHighResolution);
                            if (doHighResolution)
                                drawArgs.device.DrawIndexedUserPrimitives(PrimitiveType.TriangleList, 0, m_meshList[i].HigherResolutionVertices.Length, m_indicesHighResolution.Length / 3, m_indicesHighResolution, true, m_meshList[i].HigherResolutionVertices);
                            else
                                drawArgs.device.DrawIndexedUserPrimitives(PrimitiveType.TriangleList, 0, m_meshList[i].Vertices.Length, m_indices.Length / 3, m_indices, true, m_meshList[i].Vertices);
							
                        } */

                        if (drawArgs.device.RenderState.Lighting)
                            drawArgs.device.RenderState.Lighting = false;

                        // Update Sun
                        UpdateLightVector();
                        // Use SkyGradient geometry
                        UpdateSkyMesh(drawArgs, horizonSpan);
                        drawArgs.device.RenderState.CullMode = Cull.Clockwise;
                        skyMesh.DrawSubset(0);
                    }
                    drawArgs.device.Transform.World = ConvertDX.FromMatrix4d(drawArgs.WorldCamera.WorldMatrix);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        // Check if horizon is visible in camera viewport (PM 2006-11-28)
        // Returns the horizon span angle or 0 if not visible
        double HorizonSpan(DrawArgs drawArgs)
        {
            // Camera & Viewport shortcuts
            CameraBase camera = drawArgs.WorldCamera;
            Viewport viewport = camera.Viewport;

            // Compute camera altitude
            double cameraAltitude = camera.Position.Length - camera.WorldRadius;

            // Compute camera absolute field of view (to the horizon)
            double fovH = Math.Abs(Math.Asin(camera.WorldRadius / (camera.WorldRadius + camera.Altitude))) * 2;

            // Compute viewport diagonal field of view
            int h = viewport.Height;
            int w = viewport.Width;
            double fovV = camera.Fov.Radians;
            double fovD = Math.Abs(Math.Atan(Math.Sqrt(h * h + w * w) * Math.Tan(fovV / 2) / h)) * 2;

            // Compute camera tilt from vertical at the camera position
            double tilt = camera.Tilt.Radians * 2;
            if (camera.Altitude > 10000)
            {
                double a = camera.WorldRadius;					  // World radius
                double b = camera.WorldRadius + camera.Altitude;	 // Camera to center of planet
                double c = camera.Distance;						 // Distance to target
                tilt = Math.Abs(Math.Acos((a * a - c * c - b * b) / (-2 * c * b))) * 2;
                if (double.IsNaN(tilt)) tilt = 0;
            }

            // Check if cones intersect
            double span = 0;
            if (fovD + tilt > fovH)
            {
                span = fovD < fovH ? Math.Abs(Math.Asin(Math.Sin(fovD / 2) / Math.Sin(fovH / 2))) * 2 : Math.PI * 2;
                span *= 180 / Math.PI;
            }

            return span;
        }

        void device_DeviceReset(object sender, EventArgs e)
        {
            Device device = (Device)sender;

            try
            {
                string outerrors = "";

                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                Stream skyFromSpaceStream = assembly.GetManifestResourceStream("R.Earth.Shaders.SkyFromSpace.fx");

                skyFromSpaceEffect =
                    Effect.FromStream(
                    device,
                    skyFromSpaceStream,
                    null,
                    null,
                    ShaderFlags.None,
                    null,
                    out outerrors);

                if (outerrors != null && outerrors.Length > 0)
                    Log.Write(Log.Levels.Error, outerrors);

                Stream skyFromAtmosphereStream = assembly.GetManifestResourceStream("R.Earth.Shaders.SkyFromAtmosphere.fx");

                skyFromAtmosphere =
                    Effect.FromStream(
                    device,
                    skyFromAtmosphereStream,
                    null,
                    null,
                    ShaderFlags.None,
                    null,
                    out outerrors);

                if (outerrors != null && outerrors.Length > 0)
                    Log.Write(Log.Levels.Error, outerrors);

            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        // Updates vLight and vLightDirection according to Sun position
        void UpdateLightVector()
        {
            System.DateTime currentTime = TimeKeeper.CurrentTimeUtc;
            Vector3d sunPosition = SunCalculator.GetGeocentricPosition(currentTime);
            Vector3 sunVector = new Vector3(
                (float)-sunPosition.X,
                (float)-sunPosition.Y,
                (float)-sunPosition.Z);

            m_vLight = sunVector * 100000000f;
            m_vLightDirection = new Vector3(
                m_vLight.X / m_vLight.Length(),
                m_vLight.Y / m_vLight.Length(),
                m_vLight.Z / m_vLight.Length()
                );
        }

        void MakeOpticalDepthBuffer(float fInnerRadius, float fOuterRadius, float fRayleighScaleHeight, float fMieScaleHeight)
        {
            int nSize = 128;
            int nSamples = 50;
            float fScale = 1.0f / (fOuterRadius - fInnerRadius);

            if (m_opticalDepthBuffer1 == null)
                m_opticalDepthBuffer1 = new float[nSize * nSize * 4];

            if (m_opticalDepthBuffer2 == null)
                m_opticalDepthBuffer2 = new float[nSize * nSize * 4];

            if (m_currentOpticalBuffer == 1)
            {
                for (int i = 0; i < m_opticalDepthBuffer2.Length; i++)
                {
                    m_opticalDepthBuffer2[i] = 0;
                }
            }
            else
            {
                for (int i = 0; i < m_opticalDepthBuffer1.Length; i++)
                {
                    m_opticalDepthBuffer1[i] = 0;
                }
            }

            m_nWidth = nSize;
            m_nHeight = nSize;
            //m_nDepth = 1;
            //m_nDataType = 4;
            m_nChannels = 4;
            m_nElementSize = m_nChannels * 4;
            int nIndex = 0;
            for (int nAngle = 0; nAngle < nSize; nAngle++)
            {
                // As the y tex coord goes from 0 to 1, the angle goes from 0 to 180 degrees
                float fCos = 1.0f - (nAngle + nAngle) / (float)nSize;
                float fAngle = (float)Math.Acos(fCos);

                Vector3 vRay = new Vector3((float)Math.Sin(fAngle), (float)Math.Cos(fAngle), 0);	// Ray pointing to the viewpoint
                for (int nHeight = 0; nHeight < nSize; nHeight++)
                {
                    // As the x tex coord goes from 0 to 1, the height goes from the bottom of the atmosphere to the top
                    float fHeight = DELTA + fInnerRadius + ((fOuterRadius - fInnerRadius) * nHeight) / nSize;
                    Vector3 vPos = new Vector3(0, fHeight, 0);				// The position of the camera

                    // If the ray from vPos heading in the vRay direction intersects the inner radius (i.e. the planet), then this spot is not visible from the viewpoint
                    float B = 2.0f * Vector3.Dot(vPos, vRay);
                    float Bsq = B * B;
                    float Cpart = Vector3.Dot(vPos, vPos);
                    float C = Cpart - fInnerRadius * fInnerRadius;
                    float fDet = Bsq - 4.0f * C;
                    bool bVisible = (fDet < 0 || (0.5f * (-B - (float)Math.Sqrt(fDet)) <= 0) && (0.5f * (-B + (float)Math.Sqrt(fDet)) <= 0));
                    float fRayleighDensityRatio;
                    float fMieDensityRatio;
                    if (bVisible)
                    {
                        fRayleighDensityRatio = (float)Math.Exp(-(fHeight - fInnerRadius) * fScale / fRayleighScaleHeight);
                        fMieDensityRatio = (float)Math.Exp(-(fHeight - fInnerRadius) * fScale / fMieScaleHeight);
                    }
                    else
                    {
                        if (m_currentOpticalBuffer == 1)
                        {
                            // Smooth the transition from light to shadow (it is a soft shadow after all)
                            fRayleighDensityRatio = m_opticalDepthBuffer2[nIndex - nSize * m_nChannels] * 0.75f;
                            fMieDensityRatio = m_opticalDepthBuffer2[nIndex + 2 - nSize * m_nChannels] * 0.75f;
                        }
                        else
                        {
                            // Smooth the transition from light to shadow (it is a soft shadow after all)
                            fRayleighDensityRatio = m_opticalDepthBuffer1[nIndex - nSize * m_nChannels] * 0.75f;
                            fMieDensityRatio = m_opticalDepthBuffer1[nIndex + 2 - nSize * m_nChannels] * 0.75f;
                        }

                    }

                    // Determine where the ray intersects the outer radius (the top of the atmosphere)
                    // This is the end of our ray for determining the optical depth (vPos is the start)
                    C = Cpart - fOuterRadius * fOuterRadius;
                    fDet = Bsq - 4.0f * C;
                    float fFar = 0.5f * (-B + (float)Math.Sqrt(fDet));

                    // Next determine the length of each sample, scale the sample ray, and make sure position checks are at the center of a sample ray
                    float fSampleLength = fFar / nSamples;
                    float fScaledLength = fSampleLength * fScale;
                    Vector3 vSampleRay = vRay * fSampleLength;
                    vPos += vSampleRay * 0.5f;

                    // Iterate through the samples to sum up the optical depth for the distance the ray travels through the atmosphere
                    float fRayleighDepth = 0;
                    float fMieDepth = 0;
                    for (int i = 0; i < nSamples; i++)
                    {
                        fHeight = vPos.Length();
                        float fAltitude = (fHeight - fInnerRadius) * fScale;
                        fAltitude = (float)Math.Max(fAltitude, 0.0f);
                        fRayleighDepth += (float)Math.Exp(-fAltitude / fRayleighScaleHeight);
                        fMieDepth += (float)Math.Exp(-fAltitude / fMieScaleHeight);
                        vPos += vSampleRay;
                    }

                    // Multiply the sums by the length the ray traveled
                    fRayleighDepth *= fScaledLength;
                    fMieDepth *= fScaledLength;

                    // Store the results for Rayleigh to the light source, Rayleigh to the camera, Mie to the light source, and Mie to the camera
                    if (m_currentOpticalBuffer == 1)
                    {
                        m_opticalDepthBuffer2[nIndex++] = fRayleighDensityRatio;
                        m_opticalDepthBuffer2[nIndex++] = fRayleighDepth;
                        m_opticalDepthBuffer2[nIndex++] = fMieDensityRatio;
                        m_opticalDepthBuffer2[nIndex++] = fMieDepth;
                    }
                    else
                    {
                        m_opticalDepthBuffer1[nIndex++] = fRayleighDensityRatio;
                        m_opticalDepthBuffer1[nIndex++] = fRayleighDepth;
                        m_opticalDepthBuffer1[nIndex++] = fMieDensityRatio;
                        m_opticalDepthBuffer1[nIndex++] = fMieDepth;
                    }
                }
            }

            if (m_currentOpticalBuffer == 1)
                m_currentOpticalBuffer = 2;
            else
                m_currentOpticalBuffer = 1;

        }
        int m_currentOpticalBuffer = 2;
        float[] m_opticalDepthBuffer1 = null;
        float[] m_opticalDepthBuffer2 = null;

        internal void UpdateMesh()
        {
            this.Init(this.m_radius, this.m_numberSlices, this.m_numberSections);
        }
    }
}
