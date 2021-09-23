using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Pinwheel.Griffin.Rendering;
using Pinwheel.Griffin.DataTool;
using Unity.Collections;
using System.Text;
using Pinwheel.Griffin.Wizard;
#if __MICROSPLAT_POLARIS__
using JBooth.MicroSplat;
#endif

namespace Pinwheel.Griffin
{
    [CustomEditor(typeof(GStylizedTerrain))]
    public class GStylizedTerrainInspector : Editor
    {
        public delegate void GTerrainGUIInjectionHandler(GStylizedTerrain terrain, int order);
        public static event GTerrainGUIInjectionHandler GUIInject;

        private GStylizedTerrain terrain;
        private GTerrainData data;
        private bool isNeighboringFoldoutExpanded;
        private List<GGenericMenuItem> geometryAdditionalContextAction;
        private List<GGenericMenuItem> foliageAdditionalContextAction;

        private static Vector2Int debugGrassCellIndex;
        private const string DEFERRED_UPDATE_KEY = "geometry-deferred-update";

        private void OnEnable()
        {
            terrain = (GStylizedTerrain)target;
            if (terrain.TerrainData != null)
                terrain.TerrainData.Shading.UpdateMaterials();

            geometryAdditionalContextAction = new List<GGenericMenuItem>();
            foliageAdditionalContextAction = new List<GGenericMenuItem>();
            foliageAdditionalContextAction.Add(new GGenericMenuItem(
                "Update Trees",
                false,
                () =>
                {
                    if (terrain.TerrainData != null)
                    {
                        terrain.TerrainData.Foliage.SetTreeRegionDirty(new Rect(0, 0, 1, 1));
                        terrain.UpdateTreesPosition(true);
                        terrain.TerrainData.Foliage.ClearTreeDirtyRegions();
                        terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Foliage);
                    }
                }));
            foliageAdditionalContextAction.Add(new GGenericMenuItem(
                "Update Grasses",
                false,
                () =>
                {
                    if (terrain.TerrainData != null)
                    {
                        terrain.TerrainData.Foliage.SetGrassRegionDirty(new Rect(0, 0, 1, 1));
                        terrain.UpdateGrassPatches(-1, true);
                        terrain.TerrainData.Foliage.ClearGrassDirtyRegions();
                        terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Foliage);
                    }
                }));


            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            if (terrain.TerrainData == null)
            {
                DrawNullTerrainDataGUI();
            }
            else
            {
                DrawGUI();
            }
        }

        private void DrawNullTerrainDataGUI()
        {
            terrain.TerrainData = EditorGUILayout.ObjectField("Terrain Data", terrain.TerrainData, typeof(GTerrainData), false) as GTerrainData;
        }

        private void DrawGUI()
        {
            InjectGUI(0);
            terrain.TerrainData = EditorGUILayout.ObjectField("Terrain Data", terrain.TerrainData, typeof(GTerrainData), false) as GTerrainData;
            data = terrain.TerrainData;

            if (data.Geometry.StorageMode == GGeometry.GStorageMode.SaveToAsset)
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Generated Geometry", data.GeometryData, typeof(GTerrainGeneratedData), false);
                GUI.enabled = true;
            }
            else
            {
                data.GeometryData = null;
            }
            InjectGUI(1);
            DrawGeometryGUI();
            InjectGUI(2);
            DrawShadingGUI();
            InjectGUI(3);
            DrawRenderingGUI();
            InjectGUI(4);
            DrawFoliageGUI();
            InjectGUI(5);
            DrawMaskGUI();
            InjectGUI(6);
            DrawDataGUI();
            InjectGUI(7);
            if (data != null)
                GEditorCommon.SetTerrainDataDirty(data);
            DrawNeighboringGUI();
            InjectGUI(8);
            DrawStatisticsGUI();
            InjectGUI(9);
        }

        private void DrawGeometryGUI()
        {
            string label = "Geometry";
            string id = "geometry" + data.Id;

            bool deferredUpdate = EditorPrefs.GetBool(GEditorCommon.GetProjectRelatedEditorPrefsKey(DEFERRED_UPDATE_KEY), false);

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Reset"),
                false,
                () => { ConfirmAndResetGeometry(); });
            menu.AddItem(
                new GUIContent("Update"),
                false,
                () => { data.Geometry.SetRegionDirty(GCommon.UnitRect); data.SetDirty(GTerrainData.DirtyFlags.Geometry); });
            menu.AddItem(
                new GUIContent("Match Edges"),
                false,
                () => { terrain.MatchEdges(); });
            menu.AddItem(
                new GUIContent("Clean Up"),
                false,
                () => { data.Geometry.CleanUp(); });

            if (geometryAdditionalContextAction != null && geometryAdditionalContextAction.Count > 0)
            {
                menu.AddSeparator(null);
                for (int i = 0; i < geometryAdditionalContextAction.Count; ++i)
                {
                    GGenericMenuItem item = geometryAdditionalContextAction[i];
                    menu.AddItem(
                        new GUIContent(item.Name),
                        item.IsOn,
                        item.Action);
                }
            }

            menu.AddSeparator(null);
            menu.AddItem(
                new GUIContent("Toggle Deferred Update"),
                deferredUpdate,
                () =>
                {
                    deferredUpdate = !deferredUpdate;
                    EditorPrefs.SetBool(GEditorCommon.GetProjectRelatedEditorPrefsKey(DEFERRED_UPDATE_KEY), deferredUpdate);
                });
            menu.AddSeparator(null);
            menu.AddItem(
                new GUIContent("Advanced/Remove Height Map"),
                false,
                () => { ConfirmAndRemoveHeightMap(); });
            List<string> warnings = new List<string>();
            if (terrain != null &&
                terrain.geometryVersion < GStylizedTerrain.GEOMETRY_VERSION_CHUNK_POSITION_AT_CHUNK_CENTER)
            {
                warnings.Add("- Chunk position placement has been changed for better level streaming and baking. Go to CONTEXT>Update to re-generate the terrain.");
            }

