using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;


namespace Pinwheel.Griffin.Wizard
{
    public static class GSetShaderTabDrawer
    {
        internal static bool bulkSetShader = true;

        internal static void Draw()
        {
            GEditorSettings.WizardToolsSettings settings = GEditorSettings.Instance.wizardTools;
            if (bulkSetShader)
            {
                settings.setShaderGroupId = GEditorCommon.ActiveTerrainGroupPopupWithAllOption("Group Id", settings.setShaderGroupId);
            }
            else
            {
                settings.setShaderTerrain = EditorGUILayout.ObjectField("Terrain", settings.setShaderTerrain, typeof(GStylizedTerrain), true) as GStylizedTerrain;
            }
            GWizardEditorCommon.DrawMaterialSettingsGUI();

            if (GUILayout.Button("Set"))
            {
                if (bulkSetShader)
                {
                    GWizard.SetShader(settings.setShaderGroupId);
                }
                else
                {
                    GWizard.SetShader(settings.setShaderTerrain);
                }
            }
        }
    }
}
