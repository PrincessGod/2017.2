using System;
using Microsoft.DirectX;


namespace R.Earth
{
    public sealed class SMath
    {
        internal static double MeterPerDegree()
        {
            double m = Math.PI * World.EquatorialRadius / 180.0;
            return m;
        }
        
        /// <summary>
        /// 面向我的为X，我的右手为Y，我的上方为Z
        /// 单位为度数
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Vector3 SphericalToCartesian(double latitude, double longitude, double radius)
        {
            Vector3 v = new Vector3();

            if (World.Settings.Project == Projection.Perspective)
            {
                latitude *= System.Math.PI / 180.0f;
                longitude *= System.Math.PI / 180.0f;

                double radCosLat = radius * Math.Cos(latitude);

                v.X = (float)(radCosLat * Math.Cos(longitude));
                v.Y = (float)(radCosLat * Math.Sin(longitude));
                v.Z = (float)(radius * Math.Sin(latitude));
            }
            else
            {
                v.X = (float)(radius);
                v.Y = (float)(longitude * World.MeterPerDegree);
                v.Z = (float)(latitude * World.MeterPerDegree);
            }
            return v;
        }
        /// <summary>
        /// Converts position in spherical coordinates (lat/lon/altitude) to cartesian (XYZ) coordinates.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees</param>
        /// <param name="longitude">Longitude in decimal degrees</param>
        /// <param name="radius">Radius (OBS: not altitude)</param>
        /// <returns>Coordinates converted to cartesian (XYZ)</returns>
        public static Vector3d SphericalToCartesianV3D(
            double latitude,
            double longitude,
            double radius)
        {
            Vector3d v = new Vector3d();
            if (World.Settings.Project == Projection.Perspective)
            {
                latitude *= System.Math.PI / 180.0f;
                longitude *= System.Math.PI / 180.0f;

                double radCosLat = radius * Math.Cos(latitude);

                v.X = radCosLat * Math.Cos(longitude);
                v.Y = radCosLat * Math.Sin(longitude);
                v.Z = radius * Math.Sin(latitude);

            }
            else
            {
                v.X = (radius );
                v.Y = longitude * World.MeterPerDegree;
                v.Z = latitude * World.MeterPerDegree;
            }

            return v;
        }

        /// <summary>
        /// Converts position in spherical coordinates (lat/lon/altitude) to cartesian (XYZ) coordinates.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees</param>
        /// <param name="longitude">Longitude in decimal degrees</param>
        /// <param name="radius">Radius (OBS: not altitude)</param>
        /// <returns>Coordinates converted to cartesian (XYZ)</returns>
        public static void SphericalToCartesianV3D(
            double latitude,
            double longitude,
            double radius,
         ref Vector3d v
            )
        {
            v = SphericalToCartesianV3D(latitude, longitude, radius);
        }
        /// <summary>
        /// Converts position in cartesian coordinates (XYZ) to spherical (lat/lon/radius) coordinates in radians.
        /// </summary>
        /// <returns>Coordinates converted to spherical coordinates.  X=radius, Y=latitude (radians), Z=longitude (radians).</returns>
        public static Vector3d CartesianToSpherical(double x, double y, double z)
        {
            double rho = Math.Sqrt((x * x + y * y + z * z));
            double longitude = Math.Atan2(y, x);
            double latitude = (Math.Asin(z / rho));

            return new Vector3d(rho, latitude, longitude);
        }

        /// <summary>
        /// 将世界坐标系的表示方法转换为经纬度坐标系，返回为vector3（x = 半径， y = 纬度，z = 经度）
        /// Converts position in cartesian coordinates (XYZ) to spherical (lat/lon/radius) coordinates in radians.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Coordinates converted to spherical coordinates.  X=radius, Y=latitude (radians), Z=longitude (radians).</returns>

        public static Vector3 CartesianToSpherical(float x, float y, float z)
        {
            double rho = Math.Sqrt((double)(x * x + y * y + z * z));
            float longitude = (float)Math.Atan2(y, x);
            float latitude = (float)(Math.Asin(z / rho));

            return new Vector3((float)rho, latitude, longitude);
        }

        /// <summary>
        /// 将角度转换为弧度
        /// Converts an angle in decimal degrees to angle in radians
        /// </summary>
        /// <param name="degrees">角度，取值在（0-360）之间 Angle in decimal degrees (0-360)</param>
        /// <returns>弧度，取值在（0 - 2* PI）之间 Angle in radians (0-2*Pi)</returns>
        public static double DegreesToRadians(double degrees)
        {
            return Math.PI * degrees / 180.0f;
        }

