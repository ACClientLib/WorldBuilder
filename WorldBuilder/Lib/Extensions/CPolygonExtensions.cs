using AcClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3 = System.Numerics.Vector3;

namespace WorldBuilder.Lib.Extensions {
    public static class CPolygonExtensions {
        public static bool IsWalkable(this ref CPolygon poly) {
            var a = new Vector3();
            var b = new Vector3();
            var c = new Vector3();

            Vector3 normal = new Vector3();
            Vector3 u = new Vector3();
            Vector3 v = new Vector3();

            u.X = b.X - a.X;
            u.Y = b.Y - a.Y;

            v.X = c.X - a.X;
            v.Y = c.Y - a.Y;

            normal.Z = u.X * v.Y - u.Y * v.X;

            normal = Vector3.Normalize(normal);

            return normal.Z <= -0.66417414618662751f;
        }
    }
}
