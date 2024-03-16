using AcClient;
using WorldBuilder.Lib.Extensions;
using System;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service;
using UtilityBelt.Service.Lib.ACClientModule;

namespace WorldBuilder.Lib {
    public unsafe class Picker : IGamePicking {
        /// <inheritdoc/>
        public override Coordinates? PickTerrain() {
            return PickTerrain(*Render.selection_x, *Render.selection_y);
        }

        /// <inheritdoc/>
        public override Coordinates? PickTerrain(int screenX, int screenY) {
            if ((SmartBox.smartbox[0]->viewer.objcell_id & 0xFFFF) < 0x100) {
                return PickLandscape(screenX, screenY);
            }
            else {
                return PickEnvironment(screenX, screenY);
            }
        }

        public uint grab_visible_cells<UInt16>(ref LScape This) => ((delegate* unmanaged[Thiscall]<ref LScape, uint>)0x00505920)(ref This);
        //.text:00505920 ; unsigned int __thiscall LScape::grab_visible_cells(LScape *this)

        private Coordinates PickEnvironment(int screenX, int screenY) {
            Coordinates? bestHit = null;
            var cf = SmartBox.smartbox[0]->viewer.frame.m_fOrigin;
            var rayOrigin = new System.Numerics.Vector3(cf.x, cf.y, cf.z);
            var cameraCoords = PluginCore.Instance.Camera.Coordinates;

            var rayDir = GetPickRay().dir.ToNumerics();

            var cEnvCell = CEnvCell.GetVisible(SmartBox.smartbox[0]->viewer_cell->pos.objcell_id);
            bestHit = PickCell(cEnvCell, rayOrigin, rayDir);
            
            for (var i = 0; i < cEnvCell->cObjCell.num_stabs; i++) {
                var cell = CEnvCell.GetVisible(cEnvCell->cObjCell.stab_list[i]);
                var newHit = PickCell(cell, rayOrigin, rayDir);
                
                if (newHit == null) continue;

                if (bestHit == null || newHit.DistanceTo(cameraCoords) < bestHit.DistanceTo(cameraCoords)) {
                    bestHit = newHit;
                }
            }

            return bestHit;
        }

        internal Ray GetPickRay() {
            var pickRay = new AC1Legacy.Vector3();
            Render.pick_ray(&pickRay, *Render.selection_x, *Render.selection_y);
            var rayDirection = new System.Numerics.Vector3(pickRay.a0.x, pickRay.a0.y, pickRay.a0.z);

            return new Ray() {
                dir = pickRay.a0,
                pt = SmartBox.smartbox[0]->viewer.frame.m_fOrigin,
                length = 2000f
            };
        }

        private Coordinates? PickCell(CEnvCell* cEnvCell, System.Numerics.Vector3 rayOrigin, System.Numerics.Vector3 rayDirection) {
            if (cEnvCell is null || cEnvCell->structure is null) return null;

            Coordinates? bestHit = null;
            var structure = cEnvCell->structure;

            var cellMat = cEnvCell->cObjCell.pos.ToMatrix();
            var cameraCoords = PluginCore.Instance.Camera.Coordinates;
            
            for (var i = 0; i < cEnvCell->num_static_objects; i++) {
                var sObj = cEnvCell->static_objects[i];

                for (var j = 0; j < sObj->part_array->num_parts; j++) {
                    var part = sObj->part_array->parts[j];
                    var partMat = part->pos.ToMatrix();
                    for (var k = 0; k < part->gfxobj[0]->num_physics_polygons; k++) {
                        var poly = part->gfxobj[0]->physics_polygons[k];
                        
                        Coordinates newHit = GetPolyHit(rayOrigin, rayDirection, poly, cEnvCell, partMat, part->gfxobj[0]->vertex_array.vertices);

                        if (newHit is null) continue;

                        if (bestHit == null || newHit.DistanceTo(cameraCoords) < bestHit.DistanceTo(cameraCoords)) {
                            if (PluginCore.Instance.Camera.ToScreen(newHit, out var _)) {
                                bestHit = newHit;
                            }
                        }
                    }
                }
            }

            for (var i = 0; i < structure->num_physics_polygons; i++) {
                var poly = structure->physics_polygons[i];

                Coordinates newHit = GetPolyHit(rayOrigin, rayDirection, poly, cEnvCell, cellMat, structure->vertex_array.vertices);

                if (newHit is null) continue;

                if (bestHit == null || newHit.DistanceTo(cameraCoords) < bestHit.DistanceTo(cameraCoords)) {
                    if (PluginCore.Instance.Camera.ToScreen(newHit, out var _)) {
                        bestHit = newHit;
                    }
                }
            }

            return bestHit;
        }

