using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Pinwheel.Griffin.Wizard
{
    public static class GWizardEditorCommon
    {
        public static void DrawMaterialSettingsGUI()
        {
            GUIContent rpLabel = new GUIContent(
                    "Render Pipeline",
                    "The render pipeline currently in used.");
            EditorGUILayout.LabelField(rpLabel, new GUIContent(GCommon.CurrentRenderPipeline.ToString()));

            GUI.enabled = GCommon.CurrentRenderPipeline == GRenderPipelineType.Builtin;
            string lightModelTooltip = null;
            if (GCommon.CurrentRenderPipeline == GRenderPipelineType.Builtin)
            {
                lightModelTooltip =
                    "Lighting model to use.\n" +
                    "- PBR: Best visual quality with metallic & smoothness setup.\n" +
                    "- Lambert: Simple shading with no specularity.\n" +
                    "- Blinn-Phong: Simple shading with specularity.";
            }
            else
            {
                lightModelTooltip = string.Format(
                    "Lighting model to use.\n" +
                    "{0} Render Pipeline only use PBR model which yield high visual quality yet still performant.",
                    GCommon.CurrentRenderPipeline.ToString());
            }
            GUIContent lightModelLabel = new GUIContent(
                "Lighting Model",
                lightModelTooltip);
            GEditorSettings.Instance.wizardTools.lightingModel = (GLightingModel)EditorGUILayout.EnumPopup(lightModelLabel, GEditorSettings.Instance.wizardTools.lightingModel);
            if (GCommon.CurrentRenderPipeline == GRenderPipelineType.Universal)
            {
                GEditorSettings.Instance.wizardTools.lightingModel = GLightingModel.PBR;
            }
            GUI.enabled = true;

            GUIContent texturingLabel = new GUIContent(
                "Texturing Model",
                "Terrain texturing/coloring method to use.\n" +
                "- Gradient Lookup: use Gradients and Curves to shade the vertex based on it height and normal vector.\n" +
                "- Color Map: Use a single Albedo map for the whole terrain. Fast but only suitable for small terrain.\n" +
                "- Splats: Blend between multiple textures stacked on top of each others. Similar to Unity terrain.\n" +
                "- Vertex Color: Use the color of each vertex to shade the terrain.");
            GEditorSettings.Instance.wizardTools.texturingModel = (GTexturingModel)EditorGUILayout.EnumPopup(texturingLabel, GEditorSettings.Instance.wizardTools.texturingModel);
            if (GEditorSettings.Instance.wizardTools.texturingModel == GTexturingModel.Splat)
            {
                GUIContent splatModelLabel = new GUIContent(
                    "Splats Model",
                    "Number of texture layers and whether to use normal maps or not.");
                GEditorSettings.Instance.wizardTools.splatsModel = (GSplatsModel)EditorGUILayout.EnumPopup(splatModelLabel, GEditorSettings.Instance.wizardTools.splatsModel);
            }
        }
    }
}
