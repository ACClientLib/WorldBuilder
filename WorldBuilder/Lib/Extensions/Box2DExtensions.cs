using AcClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldBuilder.Lib.Extensions {
    public static class Box2DExtensions {
        public static int Width(this Box2D box) {
            return box.m_x1 - box.m_x0;
        }
        public static int Height(this Box2D box) {
            return box.m_y1 - box.m_y0;
        }
    }
}
