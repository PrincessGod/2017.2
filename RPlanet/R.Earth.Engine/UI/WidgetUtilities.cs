using System;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace R.Earth.OverLayer
{
    public sealed class WidgetUtilities
    {
        private WidgetUtilities() { }

        public static void DrawLine(Vector2[] linePoints, int color, Device device)
        {
            CustomVertex.TransformedColored[] lineVerts = new CustomVertex.TransformedColored[linePoints.Length];

            for (int i = 0; i < linePoints.Length; i++)
            {
                lineVerts[i].X = linePoints[i].X;
                lineVerts[i].Y = linePoints[i].Y;
                lineVerts[i].Z = 0.0f;
                lineVerts[i].Rhw = 0.5f;
                lineVerts[i].Color = color;
            }

            device.TextureState[0].ColorOperation = TextureOperation.Disable;
            device.VertexFormat = CustomVertex.TransformedColored.Format;

            device.DrawUserPrimitives(PrimitiveType.LineStrip, lineVerts.Length - 1, lineVerts);
        }

        public static void DrawBox(int ulx, int uly, int width, int height, float z, int color, Device device)
        {
            CustomVertex.TransformedColored[] verts = new CustomVertex.TransformedColored[4];
            verts[0].X = (float)ulx;
            verts[0].Y = (float)uly;
            verts[0].Z = z;
            verts[0].Rhw = 0.5f;
            verts[0].Color = color;

            verts[1].X = (float)ulx;
            verts[1].Y = (float)uly + height;
            verts[1].Z = z; verts[1].Rhw = 0.5f;
            verts[1].Color = color;

            verts[2].X = (float)ulx + width;
            verts[2].Y = (float)uly;
            verts[2].Z = z; verts[2].Rhw = 0.5f;
            verts[2].Color = color;

            verts[3].X = (float)ulx + width;
            verts[3].Y = (float)uly + height;
            verts[3].Z = z; verts[3].Rhw = 0.5f;
            verts[3].Color = color;

            device.VertexFormat = CustomVertex.TransformedColored.Format;
            device.TextureState[0].ColorOperation = TextureOperation.Disable;
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, verts.Length - 2, verts);
        }

        public static void DrawSector(double startdeg, double enddeg, int centerX, int centerY, int radius, float z, int color, Device device)
        {
            int prec = 7;

            CustomVertex.TransformedColored[] verts = new CustomVertex.TransformedColored[prec + 2];
            verts[0].X = centerX;
            verts[0].Y = centerY;
            verts[0].Z = z; 
            verts[0].Rhw = 0.5f;
            verts[0].Color = System.Drawing.Color.Red.ToArgb(); // color;
            double degInc = (double)(enddeg - startdeg) / prec;

            for (int i = 0; i <= prec; i++)
            {
                verts[i + 1].X = (float)Math.Cos((double)(startdeg + degInc * i)) * radius + centerX;
                verts[i + 1].Y = (float)Math.Sin((double)(startdeg + degInc * i)) * radius * (-1.0f) + centerY;
                verts[i + 1].Z = z;
                verts[i + 1].Rhw = 0.5f;
                verts[i + 1].Color = color;
            }

            device.VertexFormat = CustomVertex.TransformedColored.Format;
            device.TextureState[0].ColorOperation = TextureOperation.Disable;
            device.DrawUserPrimitives(PrimitiveType.TriangleFan, verts.Length - 2, verts);
        }

        public static string Degrees2DMS(double decimalDegrees, char pos, char neg)
        {
            char dir = pos;

            if (decimalDegrees < 0)
            {
                dir = neg;
                decimalDegrees *= -1.0;
            }

            long deg = (long)decimalDegrees;

            decimalDegrees = (decimalDegrees - (double)deg) * 60.0;

            long min = (long)decimalDegrees;

            decimalDegrees = (decimalDegrees - (double)min) * 60.0;

            double sec = ((double)Math.Round(decimalDegrees * Math.Pow(10, 3))) / Math.Pow(10, 3);

            if ((long)sec == 60.0)
            {
                sec -= 60.0;
                min++;
            }

            if (min >= 60L)
            {
                deg++;
                min = 0L;
            }

            deg = deg % 360;

            while (deg >= 360L) deg -= 360L;

            if (pos.ToString().ToUpper() == "E")
                return String.Format("{0:000}?{1:00}' {2:00.000}\" {3}", deg, min, sec, dir);
            else
                return String.Format("{0:00}?{1:00}' {2:00.000}\" {3}", deg, min, sec, dir);
        }
    }
}