using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace R.Earth
{
    public struct DoubleTextureVertex
    {
        private Vector3 position;
        private Vector3 normal;
        private float Tu, Tv;
        private float b1, b2, b3, b4;

        public DoubleTextureVertex(Vector3 p, Vector3 n, float u, float v, float B1, float B2, float B3, float B4, bool normalize)
        {
            this.position = p;
            this.normal = n;
            this.Tu = u;
            this.Tv = v;
            this.b1 = B1;
            this.b2 = B2;
            this.b3 = B3;
            this.b4 = B4;

            float total = b1 + b2 + b3 + b4;

            if (normalize)
            {
                b1 /= total;
                b2 /= total;
                b3 /= total;
                b4 /= total;
            }
        }

        public Vector3 Normal { get { return this.normal; } set { this.normal = value; } }
        public Vector3 Position { get { return this.position; } set { this.position = value; } }

        public static VertexFormats Format = VertexFormats.Position | VertexFormats.Normal | VertexFormats.Texture0 | VertexFormats.Texture1; 
    }
}
