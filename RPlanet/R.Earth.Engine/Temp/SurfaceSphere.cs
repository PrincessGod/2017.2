using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace R.Earth
{
    public class SurfaceSphere :GeoLayer
    {
        private SphereModel m_model;

        public SurfaceSphere(string name)
            : base(name)
        {
 
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
            try { this.m_model = new SphereModel((float)World.EquatorialRadius, 18, 18); this.m_model.Initialize(drawArgs.device); }
            catch (Exception e) { Log.Write(e); }
            base.OnInitialize(drawArgs);
        }
        public override void OnRender(DrawArgs drawArgs)
        {
            if (!this.IsInitialized || !this.IsVisible)
            {
                return;
            }
            if (this.m_model == null)
            {
                return;
            }


            //Matrix WorldMatrix = drawArgs.device.Transform.World;
            //TextureOperation co = drawArgs.device.TextureState[0].ColorOperation;
            //TextureOperation ao = drawArgs.device.TextureState[0].AlphaOperation;
            //float size = drawArgs.device.RenderState.PointSize;

            //bool zwrite = drawArgs.device.RenderState.ZBufferWriteEnable;
            //bool isalpha = drawArgs.device.RenderState.AlphaBlendEnable;
            //bool issprite = drawArgs.device.RenderState.PointSpriteEnable;
            //bool isscale = drawArgs.device.RenderState.PointScaleEnable;

            //FillMode fill = drawArgs.device.RenderState.FillMode;
            //try
            //{
            //    Device device = drawArgs.device;

            //    device.Transform.World = Microsoft.DirectX.Matrix.Translation(
            //        -(float)drawArgs.WorldCamera.ReferenceCenter.X,
            //        -(float)drawArgs.WorldCamera.ReferenceCenter.Y,
            //        -(float)drawArgs.WorldCamera.ReferenceCenter.Z);

                           
            //    device.RenderState.ZBufferWriteEnable = false;
            //    device.RenderState.AlphaBlendEnable = true;

            //    device.RenderState.PointSpriteEnable = true;
            //    device.RenderState.PointScaleEnable = false;

            //    device.RenderState.PointSizeMin = 0.00f;
            //    device.RenderState.PointScaleA = 0.00f;
            //    device.RenderState.PointScaleB = 0.00f;
            //    device.RenderState.PointScaleC = 1.00f;

            //    device.RenderState.PointSize = 2.0f;
            //    device.TextureState[0].ColorOperation = TextureOperation.Modulate;
            //    device.TextureState[0].AlphaOperation = TextureOperation.BlendCurrentAlpha;

            //    device.RenderState.FillMode = FillMode.Point;

            //    float scale = 10.0f * 1.0f;
            //    device.Transform.World = Matrix.Identity;
            //    device.Transform.World = Matrix.Scaling(scale, scale, scale);
            //    device.Transform.World *= Matrix.RotationZ((float)(Math.PI * 2.0));

            //    device.Transform.World *= Matrix.Translation(0, 0, 0);



            //    device.Transform.World *= Matrix.Translation(
            //        (float)-drawArgs.WorldCamera.ReferenceCenter.X,
            //        (float)-drawArgs.WorldCamera.ReferenceCenter.Y,
            //        (float)-drawArgs.WorldCamera.ReferenceCenter.Z);

            //    this.m_model.Render(drawArgs.device);
            //}
            //catch (Exception e)
            //{
            //}
            //finally
            //{
            //    drawArgs.device.RenderState.FillMode = fill;
            //    drawArgs.device.Transform.World = WorldMatrix;
            //    drawArgs.device.TextureState[0].ColorOperation = co;
            //    drawArgs.device.TextureState[0].AlphaOperation = ao;
            //    drawArgs.device.RenderState.PointSize = size;
            //    drawArgs.device.RenderState.ZBufferWriteEnable = zwrite;

            //    drawArgs.device.RenderState.AlphaBlendEnable = isalpha;
            //    drawArgs.device.RenderState.PointSpriteEnable = issprite;
            //    drawArgs.device.RenderState.PointScaleEnable = isscale;
            //}
         


            
        }

        public override void UpdateMesh(DrawArgs drawArgs)
        {
            
        }

        public override void OnRenderOrtho(DrawArgs drawArgs)
        {
        }

        public override void Dispose()
        {
        }
    }


    public class SphereModel : IDisposable
    {
        #region IDisposable Members

        public void Dispose()
        {

        }

        #endregion

        private VertexBuffer vb;
        private IndexBuffer ib;
        private VertexDeclaration decl;
        private float radius;
        private int numvertices, numindices, numtriangles;
        private int slice, stack;

        public SphereModel(float radius, int slice, int stack)
        {
            this.radius = radius;
            this.slice = slice;
            this.stack = stack;
        }
        public void Initialize(Device device)
        {
            this.numvertices = (this.slice + 1) * (this.stack + 1);
            this.numtriangles = this.slice * this.stack * 2;
            this.numindices = this.numtriangles * 3;

            DoubleTextureVertex[] verts = new DoubleTextureVertex[this.numvertices];
            int[] ind = new int[this.numindices];

            double stacksize = 180.0 / stack;
            double slicesize = 360.0 / slice;
            int index = 0;

            for (int i = 0; i <= stack; i++)
            {
                double lat = -90.0 + i * stacksize;
                float tv = (float)(i * 1.0 / stack);

                for (int j = 0; j <= slice; j++)
                {
                    double lon = -180.0 + j * slicesize;
                    Vector3 v = SMath.SphericalToCartesian(lat, lon, this.radius);
                    Vector3 n = Vector3.Normalize(v);
                    verts[index++] = new DoubleTextureVertex(
                        v,
                        n,
                        (float)(j * 1.0 / slice),
                        tv,
                        0.5f, 0.5f, 0, 0, false);
                }
            }

            int io = 0;
            int bottomVertex = 0;
            int topVertex = 0;

            for (int x = 0; x < stack; x++)
            {
                bottomVertex = (slice + 1) * x;
                topVertex = (bottomVertex + slice + 1);

                for (int y = 0; y < slice; y++)
                {
                    ind[io++] = bottomVertex;
                    ind[io++] = topVertex;
                    ind[io++] = topVertex + 1;
                    ind[io++] = bottomVertex;
                    ind[io++] = topVertex + 1;
                    ind[io++] = bottomVertex + 1;
                }
            }

            this.vb = new VertexBuffer(typeof(DoubleTextureVertex),
                this.numvertices,
                device,
                Usage.Dynamic,
                DoubleTextureVertex.Format,
                Pool.Default);
            this.vb.SetData(verts, 0, 0);

            this.ib = new IndexBuffer(typeof(int), this.numindices,
                device, 0, 0);
            this.ib.SetData(ind, 0, 0);

            // Create the vertexdeclaration
            VertexElement[] vs = new VertexElement[] { new VertexElement(0,0,DeclarationType.Float3,DeclarationMethod.Default,DeclarationUsage.Position,0), 
														new VertexElement(0,12,DeclarationType.Float3,DeclarationMethod.Default,DeclarationUsage.Normal,0),
														new VertexElement(0,24,DeclarationType.Float2,DeclarationMethod.Default,DeclarationUsage.TextureCoordinate,0),
														new VertexElement(0,32,DeclarationType.Float4,DeclarationMethod.Default,DeclarationUsage.TextureCoordinate,1), 
														VertexElement.VertexDeclarationEnd};

            this.decl = new VertexDeclaration(device, vs);

        }

        public void Render(Device device)
        {
            device.VertexDeclaration = decl;
            //device.SetStreamSource(0, this.vb, 0);
            //device.Indices = this.ib;

            device.DrawIndexedUserPrimitives(PrimitiveType.TriangleList, 0, 0, this.numtriangles, this.ib, false, this.vb);
        }
    }
}