        /// <summary>
        /// 将弧度转换为角度
        /// Converts an angle in radians to angle in decimal degrees 
        /// </summary>
        /// <param name="radians">弧度，取值在（0 - 2* PI）之间  Angle in radians (0-2*Pi)</param>
        /// <returns>角度，取值在（0-360）之间  Angle in decimal degrees (0-360)</returns>
        public static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }


        /// <summary>
        /// Computes the angular distance between two pairs of lat/longs.
        /// Fails for distances (on earth) smaller than approx. 2km. (returns 0)
        /// </summary>
        public static Angle SphericalDistance(Angle latA, Angle lonA, Angle latB, Angle lonB)
        {
            double radLatA = latA.Radians;
            double radLatB = latB.Radians;
            double radLonA = lonA.Radians;
            double radLonB = lonB.Radians;

            return Angle.FromRadians(Math.Acos(
                Math.Cos(radLatA) * Math.Cos(radLatB) * Math.Cos(radLonA - radLonB) +
                Math.Sin(radLatA) * Math.Sin(radLatB)));
        }


        /// <summary>
        /// Transforms a set of Euler angles to a quaternion
        /// </summary>
        /// <param name="yaw">Yaw (radians)</param>
        /// <param name="pitch">Pitch (radians)</param>
        /// <param name="roll">Roll (radians)</param>
        /// <returns>The rotation transformed to a quaternion.</returns>
        public static Quaternion EulerToQuaternion(double yaw, double pitch, double roll)
        {
            double cy = Math.Cos(yaw * 0.5);
            double cp = Math.Cos(pitch * 0.5);
            double cr = Math.Cos(roll * 0.5);
            double sy = Math.Sin(yaw * 0.5);
            double sp = Math.Sin(pitch * 0.5);
            double sr = Math.Sin(roll * 0.5);

            double qw = cy * cp * cr + sy * sp * sr;
            double qx = sy * cp * cr - cy * sp * sr;
            double qy = cy * sp * cr + sy * cp * sr;
            double qz = cy * cp * sr - sy * sp * cr;

            return new Quaternion((float)qx, (float)qy, (float)qz, (float)qw);
        }



        /// <summary>
        /// Computes the great circle distance between two pairs of lat/longs.
        /// TODO: Compute distance using ellipsoid.
        /// </summary>
        public static Angle ApproxAngularDistance(Angle latA, Angle lonA, Angle latB, Angle lonB)
        {
            Angle dlon = lonB - lonA;
            Angle dlat = latB - latA;
            double k = Math.Sin(dlat.Radians * 0.5);
            double l = Math.Sin(dlon.Radians * 0.5);
            double a = k * k + Math.Cos(latA.Radians) * Math.Cos(latB.Radians) * l * l;
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            return Angle.FromRadians(c);
        }

        /// <summary>
        /// Computes the distance between two pairs of lat/longs in meters.
        /// </summary>
        public static double ApproxDistance(Angle latA, Angle lonA, Angle latB, Angle lonB)
        {
            double distance = World.EquatorialRadius * ApproxAngularDistance(latA, lonA, latB, lonB).Radians;
            return distance;
        }

        /// <summary>
        /// Intermediate points on a great circle
        /// In previous sections we have found intermediate points on a great circle given either
        /// the crossing latitude or longitude. Here we find points (lat,lon) a given fraction of the
        /// distance (d) between them. Suppose the starting point is (lat1,lon1) and the final point
        /// (lat2,lon2) and we want the point a fraction f along the great circle route. f=0 is
        /// point 1. f=1 is point 2. The two points cannot be antipodal ( i.e. lat1+lat2=0 and
        /// abs(lon1-lon2)=pi) because then the route is undefined.
        /// </summary>
        /// <param name="f">Fraction of the distance for intermediate point (0..1)</param>
        public static void IntermediateGCPoint(float f, Angle lat1, Angle lon1, Angle lat2, Angle lon2, Angle d,
            out Angle lat, out Angle lon)
        {
            double sind = Math.Sin(d.Radians);
            double cosLat1 = Math.Cos(lat1.Radians);
            double cosLat2 = Math.Cos(lat2.Radians);
            double A = Math.Sin((1 - f) * d.Radians) / sind;
            double B = Math.Sin(f * d.Radians) / sind;
            double x = A * cosLat1 * Math.Cos(lon1.Radians) + B * cosLat2 * Math.Cos(lon2.Radians);
            double y = A * cosLat1 * Math.Sin(lon1.Radians) + B * cosLat2 * Math.Sin(lon2.Radians);
            double z = A * Math.Sin(lat1.Radians) + B * Math.Sin(lat2.Radians);
            lat = Angle.FromRadians(Math.Atan2(z, Math.Sqrt(x * x + y * y)));
            lon = Angle.FromRadians(Math.Atan2(y, x));
        }

