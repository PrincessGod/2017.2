using System;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace R.Earth.WorldSurface
{
    public class DynamicTexture : System.IDisposable
    {
        public CustomVertex.PositionNormalTextured[] nwVerts;
        public CustomVertex.PositionNormalTextured[] neVerts;
        public CustomVertex.PositionNormalTextured[] swVerts;
        public CustomVertex.PositionNormalTextured[] seVerts;

        public DynamicTexture()
        {
        }

        #region IDisposable Members

        public void Dispose()
        {
        }
        #endregion
    }
}
