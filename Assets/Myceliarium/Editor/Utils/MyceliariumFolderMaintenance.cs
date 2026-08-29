using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Myceliarium
{
    [InitializeOnLoad]
    internal static class MyceliariumFolderMaintenance
    {
        #region Configurables
        private const string ResourcesFolderPath = 
            "Assets/Resources/AtMycelia/Myceliarium/Editor";//
        private const string AsmDefPath = ResourcesFolderPath + 
            "/AtMycelia.Myceliarium.UserExt.asmdef";

        private const string AsmDefName = "AtMycelia.Myceliarium.UserExt";
        private const string CommonCoreGuid = "315f594c192fac84b811333747e5ecb0";
        private const string CommonEditorGuid = "545d5e6f2e39eb840a1084bebacdc8e2";
        private const string MyceliariumEditorGuid = "2cb4e7e4862ca4247954da8e50e0090d";
        #endregion

        static MyceliariumFolderMaintenance()
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private static void OnAfterAssemblyReload()
        {
            EditorApplication.delayCall += () =>
            {
                EditorCoroutineUtility.StartCoroutine(
                    WaitAndEnsureUserExtensionHolder(), 
                    null);
            };
        }

        private static IEnumerator WaitAndEnsureUserExtensionHolder()
        {
            // Need to wait a few frames to ensure that the assembly reload has fully completed
            for (int i = 0; i < _framesToWait; i++)
            {
                yield return null;
            }

            EnsureResourcesFolderExists(out bool ensuredResourceFolder);
            EnsureAssemblyDefinitionExists(out bool ensuredAsmDef);
            bool madeChanges = ensuredResourceFolder || ensuredAsmDef;
            OnEnsuringDone(ref madeChanges);
        }

        private static readonly int _framesToWait = 5;

        private static void EnsureResourcesFolderExists(out bool madeChanges)
        {
            madeChanges = false;

            if (!Directory.Exists(ResourcesFolderPath))
            {
                Directory.CreateDirectory(ResourcesFolderPath);
                Debug.Log($"[Myceliarium] Created Resources folder at: {ResourcesFolderPath}");
                madeChanges = true;
            }
        }

        private static void EnsureAssemblyDefinitionExists(out bool madeChanges)
        {
            madeChanges = false;
            if (!File.Exists(AsmDefPath))
            {
                CreateAssemblyDefinition();
                Debug.Log($"[Myceliarium] Created assembly definition at: {AsmDefPath}");
                AssetDatabase.Refresh();
                madeChanges = true;
            }
        }

        private static void CreateAssemblyDefinition()
        {
            var asmDefContent = new AssemblyDefinitionData
            {
                name = AsmDefName,
                rootNamespace = "",
                references = new[]
                {
                    $"GUID:{CommonCoreGuid}",
                    $"GUID:{CommonEditorGuid}",
                    $"GUID:{MyceliariumEditorGuid}"
                },
                includePlatforms = new[] { "Editor" },
                excludePlatforms = new string[0],
                allowUnsafeCode = false,
                overrideReferences = false,
                precompiledReferences = new string[0],
                autoReferenced = true,
                defineConstraints = new string[0],
                versionDefines = new string[0],
                noEngineReferences = false
            };

            string json = JsonUtility.ToJson(asmDefContent, true);
            File.WriteAllText(AsmDefPath, json);
        }

        private static void OnEnsuringDone(ref bool madeChanges)
        {
            if (madeChanges)
            {
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("[Myceliarium] Resources folder structure is already up to date.");
            }
        }

        [System.Serializable]
        private class AssemblyDefinitionData
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
            public string[] versionDefines;
            public bool noEngineReferences;
        }
    }

    internal static class EditorCoroutineUtility
    {
        public static void StartCoroutine(IEnumerator routine, object owner)
        {
            EditorApplication.update += UpdateCoroutine;

            void UpdateCoroutine()
            {
                if (!routine.MoveNext())
                {
                    EditorApplication.update -= UpdateCoroutine;
                }
            }
        }
    }
}
