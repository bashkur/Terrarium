using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Griffin;
using Pinwheel.Griffin.PaintTool;
using Pinwheel.Griffin.SplineTool;
using Pinwheel.Griffin.StampTool;
using Pinwheel.Griffin.GroupTool;

namespace Pinwheel.Griffin.Wizard
{
    public static class GCreateLevelTabDrawer
    {
        internal static Vector2 scrollPos;
        internal static MenuCommand menuCmd;

        internal static void Draw()
        {
            EditorGUILayout.LabelField("Follow the steps below to create your level. Hover on labels for instruction.", GEditorCommon.BoldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (GCommon.CurrentRenderPipeline == GRenderPipelineType.Universal)
            {
                DrawRenderPipelineSettingGUI();
            }
            DrawCreateTerrainsGUI();
            DrawTerrainsManagementGUI();
            DrawSculptingGUI();
            DrawTexturingGUI();
            DrawVertexColorTexturingGUI();
            DrawFoliageAndObjectSpawningGUI();
            DrawCreateSplineGUI();
            DrawWaterGUI();
            DrawUtilitiesGUI();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawRenderPipelineSettingGUI()
        {
            GRenderPipelineType pipeline = GCommon.CurrentRenderPipeline;
            string label = string.Format("0. {0} Render Pipeline Setup", pipeline.ToString());
            string id = "wizard-rp-setup";

            GEditorCommon.Foldout(label, true, id, () =>
            {
                string instruction = string.Format(
                    "Install additional package for {0} Render Pipeline.\n" +
                    "Status: {1}.",
                    pipeline,
                    GPackageInitializer.isUrpSupportInstalled ? "INSTALLED" : "NOT INSTALLED"); 
                EditorGUILayout.LabelField(instruction, GEditorCommon.WordWrapLeftLabel);
                if (pipeline == GRenderPipelineType.Universal)
                {
                    if (GUILayout.Button("Install"))
                    {
                        GUrpPackageImporter.Import();
#if GRIFFIN_URP
                        Griffin.URP.GGriffinUrpInstaller.Install();
#endif
                    }
                }
            });
        }

        private static void DrawCreateTerrainsGUI()
        {
            string label = "1. Create Terrains";
            string id = "wizard-create-terrains";

            GEditorCommon.Foldout(label, true, id, () =>
            {
                GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;

                GEditorCommon.Header("Physical");

                GUIContent originLabel = new GUIContent(
                    "Origin", "Position of the first terrain in the grid.");
                settings.origin = GEditorCommon.InlineVector3Field(originLabel, settings.origin);

                GUIContent tileSizeLabel = new GUIContent(
                    "Tile Size", "Size of each terrain tile in world space.");
                settings.tileSize = GEditorCommon.InlineVector3Field(tileSizeLabel, settings.tileSize);
                settings.tileSize = new Vector3(
                    Mathf.Max(1, settings.tileSize.x),
                    Mathf.Max(1, settings.tileSize.y),
                    Mathf.Max(1, settings.tileSize.z));

                GUIContent tileXLabel = new GUIContent(
                    "Tile Count X", "Number of tiles along X-axis.");
                settings.tileCountX = EditorGUILayout.IntField(tileXLabel, settings.tileCountX);
                settings.tileCountX = Mathf.Max(1, settings.tileCountX);

                GUIContent tileZLabel = new GUIContent(
                    "Tile Count Z", "Number of tiles along Z-axis.");
                settings.tileCountZ = EditorGUILayout.IntField(tileZLabel, settings.tileCountZ);
                settings.tileCountZ = Mathf.Max(1, settings.tileCountZ);

                GEditorCommon.Header("Material");
                GWizardEditorCommon.DrawMaterialSettingsGUI();

                GEditorCommon.Header("Utilities");

                GUIContent namePrefixLabel = new GUIContent(
                    "Name Prefix",
                    "The beginning of each terrain's name. Useful for some level streaming system.");
                settings.terrainNamePrefix = EditorGUILayout.TextField(namePrefixLabel, settings.terrainNamePrefix);

                GUIContent groupIdLabel = new GUIContent(
                    "Group Id",
                    "An integer for grouping and connecting adjacent terrain tiles.");
                settings.groupId = EditorGUILayout.IntField(groupIdLabel, settings.groupId);

                GEditorCommon.Header("Data");

                GUIContent directoryLabel = new GUIContent(
                    "Directory",
                    "Where to store created terrain data. A sub-folder of Assets/ is recommended.");
                string dir = settings.dataDirectory;
                GEditorCommon.BrowseFolder(directoryLabel, ref dir);
                if (string.IsNullOrEmpty(dir))
                {
                    dir = "Assets/";
                }
                settings.dataDirectory = dir;

                if (GUILayout.Button("Create"))
                {
                    GameObject environmentRoot = null;
                    if (menuCmd != null && menuCmd.context != null)
                    {
                        environmentRoot = menuCmd.context as GameObject;
                    }
                    if (environmentRoot == null)
                    {
                        environmentRoot = new GameObject("Low Poly Environment");
                        environmentRoot.transform.position = settings.origin;
                    }
                    GWizard.CreateTerrains(environmentRoot);
                }
            });
        }

        private static void DrawTerrainsManagementGUI()
        {
            string label = "2. Terrains Management";
            string id = "wizard-terrains-management";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUILayout.LabelField(
                    "Edit properties of an individual terrain by selecting it and use the Inspector.",
                    GEditorCommon.WordWrapLeftLabel);
                EditorGUILayout.LabelField(
                    string.Format("Use context menus ({0}) in the terrain Inspector to perform additional tasks.", GEditorCommon.contextIconText),
                    GEditorCommon.WordWrapLeftLabel);
                EditorGUILayout.LabelField(
                    "Use the Group Tool to edit properties of multiple terrains at once.",
                    GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Create Group Tool"))
                {
                    GTerrainGroup group = GWizard.CreateGroupTool();
                    EditorGUIUtility.PingObject(group);
                    Selection.activeGameObject = group.gameObject;
                }
            });
        }

        private static void DrawSculptingGUI()
        {
            string label = "3. Sculpting";
            string id = "wizard-sculpting";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUILayout.LabelField("Select the workflow you prefer.", GEditorCommon.WordWrapLeftLabel);

                GEditorCommon.Header("Painting");
                EditorGUILayout.LabelField("Use a set of painters for hand sculpting terrain shape.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Create Geometry - Texture Painter"))
                {
                    GTerrainTexturePainter painter = GWizard.CreateGeometryTexturePainter();
                    EditorGUIUtility.PingObject(painter.gameObject);
                    Selection.activeGameObject = painter.gameObject;
                }

                GEditorCommon.Header("Stamping");
                EditorGUILayout.LabelField("Use grayscale textures to stamp mountains, plateaus, rivers, etc. and blend using some math operations.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Create Geometry Stamper"))
                {
                    GGeometryStamper stamper = GWizard.CreateGeometryStamper();
                    EditorGUIUtility.PingObject(stamper.gameObject);
                    Selection.activeGameObject = stamper.gameObject;
                }
            });
        }

        private static void DrawTexturingGUI()
        {
            string label = "4. Texturing";
            string id = "wizard-texturing";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUILayout.LabelField("Select the workflow you prefer.", GEditorCommon.WordWrapLeftLabel);

                GEditorCommon.Header("Painting");
                EditorGUILayout.LabelField("Use a set of painters for hand painting terrain color.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Create Geometry - Texture Painter"))
                {
                    GTerrainTexturePainter painter = GWizard.CreateGeometryTexturePainter();
                    EditorGUIUtility.PingObject(painter.gameObject);
                    Selection.activeGameObject = painter.gameObject;
                }

                GEditorCommon.Header("Stamping");
                EditorGUILayout.LabelField("Use stamper to color the terrain procedurally with some rules such as height, normal vector and noise.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Create Texture Stamper"))
                {
                    GTextureStamper stamper = GWizard.CreateTextureStamper();
                    EditorGUIUtility.PingObject(stamper.gameObject);
                    Selection.activeGameObject = stamper.gameObject;
                }
            });
        }

        private static void DrawVertexColorTexturingGUI()
        {
            string label = "4.1. Vertex Color Texturing";
            string id = "wizard-vertex-color-texturing";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUILayout.LabelField(
                    "To enable vertex coloring, do the following steps.", GEditorCommon.WordWrapLeftLabel);
                EditorGUILayout.LabelField(
                    "Set <i>terrain> Geometry> Albedo To Vertex Color</i> to Sharp or Smooth", GEditorCommon.RichTextLabel);
                EditorGUILayout.LabelField(
                    "For Painting workflow: Select the Geometry - Texture Painter and enable <i>Force Update Geometry</i>, then use Albedo mode to paint.", GEditorCommon.RichTextLabel);
                EditorGUILayout.LabelField(
                    "For Stamping workflow: Stamp to Albedo map and regenerate terrain meshes by select <i>terrain> Geometry> CONTEXT (≡)> Update</i>", GEditorCommon.RichTextLabel);
            });
        }

        private static void DrawFoliageAndObjectSpawningGUI()
        {
            string label = "5. Foliage & Object Spawning";
            string id = "wizard-foliage-object-spawning";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUILayout.LabelField("Select the workflow you prefer.", GEditorCommon.WordWrapLeftLabel);

                GEditorCommon.Header("Painting");
                EditorGUILayout.LabelField("Place trees, grasses and game objects by painting.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Create Foliage Painter & Object Painter"))
                {
                    GFoliagePainter fPainter = GWizard.CreateFoliagePainter();
                    GObjectPainter oPainter = GWizard.CreateObjectPainter();
                    EditorGUIUtility.PingObject(fPainter);
                    Selection.objects = new GameObject[] { fPainter.gameObject, oPainter.gameObject };
                    Selection.activeGameObject = fPainter.gameObject;
                }

                GEditorCommon.Header("Stamping");
                EditorGUILayout.LabelField("Procedurally spawn trees, grasses and game objects using some rules such as height, normal vector and noise.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Create Foliage Stamper & Object Stamper"))
                {
                    GFoliageStamper fStamper = GWizard.CreateFoliageStamper();
                    GObjectStamper oStamper = GWizard.CreateObjectStamper();
                    EditorGUIUtility.PingObject(fStamper);
                    Selection.objects = new GameObject[] { fStamper.gameObject, oStamper.gameObject };
                    Selection.activeGameObject = fStamper.gameObject;
                }
            });
        }

        private static void DrawCreateSplineGUI()
        {
            string label = "6. Create Roads, Ramps, Rivers, etc.";
            string id = "wizard-spline";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                EditorGUILayout.LabelField("Use Spline Tool to paint roads, make ramps and riverbeds, etc.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Create Spline Tool"))
                {
                    GSplineCreator spline = GWizard.CreateSplineTool();
                    EditorGUIUtility.PingObject(spline);
                    Selection.activeGameObject = spline.gameObject;
                }
            });
        }

        private static void DrawWaterGUI()
        {
            string label = "7. Adding Water & Sky";
            string id = "wizard-id";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                GEditorCommon.Header("Water");
                EditorGUILayout.LabelField(
                    "Poseidon is a low poly water system with high visual quality and performance.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Get Poseidon"))
                {
                    Application.OpenURL(GAssetLink.POSEIDON);
                }

                GEditorCommon.Header("Sky");
                EditorGUILayout.LabelField(
                    "Jupiter is a single pass sky shader with day night cycle support.", GEditorCommon.WordWrapLeftLabel);
                if (GUILayout.Button("Get Jupiter"))
                {
                    Application.OpenURL(GAssetLink.JUPITER);
                }
            });
        }

        private static void DrawUtilitiesGUI()
        {
            string label = "8. Utilities";
            string id = "wizard-utilities";

            GEditorCommon.Foldout(label, false, id, () =>
            {
                GEditorCommon.Header("Wind Zone");
                EditorGUILayout.LabelField("Adding Wind Zone to customize how grass react to wind in this level.");
                if (GUILayout.Button("Create Wind Zone"))
                {
                    GWindZone wind = GWizard.CreateWindZone();
                    EditorGUIUtility.PingObject(wind.gameObject);
                    Selection.activeGameObject = wind.gameObject;
                }
            });
        }
    }
}
