using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Enum = System.Enum;
using Type = System.Type;

namespace Pinwheel.Griffin.PaintTool
{
    [CustomEditor(typeof(GObjectPainter))]
    public class GObjectPainterInspector : Editor
    {
        private GObjectPainter painter;
        private Rect addFilterButtonRect;

        private Vector3[] worldPoints = new Vector3[4];
        private Vector2[] normalizedPoints = new Vector2[4];

        private void OnEnable()
        {
            GCommon.RegisterBeginRender(OnBeginRender);
            GCommon.RegisterBeginRenderSRP(OnBeginRenderSRP);
            SceneView.duringSceneGui += DuringSceneGUI;
            LoadPrefs();
            painter = (GObjectPainter)target;
            Tools.hidden = true;
        }

        private void OnDisable()
        {
            GCommon.UnregisterBeginRender(OnBeginRender);
            GCommon.UnregisterBeginRenderSRP(OnBeginRenderSRP);
            SceneView.duringSceneGui -= DuringSceneGUI;
            SavePrefs();
            Tools.hidden = false;
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
            painter.EnableTerrainMask = EditorGUILayout.Toggle("Enable Terrain Mask", painter.EnableTerrainMask);

            DrawPaintMode();
            IGObjectPainter p = painter.ActivePainter;
            if (p == null)
            {
                EditorGUILayout.LabelField("No painter found!", GEditorCommon.WordWrapItalicLabel);
            }
            else
            {
                DrawInstructionGUI();
                DrawBrushMaskGUI();
                DrawObjectSelectionGUI();
                DrawBrushGUI();
                DrawFilterGUI();
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

        private void DrawCustomPainterSelectionGUI()
        {
            if (painter.Mode != GObjectPaintingMode.Custom)
                return;
            List<Type> customPainterTypes = GObjectPainter.CustomPainterTypes;
            string[] labels = new string[customPainterTypes.Count];
            for (int i = 0; i < labels.Length; ++i)
            {
                labels[i] = ObjectNames.NicifyVariableName(customPainterTypes[i].Name);
            }
            int[] indices = GUtilities.GetIndicesArray(customPainterTypes.Count);

            painter.CustomPainterIndex = EditorGUILayout.IntPopup("Custom Painter", painter.CustomPainterIndex, labels, indices);
            painter.CustomPainterArgs = EditorGUILayout.DelayedTextField("Custom Painter Args", painter.CustomPainterArgs);
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
            string id = "brushmasks" + painter.GetInstanceID().ToString();

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

        private void DrawObjectSelectionGUI()
        {
            string label = "Objects";
            string id = "objects" + painter.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                if (painter.Prototypes.Count > 0)
                {
                    GSelectionGridArgs args = new GSelectionGridArgs();
                    args.collection = painter.Prototypes;
                    args.selectedIndices = painter.SelectedPrototypeIndices;
                    args.itemSize = GEditorCommon.selectionGridTileSizeSmall;
                    args.itemPerRow = 4;
                    args.drawPreviewFunction = GEditorCommon.DrawGameObjectPreview;
                    args.contextClickFunction = OnObjectSelectorContextClick;
                    painter.SelectedPrototypeIndices = GEditorCommon.MultiSelectionGrid(args);
                }
                else
                {
                    EditorGUILayout.LabelField("No Game Object found!", GEditorCommon.WordWrapItalicLabel);
                }
                GEditorCommon.Separator();

                Rect r1 = EditorGUILayout.GetControlRect(GUILayout.Height(GEditorCommon.objectSelectorDragDropHeight));
                GameObject prefab = GEditorCommon.ObjectSelectorDragDrop<GameObject>(r1, "Drop a Game Object here!", "t:GameObject");
                if (prefab != null)
                {
                    painter.Prototypes.AddIfNotContains(prefab);
                }

                GEditorCommon.SpacePixel(0);
                Rect r2 = EditorGUILayout.GetControlRect(GUILayout.Height(GEditorCommon.objectSelectorDragDropHeight));
                GPrefabPrototypeGroup group = GEditorCommon.ObjectSelectorDragDrop<GPrefabPrototypeGroup>(r2, "Drop a Prefab Prototype Group here!", "t:GPrefabPrototypeGroup");
                if (group != null)
                {
                    painter.Prototypes.AddIfNotContains(group.Prototypes);
                }
            });
        }

        private void OnObjectSelectorContextClick(Rect r, int index, ICollection collection)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Remove"),
                false,
                () =>
                {
                    painter.Prototypes.RemoveAt(index);
                });

            menu.ShowAsContext();
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

                painter.BrushDensity = EditorGUILayout.IntSlider("Density", painter.BrushDensity, 1, 100);
                painter.BrushDensityJitter = EditorGUILayout.Slider("Jitter", painter.BrushDensityJitter, 0f, 1f);
                GEditorCommon.Separator();

                painter.BrushScatter = EditorGUILayout.Slider("Scatter", painter.BrushScatter, 0f, 1f);
                painter.BrushScatterJitter = EditorGUILayout.Slider("Jitter", painter.BrushScatterJitter, 0f, 1f);
                GEditorCommon.Separator();

                painter.EraseRatio = EditorGUILayout.Slider("Erase Ratio", painter.EraseRatio, 0f, 1f);
                painter.ScaleStrength = EditorGUILayout.FloatField("Scale Strength", painter.ScaleStrength);
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
                    painter.BrushDensity += GEditorSettings.Instance.paintTools.densityStep;
                }
                else if (k == KeyCode.Semicolon)
                {
                    painter.BrushDensity -= GEditorSettings.Instance.paintTools.densityStep;
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
                    if (painter.Mode != GObjectPaintingMode.Custom &&
                        (k - KeyCode.F1) != (int)GTexturePaintingMode.Custom)
                    {
                        painter.Mode = (GObjectPaintingMode)(k - KeyCode.F1);
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
                DrawHandleAtCursor(hitInfo);
                if (GGuiEventUtilities.IsLeftMouse)
                {
                    Paint(hitInfo);
                }
            }
        }