        private Coordinates? GetPolyHit(System.Numerics.Vector3 rayOrigin, System.Numerics.Vector3 rayDirection, CPolygon poly, CEnvCell* cEnvCell, System.Numerics.Matrix4x4 cellMat, CVertex* vertices) {

            Coordinates? bestHit = null;
            var cameraCoords = PluginCore.Instance.Camera.Coordinates;

            for (var j = 2; j < poly.num_pts; j++) {
                var a = new System.Numerics.Vector3(
                    vertices[poly.vertex_ids[j]].x,
                    vertices[poly.vertex_ids[j]].y,
                    vertices[poly.vertex_ids[j]].z);
                var b = new System.Numerics.Vector3(
                    vertices[poly.vertex_ids[j - 1]].x,
                    vertices[poly.vertex_ids[j - 1]].y,
                    vertices[poly.vertex_ids[j - 1]].z);
                var c = new System.Numerics.Vector3(
                    vertices[poly.vertex_ids[0]].x,
                    vertices[poly.vertex_ids[0]].y,
                    vertices[poly.vertex_ids[0]].z);

                var at = System.Numerics.Vector3.Transform(a, cellMat);
                var bt = System.Numerics.Vector3.Transform(b, cellMat);
                var ct = System.Numerics.Vector3.Transform(c, cellMat);

                var hit = Collision.GetTimeAndUvCoord(rayOrigin, rayDirection, at, bt, ct);

                if (hit is not null) {
                    var hitLoc = Collision.GetTrilinearCoordinateOfTheHit(hit.Value.X, rayOrigin, rayDirection);
                    var newHit = new Coordinates(cEnvCell->cObjCell.pos.objcell_id, hitLoc.X, hitLoc.Y, hitLoc.Z);

                    if (newHit is null) continue;

                    if (bestHit == null || newHit.DistanceTo(cameraCoords) < bestHit.DistanceTo(cameraCoords)) {
                        if (PluginCore.Instance.Camera.ToScreen(hitLoc, out var _)) {
                            bestHit = newHit;
                        }
                    }
                }
            }

            return bestHit;
        }

        private Coordinates? PickLandscape(int screenX, int screenY) {
            Coordinates? bestHit = null;
            try {
                var cf = SmartBox.smartbox[0]->viewer.frame.m_fOrigin;
                var rayOrigin = new System.Numerics.Vector3(cf.x, cf.y, cf.z);

                var pickRay = new AC1Legacy.Vector3();
                Render.pick_ray(&pickRay, screenX, screenY);
                var rayDirection = new System.Numerics.Vector3(pickRay.a0.x, pickRay.a0.y, pickRay.a0.z);

                var ui = ClientUISystem.GetUISystem();
                var cs = ui->AccessCameraSet();
                var cm = cs->cm;
                var smartbox = cs->sbox;
                var lscape = smartbox->lscape;

                var cLbX = (lscape->loaded_cell_id >> 24) & 0xFF;
                var cLbY = (lscape->loaded_cell_id >> 16) & 0xFF;

                var camera = new Coordinates(SmartBox.smartbox[0]->viewer.objcell_id, cf.x, cf.y, cf.z);
                var camLbX = (camera.LandCell >> 24) & 0xFF;
                var camLbY = (camera.LandCell >> 16) & 0xFF;

                for (var lbIdxX = 0; lbIdxX < lscape->mid_width; ++lbIdxX) {
                    for (var lbIdxY = 0; lbIdxY < lscape->mid_width; ++lbIdxY) {
                        var lb = lscape->land_blocks[lbIdxX + lbIdxY * lscape->mid_width];
                        var lbX = (lb->block_coord.x / 8);
                        var lbY = (lb->block_coord.y / 8);
                        var lbid = (uint)((lbX << 24) + (lbY << 16));
                        var visible = lb->in_view != BoundingType.OUTSIDE;
                        if (visible) {
                            for (int cellIdxX = 0; cellIdxX < lb->cLandBlockStruct.side_cell_count; cellIdxX++) {
                                for (int cellIdxY = 0; cellIdxY < lb->cLandBlockStruct.side_cell_count; cellIdxY++) {
                                    var idx = ((cellIdxY + cellIdxX * lb->cLandBlockStruct.side_cell_count));
                                    var idx2 = (2 * (cellIdxY + cellIdxX * lb->cLandBlockStruct.side_polygon_count));
                                    var cell = lb->cLandBlockStruct.lcell[idx];

                                    var m = lb->cLandBlockStruct.side_cell_count - 1;
                                    var cellIsVisible = cell.in_view != BoundingType.OUTSIDE;

                                    if (cellIsVisible) {
                                        for (var polyIdx = 0; polyIdx < 2; polyIdx++) {
                                            var _ox = (lbX - camLbX) * 192f;
                                            var _oy = (lbY - camLbY) * 192f;
                                            var poly = cell.polygons[polyIdx];

                                            var vert0 = new System.Numerics.Vector3(poly->vertices[0]->x + _ox, poly->vertices[0]->y + _oy, poly->vertices[0]->z);
                                            var vert1 = new System.Numerics.Vector3(poly->vertices[1]->x + _ox, poly->vertices[1]->y + _oy, poly->vertices[1]->z);
                                            var vert2 = new System.Numerics.Vector3(poly->vertices[2]->x + _ox, poly->vertices[2]->y + _oy, poly->vertices[2]->z);

                                            var hit = Collision.GetTimeAndUvCoord(rayOrigin, rayDirection, vert0, vert1, vert2);

                                            if (hit is not null) {
                                                var hitLoc = Collision.GetTrilinearCoordinateOfTheHit(hit.Value.X, rayOrigin, rayDirection);

                                                var newHit = new Coordinates(lbid, hitLoc.X - _ox, hitLoc.Y - _oy, hitLoc.Z);
                                                if (bestHit == null || newHit.DistanceTo(camera) < bestHit.DistanceTo(camera)) {
                                                    bestHit = newHit;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { UBService.LogException(ex); }

            return bestHit;
        }

        public override WorldObject? PickWorldObject() {
            try {
                var hoveredId = AcClient.SmartBox.get_found_object_id();
                if (PluginCore.Instance.Game?.World?.TryGet(hoveredId, out var wo) == true) {
                    return wo;
                }
            }
            catch (Exception ex) { UBService.LogException(ex); }

            return null;
        }
    }
}
