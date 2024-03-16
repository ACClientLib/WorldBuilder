using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldBuilder.Lib.Extensions {
    public static class ACVector3Extensions {
        public static System.Numerics.Vector3 ToNumerics(this AcClient.Vector3 v) {
            return new System.Numerics.Vector3(v.x, v.y, v.z);
        }

        public static AcClient.Vector3 ToAC(this System.Numerics.Vector3 v) {
            return new AcClient.Vector3() {
                x = v.X,
                y = v.Y,
                z = v.Z
            };
        }
    }
}
