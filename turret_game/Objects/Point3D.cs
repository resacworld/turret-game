using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace turret_game.Objects
{
    public class Point3D
    {
        public double Tx { get; set; } = 0;
        public double Ty { get; set; } = 0;
        public double Tz { get; set; } = 0;
        public double Rx { get; set; } = 0;
        public double Ry { get; set; } = 0;
        public double Rz { get; set; } = 0;

        public Point3D()
        { }

        public Point3D(double tx, double ty, double tz, double rx, double ry, double rz)
        {
            Tx = tx;
            Ty = ty;
            Tz = tz;
            Rx = rx;
            Ry = ry;
            Rz = rz;
        }

        public Point3D(double tx, double ty, double tz)
        {
            Tx = tx;
            Ty = ty;
            Tz = tz;
        }

        public Point3D(List<double> position) : this(position[0], position[1], position[2], position[3], position[4], position[5])
        { }

        public Point3D(double[] position) : this(position[0], position[1], position[2], position[3], position[4], position[5])
        { }

        public Point3D(double[] pos, double[] rot) : this(pos[0], pos[1], pos[2], rot[0], rot[1], rot[2])
        { }

        public static Point3D Interpolate(Point3D pos1, Point3D pos2)
        {
            throw new NotImplementedException();
        }
    }
}