#if !GRIFFIN_BURST
            warnings.Add("- Install Burst Compiler (com.unity.burst) to speed up generation.");
#endif
#if !GRIFFIN_EDITOR_COROUTINES
            if (data.Geometry.AllowTimeSlicedGeneration == true)
            {
                warnings.Add("- Install Editor Coroutines (com.unity.editorcoroutines) to enable time-sliced generation in editor.");
            }
#endif

            string headerWarning = GUtilities.ListElementsToString(warnings, "\n");

            GEditorCommon.Foldout(label, false, id, () =>
            {
                GGeometry settings = data.Geometry;
                EditorGUI.BeginChangeCheck();

                GEditorCommon.Header("Dimension");
                settings.Width = EditorGUILayout.DelayedFloatField("Width", settings.Width);
                settings.Height = EditorGUILayout.DelayedFloatField("Height", settings.Height);
                settings.Length = EditorGUILayout.DelayedFloatField("Length", settings.Length);

                GEditorCommon.Header("Height Map");
                settings.HeightMapResolution = EditorGUILayout.DelayedIntField("Height Map Resolution", settings.HeightMapResolution);

                GEditorCommon.Header("Mesh Generation");
                settings.MeshBaseResolution = EditorGUILayout.DelayedIntField("Mesh Base Resolution", settings.MeshBaseResolution);
                settings.MeshResolution = EditorGUILayout.DelayedIntField("Mesh Resolution", settings.MeshResolution);
                settings.ChunkGridSize = EditorGUILayout.DelayedIntField("Grid Size", settings.ChunkGridSize);
                settings.LODCount = EditorGUILayout.DelayedIntField("LOD Count", settings.LODCount);
                settings.DisplacementSeed = EditorGUILayout.DelayedIntField("Displacement Seed", settings.DisplacementSeed);
                settings.DisplacementStrength = EditorGUILayout.DelayedFloatField("Displacement Strength", settings.DisplacementStrength);
                settings.AlbedoToVertexColorMode = (GAlbedoToVertexColorMode)EditorGUILayout.EnumPopup("Albedo To Vertex Color", settings.AlbedoToVertexColorMode);
                settings.SmoothNormal = EditorGUILayout.Toggle("Smooth Normal", settings.SmoothNormal);
                if (settings.SmoothNormal)
                {
                    settings.UseSmoothNormalMask = EditorGUILayout.Toggle("Smooth Normal Use Mask (G)", settings.UseSmoothNormalMask);
                }
                GEditorCommon.Header("Utilities");
                settings.StorageMode = (GGeometry.GStorageMode)EditorGUILayout.EnumPopup("Storage", settings.StorageMode);
                settings.AllowTimeSlicedGeneration = EditorGUILayout.Toggle("Time Sliced", settings.AllowTimeSlicedGeneration);

                if (EditorGUI.EndChangeCheck() && !deferredUpdate)
                {
                    data.Geometry.SetRegionDirty(new Rect(0, 0, 1, 1));
                    data.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }

                if (deferredUpdate)
                {
                    GEditorCommon.Separator();
                    if (GUILayout.Button("Update Immediately"))
                    {
                        data.Geometry.SetRegionDirty(new Rect(0, 0, 1, 1));
                        data.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                    }
                }
            },
            menu,
            headerWarning.ToString());
        }

        private void ConfirmAndResetGeometry()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Reset geometry data on this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Geometry.ResetFull();
            }
        }

        private void ConfirmAndRemoveHeightMap()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove the Height Map of this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Geometry.RemoveHeightMap();
            }
        }

        private void DrawRenderingGUI()
        {
            string label = "Rendering";
            string id = "rendering" + data.Id;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Reset"),
                false,
                () => { data.Rendering.ResetFull(); });
            GEditorCommon.Foldout(label, false, id, () =>
            {
                GRendering settings = data.Rendering;
                EditorGUI.BeginChangeCheck();
                GEditorCommon.Header("Terrain Shadow");
                settings.CastShadow = EditorGUILayout.Toggle("Cast Shadow", settings.CastShadow);
                settings.ReceiveShadow = EditorGUILayout.Toggle("Receive Shadow", settings.ReceiveShadow);

                GEditorCommon.Header("Tree Rendering");
                settings.DrawTrees = EditorGUILayout.Toggle("Draw", settings.DrawTrees);
                GUI.enabled = SystemInfo.supportsInstancing;
                settings.EnableInstancing = EditorGUILayout.Toggle("Enable Instancing", settings.EnableInstancing);
                GUI.enabled = true;
                settings.BillboardStart = EditorGUILayout.Slider("Billboard Start", settings.BillboardStart, 0f, GCommon.MAX_TREE_DISTANCE);
                settings.TreeDistance = EditorGUILayout.Slider("Tree Distance", settings.TreeDistance, 0f, GCommon.MAX_TREE_DISTANCE);
                GRuntimeSettings.Instance.renderingDefault.cullVolumeBias = EditorGUILayout.Slider("Cull Volume Bias", GRuntimeSettings.Instance.renderingDefault.cullVolumeBias, 0f, 100f);

                GEditorCommon.Header("Grass & Detail Rendering");
                settings.DrawGrasses = EditorGUILayout.Toggle("Draw", settings.DrawGrasses);
                settings.GrassDistance = EditorGUILayout.Slider("Grass Distance", settings.GrassDistance, 0f, GCommon.MAX_GRASS_DISTANCE);
                settings.GrassFadeStart = EditorGUILayout.Slider("Fade Start", settings.GrassFadeStart, 0f, 1f);

                if (EditorGUI.EndChangeCheck())
                {
                    data.SetDirty(GTerrainData.DirtyFlags.Rendering);
                    if (settings.EnableInstancing)
                    {
                        GAnalytics.Record(GAnalytics.ENABLE_INSTANCING, true);
                    }
                }

                GEditorCommon.Header("Topographic");
                GEditorSettings.Instance.topographic.enable = EditorGUILayout.Toggle("Enable", GEditorSettings.Instance.topographic.enable);

            }, menu);
        }

        private void DrawShadingGUI()
        {
            string label = "Shading";
            string id = "shading" + data.Id;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Reset"),
                false,
                () => { data.Shading.ResetFull(); });
            menu.AddItem(
                new GUIContent("Refresh"),
                false,
                () => { data.Shading.UpdateMaterials(); });
            menu.AddItem(
                new GUIContent("Set Shader"),
                false,
                () => { GWizardWindow.ShowSetShaderTab(terrain); });

            menu.AddSeparator(null);

            menu.AddItem(
                new GUIContent("Advanced/Convert Splats To Albedo"),
                false,
                () => { data.Shading.ConvertSplatsToAlbedo(); });
            menu.AddSeparator("Advanced/");
            menu.AddItem(
                new GUIContent("Advanced/Remove Albedo Map"),
                false,
                () => { ConfirmAndRemoveAlbedoMap(); });
            menu.AddItem(
                new GUIContent("Advanced/Remove Metallic Map"),
                false,
                () => { ConfirmAndRemoveMetallicMap(); });
            menu.AddItem(
                new GUIContent("Advanced/Remove Splat Control Maps"),
                false,
                () => { ConfirmAndRemoveControlMaps(); });
            menu.AddItem(
                new GUIContent("Advanced/Remove Gradient Lookup Maps"),
                false,
                () => { ConfirmAndRemoveGradientLookupMaps(); });

            GEditorCommon.Foldout(label, false, id, () =>
            {
                GShading settings = data.Shading;
                EditorGUI.BeginChangeCheck();
#if __MICROSPLAT_POLARIS__
                GEditorCommon.Header("System");
                settings.ShadingSystem = (GShadingSystem)EditorGUILayout.EnumPopup("Shading System", settings.ShadingSystem);
#endif

                GEditorCommon.Header("Material & Shader");
                settings.CustomMaterial = EditorGUILayout.ObjectField("Material", settings.CustomMaterial, typeof(Material), false) as Material;
                if (settings.CustomMaterial != null)
                {
                    GUI.enabled = false;
                    EditorGUILayout.LabelField("Shader", settings.CustomMaterial.shader.name);
                    GUI.enabled = true;
                }

#if __MICROSPLAT_POLARIS__
                if (settings.ShadingSystem == GShadingSystem.MicroSplat)
                {
                    settings.MicroSplatTextureArrayConfig = EditorGUILayout.ObjectField("Texture Array Config", settings.MicroSplatTextureArrayConfig, typeof(TextureArrayConfig), false) as TextureArrayConfig;
                }
#endif

                if (settings.ShadingSystem == GShadingSystem.Polaris)
                {
                    GEditorCommon.Header("Color Map & Gradient Lookup");
                    settings.AlbedoMapResolution = EditorGUILayout.DelayedIntField("Albedo Map Resolution", settings.AlbedoMapResolution);
                    settings.MetallicMapResolution = EditorGUILayout.DelayedIntField("Metallic Map Resolution", settings.MetallicMapResolution);
                    if (EditorGUI.EndChangeCheck())
                    {
                        data.SetDirty(GTerrainData.DirtyFlags.Shading);
                    }

                    EditorGUI.BeginChangeCheck();
                    SerializedObject so = new SerializedObject(settings);
                    SerializedProperty colorByNormalProps = so.FindProperty("colorByNormal");
                    EditorGUILayout.PropertyField(colorByNormalProps);
                    settings.ColorBlendCurve = EditorGUILayout.CurveField("Blend By Height", settings.ColorBlendCurve, Color.red, new Rect(0, 0, 1, 1));
                    SerializedProperty colorByHeightProps = so.FindProperty("colorByHeight");
                    EditorGUILayout.PropertyField(colorByHeightProps);
                    if (EditorGUI.EndChangeCheck())
                    {
                        so.ApplyModifiedProperties();
                        settings.UpdateLookupTextures();
                        data.SetDirty(GTerrainData.DirtyFlags.Shading);
                    }
                    colorByHeightProps.Dispose();
                    colorByNormalProps.Dispose();
                    so.Dispose();
                }

                EditorGUI.BeginChangeCheck();
                GEditorCommon.Header("Splats");
                if (settings.ShadingSystem == GShadingSystem.Polaris)
                {
                    settings.Splats = EditorGUILayout.ObjectField("Prototypes", settings.Splats, typeof(GSplatPrototypeGroup), false) as GSplatPrototypeGroup;
                }
                settings.SplatControlResolution = EditorGUILayout.DelayedIntField("Control Map Resolution", settings.SplatControlResolution);

                if (settings.ShadingSystem == GShadingSystem.Polaris)
                {
                    GEditorCommon.Header("Advanced");
                    DrawAdvancedShading();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    data.SetDirty(GTerrainData.DirtyFlags.Shading);
                }
            }, menu);
        }

        private void ConfirmAndRemoveAlbedoMap()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove the Albedo Map of this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Shading.RemoveAlbedoMap();
            }
        }

        private void ConfirmAndRemoveMetallicMap()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove the Metallic Map of this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Shading.RemoveMetallicMap();
            }
        }

        private void ConfirmAndRemoveControlMaps()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove the Splat Control Maps of this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Shading.RemoveSplatControlMaps();
            }
        }

        private void ConfirmAndRemoveGradientLookupMaps()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove the Gradient Lookup Maps of this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Shading.RemoveGradientLookupMaps();
            }
        }

        private void DrawAdvancedShading()
        {
            string prefKey = GEditorCommon.GetProjectRelatedEditorPrefsKey("foldout", "shading", "properties-name", data.Id);
            bool expanded = EditorPrefs.GetBool(prefKey, false);
            expanded = EditorGUILayout.Foldout(expanded, "Properties Name");
            EditorPrefs.SetBool(prefKey, expanded);
            if (expanded)
            {
                EditorGUI.indentLevel += 1;

                GShading settings = data.Shading;
                settings.AlbedoMapPropertyName = EditorGUILayout.DelayedTextField("Albedo Map", settings.AlbedoMapPropertyName);
                settings.MetallicMapPropertyName = EditorGUILayout.DelayedTextField("Metallic Map", settings.MetallicMapPropertyName);
                settings.ColorByHeightPropertyName = EditorGUILayout.DelayedTextField("Color By Height", settings.ColorByHeightPropertyName);
                settings.ColorByNormalPropertyName = EditorGUILayout.DelayedTextField("Color By Normal", settings.ColorByNormalPropertyName);
                settings.ColorBlendPropertyName = EditorGUILayout.DelayedTextField("Color Blend", settings.ColorBlendPropertyName);
                settings.DimensionPropertyName = EditorGUILayout.DelayedTextField("Dimension", settings.DimensionPropertyName);
                settings.SplatControlMapPropertyName = EditorGUILayout.DelayedTextField("Splat Control Map", settings.SplatControlMapPropertyName);
                settings.SplatMapPropertyName = EditorGUILayout.DelayedTextField("Splat Map", settings.SplatMapPropertyName);
                settings.SplatNormalPropertyName = EditorGUILayout.DelayedTextField("Splat Normal Map", settings.SplatNormalPropertyName);
                settings.SplatMetallicPropertyName = EditorGUILayout.DelayedTextField("Splat Metallic", settings.SplatMetallicPropertyName);
                settings.SplatSmoothnessPropertyName = EditorGUILayout.DelayedTextField("Splat Smoothness", settings.SplatSmoothnessPropertyName);

                EditorGUI.indentLevel -= 1;
            }
        }

        private void DrawFoliageGUI()
        {
            string label = "Foliage";
            string id = "foliage" + data.Id;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Reset"),
                false,
                () => { ConfirmAndResetFoliage(); });
            menu.AddItem(
                new GUIContent("Refresh"),
                false,
                () => { data.Foliage.Refresh(); });
            menu.AddItem(
                new GUIContent("Clear All Trees"),
                false,
                () => { ConfirmAndClearAllTrees(); });
            menu.AddItem(
                new GUIContent("Clear All Grasses"),
                false,
                () => { ConfirmAndClearAllGrasses(); });

            if (foliageAdditionalContextAction != null && foliageAdditionalContextAction.Count > 0)
            {
                menu.AddSeparator(null);
                for (int i = 0; i < foliageAdditionalContextAction.Count; ++i)
                {
                    menu.AddItem(
                        new GUIContent(foliageAdditionalContextAction[i].Name),
                        foliageAdditionalContextAction[i].IsOn,
                        foliageAdditionalContextAction[i].Action);
                }
            }

            GFoliage settings = data.Foliage;
            bool showUpgradeMessage = settings.grassVersion < GFoliage.GRASS_VERSION_COMPRESSED;
            if (showUpgradeMessage)
            {
                menu.AddSeparator(null);
                menu.AddItem(
                    new GUIContent("Upgrade Grass Serialize Version"),
                    false,
                    () =>
                    {
                        settings.Internal_UpgradeGrassSerializeVersion();
                    });
            }

            string headerWarning = null;
            if (showUpgradeMessage)
            {
                headerWarning = "New grass serialize version is available, use context menu to upgrade (Recommended).";
            }

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUI.BeginChangeCheck();
                GEditorCommon.Header("Trees");
                settings.Trees = EditorGUILayout.ObjectField("Prototypes", settings.Trees, typeof(GTreePrototypeGroup), false) as GTreePrototypeGroup;
                settings.TreeSnapMode = (GSnapMode)EditorGUILayout.EnumPopup("Snap Mode", settings.TreeSnapMode);
                if (settings.TreeSnapMode == GSnapMode.World)
                {
                    settings.TreeSnapLayerMask = GEditorCommon.LayerMaskField("Snap Layer", settings.TreeSnapLayerMask);
                }

                GUI.enabled = false;
                EditorGUILayout.LabelField("Tree Instance Count", settings.TreeInstances.Count.ToString());
                GUI.enabled = true;
                if (EditorGUI.EndChangeCheck())
                {
                    data.SetDirty(GTerrainData.DirtyFlags.Foliage);
                }

                EditorGUI.BeginChangeCheck();
                GEditorCommon.Header("Grasses & Details");
                settings.Grasses = EditorGUILayout.ObjectField("Prototypes", settings.Grasses, typeof(GGrassPrototypeGroup), false) as GGrassPrototypeGroup;
                settings.PatchGridSize = EditorGUILayout.DelayedIntField("Patch Grid Size", settings.PatchGridSize);

                settings.GrassSnapMode = (GSnapMode)EditorGUILayout.EnumPopup("Snap Mode", settings.GrassSnapMode);
                if (settings.GrassSnapMode == GSnapMode.World)
                {
                    settings.GrassSnapLayerMask = GEditorCommon.LayerMaskField("Snap Layer", settings.GrassSnapLayerMask);
                }
                settings.EnableInteractiveGrass = EditorGUILayout.Toggle("Interactive Grass", settings.EnableInteractiveGrass);
                if (settings.EnableInteractiveGrass)
                {
                    settings.VectorFieldMapResolution = EditorGUILayout.DelayedIntField("Vector Field Map Resolution", settings.VectorFieldMapResolution);
                    settings.BendSensitive = EditorGUILayout.Slider("Bend Sensitive", settings.BendSensitive, 0f, 1f);
                    settings.RestoreSensitive = EditorGUILayout.Slider("Restore Sensitive", settings.RestoreSensitive, 0f, 1f);
                }

                GUI.enabled = false;
                EditorGUILayout.LabelField("Grass Instance Count", settings.GrassInstanceCount.ToString());

                GUI.enabled = true;
                if (EditorGUI.EndChangeCheck())
                {
                    data.SetDirty(GTerrainData.DirtyFlags.Foliage);
                    if (settings.EnableInteractiveGrass)
                    {
                        GAnalytics.Record(GAnalytics.ENABLE_INTERACTIVE_GRASS, true);
                    }
                }
            },
            menu,
            headerWarning);
        }

        private void ConfirmAndResetFoliage()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Reset foliage data on this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Foliage.ResetFull();
            }
        }

        private void ConfirmAndClearAllTrees()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Clear all trees on this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Foliage.ClearTreeInstances();
            }
        }

        private void ConfirmAndClearAllGrasses()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Clear all grasses on this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Foliage.ClearGrassInstances();
            }
        }

        private void DrawMaskGUI()
        {
            string label = "Mask";
            string id = "mask" + data.GetInstanceID();

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Reset"),
                false,
                () => { data.Mask.ResetFull(); });
            menu.AddSeparator(null);
            menu.AddItem(
                new GUIContent("Advanced/Remove Mask Map"),
                false,
                () => { ConfirmAndRemoveMaskMap(); });

            GEditorCommon.Foldout(label, false, id, () =>
            {
                GMask mask = data.Mask;
                mask.MaskMapResolution = EditorGUILayout.DelayedIntField("Resolution", mask.MaskMapResolution);

                GEditorCommon.Header("Mask Usage");
                EditorGUILayout.LabelField("R", "Lock regions from editing.");
                EditorGUILayout.LabelField("G", "Sharp/Smooth normals blend factor.");
                EditorGUILayout.LabelField("B", "Custom");
                EditorGUILayout.LabelField("A", "Custom");
            },
            menu);
        }

        private void ConfirmAndRemoveMaskMap()
        {
            if (EditorUtility.DisplayDialog(
                "Confirm",
                "Remove the Mask Map of this terrain? This action cannot be undone!",
                "OK", "Cancel"))
            {
                data.Mask.RemoveMaskMap();
            }
        }

        private void DrawDataGUI()
        {
            string label = "Data";
            string id = "data" + data.GetInstanceID();

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Import", EditorStyles.miniButtonLeft))
                {
                    ShowImportContext();
                }
                if (GUILayout.Button("Export", EditorStyles.miniButtonRight))
                {
                    ShowExportContext();
                }
                EditorGUILayout.EndHorizontal();
            });
        }

        private void ShowImportContext()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Unity Terrain Data"),
                false,
                () =>
                {
                    ShowUnityTerrainDataImporter();
                });
            menu.AddItem(
                new GUIContent("Raw"),
                false,
                () =>
                {
                    ShowRawImporter();
                });
            menu.AddItem(
                new GUIContent("Textures"),
                false,
                () =>
                {
                    ShowTextureImporter();
                });

            menu.ShowAsContext();
        }

        private void ShowUnityTerrainDataImporter()
        {
            GUnityTerrainDataImporterWindow window = GUnityTerrainDataImporterWindow.ShowWindow();
            window.DesData = data;

            GameObject g = Selection.activeGameObject;
            if (g != null)
            {
                GStylizedTerrain t = g.GetComponent<GStylizedTerrain>();
                if (t != null && t.TerrainData == data)
                {
                    window.DesTerrain = t;
                }
            }
        }

        private void ShowRawImporter()
        {
            GRawImporterWindow window = GRawImporterWindow.ShowWindow();
            window.DesData = data;
            GameObject g = Selection.activeGameObject;
            if (g != null)
            {
                GStylizedTerrain t = g.GetComponent<GStylizedTerrain>();
                if (t != null && t.TerrainData == data)
                {
                    window.Terrain = t;
                }
            }
        }

        private void ShowTextureImporter()
        {
            GTextureImporterWindow window = GTextureImporterWindow.ShowWindow();
            window.DesData = data;
            GameObject g = Selection.activeGameObject;
            if (g != null)
            {
                GStylizedTerrain t = g.GetComponent<GStylizedTerrain>();
                if (t != null && t.TerrainData == data)
                {
                    window.Terrain = t;
                }
            }
        }

        private void ShowExportContext()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Unity Terrain Data"),
                false,
                () =>
                {
                    ShowUnityTerrainDataExporter();
                });
            menu.AddItem(
                new GUIContent("Raw"),
                false,
                () =>
                {
                    ShowRawExporter();
                });
            menu.AddItem(
                new GUIContent("Textures"),
                false,
                () =>
                {
                    ShowTexturesExporter();
                });

            menu.ShowAsContext();
        }

        private void ShowUnityTerrainDataExporter()
        {
            GUnityTerrainDataExporterWindow window = GUnityTerrainDataExporterWindow.ShowWindow();
            window.SrcData = data;
        }

        private void ShowRawExporter()
        {
            GRawExporterWindow window = GRawExporterWindow.ShowWindow();
            window.SrcData = data;
        }

        private void ShowTexturesExporter()
        {
            GTextureExporterWindow window = GTextureExporterWindow.ShowWindow();
            window.SrcData = data;
        }

        private void DrawNeighboringGUI()
        {
            string label = "Neighboring";
            string id = "polaris-v2-neighboring";

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Reset"),
                false,
                () => { ResetNeighboring(); });
            menu.AddItem(
                new GUIContent("Connect"),
                false,
                () => { GStylizedTerrain.ConnectAdjacentTiles(); });

            isNeighboringFoldoutExpanded = GEditorCommon.Foldout(label, false, id, () =>
             {
                 EditorGUI.BeginChangeCheck();
                 terrain.AutoConnect = EditorGUILayout.Toggle("Auto Connect", terrain.AutoConnect);
                 terrain.GroupId = EditorGUILayout.DelayedIntField("Group Id", terrain.GroupId);
                 terrain.TopNeighbor = EditorGUILayout.ObjectField("Top Neighbor", terrain.TopNeighbor, typeof(GStylizedTerrain), true) as GStylizedTerrain;
                 terrain.BottomNeighbor = EditorGUILayout.ObjectField("Bottom Neighbor", terrain.BottomNeighbor, typeof(GStylizedTerrain), true) as GStylizedTerrain;
                 terrain.LeftNeighbor = EditorGUILayout.ObjectField("Left Neighbor", terrain.LeftNeighbor, typeof(GStylizedTerrain), true) as GStylizedTerrain;
                 terrain.RightNeighbor = EditorGUILayout.ObjectField("Right Neighbor", terrain.RightNeighbor, typeof(GStylizedTerrain), true) as GStylizedTerrain;

                 if (EditorGUI.EndChangeCheck())
                 {
                     if (terrain.TopNeighbor != null || terrain.BottomNeighbor != null || terrain.LeftNeighbor != null || terrain.RightNeighbor != null)
                     {
                         GAnalytics.Record(GAnalytics.MULTI_TERRAIN, true);
                     }
                 }
             }, menu);
        }

        public static string GetNeighboringFoldoutID(GStylizedTerrain t)
        {
            string id = "neighboring" + t.GetInstanceID().ToString();
            return id;
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        private void DuringSceneGUI(SceneView sv)
        {
            DrawDebugGUI();

            if (terrain.TerrainData == null)
                return;
            if (!terrain.AutoConnect)
                return;
            if (!isNeighboringFoldoutExpanded)
                return;

            Vector3 terrainSizeXZ = new Vector3(
                terrain.TerrainData.Geometry.Width,
                0,
                terrain.TerrainData.Geometry.Length);

            if (terrain.LeftNeighbor == null)
            {
                Vector3 pos = terrain.transform.position + Vector3.left * terrainSizeXZ.x + terrainSizeXZ * 0.5f;
                if (Handles.Button(pos, Quaternion.Euler(90, 0, 0), terrainSizeXZ.z * 0.5f, terrainSizeXZ.z * 0.5f, Handles.RectangleHandleCap) && Event.current.button == 0)
                {
                    GStylizedTerrain t = CreateNeighborTerrain();
                    t.transform.parent = terrain.transform.parent;
                    t.transform.position = terrain.transform.position + Vector3.left * terrainSizeXZ.x;
                    t.name = string.Format("{0}-{1}", t.name, t.transform.position.ToString());
                    Selection.activeGameObject = t.gameObject;
                    GStylizedTerrain.ConnectAdjacentTiles();
                }
            }
            if (terrain.TopNeighbor == null)
            {
                Vector3 pos = terrain.transform.position + Vector3.forward * terrainSizeXZ.z + terrainSizeXZ * 0.5f;
                if (Handles.Button(pos, Quaternion.Euler(90, 0, 0), terrainSizeXZ.z * 0.5f, terrainSizeXZ.z * 0.5f, Handles.RectangleHandleCap) && Event.current.button == 0)
                {
                    GStylizedTerrain t = CreateNeighborTerrain();
                    t.transform.parent = terrain.transform.parent;
                    t.transform.position = terrain.transform.position + Vector3.forward * terrainSizeXZ.z;
                    t.name = string.Format("{0}-{1}", t.name, t.transform.position.ToString());
                    Selection.activeGameObject = t.gameObject;
                    GStylizedTerrain.ConnectAdjacentTiles();
                }
            }
            if (terrain.RightNeighbor == null)
            {
                Vector3 pos = terrain.transform.position + Vector3.right * terrainSizeXZ.z + terrainSizeXZ * 0.5f;
                if (Handles.Button(pos, Quaternion.Euler(90, 0, 0), terrainSizeXZ.z * 0.5f, terrainSizeXZ.z * 0.5f, Handles.RectangleHandleCap) && Event.current.button == 0)
                {
                    GStylizedTerrain t = CreateNeighborTerrain();
                    t.transform.parent = terrain.transform.parent;
                    t.transform.position = terrain.transform.position + Vector3.right * terrainSizeXZ.x;
                    t.name = string.Format("{0}-{1}", t.name, t.transform.position.ToString());
                    Selection.activeGameObject = t.gameObject;
                    GStylizedTerrain.ConnectAdjacentTiles();
                }
            }
            if (terrain.BottomNeighbor == null)
            {
                Vector3 pos = terrain.transform.position + Vector3.back * terrainSizeXZ.z + terrainSizeXZ * 0.5f;
                if (Handles.Button(pos, Quaternion.Euler(90, 0, 0), terrainSizeXZ.z * 0.5f, terrainSizeXZ.z * 0.5f, Handles.RectangleHandleCap) && Event.current.button == 0)
                {
                    GStylizedTerrain t = CreateNeighborTerrain();
                    t.transform.parent = terrain.transform.parent;
                    t.transform.position = terrain.transform.position + Vector3.back * terrainSizeXZ.z;
                    t.name = string.Format("{0}-{1}", t.name, t.transform.position.ToString());
                    Selection.activeGameObject = t.gameObject;
                    GStylizedTerrain.ConnectAdjacentTiles();
                }
            }

            GEditorCommon.SceneViewMouseMessage(new GUIContent(
                "Click on a rectangle to pin.\n" +
                "Close the Neighboring foldout to disable terrain pinning mode."));
        }

        private GStylizedTerrain CreateNeighborTerrain()
        {
            GStylizedTerrain t = GWizard.CreateTerrainFromSource(terrain.TerrainData);
            GEditorCommon.ExpandFoldout(GetNeighboringFoldoutID(t));

            return t;
        }

        private void ResetNeighboring()
        {
            terrain.AutoConnect = true;
            terrain.GroupId = 0;
            terrain.TopNeighbor = null;
            terrain.BottomNeighbor = null;
            terrain.LeftNeighbor = null;
            terrain.RightNeighbor = null;
        }

        private void InjectGUI(int order)
        {
            if (GUIInject != null)
            {
                GUIInject.Invoke(terrain, order);
            }
        }

        private void DrawStatisticsGUI()
        {
            string label = "Statistics";
            string id = "polaris-terrain-statistic";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                float kiloToByte = 1024;

                GEditorCommon.Header("Textures Memory");
                float heightMapStats = data.Geometry.GetHeightMapMemoryStats();
                float albedoMapStats = data.Shading.GetAlbedoMapMemStats();
                float metallicMapStats = data.Shading.GetMetallicMapMemStats();
                float splatControlMapsStats = data.Shading.GetControlMapMemStats();
                float lookupMapsStats = data.Shading.GetLookupTexturesMemStats();
                float maskMapStats = data.Mask.GetMaskMapMemStats();
                EditorGUILayout.LabelField("Height Map", (heightMapStats / kiloToByte).ToString("0 KB"));
                EditorGUILayout.LabelField("Albedo Map", (albedoMapStats / kiloToByte).ToString("0 KB"));
                EditorGUILayout.LabelField("Metallic Map", (metallicMapStats / kiloToByte).ToString("0 KB"));
                EditorGUILayout.LabelField("Splat Control Maps", (splatControlMapsStats / kiloToByte).ToString("0 KB"));
                EditorGUILayout.LabelField("Gradient Lookup Maps", (lookupMapsStats / kiloToByte).ToString("0 KB"));
                EditorGUILayout.LabelField("Mask Map", (maskMapStats / kiloToByte).ToString("0 KB"));

                GEditorCommon.Header("Persistent Foliage Memory");
                float treeStats = data.Foliage.GetTreeMemStats();
                float grassStats = data.Foliage.GetGrassMemStats();
                EditorGUILayout.LabelField("Tree", (treeStats / kiloToByte).ToString("0 KB"));
                EditorGUILayout.LabelField("Grass", (grassStats / kiloToByte).ToString("0 KB"));

                GEditorCommon.Header("Total Memory");
                float total = heightMapStats + albedoMapStats + metallicMapStats + splatControlMapsStats + lookupMapsStats + maskMapStats + treeStats + grassStats;
                EditorGUILayout.LabelField("Total", (total / kiloToByte).ToString("0 KB"));

                GEditorCommon.Header("Note");
                EditorGUILayout.LabelField("Terrain Data file on disk may take up more space. Set project serialization mode to Binary yields smaller file, but cannot be diff/merged using version control softwares.", GEditorCommon.WordWrapItalicLabel);
            });
        }

        private void DrawDebugGUI()
        {
            //Vector3 cameraPos = Camera.current.transform.position;
            //GTerrainChunk[] chunks = terrain.GetChunks();
            //if (chunks.Length == 0)
            //    return;

            //for (int i = 0; i < chunks.Length; ++i)
            //{
            //    GTerrainChunk c = chunks[i];
            //    if (Vector3.Distance(cameraPos, c.transform.position) > 100)
            //        continue;
            //    Vector3[] vertices = c.MeshFilterComponent.sharedMesh.vertices;
            //    Vector3[] normals = c.MeshFilterComponent.sharedMesh.normals;

            //    Handles.color = Color.red;
            //    for (int j = 0; j < vertices.Length; ++j)
            //    {
            //        Vector3 p = c.transform.TransformPoint(vertices[j]);
            //        if (Vector3.Distance(cameraPos, p) > 100)
            //            continue;
            //        Handles.DrawLine(p, p + normals[j] * 5);

            //    }
            //}
        }
    }
}
