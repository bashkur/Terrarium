using Pinwheel.Griffin.BackupTool;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Enum = System.Enum;
using Type = System.Type;

namespace Pinwheel.Griffin.PaintTool
{
    [CustomEditor(typeof(GTerrainTexturePainter))]
    public class GTerrainPainterInspector : Editor
    {
        private GTerrainTexturePainter painter;
        private Vector3[] worldPoints = new Vector3[4];
        private Vector2[] normalizedPoints = new Vector2[4];

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            SceneView.duringSceneGui += DuringSceneGUI;
            LoadPrefs();
            painter = (GTerrainTexturePainter)target;
            Tools.hidden = true;

            GCommon.UpdateMaterials(painter.GroupId);
            GCommon.RegisterBeginRender(OnBeginRender);
            GCommon.RegisterBeginRenderSRP(OnBeginRenderSRP);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            SceneView.duringSceneGui -= DuringSceneGUI;
            SavePrefs();
            Tools.hidden = false;

            GCommon.UnregisterBeginRender(OnBeginRender);
            GCommon.UnregisterBeginRenderSRP(OnBeginRenderSRP);
            GTerrainTexturePainter.Internal_ReleaseRenderTextures();
        }

        private void OnUndoRedo()
        {
            if (Selection.activeGameObject != painter.gameObject)
                return;
            if (string.IsNullOrEmpty(GUndoCompatibleBuffer.Instance.CurrentBackupName))
                return;
            GBackup.Restore(GUndoCompatibleBuffer.Instance.CurrentBackupName);
        }

        private void SavePrefs()
        {
        }

        private void LoadPrefs()
        {
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            painter.GroupId = GEditorCommon.ActiveTerrainGroupPopupWithAllOption("Group Id", painter.GroupId);
            painter.ForceUpdateGeometry = EditorGUILayout.Toggle("Force Update Geometry", painter.ForceUpdateGeometry);
            painter.Editor_EnableHistory = EditorGUILayout.Toggle("Enable History", painter.Editor_EnableHistory);
            painter.Editor_EnableLivePreview = EditorGUILayout.Toggle("Enable Live Preview", painter.Editor_EnableLivePreview);

            bool isMaskPainterInUsed = painter.ActivePainter is GMaskPainter;
            if (!isMaskPainterInUsed)
            {
                painter.EnableTerrainMask = EditorGUILayout.Toggle("Enable Terrain Mask", painter.EnableTerrainMask);
            }
            GEditorSettings.Instance.topographic.enable = EditorGUILayout.Toggle("Enable Topographic", GEditorSettings.Instance.topographic.enable);

            DrawPaintMode();
            IGTexturePainter p = painter.ActivePainter;
            if (p == null)
            {
                EditorGUILayout.LabelField("No painter found!", GEditorCommon.WordWrapItalicLabel);
            }
            else
            {
                DrawInstructionGUI();
                DrawBrushMaskGUI();
                if (painter.Mode == GTexturePaintingMode.Splat || painter.Mode == GTexturePaintingMode.Custom)
                    DrawSplatGUI();
                DrawBrushGUI();
                if (painter.ActivePainter != null && painter.ActivePainter is IGTexturePainterWithCustomParams)
                {
                    IGTexturePainterWithCustomParams activePainter = painter.ActivePainter as IGTexturePainterWithCustomParams;
                    activePainter.Editor_DrawCustomParamsGUI();
                }

#if GRIFFIN_VEGETATION_STUDIO_PRO
                GEditorCommon.DrawVspIntegrationGUI();
#endif

                GEditorCommon.DrawBackupHelpBox();
            }

            if (EditorGUI.EndChangeCheck())
            {
                GUtilities.MarkCurrentSceneDirty();
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        private void DrawInstructionGUI()
        {
            string label = "Instruction";
            string id = "instruction" + painter.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                string text = null;
                try
                {
                    text = painter.ActivePainter.Instruction;
                }
                catch (System.Exception e)
                {
                    text = e.Message;
                }
                EditorGUILayout.LabelField(text, GEditorCommon.WordWrapItalicLabel);
            });
        }

        private void DrawBrushMaskGUI()
        {
            string label = "Brush Masks";
            string id = "mask" + painter.GetInstanceID().ToString();

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("New Brush"),
                false,
                DisplayNewBrushDialog);
            menu.AddItem(
                new GUIContent("Refresh"),
                false,
                painter.ReloadBrushMasks);

