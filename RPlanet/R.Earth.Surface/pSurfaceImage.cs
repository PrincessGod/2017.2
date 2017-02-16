using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace R.Earth.Plugin
{
    public class pSurfaceImage : TechDemoPlugin
    {
        private string surfaceimagepath = Config.EarthSetting.RSourcePath + @"\Earth\TM02.jpg";
        private string Nm = "asurface";
        public override void Load()
        {
            return;
            //////////////////   
            SurfaceImage image = new SurfaceImage(Nm, this.surfaceimagepath);
            image.IsVisible = true;
            this.Viewer.CurrentWorld.RenderLayerList.Add(image);
        }
        public override void UnLoad()
        {
            base.UnLoad();
        }
    }


    public class SurfaceImage : GeoLayer
    {
        private Texture m_texture = null;
        private Mesh m_mesh = null;
        private string SurfaceImagePath = "";




        public override void UpdateMesh(DrawArgs drawArgs)
        {
            if (this.m_mesh != null)
            {
                this.m_mesh.Dispose();
            }
            this.m_mesh = this.CreatMesh(drawArgs.device, (float)World.EquatorialRadius, 72, 72);
        }

        public SurfaceImage(string name, string filepath)
            : base(name)
        {
            this.SurfaceImagePath = filepath;


        }
        public override void OnFrameMove(DrawArgs drawArgs)
        {
            if (!this.IsInitialized)
            {
                this.OnInitialize(drawArgs);
                return;
            }

        }
        public override void OnInitialize(DrawArgs drawArgs)
        {
            try
            {
                this.LoadTexture(drawArgs.device, this.SurfaceImagePath);
                if (this.m_mesh == null)
                {
                    this.m_mesh = this.CreatMesh(drawArgs.device, (float)World.EquatorialRadius, 72, 72);
                }
            }
            catch (Exception e)
            {
                Log.Write(e);
            }
            base.OnInitialize(drawArgs);
        }
        private Effect shader;

        public override void OnRender(DrawArgs drawArgs)
        {
            if (!this.IsInitialized)
            {
                return;
            }
            if (this.shader == null)
            {
                string erro = "";
                string name = "R.Earth.Shaders.grayscale.fx";
                System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                this.shader = Effect.FromStream(drawArgs.device, stream, null, null, ShaderFlags.None, null, out erro);
                if (erro != null && erro.Length > 0)
                {
                    Log.Write(erro);
                }
            }

            drawArgs.device.RenderState.CullMode = Cull.Clockwise;
            try
            {
                float time = (Environment.TickCount % 5000) / 5000.0f;
                shader.Technique = "RenderGrayscaleBrightness";

                shader.SetValue("Tex0", this.m_texture);

                shader.SetValue("Opacity", time);
                shader.SetValue("WorldViewProj", Matrix.Multiply(drawArgs.device.Transform.World, Matrix.Multiply(drawArgs.device.Transform.View, drawArgs.device.Transform.Projection)));

                shader.SetValue("Brightness", 2.0f);


                int numPasses = shader.Begin(0);
                for (int j = 0; j < numPasses; j++)
                {
                    shader.BeginPass(j);
                    this.m_mesh.DrawSubset(0);

                    shader.EndPass();
                }
                shader.End();
            }
            catch
            { }
            finally
            { }
        }

        public override void Dispose()
        {

        }

        private Mesh CreatMesh(Device device, float radius, int slices, int stacks)
        {
            int numVertices = (slices + 1) * (stacks + 1);
            int numFaces = slices * stacks * 2;
            int indexCount = numFaces * 3;


            Mesh mesh = new Mesh(numFaces, numVertices, MeshFlags.Managed, CustomVertex.PositionNormalTextured.Format, device);

            // Get the original sphere's vertex buffer.
            int[] ranks = new int[1];
            ranks[0] = mesh.NumberVertices;
            System.Array arr = mesh.VertexBuffer.Lock(0, typeof(CustomVertex.PositionNormalTextured), LockFlags.None, ranks);

            // Set the vertex buffer
            int vertIndex = 0;
            for (int stack = 0; stack <= stacks; stack++)
            {
                double latitude = -90 + ((float)stack / stacks * (float)180.0);
                for (int slice = 0; slice <= slices; slice++)
                {
                    CustomVertex.PositionNormalTextured pnt = new CustomVertex.PositionNormalTextured();
                    double longitude = 180 - ((float)slice / slices * (float)360);
                    Vector3 v = SMath.SphericalToCartesian(latitude, longitude, radius);
                    pnt.X = v.X;
                    pnt.Y = v.Y;
                    pnt.Z = v.Z;
                    pnt.Tu = 1.0f - (float)slice / slices;
                    pnt.Tv = 1.0f - (float)stack / stacks;
                    arr.SetValue(pnt, vertIndex++);

                }
            }

            mesh.VertexBuffer.Unlock();
            ranks[0] = indexCount;
            arr = mesh.LockIndexBuffer(typeof(short), LockFlags.None, ranks);
            int i = 0;
            short bottomVertex = 0;
            short topVertex = 0;
            for (short x = 0; x < stacks; x++)
            {
                bottomVertex = (short)((slices + 1) * x);
                topVertex = (short)(bottomVertex + slices + 1);
                for (int y = 0; y < slices; y++)
                {
                    arr.SetValue(bottomVertex, i++);
                    arr.SetValue(topVertex, i++);		// outside text.
                    arr.SetValue((short)(topVertex + 1), i++);	// outside text.
                    arr.SetValue(bottomVertex, i++);
                    arr.SetValue((short)(topVertex + 1), i++);	// outside text.
                    arr.SetValue((short)(bottomVertex + 1), i++); // outside text.
                    bottomVertex++;
                    topVertex++;
                }
            }
            mesh.IndexBuffer.SetData(arr, 0, LockFlags.None);
            mesh.ComputeNormals();
            mesh.IndexBuffer.Unlock();





            return mesh;
        }

        private void LoadTexture(Device device, string filepath)
        {
            if (!System.IO.File.Exists(filepath))
            {
                return;
            }
            this.m_texture = TextureLoader.FromFile(device, filepath);
        }


        public override void OnRenderOrtho(DrawArgs drawArgs)
        {
            if (!this.IsInitialized)
            {
                return;
            }
            try
            {
                Device device = drawArgs.device;

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                device.SetTexture(0, this.m_texture);

                device.TextureState[0].ColorOperation = TextureOperation.Modulate;
                device.TextureState[0].AlphaOperation = TextureOperation.BlendCurrentAlpha;

                this.m_mesh.DrawSubset(0);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}