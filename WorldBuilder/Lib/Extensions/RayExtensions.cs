using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WorldBuilder.Lib.Extensions {
    public static class RayExtensions {
        public static bool IntersectsSphere(this AcClient.Ray ray, AcClient.CSphere sphere) {
            var m = ray.pt.ToNumerics() - sphere.center.ToNumerics();
            float b = Vector3.Dot(m, ray.dir.ToNumerics());
            float c = Vector3.Dot(m, m) - sphere.radius * sphere.radius;

            // Exit if r’s origin outside s (c > 0) and r pointing away from s (b > 0) 
            if (c > 0.0f && b > 0.0f) {
                return false;
            }
            float discr = b * b - c;

            // A negative discriminant corresponds to ray missing sphere 
            if (discr < 0.0f) {
                return false;
            }

            return true;
        }
    }
}