            GEditorCommon.Foldout(label, true, id, () =>
            {
                GSelectionGridArgs args = new GSelectionGridArgs();
                args.selectedIndex = painter.SelectedBrushMaskIndex;
                args.collection = painter.BrushMasks;
                args.itemSize = GEditorCommon.selectionGridTileSizeSmall;
                args.itemPerRow = 4;
                args.drawPreviewFunction = DrawBrushMaskPreview;
                painter.SelectedBrushMaskIndex = GEditorCommon.SelectionGrid(args);
            },
            menu);
        }

        private void DisplayNewBrushDialog()
        {
            EditorUtility.DisplayDialog(
                "Info",
                "To add a new brush, copy your brush texture to a Resources/PolarisBrushes/ folder, then Refresh.",
                "OK");
        }

        private void DrawBrushMaskPreview(Rect r, object o)
        {
            Texture2D tex = (Texture2D)o;
            if (tex != null)
            {
                EditorGUI.DrawPreviewTexture(r, tex);
            }
            else
            {
                EditorGUI.DrawRect(r, Color.black);
            }
        }

        private void DrawSplatGUI()
        {
            string label = "Splats";
            string id = "splats" + painter.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                if (GEditorSettings.Instance.paintTools.useMultiSplatsSelector)
                {
                    painter.SelectedSplatIndices = GEditorCommon.SplatSetMultiSelectionGrid(painter.GroupId, painter.SelectedSplatIndices);
                }
                else
                {
                    painter.SelectedSplatIndex = GEditorCommon.SplatSetSelectionGrid(painter.GroupId, painter.SelectedSplatIndex);
                }
                GEditorSettings.Instance.paintTools.useMultiSplatsSelector = EditorGUILayout.Toggle("Multi Selection", GEditorSettings.Instance.paintTools.useMultiSplatsSelector);
                EditorUtility.SetDirty(GEditorSettings.Instance);
                EditorGUILayout.Space();
            });
        }

        private void DrawBrushGUI()
        {
            string label = "Brush";
            string id = "brush" + painter.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                painter.BrushRadius = EditorGUILayout.FloatField("Radius", painter.BrushRadius);
                painter.BrushRadiusJitter = EditorGUILayout.Slider("Jitter", painter.BrushRadiusJitter, 0f, 1f);
                GEditorCommon.Separator();

                painter.BrushRotation = EditorGUILayout.Slider("Rotation", painter.BrushRotation, -360f, 360f);
                painter.BrushRotationJitter = EditorGUILayout.Slider("Jitter", painter.BrushRotationJitter, 0f, 1f);
                GEditorCommon.Separator();

                Rect r0 = EditorGUILayout.GetControlRect();
                if (painter.BrushOpacity <= 0 || painter.BrushOpacity >= 1)
                {
                    EditorGUI.DrawRect(r0, GEditorCommon.WarningLabel.normal.textColor);
                }
                painter.BrushOpacity = EditorGUI.Slider(r0, "Opacity", painter.BrushOpacity, 0f, 1f);
                painter.BrushOpacityJitter = EditorGUILayout.Slider("Jitter", painter.BrushOpacityJitter, 0f, 1f);

                Rect r1 = EditorGUILayout.GetControlRect();
                if (painter.BrushTargetStrength <= 0)
                {
                    EditorGUI.DrawRect(r1, GEditorCommon.WarningLabel.normal.textColor);
                }
                painter.BrushTargetStrength = EditorGUI.Slider(r1, "Target Strength", painter.BrushTargetStrength, 0f, 1f);
                GEditorCommon.Separator();

                painter.BrushScatter = EditorGUILayout.Slider("Scatter", painter.BrushScatter, 0f, 1f);
                painter.BrushScatterJitter = EditorGUILayout.Slider("Jitter", painter.BrushScatterJitter, 0f, 1f);
                GEditorCommon.Separator();

                painter.BrushColor = EditorGUILayout.ColorField("Color", painter.BrushColor);
                painter.SamplePoint = GEditorCommon.InlineVector3Field("Sample Point", painter.SamplePoint);
            });
        }

        private void DuringSceneGUI(SceneView sv)
        {
            HandleTerrainEditingInSceneView();
            HandleBrushSettingsShortcuts();
            HandleFunctionKeys();
            if (Event.current != null && Event.current.type == EventType.MouseMove)
                SceneView.RepaintAll();
        }

        private void HandleBrushSettingsShortcuts()
        {
            if (Event.current != null && Event.current.isKey)
            {
                KeyCode k = Event.current.keyCode;
                if (k == KeyCode.Equals)
                {
                    painter.BrushRadius += GEditorSettings.Instance.paintTools.radiusStep;
                }
                else if (k == KeyCode.Minus)
                {
                    painter.BrushRadius -= GEditorSettings.Instance.paintTools.radiusStep;
                }
                else if (k == KeyCode.RightBracket)
                {
                    painter.BrushRotation += GEditorSettings.Instance.paintTools.rotationStep;
                }
                else if (k == KeyCode.LeftBracket)
                {
                    painter.BrushRotation -= GEditorSettings.Instance.paintTools.rotationStep;
                }
                else if (k == KeyCode.Quote)
                {
                    painter.BrushOpacity += GEditorSettings.Instance.paintTools.opacityStep;
                }
                else if (k == KeyCode.Semicolon)
                {
                    painter.BrushOpacity -= GEditorSettings.Instance.paintTools.opacityStep;
                }
            }
        }

        private void HandleFunctionKeys()
        {
            if (Event.current != null && Event.current.type == EventType.KeyDown)
            {
                KeyCode k = Event.current.keyCode;
                if (k >= KeyCode.F1 && k <= KeyCode.F12)
                {
                    if (painter.Mode != GTexturePaintingMode.Custom &&
                        (k - KeyCode.F1) != (int)GTexturePaintingMode.Custom)
                    {
                        painter.Mode = (GTexturePaintingMode)(k - KeyCode.F1);
                    }
                    else
                    {
                        painter.CustomPainterIndex = k - KeyCode.F1;
                    }
                }
            }
        }

        private void HandleTerrainEditingInSceneView()
        {
            if (!painter.enabled)
                return;

            if (Event.current == null)
                return;

            if (Event.current.alt == true)
                return;

            if (Event.current.type != EventType.Repaint &&
                Event.current.type != EventType.MouseDown &&
                Event.current.type != EventType.MouseDrag &&
                Event.current.type != EventType.MouseUp &&
                Event.current.type != EventType.KeyDown)
                return;

            int controlId = GUIUtility.GetControlID(this.GetHashCode(), FocusType.Passive);
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0)
                {
                    //Set the hot control to this tool, to disable marquee selection tool on mouse dragging
                    GUIUtility.hotControl = controlId;
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                GRuntimeSettings.Instance.isEditingGeometry = false;
                if (GUIUtility.hotControl == controlId)
                {
                    //Return the hot control back to Unity, use the default
                    GUIUtility.hotControl = 0;
                }
            }

            Ray r = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (GStylizedTerrain.Raycast(r, out hit, float.MaxValue, painter.GroupId))
            {
                OnRaycast(true, hit);
            }
            else
            {
                OnRaycast(false, hit);
            }

            if (GGuiEventUtilities.IsLeftMouse)
            {
                Event.current.Use();
            }
        }

        private void OnRaycast(bool isHit, RaycastHit hitInfo)
        {
            if (isHit)
            {
                if (painter.Editor_EnableLivePreview &&
                    painter.ActivePainter != null &&
                    painter.ActivePainter is IGTexturePainterWithLivePreview)
                {
                    //DrawLivePreview(hitInfo);
                }
                else
                {
                    DrawHandleAtCursor(hitInfo);
                }

                if (GGuiEventUtilities.IsLeftMouse)
                {
                    Paint(hitInfo);
                }
            }
        }

        private GTexturePainterArgs CreateBasicArgs(RaycastHit hit)
        {
            GTexturePainterArgs args = new GTexturePainterArgs();
            args.HitPoint = hit.point;
            args.Collider = hit.collider;
            args.Transform = hit.transform;
            args.UV = hit.textureCoord;
            args.TriangleIndex = hit.triangleIndex;
            args.BarycentricCoord = hit.barycentricCoordinate;
            args.Distance = hit.distance;
            args.Normal = hit.normal;
            args.LightMapCoord = hit.lightmapCoord;

            args.MouseEventType =
                Event.current.type == EventType.MouseDown ? GPainterMouseEventType.Down :
                Event.current.type == EventType.MouseDrag ? GPainterMouseEventType.Drag :
                GPainterMouseEventType.Up;
            args.ActionType =
                Event.current.shift ? GPainterActionType.Alternative :
                Event.current.control ? GPainterActionType.Negative :
                GPainterActionType.Normal;

            return args;
        }

        private void Paint(RaycastHit hit)
        {
            GTexturePainterArgs args = CreateBasicArgs(hit);
            painter.Paint(args);
        }

        private void DrawHandleAtCursor(RaycastHit hit)
        {
            Color cursorColor =
                Event.current.shift ? GEditorSettings.Instance.paintTools.alternativeActionCursorColor :
                Event.current.control ? GEditorSettings.Instance.paintTools.negativeActionCursorColor :
                GEditorSettings.Instance.paintTools.normalActionCursorColor;

            cursorColor.a = cursorColor.a * Mathf.Lerp(0.5f, 1f, painter.BrushOpacity);

            if (GEditorSettings.Instance.paintTools.useSimpleCursor)
            {
                Handles.color = cursorColor;
                Vector3[] corner = GCommon.GetBrushQuadCorners(hit.point, painter.BrushRadius, painter.BrushRotation);
                Handles.DrawAAPolyLine(5, corner[0], corner[1], corner[2], corner[3], corner[0]);
            }
            else
            {
                Matrix4x4 cursorToWorld = Matrix4x4.TRS(hit.point, Quaternion.Euler(0, painter.BrushRotation, 0), 2 * painter.BrushRadius * Vector3.one);
                worldPoints[0] = cursorToWorld.MultiplyPoint(new Vector3(-0.5f, 0, -0.5f));
                worldPoints[1] = cursorToWorld.MultiplyPoint(new Vector3(-0.5f, 0, 0.5f));
                worldPoints[2] = cursorToWorld.MultiplyPoint(new Vector3(0.5f, 0, 0.5f));
                worldPoints[3] = cursorToWorld.MultiplyPoint(new Vector3(0.5f, 0, -0.5f));

                Material mat = GInternalMaterials.PainterCursorProjectorMaterial;
                mat.SetColor("_Color", cursorColor);
                mat.SetTexture("_MainTex", painter.BrushMasks[painter.SelectedBrushMaskIndex]);
                mat.SetMatrix("_WorldToCursorMatrix", cursorToWorld.inverse);
                mat.SetPass(0);

                IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
                while (terrains.MoveNext())
                {
                    GStylizedTerrain t = terrains.Current;
                    if (t.TerrainData == null)
                        continue;
                    if (painter.GroupId >= 0 &&
                        painter.GroupId != t.GroupId)
                        continue;
                    DrawCursorProjected(t);
                }
            }
        }

        private void DrawCursorProjected(GStylizedTerrain t)
        {
            for (int i = 0; i < normalizedPoints.Length; ++i)
            {
                normalizedPoints[i] = t.WorldPointToUV(worldPoints[i]);
            }

            Rect dirtyRect = GUtilities.GetRectContainsPoints(normalizedPoints);
            GTerrainChunk[] chunks = t.GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                Rect uvRect = chunks[i].GetUvRange();
                if (!uvRect.Overlaps(dirtyRect))
                    continue;
                Mesh m = chunks[i].MeshFilterComponent.sharedMesh;
                if (m == null)
                    continue;
                Graphics.DrawMeshNow(
                    m,
                    Matrix4x4.TRS(chunks[i].transform.position, chunks[i].transform.rotation, chunks[i].transform.lossyScale));
            }
        }

        private void DrawPaintMode()
        {
            string label = "Mode";
            string id = "mode" + painter.GetInstanceID().ToString();

            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Grid"),
                GEditorSettings.Instance.paintTools.paintModeSelectorType == GEditorSettings.PaintToolsSettings.GPaintModeSelectorType.Grid,
                () =>
                {
                    GEditorSettings.Instance.paintTools.paintModeSelectorType = GEditorSettings.PaintToolsSettings.GPaintModeSelectorType.Grid;
                });
            menu.AddItem(
                new GUIContent("Dropdown"),
                GEditorSettings.Instance.paintTools.paintModeSelectorType == GEditorSettings.PaintToolsSettings.GPaintModeSelectorType.Dropdown,
                () =>
                {
                    GEditorSettings.Instance.paintTools.paintModeSelectorType = GEditorSettings.PaintToolsSettings.GPaintModeSelectorType.Dropdown;
                });

            GEditorCommon.Foldout(label, true, id, () =>
            {
                if (GEditorSettings.Instance.paintTools.paintModeSelectorType == GEditorSettings.PaintToolsSettings.GPaintModeSelectorType.Grid)
                {
                    ShowPaintModeAsGrid();
                }
                else
                {
                    ShowPaintModeAsDropdown();
                }
            },
            menu);
        }

        private void ShowPaintModeAsGrid()
        {
            GSelectionGridArgs args0 = new GSelectionGridArgs();
            args0.selectedIndex = (int)painter.Mode;
            args0.collection = Enum.GetValues(typeof(GTexturePaintingMode));
            args0.itemSize = GEditorCommon.selectionGridTileSizeWide;
            args0.itemPerRow = 2;
            args0.drawPreviewFunction = DrawModePreview;
            EditorGUI.BeginChangeCheck();
            painter.Mode = (GTexturePaintingMode)GEditorCommon.SelectionGrid(args0);
            if (EditorGUI.EndChangeCheck())
            {
                RecordPaintModeAnalytics();
            }

            if (painter.Mode == GTexturePaintingMode.Custom)
            {
                GEditorCommon.Separator();
                List<Type> customPainterTypes = GTerrainTexturePainter.GetCustomPainterTypes();
                if (customPainterTypes.Count == 0)
                {
                    EditorGUILayout.LabelField("No Custom Painter defined!", GEditorCommon.WordWrapItalicLabel);
                }
                else
                {
                    GSelectionGridArgs args1 = new GSelectionGridArgs();
                    args1.selectedIndex = painter.CustomPainterIndex;
                    args1.collection = customPainterTypes;
                    args1.itemSize = GEditorCommon.selectionGridTileSizeWide;
                    args1.itemPerRow = 2;
                    args1.drawPreviewFunction = DrawCustomMode;
                    painter.CustomPainterIndex = GEditorCommon.SelectionGrid(args1);
                    GEditorCommon.Separator();
                    painter.CustomPainterArgs = EditorGUILayout.TextField("Custom Args", painter.CustomPainterArgs);
                }
            }
        }

        private void DrawModePreview(Rect r, object o)
        {
            GTexturePaintingMode mode = (GTexturePaintingMode)o;

            Texture2D icon = GEditorSkin.Instance.GetTexture(mode.ToString() + "Icon");
            if (icon == null)
            {
                icon = GEditorSkin.Instance.GetTexture("GearIcon");
            }
            string label = ObjectNames.NicifyVariableName(mode.ToString());
            DrawMode(r, label, icon);
        }

        private void DrawCustomMode(Rect r, object o)
        {
            Type t = (Type)o;
            if (t != null)
            {
                string label = ObjectNames.NicifyVariableName(GEditorCommon.GetClassDisplayName(t));
                Texture2D icon = GEditorSkin.Instance.GetTexture("GearIcon");
                DrawMode(r, label, icon);
            }
        }

        private void DrawMode(Rect r, string label, Texture icon)
        {
            GUIStyle labelStyle = EditorStyles.miniLabel;
            Rect iconRect = new Rect(r.min.x, r.min.y, r.height, r.height);
            Rect labelRect = new Rect(r.min.x + r.height + 1, r.min.y, r.width - r.height, r.height);

            GEditorCommon.DrawBodyBox(r);
            GEditorCommon.DrawHeaderBox(iconRect);
            if (icon != null)
            {
                GUI.color = labelStyle.normal.textColor;
                RectOffset offset = new RectOffset(3, 3, 3, 3);
                GUI.DrawTexture(offset.Remove(iconRect), icon);
                GUI.color = Color.white;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GUI.Label(labelRect, label, labelStyle);
            EditorGUI.indentLevel = indent;
        }

        private void ShowPaintModeAsDropdown()
        {
            EditorGUI.BeginChangeCheck();
            painter.Mode = (GTexturePaintingMode)EditorGUILayout.EnumPopup("Paint Mode", painter.Mode);
            if (EditorGUI.EndChangeCheck())
            {
                RecordPaintModeAnalytics();
            }
            if (painter.Mode != GTexturePaintingMode.Custom)
                return;
            List<Type> customPainterTypes = GTerrainTexturePainter.GetCustomPainterTypes();
            string[] labels = new string[customPainterTypes.Count];
            for (int i = 0; i < labels.Length; ++i)
            {
                labels[i] = ObjectNames.NicifyVariableName(GEditorCommon.GetClassDisplayName(customPainterTypes[i]));
            }
            int[] indices = GUtilities.GetIndicesArray(customPainterTypes.Count);

            painter.CustomPainterIndex = EditorGUILayout.IntPopup("Custom Painter", painter.CustomPainterIndex, labels, indices);
            painter.CustomPainterArgs = EditorGUILayout.DelayedTextField("Custom Args", painter.CustomPainterArgs);
        }

        private void OnBeginRender(Camera cam)
        {
            if (cam.cameraType != CameraType.SceneView)
                return;

            if (painter.Editor_EnableLivePreview)
            {
                bool canDrawLivePreview = painter.ActivePainter != null && painter.ActivePainter is IGTexturePainterWithLivePreview;
                if (canDrawLivePreview)
                {
                    Ray r = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hit;
                    if (GStylizedTerrain.Raycast(r, out hit, float.MaxValue, painter.GroupId))
                    {
                        DrawLivePreview(cam, hit);
                    }
                }
            }

            if (painter.EnableTerrainMask)
            {
                DrawMask(cam);
            }
        }

        private void OnBeginRenderSRP(UnityEngine.Rendering.ScriptableRenderContext context, Camera cam)
        {
            OnBeginRender(cam);
        }

        private void DrawLivePreview(Camera cam, RaycastHit hitInfo)
        {
            bool canDrawLivePreview = painter.ActivePainter != null && painter.ActivePainter is IGTexturePainterWithLivePreview;
            if (!canDrawLivePreview)
                return;

            IGTexturePainterWithLivePreview activePainter = painter.ActivePainter as IGTexturePainterWithLivePreview;
            GTexturePainterArgs args = CreateBasicArgs(hitInfo);
            painter.FillArgs(ref args, true);

            IEnumerator<GStylizedTerrain> terrains = GStylizedTerrain.ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                if (terrains.Current.GroupId != painter.GroupId && painter.GroupId >= 0)
                    continue;
                activePainter.Editor_DrawLivePreview(terrains.Current, args, cam);
            }
        }

        private void DrawMask(Camera cam)
        {
            GCommon.ForEachTerrain(painter.GroupId, (t) =>
            {
                if (t.TerrainData == null)
                    return;
                if (painter.ActivePainter is GMaskPainter)
                {
                    if (GTexturePainterCustomParams.Instance.Mask.Visualize)
                    {
                        GLivePreviewDrawer.DrawMask4ChannelsLivePreview(t, cam, t.TerrainData.Mask.MaskMapOrDefault, GCommon.UnitRect);
                    }
                }
                else
                {
                    GLivePreviewDrawer.DrawTerrainMask(t, cam);
                }
            });
        }

        private void RecordPaintModeAnalytics()
        {
            GTexturePaintingMode mode = painter.Mode;
            string url =
                mode == GTexturePaintingMode.Elevation ? GAnalytics.TPAINTER_ELEVATION :
                mode == GTexturePaintingMode.HeightSampling ? GAnalytics.TPAINTER_HEIGHT_SAMPLING :
                mode == GTexturePaintingMode.Terrace ? GAnalytics.TPAINTER_TERRACE :
                mode == GTexturePaintingMode.Remap ? GAnalytics.TPAINTER_REMAP :
                mode == GTexturePaintingMode.Noise ? GAnalytics.TPAINTER_NOISE :
                mode == GTexturePaintingMode.SubDivision ? GAnalytics.TPAINTER_SUBDIV :
                mode == GTexturePaintingMode.Albedo ? GAnalytics.TPAINTER_ALBEDO :
                mode == GTexturePaintingMode.Metallic ? GAnalytics.TPAINTER_METALLIC :
                mode == GTexturePaintingMode.Smoothness ? GAnalytics.TPAINTER_SMOOTHNESS :
                mode == GTexturePaintingMode.Splat ? GAnalytics.TPAINTER_SPLAT :
                mode == GTexturePaintingMode.Custom ? GAnalytics.TPAINTER_CUSTOM : string.Empty;
            GAnalytics.Record(url);
        }
    }
}
