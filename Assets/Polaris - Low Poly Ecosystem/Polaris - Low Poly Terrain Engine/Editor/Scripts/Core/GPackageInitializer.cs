using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Pinwheel.Griffin
{
    public static class GPackageInitializer
    {
        public delegate void InitCompletedHandler();
        public static event InitCompletedHandler Completed;

        public static string GRIFFIN_KW = "GRIFFIN";
        public static string GRIFFIN_2020_KW = "GRIFFIN_2020";
        public static string GRIFFIN_SHADER_GRAPH_KW = "GRIFFIN_SHADER_GRAPH";
        public static string GRIFFIN_ASE_KW = "GRIFFIN_ASE";
        public static string GRIFFIN_CSHARP_WIZARD_KW = "GRIFFIN_CSHARP_WIZARD";
        public static string GRIFFIN_MESH_TO_FILE_KW = "GRIFFIN_MESH_TO_FILE";
        public static string GRIFFIN_LWRP_KW = "GRIFFIN_LWRP";
        public static string GRIFFIN_URP_KW = "GRIFFIN_URP";
        public static string GRIFFIN_VEGETATION_STUDIO_PRO_KW = "GRIFFIN_VEGETATION_STUDIO_PRO";
        public static string GRIFFIN_BURST_KW = "GRIFFIN_BURST";
        public static string GRIFFIN_EDITOR_COROUTINES_KW = "GRIFFIN_EDITOR_COROUTINES";

        private static ListRequest listPackageRequest = null;

#pragma warning disable 0414
        public static bool isShaderGraphInstalled = false;
        public static bool isASEInstalled = false;
        public static bool isPoseidonInstalled = false;
        public static bool isJupiterInstalled = false;
        public static bool isCSharpWizardInstalled = false;
        public static bool isMeshToFileInstalled = false;
        public static bool isLwrpSupportInstalled = false;
        public static bool isUrpSupportInstalled = false;
        public static bool isVegetationStudioProExtensionInstalled = false;
        public static bool isMicroSplatIntegrationInstalled = false;
        public static bool isBurstInstalled = false;
        public static bool isEditorCoroutinesInstalled = false;
        public static bool isXrManagementInstalled = false;
#pragma warning restore 0414

        [DidReloadScripts]
        public static void Init()
        {
            isShaderGraphInstalled = false;
            isASEInstalled = false;
            isPoseidonInstalled = false;
            isJupiterInstalled = false;
            isCSharpWizardInstalled = false;
            isMeshToFileInstalled = false;
            isLwrpSupportInstalled = false;
            isUrpSupportInstalled = false;
            isVegetationStudioProExtensionInstalled = false;
            isMicroSplatIntegrationInstalled = false;
            isBurstInstalled = false;
            isEditorCoroutinesInstalled = false;
            isXrManagementInstalled = false;
            CheckThirdPartyPackages();
            CheckUnityPackagesAndInit();
        }

        private static void CheckThirdPartyPackages()
        {
#if AMPLIFY_SHADER_EDITOR
            isASEInstalled = true;
#else
            isASEInstalled = false;
#endif

#if POSEIDON
            isPoseidonInstalled = true;
#else
            isPoseidonInstalled = false;
#endif

#if JUPITER
            isJupiterInstalled = true;
#else
            isJupiterInstalled = false;
#endif

#if __MICROSPLAT_POLARIS__
            isMicroSplatIntegrationInstalled = true;
#else
            isMicroSplatIntegrationInstalled = false;
#endif

            List<Type> loadedTypes = GetAllLoadedTypes();
            for (int i = 0; i < loadedTypes.Count; ++i)
            {
                Type t = loadedTypes[i];
                if (t.Namespace != null &&
                    t.Namespace.Equals("Pinwheel.CsharpWizard"))
                {
                    isCSharpWizardInstalled = true;
                }

                if (t.Namespace != null &&
                    t.Namespace.Equals("Pinwheel.MeshToFile"))
                {
                    isMeshToFileInstalled = true;
                }

                if (t.Namespace != null &&
                   t.Namespace.Equals("Pinwheel.Griffin.LWRP"))
                {
                    isLwrpSupportInstalled = true;
                }

                if (t.Namespace != null &&
                   t.Namespace.Equals("Pinwheel.Griffin.URP"))
                {
                    isUrpSupportInstalled = true;
                }

                if (t.Namespace != null &&
                   t.Namespace.Equals("Pinwheel.Griffin.VegetationStudioPro"))
                {
                    isVegetationStudioProExtensionInstalled = true;
                }
            }
        }

        private static void CheckUnityPackagesAndInit()
        {
            listPackageRequest = Client.List(true);
            EditorApplication.update += OnRequestingPackageList;
        }

        private static void OnRequestingPackageList()
        {
            if (listPackageRequest == null)
                return;
            if (!listPackageRequest.IsCompleted)
                return;
            if (listPackageRequest.Error != null)
            {
                isShaderGraphInstalled = false;
                isBurstInstalled = false;
            }
            else
            {
                foreach (UnityEditor.PackageManager.PackageInfo p in listPackageRequest.Result)
                {
                    if (p.name.Equals("com.unity.shadergraph"))
                    {
                        isShaderGraphInstalled = true;
                    }
                    if (p.name.Equals("com.unity.burst"))
                    {
                        isBurstInstalled = true;
                    }
                    if (p.name.Equals("com.unity.editorcoroutines"))
                    {
                        isEditorCoroutinesInstalled = true;
                    }
                    if (p.name.Equals("com.unity.xr.management"))
                    {
                        isXrManagementInstalled = true;
                    }
                }
            }
            EditorApplication.update -= OnRequestingPackageList;
            InitPackage();
        }

        private static void InitPackage()
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
            List<string> symbolList = new List<string>(symbols.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries));

            bool isDirty = false;
            if (!symbolList.Contains(GRIFFIN_KW))
            {
                symbolList.Add(GRIFFIN_KW);
                isDirty = true;
            }
            if (!symbolList.Contains(GRIFFIN_2020_KW))
            {
                symbolList.Add(GRIFFIN_2020_KW);
                isDirty = true;
            }

            isDirty = isDirty || SetKeywordActive(symbolList, GRIFFIN_ASE_KW, isASEInstalled);
            isDirty = isDirty || SetKeywordActive(symbolList, GRIFFIN_CSHARP_WIZARD_KW, isCSharpWizardInstalled);
            isDirty = isDirty || SetKeywordActive(symbolList, GRIFFIN_MESH_TO_FILE_KW, isMeshToFileInstalled);
            isDirty = isDirty || SetKeywordActive(symbolList, GRIFFIN_LWRP_KW, isLwrpSupportInstalled);
            isDirty = isDirty || SetKeywordActive(symbolList, GRIFFIN_URP_KW, isUrpSupportInstalled);
            isDirty = isDirty || SetKeywordActive(symbolList, GRIFFIN_VEGETATION_STUDIO_PRO_KW, isVegetationStudioProExtensionInstalled);
            isDirty = isDirty || SetKeywordActive(symbolList, GRIFFIN_BURST_KW, isBurstInstalled);
            isDirty = isDirty || SetKeywordActive(symbolList, GRIFFIN_EDITOR_COROUTINES_KW, isEditorCoroutinesInstalled);

            if (isDirty)
            {
                symbols = symbolList.ListElementsToString(";");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, symbols);
            }

            RecordAnalytics();

            if (Completed != null)
            {
                Completed.Invoke();
            }
        }

        private static bool SetKeywordActive(List<string> kwList, string keyword, bool active)
        {
            bool isDirty = false;
            if (active && !kwList.Contains(keyword))
            {
                kwList.Add(keyword);
                isDirty = true;
            }
            else if (!active && kwList.Contains(keyword))
            {
                kwList.RemoveAll(s => s.Equals(keyword));
                isDirty = true;
            }
            return isDirty;
        }

        public static List<System.Type> GetAllLoadedTypes()
        {
            List<System.Type> loadedTypes = new List<System.Type>();
            List<string> typeName = new List<string>();
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.IsVisible && !t.IsGenericType)
                    {
                        typeName.Add(t.Name);
                        loadedTypes.Add(t);
                    }
                }
            }
            return loadedTypes;
        }

        public static void RecordAnalytics()
        {
#if UNITY_EDITOR_WIN
            GAnalytics.Record(GAnalytics.OS_WINDOWS, true);
#elif UNITY_EDITOR_OSX
            GAnalytics.Record(GAnalytics.OS_MAC, true);
#elif UNITY_EDITOR_LINUX
            GAnalytics.Record(GAnalytics.OS_LINUX, true);
#endif

            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            if (buildGroup == BuildTargetGroup.Standalone)
            {
                GAnalytics.Record(GAnalytics.PLATFORM_PC, true);
            }
            else if (buildGroup == BuildTargetGroup.iOS || buildGroup == BuildTargetGroup.Android)
            {
                GAnalytics.Record(GAnalytics.PLATFORM_MOBILE, true);
            }
            else if (buildGroup == BuildTargetGroup.WebGL)
            {
                GAnalytics.Record(GAnalytics.PLATFORM_WEB, true);
            }
            else if (buildGroup == BuildTargetGroup.PS4 || buildGroup == BuildTargetGroup.XboxOne || buildGroup == BuildTargetGroup.Switch)
            {
                GAnalytics.Record(GAnalytics.PLATFORM_CONSOLE, true);
            }
            else
            {
                GAnalytics.Record(GAnalytics.PLATFORM_OTHER, true);
            }

#if UNITY_2019_1 || UNITY_2019_2
            if (PlayerSettings.virtualRealitySupported)
            {
                GAnalytics.Record(GAnalytics.XR_PROJECT, true);
            }
#elif UNITY_2019_3_OR_NEWER
            if (isXrManagementInstalled)
            {
                GAnalytics.Record(GAnalytics.XR_PROJECT, true);
            }
#endif

            if (PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                GAnalytics.Record(GAnalytics.COLOR_SPACE_GAMMA, true);
            }
            else if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                GAnalytics.Record(GAnalytics.COLOR_SPACE_LINEAR, true);
            }

            if (isASEInstalled)
            {
                GAnalytics.Record(GAnalytics.INTEGRATION_AMPLIFY_SHADER_EDITOR, true);
            }
            if (isPoseidonInstalled)
            {
                GAnalytics.Record(GAnalytics.INTEGRATION_POSEIDON, true);
            }
            if (isCSharpWizardInstalled)
            {
                GAnalytics.Record(GAnalytics.INTEGRATION_CSHARP_WIZARD, true);
            }
            if (isMeshToFileInstalled)
            {
                GAnalytics.Record(GAnalytics.INTEGRATION_MESH_TO_FILE, true);
            }
            if (isVegetationStudioProExtensionInstalled)
            {
                GAnalytics.Record(GAnalytics.INTEGRATION_VEGETATION_STUDIO, true);
            }
            if (isMicroSplatIntegrationInstalled)
            {
                GAnalytics.Record(GAnalytics.INTEGRATION_MICRO_SPLAT, true);
            }
        }
    }
}