        private void DrawHandleAtCursor(RaycastHit hit)
        {
            Color cursorColor =
                Event.current.shift ? GEditorSettings.Instance.paintTools.alternativeActionCursorColor :
                Event.current.control ? GEditorSettings.Instance.paintTools.negativeActionCursorColor :
                GEditorSettings.Instance.paintTools.normalActionCursorColor;

            cursorColor.a = cursorColor.a * Mathf.Lerp(0.5f, 1f, painter.BrushDensity * 1.0f / 100f);

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

        private void Paint(RaycastHit hit)
        {
            GObjectPainterArgs args = new GObjectPainterArgs();
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
            painter.Paint(args);
        }

        private void DrawFilterGUI()
        {
            string label = "Filters";
            string id = "filters" + painter.GetInstanceID().ToString();
            GEditorCommon.Foldout(label, true, id, () =>
            {
                List<Type> suitableFilterTypes = null;
                try
                {
                    suitableFilterTypes = painter.ActivePainter.SuitableFilterTypes;
                }
                catch (System.Exception)
                {
                    suitableFilterTypes = new List<Type>();
                }

                GSpawnFilter[] filters = painter.GetComponents<GSpawnFilter>();
                for (int i = 0; i < filters.Length; ++i)
                {
                    Type filterType = filters[i].GetType();
                    GUI.enabled = suitableFilterTypes.Contains(filterType);
                    EditorGUILayout.LabelField(GEditorCommon.GetClassDisplayName(filterType), GEditorCommon.ItalicLabel);
                    GUI.enabled = true;
                }

                if (filters.Length > 0)
                {
                    GEditorCommon.Separator();
                }

                Rect r = EditorGUILayout.GetControlRect();
                if (Event.current.type == EventType.Repaint)
                    addFilterButtonRect = r;
                if (GUI.Button(r, "Add Filter"))
                {
                    GenericMenu menu = new GenericMenu();
                    if (suitableFilterTypes.Count == 0)
                    {
                        menu.AddDisabledItem(new GUIContent("No suitable filter!"));
                    }
                    else
                    {
                        for (int i = 0; i < suitableFilterTypes.Count; ++i)
                        {
                            Type t = suitableFilterTypes[i];
                            menu.AddItem(
                                new GUIContent(GEditorCommon.GetClassDisplayName(t)),
                                false,
                                () =>
                                {
                                    AddFilter(t);
                                });
                        }
                    }
                    menu.DropDown(addFilterButtonRect);
                }
            });
        }

        private void AddFilter(Type t)
        {
            painter.gameObject.AddComponent(t);
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
            args0.collection = Enum.GetValues(typeof(GObjectPaintingMode));
            args0.itemSize = GEditorCommon.selectionGridTileSizeWide;
            args0.itemPerRow = 2;
            args0.drawPreviewFunction = DrawModePreview;
            EditorGUI.BeginChangeCheck();
            painter.Mode = (GObjectPaintingMode)GEditorCommon.SelectionGrid(args0);
            if (EditorGUI.EndChangeCheck())
            {
                RecordPaintModeAnalytics();
            }

            if (painter.Mode == GObjectPaintingMode.Custom)
            {
                GEditorCommon.Separator();
                List<Type> customPainterTypes = GObjectPainter.CustomPainterTypes;
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
            GObjectPaintingMode mode = (GObjectPaintingMode)o;
            Texture2D icon = GEditorSkin.Instance.GetTexture(mode.ToString() + "ObjectIcon");
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
            painter.Mode = (GObjectPaintingMode)EditorGUILayout.EnumPopup("Paint Mode", painter.Mode);
            if (EditorGUI.EndChangeCheck())
            {
                RecordPaintModeAnalytics();
            }
            if (painter.Mode != GObjectPaintingMode.Custom)
                return;
            List<Type> customPainterTypes = GFoliagePainter.CustomPainterTypes;
            string[] labels = new string[customPainterTypes.Count];
            for (int i = 0; i < labels.Length; ++i)
            {
                labels[i] = ObjectNames.NicifyVariableName(GEditorCommon.GetClassDisplayName(customPainterTypes[i]));
            }
            int[] indices = GUtilities.GetIndicesArray(customPainterTypes.Count);

            painter.CustomPainterIndex = EditorGUILayout.IntPopup("Custom Painter", painter.CustomPainterIndex, labels, indices);
            painter.CustomPainterArgs = EditorGUILayout.DelayedTextField("Custom Args", painter.CustomPainterArgs);
        }

        private void RecordPaintModeAnalytics()
        {
            GObjectPaintingMode mode = painter.Mode;
            string url =
                mode == GObjectPaintingMode.Spawn ? GAnalytics.OPAINTER_SPAWN :
                mode == GObjectPaintingMode.Scale ? GAnalytics.OPAINTER_SCALE :
                mode == GObjectPaintingMode.Custom ? GAnalytics.OPAINTER_CUSTOM : string.Empty;
            GAnalytics.Record(url);
        }

        private void OnBeginRender(Camera cam)
        {
            if (painter.EnableTerrainMask)
            {
                DrawTerrainMask(cam);
            }
        }

        private void DrawTerrainMask(Camera cam)
        {
            GCommon.ForEachTerrain(painter.GroupId, (t) =>
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
