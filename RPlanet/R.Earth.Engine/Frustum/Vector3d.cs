using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;

namespace R.Earth
{
    /// <summary>
    /// Summary description for Vector3d.
    /// </summary>
    public struct Vector3d
    {
        public double X, Y, Z;
        // constructors

        public Vector3d(double xi, double yi, double zi)	// x,y,z constructor
        {
            X = xi; Y = yi; Z = zi;
        }

        public static Vector3d Empty
        {
            get
            {
                return new Vector3d(0, 0, 0);
            }
        }

        public Vector3 Vector()
        {
            return new Vector3((float)X, (float)Y, (float)Z);
        }
        public void MultiplyMatrix(Matrix4d m, ref double w)
        {
            double wprev = w;
            w = this.X * m[0, 3] + this.Y * m[1, 3] + this.Z * m[2, 3] + wprev * m[3, 3];
            this = new Vector3d(this.X * m[0, 0] + this.Y * m[1, 0] + this.Z * m[2, 0] + wprev * m[3, 0],
               this.X * m[0, 1] + this.Y * m[1, 1] + this.Z * m[2, 1] + wprev * m[3, 1],
               this.X * m[0, 2] + this.Y * m[1, 2] + this.Z * m[2, 2] + wprev * m[3, 2]);
        }

        public void Unproject(object viewport, Matrix4d projection, Matrix4d view, Matrix4d world)
        {
            //Microsoft.DirectX.Vector3 test = ConvertDX.FromVector3d(this);
            //test.Unproject(viewport, ConvertDX.FromMatrix4d(projection), ConvertDX.FromMatrix4d(view), ConvertDX.FromMatrix4d(world));

            Viewport2d vp = ConvertDX.ToViewport2d((Microsoft.DirectX.Direct3D.Viewport)viewport);

            // Convert from viewport coordinates 
            this.X = (this.X - vp.X) / vp.Width;
            this.Y = (vp.Height + vp.Y - this.Y) / vp.Height;

            // Make x/y range from -1 to 1 
            this.X = this.X * 2.0 - 1.0;
            this.Y = this.Y * 2.0 - 1.0;

            Matrix4d m = Matrix4d.Invert(world * view * projection);
            double w = 1.0;
            MultiplyMatrix(m, ref w);

            this.X /= w;
            this.Y /= w;
            this.Z /= w;
        }

        public void Project(object viewport, Matrix4d projection, Matrix4d view, Matrix4d world)
        {
            //Microsoft.DirectX.Vector3 test = ConvertDX.FromVector3d(this);
            //test.Project(viewport, ConvertDX.FromMatrix4d(projection), ConvertDX.FromMatrix4d(view), ConvertDX.FromMatrix4d(world));

            Viewport2d vp = ConvertDX.ToViewport2d((Microsoft.DirectX.Direct3D.Viewport)viewport);

            Matrix4d m = world * view * projection;
            double w = 1.0;
            MultiplyMatrix(m, ref w);

            this.X /= w;
            this.Y /= w;
            this.Z /= w;

            // Make x/y range from 0 to 1 
            this.X = this.X * 0.5 + 0.5;
            this.Y = this.Y * 0.5 + 0.5;

            // Convert to to viewport coordinates (Y is inverted)
            this.X = this.X * vp.Width + vp.X;
            this.Y = vp.Height - this.Y * vp.Height + vp.Y;
        }

        // Override the Object.GetHashCode() method:
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }


        // Override the Object.Equals(object o) method:
        public override bool Equals(object o)
        {
            try
            {
                return this.X == ((Vector3d)o).X && this.Y == ((Vector3d)o).Y && this.Z == ((Vector3d)o).Z;
            }
            catch
            {
                return false;
            }
        }



        public static Angle GetAngle(Vector3d p1, Vector3d p2)
        {
            Angle returnAngle = new Angle();
            returnAngle.Radians = Math.Acos(Vector3d.dot(p1, p2) / (p1.Length * p2.Length));
            return returnAngle;
        }

        public double Length
        {
            get
            {
                return Math.Sqrt(X * X + Y * Y + Z * Z);
            }
        }

        public double LengthSq
        {
            get
            {
                return X * X + Y * Y + Z * Z;
            }
        }

        public static Vector3d normalize(Vector3d v) // normalization
        {
            double n = v.Length;
            return new Vector3d(v.X / n, v.Y / n, v.Z / n);
        }

        public void normalize() // normalization
        {
            double n = Length;
            this.X /= n; this.Y /= n; this.Z /= n;
        }

        public void scale(double scale) // normalization
        {
            this.X *= scale; this.Y *= scale; this.Z *= scale;
        }

        public static Vector3d operator +(Vector3d P1, Vector3d P2)	// addition 2
        {
            return new Vector3d(P1.X + P2.X, P1.Y + P2.Y, P1.Z + P2.Z);
        }

        public static Vector3d operator -(Vector3d P1, Vector3d P2)	// subtraction 2
        {
            return new Vector3d(P1.X - P2.X, P1.Y - P2.Y, P1.Z - P2.Z);
        }

        public static Vector3d operator *(Vector3d P, double k)	// multiply by real 2
        {
            return new Vector3d(P.X * k, P.Y * k, P.Z * k);
        }

        public static Vector3d operator *(double k, Vector3d P)	// and its reverse order!
        {
            return new Vector3d(P.X * k, P.Y * k, P.Z * k);
        }

        public static Vector3d operator /(Vector3d P, double k)	// divide by real 2
        {
            return new Vector3d(P.X / k, P.Y / k, P.Z / k);
        }

        public static Vector3d operator -(Vector3d P)	// negation
        {
            return new Vector3d(-P.X, -P.Y, -P.Z);
        }


        public static Vector3d cross(Vector3d P1, Vector3d P2) // cross product
        {
            return P1 * P2;
        }

        // Normal direction corresponds to a right handed traverse of ordered points.
        public static Vector3d unit_normal(Vector3d P0, Vector3d P1, Vector3d P2)
        {
            Vector3d p = (P1 - P0) * (P2 - P0);
            double l = p.Length;
            return new Vector3d(p.X / l, p.Y / l, p.Z / l);
        }

        public static bool operator ==(Vector3d P1, Vector3d P2) // equal?
        {
            return (P1.X == P2.X && P1.Y == P2.Y && P1.Z == P2.Z);
        }

        public static bool operator !=(Vector3d P1, Vector3d P2) // equal?
        {
            return (P1.X != P2.X || P1.Y != P2.Y || P1.Z != P2.Z);
        }

        public static double dot(Vector3d P1, Vector3d P2) // inner product 2
        {
            return (P1.X * P2.X + P1.Y * P2.Y + P1.Z * P2.Z);
        }

        public static Vector3d operator *(Vector3d P1, Vector3d P2)
        {
            return new Vector3d(P1.Y * P2.Z - P1.Z * P2.Y,
               P1.Z * P2.X - P1.X * P2.Z, P1.X * P2.Y - P1.Y * P2.X);
        }

        public override string ToString()
        {
            return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
        }

        public static Vector3d FromVector3(Vector3 vector3)
        {
            return new Vector3d((double)vector3.X, (double)vector3.Y, (double)vector3.Z);
        }
    }
}
