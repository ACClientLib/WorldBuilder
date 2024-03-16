using AcClient;
using Decal.Adapter;
using ImGuiNET;
using ImGuizmoNET;
using WorldBuilder.Lib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UtilityBelt.Service.Lib.ACClientModule;
using Plane = System.Numerics.Plane;
using Vector3 = System.Numerics.Vector3;

namespace WorldBuilder.Lib {
    public unsafe class Camera {
        public Matrix4x4 ViewTransform { get; private set; } = Matrix4x4.Identity;
        public Matrix4x4 Projection { get; private set; } = Matrix4x4.Identity;
        public Matrix4x4 ViewProj { get; private set; } = Matrix4x4.Identity;
        public Box2D ViewPortSize { get; private set; } = new Box2D();
        public Plane[] FrustumPlanes { get; private set; } = new Plane[6];
        public Coordinates Coordinates { get; private set; } = new Coordinates(0, 0, 0);

        public Camera() {

        }

        public void Update() {
            try {
                Coordinates = new Coordinates(
                    SmartBox.smartbox[0]->viewer.objcell_id,
                    SmartBox.smartbox[0]->viewer.frame.m_fOrigin.x,
                    SmartBox.smartbox[0]->viewer.frame.m_fOrigin.y,
                    SmartBox.smartbox[0]->viewer.frame.m_fOrigin.z);
                ViewTransform = GetViewTransform();
                Projection = GetProjection();
                ViewProj = ViewTransform * Projection;

                if (UIElementManager.s_pInstance != null) {
                    var rootEl = *UIElementManager.s_pInstance->m_pRootElement;
                    var viewport = ((UIElement*)GetChildRecursive(ref rootEl, 0x1000049Au));
                    if (viewport != null) {
                        ViewPortSize = viewport->a0.m_box;
                    }
                }

                UpdateFrustumPlanes();
            }
            catch (Exception ex) {
                UtilityBelt.Service.UBService.LogException(ex);
            }
        }

        public bool ToScreen(Coordinates coords, out Vector2 screenPos) {
            var a = new Vector3(coords.LocalX, coords.LocalY, coords.LocalZ);

            return ToScreen(a, out screenPos);
        }

        public bool ToScreen(Position pos, Vector3 a, out Vector2 screenPos) {
            var at = Vector3.Transform(a, pos.ToMatrix());

            return ToScreen(at, out screenPos);
        }

        public bool ToScreen(Vector3 a, out Vector2 screenPos) {
            var isOnScreen = true;
            var matrix_screen_pos = Matrix4x4.CreateTranslation(a) * ViewProj;
            var l = 1f / matrix_screen_pos.M44;

            var vx = matrix_screen_pos.M41 * l;
            var vy = matrix_screen_pos.M42 * l;
            var vz = matrix_screen_pos.M43 * l;

            if (Math.Abs(vx) > 1 || Math.Abs(vy) > 1 || vz > 1.00002) {
                isOnScreen = false;
            }

            var x = (float)Math.Floor(((vx + 1f) * 0.5f) * ViewPortSize.Width());
            var y = (float)Math.Floor(((1f - vy) * 0.5f) * ViewPortSize.Height());

            screenPos = new Vector2(ViewPortSize.m_x0 + x, ViewPortSize.m_y0 + y);

            return isOnScreen;
        }

        private static AC1Legacy.Vector3* Frame_globaltolocal(ref Frame This, AC1Legacy.Vector3* res, AC1Legacy.Vector3* _in) => ((delegate* unmanaged[Thiscall]<ref Frame, AC1Legacy.Vector3*, AC1Legacy.Vector3*, AC1Legacy.Vector3*>)0x004526C0)(ref This, res, _in);
        //.text:004526C0 ; float *__thiscall Frame::globaltolocal(float *this, float *, float *)

        private unsafe static int GetChildRecursive(ref UIElement This, uint _ID) {
            return ((delegate* unmanaged[Thiscall]<ref UIElement, uint, int>)4602880)(ref This, _ID);
        }

