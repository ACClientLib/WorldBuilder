using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WorldBuilder.Lib.Extensions {
    public static class NumericsExtensions {
        public static float DistanceTo(this Vector3 v, Plane plane) {
            return Vector3.Dot(plane.Normal, v) + plane.D;
        }
    }
}