        /// <summary>
        /// Intermediate points on a great circle
        /// In previous sections we have found intermediate points on a great circle given either
        /// the crossing latitude or longitude. Here we find points (lat,lon) a given fraction of the
        /// distance (d) between them. Suppose the starting point is (lat1,lon1) and the final point
        /// (lat2,lon2) and we want the point a fraction f along the great circle route. f=0 is
        /// point 1. f=1 is point 2. The two points cannot be antipodal ( i.e. lat1+lat2=0 and
        /// abs(lon1-lon2)=pi) because then the route is undefined.
        /// </summary>
        /// <param name="f">Fraction of the distance for intermediate point (0..1)</param>
        public Vector3 IntermediateGCPoint(float f, Angle lat1, Angle lon1, Angle lat2, Angle lon2, Angle d)
        {
            double sind = Math.Sin(d.Radians);
            double cosLat1 = Math.Cos(lat1.Radians);
            double cosLat2 = Math.Cos(lat2.Radians);
            double A = Math.Sin((1 - f) * d.Radians) / sind;
            double B = Math.Sin(f * d.Radians) / sind;
            double x = A * cosLat1 * Math.Cos(lon1.Radians) + B * cosLat2 * Math.Cos(lon2.Radians);
            double y = A * cosLat1 * Math.Sin(lon1.Radians) + B * cosLat2 * Math.Sin(lon2.Radians);
            double z = A * Math.Sin(lat1.Radians) + B * Math.Sin(lat2.Radians);
            Angle lat = Angle.FromRadians(Math.Atan2(z, Math.Sqrt(x * x + y * y)));
            Angle lon = Angle.FromRadians(Math.Atan2(y, x));

            Vector3 v = SMath.SphericalToCartesian(lat.Degrees, lon.Degrees,World.EquatorialRadius);
            return v;
        }


        /// Compute the tile number (used in file names) for given latitude and tile size.
        /// </summary>
        /// <param name="latitude">Latitude (decimal degrees)</param>
        /// <param name="tileSize">Tile size  (decimal degrees)</param>
        /// <returns>The tile number</returns>
        public static int GetRowFromLatitude(double latitude, double tileSize)
        {
            return (int)System.Math.Round((System.Math.Abs(-90.0 - latitude) % 180) / tileSize, 1);
        }

        /// <summary>
        /// Compute the tile number (used in file names) for given latitude and tile size.
        /// </summary>
        /// <param name="latitude">Latitude (decimal degrees)</param>
        /// <param name="tileSize">Tile size  (decimal degrees)</param>
        /// <returns>The tile number</returns>
        public static int GetRowFromLatitude(Angle latitude, double tileSize)
        {
            return (int)System.Math.Round((System.Math.Abs(-90.0 - latitude.Degrees) % 180) / tileSize, 1);
        }

        /// <summary>
        /// Compute the tile number (used in file names) for given longitude and tile size.
        /// </summary>
        /// <param name="longitude">Longitude (decimal degrees)</param>
        /// <param name="tileSize">Tile size  (decimal degrees)</param>
        /// <returns>The tile number</returns>
        public static int GetColFromLongitude(double longitude, double tileSize)
        {
            return (int)System.Math.Round((System.Math.Abs(-180.0 - longitude) % 360) / tileSize, 1);
        }

        /// <summary>
        /// Compute the tile number (used in file names) for given longitude and tile size.
        /// </summary>
        /// <param name="longitude">Longitude (decimal degrees)</param>
        /// <param name="tileSize">Tile size  (decimal degrees)</param>
        /// <returns>The tile number</returns>
        public static int GetColFromLongitude(Angle longitude, double tileSize)
        {
            return (int)System.Math.Round((System.Math.Abs(-180.0 - longitude.Degrees) % 360) / tileSize, 1);
        }


        /// <summary>
        /// 已知两直线的斜率，返回此两直线角平分线的斜率.
        /// 指定A到B为顺时针方向。也就是A绕两线的交点到B为顺时针方向
        /// </summary>
        /// <param name="A">交点左边的线的斜率</param>
        /// <param name="B">交点右边的线的斜率</param>
        /// <returns></returns>
        public static double GetAngleBisectorSlope(double A, double B)
        {
            if (A + B == 0)
            {
                return double.MaxValue;
            }
            else
            {
                double k;
                double sq = (A * A + 1) * (B * B + 1);
                double up = Math.Sqrt(sq) + A * B - 1;
                k = up / (A + B);
                return k;
            }
        }

