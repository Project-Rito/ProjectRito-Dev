﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ImGuiNET;
using MapStudio.UI;
using GLFrameworkEngine;

namespace UKingLibrary
{
    public class MubinToolSettings : IToolWindowDrawer
    {
        public MubinToolSettings() {
        }

        string _collisionCacheStatus = "";
        string _navmeshBuildStatus = "";
        string _navmeshPrepareStatus = "";
        private string _removeOnlyOneUnitConfigName = @"";
        private string _removeOnlyOneFieldName = @"MainField";
        bool _removeOnlyOneProcessing = false;
        public void Render()
        {
            UKingEditor editor = (UKingEditor)Workspace.ActiveWorkspace.ActiveEditor; // If this is being rendered we know that the active editor is a UKingEditor.

            bool refreshScene = false;
            if (ImGui.CollapsingHeader(TranslationSource.GetText("OBJS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_VISIBLE_ACTORS")}", ref MapData.ShowVisibleActors);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_INVISIBLE_ACTORS")}", ref MapData.ShowInvisibleActors);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_MAP_MODEL")}", ref MapData.ShowMapModel);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_ACTOR_LINKS")}", ref MapData.ShowActorLinks);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_COLLISION_SHAPES")}", ref MapData.ShowCollisionShapes);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_NAVMESH_SHAPES")}", ref MapData.ShowNavmeshShapes);
            }
            if (ImGui.CollapsingHeader(TranslationSource.GetText("RAILS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("POINT_SIZE")}##vmenu11", ref RenderablePath.LinearPointScale, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("BEZIER_POINT_SIZE")}##vmenu12", ref RenderablePath.BezierPointScale, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("LINE_WIDTH")}##vmenu13", ref RenderablePath.BezierLineWidth, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("ARROW_LENGTH")}##vmenu14", ref RenderablePath.BezierArrowLength, 0.05f, 0f, 10f);
            }

            // Task tools
            if (ImGui.CollapsingHeader(TranslationSource.GetText("COLLISION_TOOLS")))
            {
                if (ImGui.Button(TranslationSource.GetText("CACHE_BAKED_COLLISION")))
                {
                    _collisionCacheStatus = "PROCESSING";
                    new Task(
                        () => 
                        {
                            try
                            {
                                CollisionCacher.CacheAll(PluginConfig.CollisionCacheDir);
                                _collisionCacheStatus = "";
                            }
                            catch (Exception ex)
                            {
                                _collisionCacheStatus = "ERROR";
                                Console.WriteLine(ex.ToString());
                            }
                            
                        }).Start();
                }
                ImGui.SameLine(); ImGui.TextDisabled($"{TranslationSource.GetText(_collisionCacheStatus)}");
            }

            if (ImGui.CollapsingHeader(TranslationSource.GetText("NAVMESH_TOOLS")))
            {
                ImGui.SliderFloat(TranslationSource.GetText("CELL_SIZE"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.CellSize, 0.001f, 10f);
                ImGui.SliderFloat(TranslationSource.GetText("CELL_HEIGHT"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.CellHeight, 0.001f, 10f);
                ImGui.SliderFloat(TranslationSource.GetText("WALKABLE_SLOPE_ANGLE"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.WalkableSlopeAngle, 0.001f, 180f);
                ImGui.SliderFloat(TranslationSource.GetText("WALKABLE_HEIGHT"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.WalkableHeight, 0.001f, 10f);
                ImGui.SliderFloat(TranslationSource.GetText("WALKABLE_CLIMB"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.WalkableClimb, 0.001f, 10f);
                ImGui.SliderFloat(TranslationSource.GetText("WALKABLE_RADIUS"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.WalkableRadius, 0.001f, 10f);
                ImGui.SliderInt(TranslationSource.GetText("MIN_REGION_AREA"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.MinRegionArea, 1, 256);
                ImGui.SliderFloat(TranslationSource.GetText("DETAIL_SAMPLE_DIST"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.DetailSampleDist, 0.001f, 10f);
                ImGui.SliderFloat(TranslationSource.GetText("DETAIL_SAMPLE_MAX_ERROR"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.DetailSampleMaxError, 0.001f, 10f);

                ImGui.SliderFloat(TranslationSource.GetText("COST_Y_SCALE"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.CostYScale, 0f, 10f);
                ImGui.SliderInt(TranslationSource.GetText("KMEANS_CLUSTERING_K"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.KMeansClusteringK, 1, 100);
                //ImGui.SliderFloat(TranslationSource.GetText("GRAPH_MIN_GROUPING"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.GraphMinGrouping, 5f, 20f);
                //ImGui.SliderFloat(TranslationSource.GetText("GRAPH_EDGE_RADIUS"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.GraphEdgeRadius, 0.5f, 5f);
                ImGui.SliderFloat(TranslationSource.GetText("STREAMING_SET_SEARCH_RADIUS"), ref UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.StreamingSetSearchRadius, 1f, 20f);

                if (ImGui.Button(TranslationSource.GetText("BUILD_NAVMESH")))
                {
                    NavmeshBuilder.Build();
                    /*
                    _navmeshBuildStatus = "PROCESSING";
                    new Task(
                        () => 
                        {
                            try
                            {
                                NavmeshBuilder.Build();
                                _navmeshBuildStatus = "";
                            }
                            catch
                            {
                                _navmeshBuildStatus = "ERROR";
                            }
                        }).Start();
                    */
                }
                ImGui.SameLine(); ImGui.TextDisabled($"{TranslationSource.GetText(_navmeshBuildStatus)}");

                /*
                if (ImGui.Button(TranslationSource.GetText("PREPARE_NAVMESH")))
                {
                    _navmeshPrepareStatus = "PROCESSING";
                    new Task(
                        () =>
                        {
                            NavmeshBuilder.Prepare(Path.GetFullPath(Path.Join(Path.GetDirectoryName(editor.FileInfo.FilePath), $"{editor.EditorConfig.FolderName}")));
                            _navmeshPrepareStatus = "";
                        }).Start();
                }
                ImGui.SameLine(); ImGui.TextDisabled($"{TranslationSource.GetText(_navmeshPrepareStatus)}");
                */
            }

            if (ImGui.CollapsingHeader(TranslationSource.GetText("ONLYONE_TOOLS"))) {
                // Select UnitConfigName
                ImGui.InputText(TranslationSource.GetText("ACTOR_NAME"), ref _removeOnlyOneUnitConfigName, 128);

                // Select field to act on
                if (ImGui.BeginCombo("##removeOnlyOneFieldName", _removeOnlyOneFieldName))
                {
                    foreach (string fieldName in GlobalData.FieldNames)
                    {
                        bool isSelected = _removeOnlyOneFieldName == fieldName;

                        if (ImGui.Selectable(fieldName, isSelected))
                        {
                            _removeOnlyOneFieldName = fieldName;
                        }

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                if (ImGui.Button($"{TranslationSource.GetText("REMOVE_ONLYONE")}"))
                {
                    _removeOnlyOneProcessing = true;
                    new Task(() => { OnlyOneRemover.Remove(_removeOnlyOneUnitConfigName, _removeOnlyOneFieldName, Path.GetFullPath(Path.Join(Path.GetDirectoryName(editor.FileInfo.FilePath), $"{editor.EditorConfig.FolderName}"))); _removeOnlyOneProcessing = false; }).Start();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(TranslationSource.GetText("ONLYONE_REMOVER_NOTICE"));
                if (_removeOnlyOneProcessing)
                {
                    ImGui.SameLine(); ImGui.TextDisabled($"{TranslationSource.GetText("PROCESSING")}");
                }
            }

            if (ImGui.CollapsingHeader(TranslationSource.GetText("EXTRA_DEBUG_STUFF")))
            {
                ImGui.DragFloat("Havok shape opacity", ref HavokMeshShapeRender.COLLISIONSHAPE_DEBUG_OPACITY);
            }

            if (refreshScene)
                GLContext.ActiveContext.UpdateViewport = true;
        }
    }
}
