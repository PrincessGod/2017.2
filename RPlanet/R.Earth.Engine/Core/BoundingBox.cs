using System;

namespace R.Earth
{
    /// <summary>
    /// The closed volume that completely contains a set of objects.
    /// </summary>
    public class BoundingBox
    {
        public readonly Vector3d[] corners = new Vector3d[8];
        public readonly BoundingSphere boundsphere = new BoundingSphere(new Vector3d(0.0, 0.0, 0.0), 0.0);

        /// <summary>
        /// Initializes a new instance of the <see cref= "T:WorldWind.BoundingSphere"/> class.
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="v4"></param>
        /// <param name="v5"></param>
        /// <param name="v6"></param>
        /// <param name="v7"></param>
        public BoundingBox(Vector3d v0, Vector3d v1, Vector3d v2, Vector3d v3, Vector3d v4, Vector3d v5, Vector3d v6, Vector3d v7)
        {
            Update(v0, v1, v2, v3, v4, v5, v6, v7);
        }
        public void Update(Vector3d v0, Vector3d v1, Vector3d v2, Vector3d v3, Vector3d v4, Vector3d v5, Vector3d v6, Vector3d v7)
        {
            this.corners[0] = v0;
            this.corners[1] = v1;
            this.corners[2] = v2;
            this.corners[3] = v3;
            this.corners[4] = v4;
            this.corners[5] = v5;
            this.corners[6] = v6;
            this.corners[7] = v7;

            ComputeBoundSphere();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref= "T:WorldWind.BoundingSphere"/> class
        /// from a set of lat/lon values (degrees)
        /// </summary>
        /// <param name="south"></param>
        /// <param name="north"></param>
        /// <param name="west"></param>
        /// <param name="east"></param>
        /// <param name="radius1"></param>
        /// <param name="radius2"></param>
        public BoundingBox(double south, double north, double west, double east, double radius1, double radius2)
        {
            Update(south, north, west, east, radius1, radius2);
        }
        public void Update(double south, double north, double west, double east, double radius1, double radius2)
        {
            double scale = radius2 / radius1;
            SMath.SphericalToCartesianV3D(south, west, radius1, ref this.corners[0]);
            this.corners[1] = this.corners[0];
            this.corners[1].scale(scale);
            SMath.SphericalToCartesianV3D(south, east, radius1, ref this.corners[2]);
            this.corners[3] = this.corners[2];
            this.corners[3].scale(scale);
            SMath.SphericalToCartesianV3D(north, west, radius1, ref this.corners[4]);
            this.corners[5] = this.corners[4];
            this.corners[5].scale(scale);
            SMath.SphericalToCartesianV3D(north, east, radius1, ref this.corners[6]);
            this.corners[7] = this.corners[6];
            this.corners[7].scale(scale);

            ComputeBoundSphere();
        }

        public void ComputeBoundSphere()
        {
            this.boundsphere.Center.X = 0.0;
            this.boundsphere.Center.Y = 0.0;
            this.boundsphere.Center.Z = 0.0;
            this.boundsphere.RadiusSq = 0.0;

            //Find the center.  In this case, we'll simply average the coordinates. 
            foreach (Vector3d v in this.corners)
                this.boundsphere.Center += v;
            this.boundsphere.Center *= 1.0 / 8.0;

            //Loop through the coordinates and find the maximum distance from the center.  This is the radius.		
            foreach (Vector3d v in this.corners)
            {
                double distSq = (v - this.boundsphere.Center).LengthSq;
                if (distSq > this.boundsphere.RadiusSq)
                    this.boundsphere.RadiusSq = distSq;
            }
        }

        /// <summary>
        /// Calculate the screen area (pixels) covered by the bottom of the bounding box.
        /// </summary>
        public double CalcRelativeScreenArea(CameraBase camera)
        {
            Vector3d a = camera.Project(corners[0]);
            Vector3d b = camera.Project(corners[2]);
            Vector3d c = camera.Project(corners[6]);
            Vector3d d = camera.Project(corners[4]);

            Vector3d ab = b - a;
            Vector3d ac = c - a;
            Vector3d ad = d - a;

            double tri1SqArea = Vector3d.cross(ab, ac).LengthSq;
            double tri2SqArea = Vector3d.cross(ad, ac).LengthSq;
            // Real area = (sqrt(tri1SqArea)+sqrt(tri2SqArea))/2 but we're only interested in relative size
            return tri1SqArea + tri2SqArea;
        }

        public override string ToString()
        {
            return String.Format("corner(0):\n{0}\ncorner(1):\n{1}\ncorner(2):\n{2}\ncorner(3):\n{3}\ncorner(4):\n{4}\ncorner(5):\n{5}\ncorner(6):\n{6}\ncorner(7):\n{7}\n",
               corners[0], corners[1], corners[2], corners[3], corners[4], corners[5], corners[6], corners[7]);
        }
    }

    public class GeographicBoundingBox
    {
        public double North;
        public double South;
        public double West;
        public double East;

        public GeographicBoundingBox()
        {
            North = 90;
            South = -90;
            West = -180;
            East = 180;
        }
        public GeographicBoundingBox(double north, double south, double west, double east)
        {
            North = north;
            South = south;
            West = west;
            East = east;
        }

        public static GeographicBoundingBox FromQuad(GeographicQuad quad)
        {
            return new GeographicBoundingBox(Math.Max(Math.Max(Math.Max(quad.Y1, quad.Y2), quad.Y3), quad.Y4),
               Math.Min(Math.Min(Math.Min(quad.Y1, quad.Y2), quad.Y3), quad.Y4),
               Math.Min(Math.Min(Math.Min(quad.X1, quad.X2), quad.X3), quad.X4),
               Math.Max(Math.Max(Math.Max(quad.X1, quad.X2), quad.X3), quad.X4));
        }

        public bool IntersectsWith(GeographicBoundingBox test)
        {
            return (test.West < this.East && this.West < test.East && test.South < this.North && this.South < test.North);
        }

        public bool Contains(GeographicBoundingBox test)
        {
            return (test.West >= this.West && test.East <= this.East && test.South >= this.South && test.North < this.North);
        }
    }

    public class GeographicQuad
    {
        public double X1, Y1; // lower left
        public double X2, Y2; // lower right
        public double X3, Y3; // upper right
        public double X4, Y4; // upper left

        public GeographicQuad(double _X1, double _Y1, double _X2, double _Y2, double _X3, double _Y3, double _X4, double _Y4)
        {
            X1 = _X1; Y1 = _Y1;
            X2 = _X2; Y2 = _Y2;
            X3 = _X3; Y3 = _Y3;
            X4 = _X4; Y4 = _Y4;
        }
    }
}
