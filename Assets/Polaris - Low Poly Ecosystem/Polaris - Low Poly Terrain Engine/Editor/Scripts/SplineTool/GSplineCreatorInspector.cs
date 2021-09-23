using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Type = System.Type;

namespace Pinwheel.Griffin.SplineTool
{
    [CustomEditor(typeof(GSplineCreator))]
    public class GSplineCreatorInspector : Editor
    {
        private List<Type> modifierTypes;
        public List<Type> ModifierTypes
        {
            get
            {
                if (modifierTypes == null)
                {
                    modifierTypes = new List<Type>();
                }
                return modifierTypes;
            }
            set
            {
                modifierTypes = value;
            }
        }

        private GSplineCreator instance;
        private int selectedAnchorIndex = -1;
        private int selectedSegmentIndex = -1;

        private Rect addModifierButtonRect;

        private GSplineEditingGUIDrawer splineEditingDrawer;

        private void OnEnable()
        {
            instance = (GSplineCreator)target;
            InitModifierClasses();
            instance.Editor_Vertices = instance.GenerateVerticesWithFalloff();
            Tools.hidden = true;
            splineEditingDrawer = new GSplineEditingGUIDrawer(instance);

            GCommon.RegisterBeginRender(OnBeginRender);
            GCommon.RegisterBeginRenderSRP(OnBeginRenderSRP);
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            Tools.hidden = false;

            GCommon.UnregisterBeginRender(OnBeginRender);
            GCommon.UnregisterBeginRenderSRP(OnBeginRenderSRP);
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        private void InitModifierClasses()
        {
            List<Type> loadedTypes = GCommon.GetAllLoadedTypes();
            ModifierTypes = loadedTypes.FindAll(
                t => t.IsSubclassOf(typeof(GSplineModifier)));
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            GUI.enabled = !GEditorSettings.Instance.splineTools.showTransformGizmos;
            instance.GroupId = GEditorCommon.ActiveTerrainGroupPopupWithAllOption("Group Id", instance.GroupId);

            GEditorSettings.Instance.splineTools.raycastLayer = EditorGUILayout.LayerField("Raycast Layer", GEditorSettings.Instance.splineTools.raycastLayer);
            GEditorSettings.Instance.splineTools.autoTangent = EditorGUILayout.Toggle("Auto Tangent", GEditorSettings.Instance.splineTools.autoTangent);

            instance.EnableTerrainMask = EditorGUILayout.Toggle("Enable Terrain Mask", instance.EnableTerrainMask);
            GEditorSettings.Instance.topographic.enable = EditorGUILayout.Toggle("Enable Topographic", GEditorSettings.Instance.topographic.enable);

            DrawInstructionGUI();
            GUI.enabled = true;
            DrawTransformGUI();
            GUI.enabled = !GEditorSettings.Instance.splineTools.showTransformGizmos;
            DrawAnchorDefaultValueGUI();
            DrawSelectedAnchorGUI();
            DrawSegmentDefaultValueGUI();
            DrawSelectedSegmentGUI();
            DrawGizmosGUI();
            DrawActionsGUI();
            GEditorCommon.DrawBackupHelpBox();
            GUI.enabled = false;
            //DrawDebugGUI();
            if (EditorGUI.EndChangeCheck())
            {
                instance.Editor_Vertices = instance.GenerateVerticesWithFalloff();
                GSplineCreator.MarkSplineChanged(instance);
                GUtilities.MarkCurrentSceneDirty();
            }
        }

        private void DrawInstructionGUI()
        {
            string label = "Instruction";
            string id = "instruction" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                string s = string.Format(
                    "Create a edit bezier spline.\n" +
                    "   - Left Click to select element.\n" +
                    "   - Ctrl & Left Click to delete element.\n" +
                    "   - Shift & Left Click to add element.\n" +
                    "Use Add Modifier to do specific tasks with spline data.");
                EditorGUILayout.LabelField(s, GEditorCommon.WordWrapItalicLabel);
            });
        }

