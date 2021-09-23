using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Griffin.ExtensionSystem;
using System.Reflection;

namespace Pinwheel.Griffin.Wizard
{
    public static class GExtensionTabDrawer
    {
        private static Vector2 scrollPos;

        private static string searchString;
        private static string SearchString
        {
            get
            {
                if (searchString == null)
                {
                    searchString = string.Empty;
                }
                return searchString;
            }
            set
            {
                searchString = value;
            }
        }

        public static void ReloadExtension()
        {
            GExtensionManager.ReloadExtensions();
        }

        public static void Draw()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            List<GExtensionInfo> extensions = GExtensionManager.Extensions;
            for (int i = 0; i < extensions.Count; ++i)
            {
                if (extensions[i].Publisher.Equals("Pinwheel Studio"))
                {
                    DrawExtension(extensions[i]);
                }
            }

            for (int i = 0; i < extensions.Count; ++i)
            {
                if (!extensions[i].Publisher.Equals("Pinwheel Studio"))
                {
                    DrawExtension(extensions[i]);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawExtension(GExtensionInfo ex)
        {
            GUI.enabled = !EditorApplication.isCompiling;
            string filter = SearchString.ToLower();
            if (!ex.Name.ToLower().Contains(filter) &&
                !ex.Publisher.ToLower().Contains(filter) &&
                !ex.Description.ToLower().Contains(filter))
                return;

            string id = "griffin-extension" + ex.Name + ex.Publisher;
            string label = ex.Name;

            GEditorCommon.Foldout(label, false, id, () =>
            {
                try
                {
                    EditorGUILayout.LabelField("Publisher", ex.Publisher);
                    EditorGUILayout.LabelField("Version", ex.Version);
                    EditorGUILayout.LabelField("Description", ex.Description, GEditorCommon.WordWrapLeftLabel);
                    if (ex.OpenUserGuideMethod != null || ex.OpenSupportLinkMethod != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Link");
                        using (EditorGUI.IndentLevelScope level = new EditorGUI.IndentLevelScope(-1))
                        {
                            Rect r = EditorGUILayout.GetControlRect();
                            List<string> linkLabels = new List<string>();
                            linkLabels.Add("Support");
                            if (ex.OpenUserGuideMethod != null)
                            {
                                linkLabels.Add("User Guide");
                            }

                            List<Rect> linkRects = EditorGUIUtility.GetFlowLayoutedRects(r, EditorStyles.label, 7, 0, linkLabels);
                            for (int i = 0; i < linkRects.Count; ++i)
                            {
                                EditorGUIUtility.AddCursorRect(linkRects[i], MouseCursor.Link);
                            }

                            if (GUI.Button(linkRects[0], "Support", EditorStyles.label))
                            {
                                ex.OpenSupportLinkMethod.Invoke(null, null);
                            }
                            GEditorCommon.DrawBottomLine(linkRects[0], EditorStyles.label.normal.textColor);

                            if (ex.OpenUserGuideMethod != null)
                            {
                                if (GUI.Button(linkRects[1], "User Guide", EditorStyles.label))
                                {
                                    ex.OpenUserGuideMethod.Invoke(null, null);
                                }
                                GEditorCommon.DrawBottomLine(linkRects[1], EditorStyles.label.normal.textColor);
                            }


                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (ex.ButtonMethods.Count > 0)
                    {
                        GEditorCommon.Separator();
                        for (int i = 0; i < ex.ButtonMethods.Count; ++i)
                        {
                            MethodInfo method = ex.ButtonMethods[i];
                            if (method == null)
                                continue;

                            string buttonLabel = ObjectNames.NicifyVariableName(method.Name.Substring(GExtensionInfo.BUTTON_METHOD_PREFIX.Length));
                            if (GUILayout.Button(buttonLabel))
                            {
                                method.Invoke(null, null);
                            }
                        }
                    }

                    if (ex.GuiMethod != null)
                    {
                        GEditorCommon.Separator();
                        ex.GuiMethod.Invoke(null, null);
                    }
                }
                catch (System.Exception e)
                {
                    EditorGUILayout.LabelField(string.Format("<color=red>Error: {0}</color>", e.ToString()), GEditorCommon.RichTextLabel);
                    Debug.LogException(e);
                }
            });
            GUI.enabled = true;
        }
    }
}