        /// <summary>
        /// Calculates the azimuth from latA/lonA to latB/lonB
        /// Borrowed from http://williams.best.vwh.net/avform.htm
        /// </summary>
        public static Angle Azimuth(Angle latA, Angle lonA, Angle latB, Angle lonB)
        {
            double cosLatB = Math.Cos(latB.Radians);
            Angle tcA = Angle.FromRadians(Math.Atan2(
                Math.Sin(lonA.Radians - lonB.Radians) * cosLatB,
                Math.Cos(latA.Radians) * Math.Sin(latB.Radians) -
                Math.Sin(latA.Radians) * cosLatB *
                Math.Cos(lonA.Radians - lonB.Radians)));
            if (tcA.Radians < 0)
                tcA.Radians = tcA.Radians + Math.PI * 2;
            tcA.Radians = Math.PI * 2 - tcA.Radians;

            return tcA;
        }

        /// <summary>
        /// 输出对应插值的Point3d（纬度，经度，高程）。高程值为当前经纬度下的最高精度值。
        /// 空间插值,通过首尾两点的经纬度计算,由f来控制此点的次序，f=n/N;在0~1之间
        /// </summary>
        /// <param name="Args"></param>
        /// <param name="f"></param>
        /// <param name="lat1"></param>
        /// <param name="lon1"></param>
        /// <param name="lat2"></param>
        /// <param name="lon2"></param>
        /// <returns></returns>
        public static Vector3d CacuInterGCPoint(float f, Angle lat1, Angle lon1, Angle lat2, Angle lon2)
        {
            Angle lat;
            Angle lon;
            Double Z = new double();
   
            Angle d = SMath.ApproxAngularDistance(lat1.Radians, lon1.Radians, lat2.Radians, lon2.Radians);
            double sind = Math.Sin(d.Radians);
            double cosLat1 = Math.Cos(lat1.Radians);
            double cosLat2 = Math.Cos(lat2.Radians);
            double A = Math.Sin((1 - f) * d.Radians) / sind;
            double B = Math.Sin(f * d.Radians) / sind;
            double x = A * cosLat1 * Math.Cos(lon1.Radians) + B * cosLat2 * Math.Cos(lon2.Radians);
            double y = A * cosLat1 * Math.Sin(lon1.Radians) + B * cosLat2 * Math.Sin(lon2.Radians);
            double z = A * Math.Sin(lat1.Radians) + B * Math.Sin(lat2.Radians);
            lat = Angle.FromRadians(Math.Atan2(z, Math.Sqrt(x * x + y * y)));
            lon = Angle.FromRadians(Math.Atan2(y, x));
            Z = 0;
            Vector3d p = new Vector3d(lat.Degrees, lon.Degrees, Z);
            return p;
        }

        /// <summary>
        /// 给定两点的经纬度角度。返回过球心的最大圆夹角
        /// Computes the great circle distance between two pairs of lat/longs.
        /// TODO: Compute distance using ellipsoid.
        /// </summary>
        public static Angle ApproxAngularDistance(double latA, double lonA, double latB, double lonB)
        {
            double dlon = lonB - lonA;
            double dlat = latB - latA;
            double k = Math.Sin(dlat * 0.5);
            double l = Math.Sin(dlon * 0.5);
            double a = k * k + Math.Cos(latA) * Math.Cos(latB) * l * l;
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            return Angle.FromRadians(c);
        }


        /// <summary>
        /// 已知三个空间坐标，得到PO处的角平分线的单位向量
        /// </summary>
        /// <param name="A"></param>
        /// <param name="P"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Vector3 HalfAngleVector(Vector3 A, Vector3 P, Vector3 B)
        {
            Vector3 nest = Vector3.Empty;
            if (P == Vector3.Empty)
                return Vector3.Empty;
            if (A == Vector3.Empty)
            {
                nest = B - P;
            }
            else if (B == Vector3.Empty)
            {
                nest = P - A;
            }

            else
            {
                Vector3 AP = Vector3.Normalize(P - A);
                Vector3 BP = Vector3.Normalize(B - P);
                nest = Vector3.Add(Vector3.Multiply(AP, BP.Length()), Vector3.Multiply(BP, AP.Length()));
            }
            return nest;
        }
    }
}
