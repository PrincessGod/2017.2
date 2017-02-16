using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace R.Earth.Plugin
{
    public class SunScatting : R.Earth.GeoLayer
    {
        private ScattingEngine Engine;

        private double InnerRadius = World.EquatorialRadius;
        private double OutterRadius = World.EquatorialRadius * 1.015f;

        public SunScatting(string name,string texture,World world)
            : base(name)
        {
            this.Engine = new ScattingEngine((float)InnerRadius, (float)OutterRadius, texture);
            
        }
        public override void OnInitialize(DrawArgs drawArgs)
        {
            try
            {
                this.Engine.Initialize(drawArgs);
            }
            catch
            { }
            this.IsInitialized = true;
        }

        public override void OnFrameMove(DrawArgs drawArgs)
        {
            if (!this.IsInitialized)
                this.OnInitialize(drawArgs);            
        }

        public override void OnRender(DrawArgs drawArgs)
        {
            if (!this.IsInitialized)
                return;
            Matrix4d proj = drawArgs.WorldCamera.ProjectionMatrix;
            Cull cull = drawArgs.device.RenderState.CullMode;
            bool iszub = drawArgs.device.RenderState.ZBufferEnable;

            float aspectRatio = (float)drawArgs.WorldCamera.Viewport.Width / drawArgs.WorldCamera.Viewport.Height;
            float zNear = (float)drawArgs.WorldCamera.Altitude * 0.1f;
            double distToCenterOfPlanet = (drawArgs.WorldCamera.Altitude + World.EquatorialRadius);
            double tangentalDistance = Math.Sqrt(distToCenterOfPlanet * distToCenterOfPlanet - World.EquatorialRadius * World.EquatorialRadius);
            double amosphereThickness = Math.Sqrt(OutterRadius * OutterRadius + World.EquatorialRadius * World.EquatorialRadius);
           
            drawArgs.device.Transform.Projection = Matrix.PerspectiveFovRH((float)drawArgs.WorldCamera.Fov.Radians, aspectRatio, zNear, (float)(tangentalDistance + amosphereThickness));
            
            this.Engine.RenderFrame(drawArgs);

            drawArgs.device.RenderState.CullMode = cull;
            drawArgs.device.RenderState.ZBufferEnable = iszub ;
            drawArgs.device.Transform.Projection = ConvertDX.FromMatrix4d(proj);

        }

        public override void Dispose()
        {
            this.IsInitialized = false;
            if (this.Engine != null)
            {
                this.Engine.Dispose();
                this.Engine = null;
            }
        }


        public override void UpdateMesh(DrawArgs drawArgs)
        {
           
        }

        public override void OnRenderOrtho(DrawArgs drawArgs)
        {
            
        }
    }

    public class ScattingEngine : IDisposable
    {
        #region IDisposable Members

        public void Dispose()
        {
            if (this.m_outerSphereList != null)
            {
                foreach(Sphere  it in this.m_outerSphereList)
                {
                    it.Dispose();
                }
                this.m_outerSphereList.Clear();
            }
        }

        #endregion

        private bool IsInitialized = false;

        public const float PI = (float)Math.PI;

        private Sphere m_sphereInner;
        private Texture m_texture = null;

        private List<Sphere> m_outerSphereList = new List<Sphere>();

        #region Member
        private string SurfaceImagePath = "";

        protected float m_fFPS;
        protected int m_nTime;

        protected Vector3 m_vLight;
        protected Vector3 m_vLightDirection;

        protected bool m_bShowTexture;

        protected int m_nSamples;

        protected float DELTA = 1e-6f;

        protected float m_Kr, m_Kr4PI;
        protected float m_Km, m_Km4PI;
        protected float m_ESun;
        protected float m_g;

        protected float m_fInnerRadius;
        protected float m_fOuterRadius;
        protected float m_fScale;
        protected float m_fRayleighScaleDepth;
        protected float m_fMieScaleDepth;

        protected float[] m_fWavelength = new float[3];
        protected float[] m_fWavelength4 = new float[3];

        protected static int m_nChannels = 4;
        protected static int m_nWidth;				// The width of the buffer (x axis)
        protected static int m_nHeight;				// The height of the buffer (y axis)
        protected static int m_nElementSize;			// The size of one element in the buffer
        
        protected Vector3 vPos = new Vector3();
        protected Vector3 vCamera = new Vector3();

        protected float[] fCameraDepth = new float[4] { 0, 0, 0, 0 };
        protected float[] fSampleDepth = new float[4];
        protected float[] fLightDepth = new float[4];
        protected float[] fRayleighSum = new float[] { 0, 0, 0 };
        protected float[] fMieSum = new float[] { 0, 0, 0 };
        protected float[] fAttenuation = new float[3];

        protected int m_currentOpticalBuffer = 2;
        protected float[] m_opticalDepthBuffer1 = null;
        protected float[] m_opticalDepthBuffer2 = null;

        private static int TilesHigh = 3;
        private static int TilesWide = 6;

        #endregion
          
        private Effect skyFromAtmosphere = null;


        public ScattingEngine(float innerradius, float outradius, string filepath)
        {
            this.SurfaceImagePath = filepath;

            m_nSamples = 4;		// Number of sample rays to use in integral equation
            m_Kr = 0.0025f;		// Rayleigh scattering constant
            m_Kr4PI = m_Kr * 4.0f * PI;
            m_Km = 0.0025f;		// Mie scattering constant
            m_Km4PI = m_Km * 4.0f * PI;
            m_ESun = 15.0f;		// Sun brightness constant
            m_g = -0.75f;		// The Mie phase asymmetry factor

            m_fInnerRadius = innerradius;
            m_fOuterRadius = outradius;
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


        public void Initialize(DrawArgs drawArgs)
        {
            try
            {
                this.LoadTexture(drawArgs.device, this.SurfaceImagePath);
                this.UpdateLightVector();
                
                this.InitializeInnerSphere(drawArgs);
                this.InitializeOutterSphereList(drawArgs);

                this.IsInitialized = true;
            }
            catch
            {
                this.IsInitialized = false;
            }
        }

        public void RenderFrame(DrawArgs drawArgs)
        {
            if (!this.IsInitialized)
            {
                return;
            }
            //drawArgs.device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, 0, 1.0f, 0);

            drawArgs.device.RenderState.ZBufferEnable = false;
            drawArgs.device.RenderState.CullMode = Cull.CounterClockwise;

            this.RenderOutterSphere(drawArgs);

            drawArgs.device.VertexFormat = CustomVertex.PositionColoredTextured.Format;
            drawArgs.device.SetTexture(0, this.m_texture);
            drawArgs.device.TextureState[0].ColorOperation = TextureOperation.MultiplyAdd;
            drawArgs.device.TextureState[0].ColorArgument2 = TextureArgument.Current;
            drawArgs.device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
            drawArgs.device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1;
            drawArgs.device.TextureState[0].AlphaArgument2 = TextureArgument.Current;
            drawArgs.device.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;

            drawArgs.device.RenderState.FillMode = World.Settings.FillMode;
            drawArgs.device.RenderState.Lighting = false;
            drawArgs.device.RenderState.CullMode = Cull.Clockwise;

            this.RenderInnerSphere(drawArgs);

            if (World.Settings.EnableAtmosphericScattering )
            {
                this.UpdateOutter(drawArgs);
            }
        }

        private void RenderInnerSphere(DrawArgs drawArgs)
        {
            CustomVertex.PositionColoredTextured[] pBuffer = this.m_sphereInner.GetVertexBuffer();
            for (int i = 0; i < this.m_sphereInner.GetVertexCount(); i++)
            {           
                if (Vector3.Dot(drawArgs.WorldCamera.Position.Vector(), pBuffer[i].Position) > 0)
                {
                    this.SetColor(ref pBuffer[i], drawArgs);
                }                
            }
           
            this.m_sphereInner.Draw(drawArgs.device);
        }
        private void RenderOutterSphere(DrawArgs drawArgs)
        {
            try
            {
                if (this.m_outerSphereList.Count > 0 && ((!World.Settings.ForceCpuAtmosphere && m_canDoShaders) || m_opticalDepthBuffer1 != null))
                {
                    double horizonSpan = HorizonSpan(drawArgs);
                    if (horizonSpan == 0) return;   // Check if horizon visible (PM 2006-11-28)

                    if (skyFromAtmosphere == null)
                    {
                        drawArgs.device.DeviceReset += new EventHandler(device_DeviceReset);
                        device_DeviceReset(drawArgs.device, null);
                    }

                    vCamera = drawArgs.WorldCamera.Position.Vector();


                    drawArgs.device.VertexFormat = CustomVertex.PositionColored.Format;
                    drawArgs.device.TextureState[0].ColorOperation = TextureOperation.Disable;


                    Frustum frustum = new Frustum();

                    frustum.Update(ConvertDX.ToMatrix4d(Matrix.Multiply(drawArgs.device.Transform.World, Matrix.Multiply(drawArgs.device.Transform.View, drawArgs.device.Transform.Projection))));

                    if (!World.Settings.ForceCpuAtmosphere && m_canDoShaders)
                    {
                        UpdateLightVector();

                        skyFromAtmosphere.Technique = "Sky";
                        skyFromAtmosphere.SetValue("v3CameraPos", new Vector4(vCamera.X, vCamera.Y, vCamera.Z, 0));
                        skyFromAtmosphere.SetValue("v3LightPos", Vector4.Normalize(new Vector4(m_vLightDirection.X, m_vLightDirection.Y, m_vLightDirection.Z, 0)));
                        skyFromAtmosphere.SetValue("WorldViewProj", Matrix.Multiply(drawArgs.device.Transform.World, Matrix.Multiply(drawArgs.device.Transform.View, drawArgs.device.Transform.Projection)));
                        skyFromAtmosphere.SetValue("v3InvWavelength", new Vector4(1.0f / m_fWavelength4[0], 1.0f / m_fWavelength4[1], 1.0f / m_fWavelength4[2], 0));
                        skyFromAtmosphere.SetValue("fCameraHeight", vCamera.Length());
                        skyFromAtmosphere.SetValue("fCameraHeight2", vCamera.LengthSq());
                        skyFromAtmosphere.SetValue("fInnerRadius", m_fInnerRadius);
                        skyFromAtmosphere.SetValue("fInnerRadius2", m_fInnerRadius * m_fInnerRadius);
                        skyFromAtmosphere.SetValue("fOuterRadius", m_fOuterRadius);
                        skyFromAtmosphere.SetValue("fOuterRadius2", m_fOuterRadius * m_fOuterRadius);
                        skyFromAtmosphere.SetValue("fKrESun", m_Kr * m_ESun);
                        skyFromAtmosphere.SetValue("fKmESun", m_Km * m_ESun);
                        skyFromAtmosphere.SetValue("fKr4PI", m_Kr4PI);
                        skyFromAtmosphere.SetValue("fKm4PI", m_Km4PI);
                        skyFromAtmosphere.SetValue("fScale", 1.0f / (m_fOuterRadius - m_fInnerRadius));
                        skyFromAtmosphere.SetValue("fScaleDepth", m_fRayleighScaleDepth);
                        skyFromAtmosphere.SetValue("fScaleOverScaleDepth", (1.0f / (m_fOuterRadius - m_fInnerRadius)) / m_fRayleighScaleDepth);
                        skyFromAtmosphere.SetValue("g", m_g);
                        skyFromAtmosphere.SetValue("g2", m_g * m_g);
                        skyFromAtmosphere.SetValue("nSamples", m_nSamples);
                        skyFromAtmosphere.SetValue("fSamples", m_nSamples);

                        int numPasses = skyFromAtmosphere.Begin(0);

                        for (int i = 0; i < m_outerSphereList.Count; i++)
                        {
                            if (!frustum.Intersects(m_outerSphereList[i].BoundingBox) && !frustum.Contains(m_outerSphereList[i].BoundingBox))
                                continue;

                            for (int j = 0; j < numPasses; j++)
                            {
                                skyFromAtmosphere.BeginPass(j);
                                this.m_outerSphereList[i].Draw(drawArgs.device);
                                skyFromAtmosphere.EndPass();
                            }
                        }
                        skyFromAtmosphere.End();
                    }

                    else
                    {
                        for (int i = 0; i < m_outerSphereList.Count; i++)
                        {
                            if (!frustum.Intersects(m_outerSphereList[i].BoundingBox) && !frustum.Contains(m_outerSphereList[i].BoundingBox))
                                continue;
                            CustomVertex.PositionColoredTextured[] pBuffer = this.m_outerSphereList[i].GetVertexBuffer();
                            for (int m = 0; m < this.m_outerSphereList[i].GetVertexCount(); m++)
                            {
                                if (!drawArgs.WorldCamera.ViewFrustum.ContainsPoint(Vector3d.FromVector3(pBuffer[m].Position)))
                                {
                                    continue;
                                }
                                if (Vector3.Dot(drawArgs.WorldCamera.Position.Vector(), pBuffer[m].Position) > 0)
                                {
                                    this.SetColor(ref pBuffer[m], drawArgs);
                                }
                                else { pBuffer[m].Color = Color.FromArgb(0, 0, 0, 0).ToArgb(); }                              
                            }

                            this.m_outerSphereList[i].Draw(drawArgs.device);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Log.Write(e.StackTrace);
            }
            finally
            { }
        }




        private void InitializeInnerSphere(DrawArgs drawArgs)
        {
            m_sphereInner = new Sphere();
            this.MakeOpticalDepthBuffer(m_fInnerRadius, m_fOuterRadius, m_fRayleighScaleDepth, m_fMieScaleDepth);
            m_sphereInner.Init(m_fInnerRadius,32, -90, 90, -180, 180);
        }
        private void InitializeOutterSphereList(DrawArgs drawArgs)
        {
            int m_numberSlices = 32;
            int m_numberSections = 32;
            try
            {
                this.m_outerSphereList.Clear();

                double latRange = 180.0 / (double)TilesHigh;
                double lonRange = 360.0 / (double)TilesWide;

                for (int y = 0; y < TilesHigh; y++)
                {
                    for (int x = 0; x < TilesWide; x++)
                    {
                        Sphere mesh = new Sphere();
                        double north = y * latRange + latRange - 90;
                        double south = y * latRange - 90;

                        double west = x * lonRange - 180;
                        double east = x * lonRange + lonRange - 180;

                        mesh.Init(this.m_fOuterRadius, m_numberSlices, south, north, west, east);

                        this.m_outerSphereList.Add(mesh);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }


        #region Method
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pVertex"></param>
        /// <param name="drawArgs"></param>
        private void SetColor(ref CustomVertex.PositionColoredTextured pVertex, DrawArgs drawArgs)
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
            float Bsq = B * B;
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

        private void MakeOpticalDepthBuffer(float fInnerRadius, float fOuterRadius, float fRayleighScaleHeight, float fMieScaleHeight)
        {
            int nSize = 128;
            int nSamples = 10;
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

        private void Interpolate(ref float[] p, float x, float y)
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
          
        // Check if horizon is visible in camera viewport (PM 2006-11-28)
        // Returns the horizon span angle or 0 if not visible
        private double HorizonSpan(DrawArgs drawArgs)
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


        // Updates vLight and vLightDirection according to Sun position
        private void UpdateLightVector()
        {
            System.DateTime currentTime = TimeKeeper.CurrentTimeUtc;
            Vector3d sunPosition = SunCalculator.GetGeocentricPosition(currentTime);
            Vector3 sunVector = new Vector3( (float)-sunPosition.X,(float)-sunPosition.Y,(float)-sunPosition.Z);

            m_vLight = sunVector * 100000000f;
            m_vLightDirection = new Vector3( m_vLight.X / m_vLight.Length(), m_vLight.Y / m_vLight.Length(), m_vLight.Z / m_vLight.Length()        );
        }

        private void LoadTexture(Device device, string filepath)
        {
            if (!System.IO.File.Exists(filepath))
            {
                return;
            }
            this.m_texture = TextureLoader.FromFile(device, filepath);
        }

        #endregion

        
        private Thread m_backgroundThread = null;
        private bool active = false;
        private bool m_canDoShaders = false;
        private DateTime m_lastOpticalUpdate = DateTime.MinValue;

        private void UpdateOutter(DrawArgs drawArgs)
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
        private void Updater()
        {
            this.active = false;
        }
     
        private void device_DeviceReset(object sender, EventArgs e)
        {
            Device device = (Device)sender;

            try
            {
                string outerrors = "";

                string name = "R.Earth.Surface.Shades.SkyFromAtmosphere.fx";
                Stream skyFromAtmosphereStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

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
    }

    public class Sphere : IDisposable
    {
        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        public const float PI = 3.1415926535f;


        protected float m_fRadius;
        protected int m_nSlices;
        protected int m_nSections;

        protected double South;
        protected double North;
        protected double West;
        protected double East;

        protected BoundingBox m_Boundingbox;

        protected CustomVertex.PositionColoredTextured[] m_pVertex;
        protected short[] m_indicesHighResolution = null;


        protected int m_nVertices;

        public int GetVertexCount()
        {
            return this.m_nVertices;
        }
        public CustomVertex.PositionColoredTextured[] GetVertexBuffer()
        {
            return this.m_pVertex;
        }
        public BoundingBox BoundingBox
        {
            get { return this.m_Boundingbox; }
        }
       
        public void Init(float fRadius, int nSlices,double south,double north,double west,double east)
        {
            m_fRadius = fRadius;
            m_nSlices = nSlices;
            m_nSections = nSlices;

            this.South = south;
            this.North = north;
            this.West = west;
            this.East = east;

            this.m_Boundingbox = new BoundingBox(south, north, west, east, fRadius, fRadius + 10000.0);

            this.m_pVertex = this.CreateMesh(nSlices);
            this.m_indicesHighResolution = this.computeIndices(nSlices);
        }

        public void Draw(Device device)
        {

            device.DrawIndexedUserPrimitives(PrimitiveType.TriangleList, 0,
                this.m_pVertex.Length,
                m_indicesHighResolution.Length / 3,
                m_indicesHighResolution, true,
                this.m_pVertex);
        }

        private CustomVertex.PositionColoredTextured[] CreateMesh(int meshPointCount)
        {
            int upperBound = meshPointCount - 1;
            float scaleFactor = (float)1 / upperBound;
            double latrange = this.North - this.South;
            double lonrange = this.East - this.West;

            m_nVertices = meshPointCount * meshPointCount;

            CustomVertex.PositionColoredTextured[] vertices = new CustomVertex.PositionColoredTextured[meshPointCount * meshPointCount];

            for (int i = 0; i < meshPointCount; i++)
            {
                for (int j = 0; j < meshPointCount; j++)
                {
                    Vector3 pos = SMath.SphericalToCartesian(
                        this.North - scaleFactor * latrange * i,
                        this.West + scaleFactor * lonrange * j,
                        m_fRadius);

                    vertices[i * meshPointCount + j].X = pos.X;
                    vertices[i * meshPointCount + j].Y = pos.Y;
                    vertices[i * meshPointCount + j].Z = pos.Z;

                    vertices[i * meshPointCount + j].Tu = (float)j / meshPointCount;
                    vertices[i * meshPointCount + j].Tv = (float)i / meshPointCount;
                }
            }

            return vertices;
        }

        private short[] computeIndices(int meshPointCount)
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



    }    
}
