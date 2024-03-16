using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UtilityBelt.Service.Lib.ACClientModule;

namespace WorldBuilder.Lib.Extensions {
    public static class PositionExtensions {
        public static Matrix4x4 ToMatrix(this AcClient.Position pos) {
            var frame = pos.frame;

            var q = new System.Numerics.Quaternion(frame.qx, frame.qy, frame.qz, frame.qw);

            var rotM = Matrix4x4.CreateFromQuaternion(q);
            var translateM = Matrix4x4.CreateTranslation(frame.m_fOrigin.x, frame.m_fOrigin.y, frame.m_fOrigin.z);

            return rotM * translateM;
        }
        public static Coordinates ToCoords(this AcClient.Position pos) {
            return new Coordinates(pos.objcell_id, pos.frame.m_fOrigin.x, pos.frame.m_fOrigin.y, pos.frame.m_fOrigin.z);
        }
    }
}
