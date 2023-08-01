using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrivaler
{
    internal class MathHelper
    {
        public const double EARTH_RADIUS_KM = 6378.14;

        public static double ArcDistance(PolarPos from, PolarPos to)
        {
            return Math.Acos(
                Math.Cos(from.th) * Math.Cos(to.th) +
                Math.Sin(from.th) * Math.Sin(to.th) *
                Math.Cos(from.ph - to.ph)
            );
        }
    }

    internal class PolarPos
    {
        public readonly double th;
        public readonly double ph;
        public PolarPos(double th, double ph)
        {
            this.th = th;
            this.ph = ph;
        }
        public static PolarPos FromLatLonDeg(double lat, double lon)
        {
            return new PolarPos(
                (90.0 - lat) * Math.PI / 180.0,
                lon * Math.PI / 180.0
            );
        }
    }
}