        private Matrix4x4 GetProjection() {
            var aspectRatio = RenderDevice.render_device[0]->m_ViewportAspectRatio;
            var fov = SmartBox.smartbox[0]->m_fGameFOV / (aspectRatio - 0.1f);

            return Matrix4x4.CreatePerspectiveFieldOfView(fov, aspectRatio, *Render.znear, *Render.zfar);
        }

        private Matrix4x4 GetViewTransform() {
            var smartbox = SmartBox.smartbox[0];
            var viewerPos = smartbox->viewer;
            var m_fl2gv = viewerPos.frame.m_fl2gv;

            var _in = new AC1Legacy.Vector3();
            var p = new AC1Legacy.Vector3();
            Frame_globaltolocal(ref viewerPos.frame, &p, &_in);

            var xAxis = new Vector3(m_fl2gv[0], m_fl2gv[1], m_fl2gv[2]);
            // Negate Y-axis for right-handed system
            var yAxis = -new Vector3(m_fl2gv[3], m_fl2gv[4], m_fl2gv[5]);
            var zAxis = new Vector3(m_fl2gv[6], m_fl2gv[7], m_fl2gv[8]);

            var res = Matrix4x4.Identity;

            res.M11 = xAxis.X;
            res.M12 = zAxis.X;
            res.M13 = yAxis.X;
            res.M14 = 0;

            res.M21 = xAxis.Y;
            res.M22 = zAxis.Y;
            res.M23 = yAxis.Y;
            res.M24 = 0;

            res.M31 = xAxis.Z;
            res.M32 = zAxis.Z;
            res.M33 = yAxis.Z;
            res.M34 = 0;

            res.M41 = p.a0.x;
            res.M42 = p.a0.z;
            res.M43 = -p.a0.y; // Negate Z-axis for right-handed system
            res.M44 = 1;

            // Right-handed coordinate system adjustment
            var rhBasis = new Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);

            res = Matrix4x4.Multiply(rhBasis, res);

            return res;
        }

        private void UpdateFrustumPlanes() {
            FrustumPlanes[0].Normal = new Vector3(
                ViewProj.M14 - ViewProj.M11,
                ViewProj.M24 - ViewProj.M21,
                ViewProj.M34 - ViewProj.M31);
            FrustumPlanes[0].D = ViewProj.M44 - ViewProj.M41;

            FrustumPlanes[1].Normal = new Vector3(
                ViewProj.M14 + ViewProj.M11,
                ViewProj.M24 + ViewProj.M21,
                ViewProj.M34 + ViewProj.M31);
            FrustumPlanes[1].D = ViewProj.M44 + ViewProj.M41;

            FrustumPlanes[2].Normal = new Vector3(
                ViewProj.M14 - ViewProj.M12,
                ViewProj.M24 - ViewProj.M22,
                ViewProj.M34 - ViewProj.M32);
            FrustumPlanes[2].D = ViewProj.M44 - ViewProj.M42;

            FrustumPlanes[3].Normal = new Vector3(
                ViewProj.M14 + ViewProj.M12,
                ViewProj.M24 + ViewProj.M22,
                ViewProj.M34 + ViewProj.M32);
            FrustumPlanes[3].D = ViewProj.M44 + ViewProj.M42;

            FrustumPlanes[4].Normal = new Vector3(
                ViewProj.M14 - ViewProj.M13,
                ViewProj.M24 - ViewProj.M23,
                ViewProj.M34 - ViewProj.M33);
            FrustumPlanes[4].D = ViewProj.M44 - ViewProj.M43;

            FrustumPlanes[5].Normal = new Vector3(
                ViewProj.M14 + ViewProj.M13,
                ViewProj.M24 + ViewProj.M23,
                ViewProj.M34 + ViewProj.M33);
            FrustumPlanes[5].D = ViewProj.M44 + ViewProj.M43;

            for (var i = 0; i < 6; i++) {
                FrustumPlanes[i] = Plane.Normalize(FrustumPlanes[i]);
            }
        }
    }
}