        private void DrawTransformGUI()
        {
            string label = "Transform";
            string id = "transform" + instance.GetInstanceID();

            if (GEditorSettings.Instance.splineTools.showTransformGizmos)
            {
                GEditorCommon.ExpandFoldout(id);
            }

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Set Pivot To Median Point"),
                false,
                () => { SetPivotToMedianPoint(); });

            GEditorCommon.Foldout(label, true, id, () =>
            {
                instance.transform.localPosition = GEditorCommon.InlineVector3Field("Position", instance.transform.localPosition);
                GUI.enabled = !GEditorSettings.Instance.splineTools.autoTangent;
                instance.transform.localRotation = GEditorCommon.InlineEulerRotationField("Rotation", instance.transform.localRotation);
                GUI.enabled = true;
                instance.transform.localScale = GEditorCommon.InlineVector3Field("Scale", instance.transform.localScale);

                GEditorSettings.Instance.splineTools.showTransformGizmos = EditorGUILayout.Toggle("Show Transform Gizmos", GEditorSettings.Instance.splineTools.showTransformGizmos);
            },
            menu);
        }

        private void DrawAnchorDefaultValueGUI()
        {
            string label = "Anchor Defaults";
            string id = "anchordefaults" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                EditorGUIUtility.wideMode = true;
                instance.PositionOffset = EditorGUILayout.Vector3Field("Position Offset", instance.PositionOffset);
                instance.InitialRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Initial Rotation", instance.InitialRotation.eulerAngles));
                instance.InitialScale = EditorGUILayout.Vector3Field("Initial Scale", instance.InitialScale);
                EditorGUIUtility.wideMode = false;
            });
        }

        private void DrawSelectedAnchorGUI()
        {
            string label = "Selected Anchor";
            string id = "selectedanchor" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                selectedAnchorIndex = splineEditingDrawer.selectedAnchorIndex;
                if (selectedAnchorIndex >= 0 && selectedAnchorIndex < instance.Spline.Anchors.Count)
                {
                    GSplineAnchor a = instance.Spline.Anchors[selectedAnchorIndex];
                    GSplineAnchorInspectorDrawer.Create(a).DrawGUI();
                }
                else
                {
                    EditorGUILayout.LabelField("No Anchor selected!", GEditorCommon.ItalicLabel);
                }
            });
        }

        private void DrawSegmentDefaultValueGUI()
        {
            string label = "Segment Defaults";
            string id = "segmentdefaults" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                EditorGUIUtility.wideMode = true;
                instance.Smoothness = EditorGUILayout.IntField("Smoothness", instance.Smoothness);
                instance.Width = EditorGUILayout.FloatField("Width", instance.Width);
                instance.FalloffWidth = EditorGUILayout.FloatField("Falloff Width", instance.FalloffWidth);
                EditorGUIUtility.wideMode = false;
            });
        }

        private void DrawSelectedSegmentGUI()
        {
            string label = "Selected Segment";
            string id = "selectedsegment" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                selectedSegmentIndex = splineEditingDrawer.selectedSegmentIndex;
                if (selectedSegmentIndex >= 0 && selectedSegmentIndex < instance.Spline.Segments.Count)
                {
                    GSplineSegment s = instance.Spline.Segments[selectedSegmentIndex];
                    GUI.enabled = !GEditorSettings.Instance.splineTools.autoTangent;
                    s.StartTangent = GEditorCommon.InlineVector3Field("Start Tangent", s.StartTangent);
                    s.EndTangent = GEditorCommon.InlineVector3Field("End Tangent", s.EndTangent);
                    GUI.enabled = true;
                }
                else
                {
                    EditorGUILayout.LabelField("No Segment selected!", GEditorCommon.ItalicLabel);
                }
            });
        }

        private void DrawGizmosGUI()
        {
            string label = "Gizmos";
            string id = "gizmos" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                GEditorSettings.Instance.splineTools.showMesh = EditorGUILayout.Toggle("Show Mesh", GEditorSettings.Instance.splineTools.showMesh);
                EditorUtility.SetDirty(GEditorSettings.Instance);
            });
        }

        private void DrawActionsGUI()
        {
            string label = "Modifiers";
            string id = "modifiers" + instance.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                if (ModifierTypes.Count == 0)
                    return;
                Rect r = EditorGUILayout.GetControlRect();
                if (Event.current.type == EventType.Repaint)
                    addModifierButtonRect = r;
                if (GUI.Button(r, "Add Modifier"))
                {
                    GenericMenu menu = new GenericMenu();
                    for (int i = 0; i < ModifierTypes.Count; ++i)
                    {
                        Type t = ModifierTypes[i];
                        string menuLabel = string.Empty;
                        object[] alternativeClassNameAttributes = t.GetCustomAttributes(typeof(GDisplayName), false);
                        if (alternativeClassNameAttributes != null && alternativeClassNameAttributes.Length > 0)
                        {
                            GDisplayName att = alternativeClassNameAttributes[0] as GDisplayName;
                            if (att.DisplayName == null ||
                                att.DisplayName.Equals(string.Empty))
                                menuLabel = t.Name;
                            else
                                menuLabel = att.DisplayName;
                        }
                        else
                        {
                            menuLabel = t.Name;
                        }

                        menu.AddItem(
                            new GUIContent(ObjectNames.NicifyVariableName(menuLabel)),
                            false,
                            () =>
                            {
                                AddModifier(t);
                            });
                    }
                    menu.DropDown(addModifierButtonRect);
                }
            });
        }

        private void AddModifier(Type t)
        {
            GSplineModifier modifier = instance.gameObject.AddComponent(t) as GSplineModifier;
            modifier.SplineCreator = instance;
        }

        private void DuringSceneGUI(SceneView sv)
        {
            EditorGUI.BeginChangeCheck();
            splineEditingDrawer.Draw();
            if (EditorGUI.EndChangeCheck())
            {
                instance.Editor_Vertices = instance.GenerateVerticesWithFalloff();
                GSplineCreator.MarkSplineChanged(instance);
                GUtilities.MarkCurrentSceneDirty();
            }
        }

        private void SetPivotToMedianPoint()
        {
            Vector3 localMedian = Vector3.zero;
            List<GSplineAnchor> anchors = instance.Spline.Anchors;
            if (anchors.Count == 0)
                return;

            for (int i = 0; i < anchors.Count; ++i)
            {
                localMedian += anchors[i].Position;
            }
            localMedian = localMedian / anchors.Count;

            Vector3 worldMedian = instance.transform.TransformPoint(localMedian);
            Matrix4x4 medianToLocal = Matrix4x4.TRS(
                worldMedian,
                instance.transform.rotation,
                instance.transform.lossyScale).inverse;
            Matrix4x4 localToWorld = instance.transform.localToWorldMatrix;
            Matrix4x4 transformationMatrix = medianToLocal * localToWorld;

            for (int i = 0; i < anchors.Count; ++i)
            {
                anchors[i].Position = transformationMatrix.MultiplyPoint(anchors[i].Position);
            }

            List<GSplineSegment> segments = instance.Spline.Segments;
            for (int i = 0; i < segments.Count; ++i)
            {
                segments[i].StartTangent = transformationMatrix.MultiplyPoint(segments[i].StartTangent);
                segments[i].EndTangent = transformationMatrix.MultiplyPoint(segments[i].EndTangent);
            }

            instance.transform.position = worldMedian;
            instance.Editor_Vertices = instance.GenerateVerticesWithFalloff();
            GSplineCreator.MarkSplineChanged(instance);
        }

        private void OnBeginRender(Camera cam)
        {
            if (instance.EnableTerrainMask)
            {
                DrawTerrainMask(cam);
            }
        }

        private void DrawTerrainMask(Camera cam)
        {
            GCommon.ForEachTerrain(instance.GroupId, (t) =>
            {
                GLivePreviewDrawer.DrawTerrainMask(t, cam);
            });
        }

        private void OnBeginRenderSRP(ScriptableRenderContext context, Camera cam)
        {
            OnBeginRender(cam);
        }
    }
}
