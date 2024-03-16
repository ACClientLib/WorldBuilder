using AcClient;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vector3 = System.Numerics.Vector3;

namespace WorldBuilder.Lib.Extensions {
    public static class ImDrawListPtrExtensions {
        public static void AddLine(this ImDrawListPtr drawList, Vector3 start,  Vector3 end, uint color, float thickness = 1f) {

            var visible = true;
            foreach (var plane in PluginCore.Instance.Camera.FrustumPlanes) {
                var distStart = start.DistanceTo(plane);
                var distEnd = end.DistanceTo(plane);

                if (distStart < 0 && distEnd < 0) {
                    visible = false;
                    break;
                }
                else if (distStart > 0 && distEnd > 0) {
                    continue;
                }

                if (distStart < 0) {
                    start = Vector3.Lerp(start, end, Math.Abs(distStart) / Math.Abs(distStart - distEnd));
                }
                else if (distEnd < 0) {
                    end = Vector3.Lerp(end, start, Math.Abs(distEnd) / Math.Abs(distEnd - distStart));
                }
            }

            if (visible) {
                PluginCore.Instance.Camera.ToScreen(start, out var ta);
                PluginCore.Instance.Camera.ToScreen(end, out var tb);

                drawList.AddLine(ta, tb, color, thickness);
            }
        }
    }
}
