using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Pinwheel.Griffin.SplineTool
{
    public class GSplineEditingGUIDrawer
    {
        private const float BEZIER_SELECT_DISTANCE = 10;
        private const float BEZIER_WIDTH = 5;

        public int selectedAnchorIndex = -1;
        public int selectedSegmentIndex = -1;

        private GSplineCreator splineCreator;
        bool autoTangent;
        bool showTransformGizmos;

        public GSplineEditingGUIDrawer(GSplineCreator splineCreator)
        {
            this.splineCreator = splineCreator;
        }

        public void Draw()
        {
            if (Event.current == null)
                return;

            autoTangent = GEditorSettings.Instance.splineTools.autoTangent;
            showTransformGizmos = GEditorSettings.Instance.splineTools.showTransformGizmos;
            //DrawDebug();

            HandleSelectTransformRemoveAnchor();
            HandleSelectTransformRemoveSegment();
            HandleAddAnchor();
            DrawMesh();

            DrawPivot();
            DrawInstruction();
            CatchHotControl();
        }

        private void HandleSelectTransformRemoveAnchor()
        {
            List<GSplineAnchor> anchors = splineCreator.Spline.Anchors;

            for (int i = 0; i < anchors.Count; ++i)
            {
                GSplineAnchor a = anchors[i];
                Vector3 localPos = a.Position;
                Vector3 worldPos = splineCreator.transform.TransformPoint(localPos);
                float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.2f;
                if (i == selectedAnchorIndex && !showTransformGizmos)
                {
                    Handles.color = GEditorSettings.Instance.splineTools.selectedElementColor;
                    Handles.SphereHandleCap(0, worldPos, Quaternion.identity, handleSize, EventType.Repaint);
                    bool isGlobalRotation = Tools.pivotRotation == PivotRotation.Global;

                    EditorGUI.BeginChangeCheck();
                    if (Tools.current == Tool.Move)
                    {
                        worldPos = Handles.PositionHandle(worldPos, isGlobalRotation ? Quaternion.identity : a.Rotation);
                        localPos = splineCreator.transform.InverseTransformPoint(worldPos);
                        a.Position = localPos;
                    }
                    else if (Tools.current == Tool.Rotate && !GEditorSettings.Instance.splineTools.autoTangent)
                    {
                        a.Rotation = Handles.RotationHandle(a.Rotation, worldPos);
                    }
                    else if (Tools.current == Tool.Scale)
                    {
                        a.Scale = Handles.ScaleHandle(a.Scale, worldPos, isGlobalRotation ? Quaternion.identity : a.Rotation, HandleUtility.GetHandleSize(worldPos));
                    }
                    anchors[i] = a;
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (autoTangent)
                        {
                            splineCreator.Spline.SmoothTangents(selectedAnchorIndex);
                            GUI.changed = true;
                        }
                    }
                }
                else
                {
                    Handles.color = GEditorSettings.Instance.splineTools.anchorColor;
                    if (Handles.Button(worldPos, Quaternion.identity, handleSize, handleSize * 0.5f, Handles.SphereHandleCap) && !showTransformGizmos)
                    {
                        if (!Event.current.alt)
                        {
                            if (Event.current.control)
                            {
                                selectedAnchorIndex = -1;
                                selectedSegmentIndex = -1;
                                splineCreator.Spline.RemoveAnchor(i);
                                GUI.changed = true;
                            }
                            else if (Event.current.shift)
                            {
                                if (selectedAnchorIndex != i &&
                                    selectedAnchorIndex >= 0 &&
                                    selectedAnchorIndex < anchors.Count)
                                {
                                    splineCreator.Spline.AddSegment(selectedAnchorIndex, i);
                                    if (autoTangent)
                                    {
                                        int[] segmentsIndices = splineCreator.Spline.SmoothTangents(selectedAnchorIndex, i);
                                    }
                                    else
                                    {
                                    }
                                    selectedAnchorIndex = i;
                                    selectedSegmentIndex = -1;
                                    GUI.changed = true;
                                }
                            }
                            else
                            {
                                selectedAnchorIndex = i;
                                selectedSegmentIndex = -1;
                                GUI.changed = true;
                            }
                            Event.current.Use();
                        }
                    }
                }
            }
        }

        private void HandleAddAnchor()
        {
            if (showTransformGizmos)
                return;
            bool isLeftMouseUp = Event.current.type == EventType.MouseUp && Event.current.button == 0;
            bool isShift = Event.current.shift;
            bool isAlt = Event.current.alt;
            if (!isLeftMouseUp)
                return;
            int raycastLayer = GEditorSettings.Instance.splineTools.raycastLayer;
            Ray r = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(r, out hit, 10000, LayerMask.GetMask(LayerMask.LayerToName(raycastLayer))))
            {
                if (isAlt)
                {
                    return;
                }
                if (!isShift)
                {
                    selectedAnchorIndex = -1;
                    return;
                }
                Vector3 offset = splineCreator.PositionOffset;
                Vector3 worldPos = hit.point + offset;
                Vector3 localPos = splineCreator.transform.InverseTransformPoint(worldPos);
                GSplineAnchor a = new GSplineAnchor(localPos);
                splineCreator.Spline.AddAnchor(a);

                if (selectedAnchorIndex >= 0 && selectedAnchorIndex < splineCreator.Spline.Anchors.Count - 1)
                {
                    splineCreator.Spline.AddSegment(selectedAnchorIndex, splineCreator.Spline.Anchors.Count - 1);
                    if (autoTangent)
                    {
                        int[] segmentIndices = splineCreator.Spline.SmoothTangents(selectedAnchorIndex, splineCreator.Spline.Anchors.Count - 1);
                    }
                    else
                    {
                    }
                }

                selectedAnchorIndex = splineCreator.Spline.Anchors.Count - 1;
                GUI.changed = true;
                Event.current.Use();
            }
            else
            {
                selectedAnchorIndex = -1;
            }
        }

        private void HandleSelectTransformRemoveSegment()
        {
            List<GSplineSegment> segments = splineCreator.Spline.Segments;
            List<GSplineAnchor> anchors = splineCreator.Spline.Anchors;
            for (int i = 0; i < segments.Count; ++i)
            {
                if (!splineCreator.Spline.IsSegmentValid(i))
                    continue;
                if (i == selectedSegmentIndex && !autoTangent && !showTransformGizmos)
                    HandleSelectedSegmentModifications();
                int i0 = segments[i].StartIndex;
                int i1 = segments[i].EndIndex;
                GSplineAnchor a0 = anchors[i0];
                GSplineAnchor a1 = anchors[i1];
                Vector3 startPosition = splineCreator.transform.TransformPoint(a0.Position);
                Vector3 endPosition = splineCreator.transform.TransformPoint(a1.Position);
                Vector3 startTangent = splineCreator.transform.TransformPoint(segments[i].StartTangent);
                Vector3 endTangent = splineCreator.transform.TransformPoint(segments[i].EndTangent);
                Color color = (i == selectedSegmentIndex) ?
                    GEditorSettings.Instance.splineTools.selectedElementColor :
                    GEditorSettings.Instance.splineTools.segmentColor; ;
                Color colorFade = new Color(color.r, color.g, color.b, color.a * 0.1f);

                Vector3[] bezierPoints = Handles.MakeBezierPoints(startPosition, endPosition, startTangent, endTangent, splineCreator.Smoothness);
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = color;
                Handles.DrawAAPolyLine(BEZIER_WIDTH, bezierPoints);
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.color = colorFade;
                Handles.DrawAAPolyLine(BEZIER_WIDTH, bezierPoints);

                Matrix4x4 localToWorld = splineCreator.transform.localToWorldMatrix;
                Matrix4x4 splineToLocal = splineCreator.Spline.TRS(i, 0.5f);
                Matrix4x4 splineToWorld = localToWorld * splineToLocal;
                Vector3 arrow0 = splineToWorld.MultiplyPoint(Vector3.zero);
                splineToLocal = splineCreator.Spline.TRS(i, 0.45f);
                splineToWorld = localToWorld * splineToLocal;
                Vector3 arrow1 = splineToWorld.MultiplyPoint(Vector3.left * 0.5f);
                Vector3 arrow2 = splineToWorld.MultiplyPoint(Vector3.right * 0.5f);
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = color;
                Handles.DrawAAPolyLine(BEZIER_WIDTH, arrow1, arrow0, arrow2);
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.color = colorFade;
                Handles.DrawAAPolyLine(BEZIER_WIDTH, arrow1, arrow0, arrow2);

                if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && !showTransformGizmos && !Event.current.alt)
                {
                    float d0 = DistanceMouseToSpline(Event.current.mousePosition, bezierPoints);
                    float d1 = DistanceMouseToPoint(Event.current.mousePosition, bezierPoints[0]);
                    float d2 = DistanceMouseToPoint(Event.current.mousePosition, bezierPoints[bezierPoints.Length - 1]);
                    if (d0 <= BEZIER_SELECT_DISTANCE &&
                        d1 > BEZIER_SELECT_DISTANCE &&
                        d2 > BEZIER_SELECT_DISTANCE)
                    {
                        selectedSegmentIndex = i;
                        if (Event.current.control)
                        {
                            splineCreator.Spline.RemoveSegment(selectedSegmentIndex);
                            selectedSegmentIndex = -1;
                            GUI.changed = true;
                        }
                        //don't Use() the event here
                    }
                    else
                    {
                        if (selectedSegmentIndex == i)
                        {
                            selectedSegmentIndex = -1;
                        }
                    }
                }
            }
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        }

        private void HandleSelectedSegmentModifications()
        {
            if (selectedSegmentIndex < 0 || selectedSegmentIndex >= splineCreator.Spline.Segments.Count)
                return;
            if (!splineCreator.Spline.IsSegmentValid(selectedSegmentIndex))
                return;
            GSplineSegment segment = splineCreator.Spline.Segments[selectedSegmentIndex];
            GSplineAnchor startAnchor = splineCreator.Spline.Anchors[segment.StartIndex];
            GSplineAnchor endAnchor = splineCreator.Spline.Anchors[segment.EndIndex];

            Vector3 worldStartPosition = splineCreator.transform.TransformPoint(startAnchor.Position);
            Vector3 worldEndPosition = splineCreator.transform.TransformPoint(endAnchor.Position);
            Vector3 worldStartTangent = splineCreator.transform.TransformPoint(segment.StartTangent);
            Vector3 worldEndTangent = splineCreator.transform.TransformPoint(segment.EndTangent);

            EditorGUI.BeginChangeCheck();
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            worldStartTangent = Handles.PositionHandle(worldStartTangent, Quaternion.identity);
            worldEndTangent = Handles.PositionHandle(worldEndTangent, Quaternion.identity);

            segment.StartTangent = splineCreator.transform.InverseTransformPoint(worldStartTangent);
            segment.EndTangent = splineCreator.transform.InverseTransformPoint(worldEndTangent);
            if (EditorGUI.EndChangeCheck())
            {
                GUI.changed = true;
            }

            Handles.color = Color.white;
            Handles.DrawLine(worldStartPosition, worldStartTangent);
            Handles.DrawLine(worldEndPosition, worldEndTangent);
        }

        private void DrawPivot()
        {
            if (!showTransformGizmos)
            {
                Vector3 pivot = splineCreator.transform.position;
                float size = HandleUtility.GetHandleSize(pivot);

                Vector3 xStart = pivot + Vector3.left * size;
                Vector3 xEnd = pivot + Vector3.right * size;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                Handles.color = Handles.xAxisColor;
                Handles.DrawLine(xStart, xEnd);

                Vector3 yStart = pivot + Vector3.down * size;
                Vector3 yEnd = pivot + Vector3.up * size;
                Handles.color = Handles.yAxisColor;
                Handles.DrawLine(yStart, yEnd);

                Vector3 zStart = pivot + Vector3.back * size;
                Vector3 zEnd = pivot + Vector3.forward * size;
                Handles.color = Handles.zAxisColor;
                Handles.DrawLine(zStart, zEnd);
            }
            else
            {
                if (Tools.current == Tool.Move)
                {
                    splineCreator.transform.position = Handles.PositionHandle(splineCreator.transform.position, splineCreator.transform.rotation);
                }
                else if (Tools.current == Tool.Rotate)
                {
                    splineCreator.transform.rotation = Handles.RotationHandle(splineCreator.transform.rotation, splineCreator.transform.position);
                }
                else if (Tools.current == Tool.Scale)
                {
                    splineCreator.transform.localScale = Handles.ScaleHandle(splineCreator.transform.localScale, splineCreator.transform.position, splineCreator.transform.rotation, HandleUtility.GetHandleSize(splineCreator.transform.position));
                }
            }
        }

        private void DrawInstruction()
        {
            if (Event.current.alt)
                return;
            string s;

            if (showTransformGizmos)
            {
                s = "Use the Gizmos to move, rotate or scale the spline.\nUncheck Transform>Show Transform Gizmos when done.";
            }
            else
            {
                s = string.Format(
                        "{0}" +
                        "{1}" +
                        "{2}",
                        "Click to select,",
                        "\nShift+Click to add,",
                        "\nCtrl+Click to remove anchor/segment.");
            }

            GUIContent mouseMessage = new GUIContent(s);
            GEditorCommon.SceneViewMouseMessage(mouseMessage);
        }

        private void CatchHotControl()
        {
            if (Event.current.alt)
                return;
            int controlId = GUIUtility.GetControlID(this.GetHashCode(), FocusType.Passive);
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0)
                {
                    GUIUtility.hotControl = controlId;
                    //OnMouseDown(Event.current);
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                if (GUIUtility.hotControl == controlId)
                {
                    //OnMouseUp(Event.current);
                    //Return the hot control back to Unity, use the default
                    GUIUtility.hotControl = 0;
                }
            }
            else if (Event.current.type == EventType.KeyDown)
            {
                //OnKeyDown(Event.current);
            }
        }

        public static float DistanceMouseToSpline(Vector2 mousePosition, params Vector3[] splinePoint)
        {
            float d = float.MaxValue;
            for (int i = 0; i < splinePoint.Length - 1; ++i)
            {
                float d1 = HandleUtility.DistancePointToLineSegment(
                    mousePosition,
                    HandleUtility.WorldToGUIPoint(splinePoint[i]),
                    HandleUtility.WorldToGUIPoint(splinePoint[i + 1]));
                if (d1 < d)
                    d = d1;
            }
            return d;
        }

        public static float DistanceMouseToPoint(Vector2 mousePosition, Vector3 worldPoint)
        {
            float d = Vector2.Distance(
                mousePosition,
                HandleUtility.WorldToGUIPoint(worldPoint));
            return d;
        }

        private void DrawMesh()
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
            if (GEditorSettings.Instance.splineTools.showMesh)
            {
                Handles.color = GEditorSettings.Instance.splineTools.meshColor;
                int quadCount = splineCreator.Editor_Vertices.Count / 6;
                for (int i = 0; i < quadCount; ++i)
                {
                    Vector4 p0 = splineCreator.Editor_Vertices[i * 6 + 0];
                    Vector4 p1 = splineCreator.Editor_Vertices[i * 6 + 1];
                    Vector4 p2 = splineCreator.Editor_Vertices[i * 6 + 2];
                    Vector4 p3 = splineCreator.Editor_Vertices[i * 6 + 5];
                    Handles.DrawPolyLine(p0, p1, p2, p3, p0);
                }
            }
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        }
    }
}
