using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Myceliarium
{
    internal static class ControlPanelTemplateGenerator
    {
        #region Resource Paths
        private const string BaseUxmlResourcePath = 
            "Editor/Uxml/_BaseTab";
        private const string BaseStyleResourcePath = 
            "Editor/Stylesheets/_baseTabStyle";
        private const string BaseSubwindowUxmlResourcePath = 
            "Editor/Uxml/_BaseSubwindow";
        private const string BaseSubwindowStyleResourcePath = 
            "Editor/Stylesheets/_baseSubwindowStyle";
        private const string TabTemplateResourcePath = 
            "Editor/Templates/ControlPanelTabTemplate.cs";
        private const string EntryTemplateResourcePath = 
            "Editor/Templates/ControlPanelEntryTemplate.cs";
        private const string SubwindowTemplateResourcePath = 
            "Editor/Templates/ControlPanelSubwindowTemplate.cs";
        #endregion

        /// <summary>
        /// Generates a complete set of Control Panel entry files in the specified folder.
        /// </summary>
        /// <param name="templateName">The name for the template (will be sanitized).</param>
        /// <param name="targetFolderAssetPath">The asset path where files will be generated (e.g., "Assets/Resources/MyFolder").</param>
        /// <param name="errorMessage">Output error message if generation fails.</param>
        /// <returns>True if generation succeeded, false otherwise.</returns>
        public static bool Generate(string templateName, string targetFolderAssetPath, 
            out string errorMessage)
        {
            errorMessage = null;

            // Validate input
            string coreName = BuildCoreName(templateName);
            if (string.IsNullOrWhiteSpace(coreName))
            {
                errorMessage = "Please enter a valid name.";
                return false;
            }

            // Resolve base assets
            string baseUxmlAssetPath = FindAssetPathFromResources(BaseUxmlResourcePath);
            string baseStyleAssetPath = FindAssetPathFromResources(BaseStyleResourcePath);
            string baseSubwindowUxmlAssetPath = FindAssetPathFromResources(BaseSubwindowUxmlResourcePath);
            string baseSubwindowStyleAssetPath = FindAssetPathFromResources(BaseSubwindowStyleResourcePath);

            if (string.IsNullOrEmpty(baseUxmlAssetPath) || string.IsNullOrEmpty(baseStyleAssetPath))
            {
                errorMessage = "Could not locate base template assets. Ensure _BaseTab.uxml and " +
                    "_baseTabStyle.uss are in a Resources folder.";
                return false;
            }

            if (string.IsNullOrEmpty(baseSubwindowUxmlAssetPath) || string.IsNullOrEmpty(baseSubwindowStyleAssetPath))
            {
                errorMessage = "Could not locate base subwindow template assets. Ensure " +
                    "_BaseSubwindow.uxml and _baseSubwindowStyle.uss are in a Resources folder.";
                return false;
            }

            // Build file names and paths
            string displayName = ObjectNames.NicifyVariableName(coreName);
            string tabClassName = coreName + "Tab";
            string entryClassName = coreName + "Entry";
            string subwindowClassName = coreName + "Subwindow";
            string styleFileName = coreName + "Style.uss";
            string tabUxmlFileName = tabClassName + ".uxml";
            string subwindowStyleFileName = subwindowClassName + "Style.uss";
            string subwindowUxmlFileName = subwindowClassName + ".uxml";

            string entryFolderAssetPath = $"{targetFolderAssetPath}/{coreName}";

            string tabScriptAssetPath = $"{entryFolderAssetPath}/{tabClassName}.cs";
            string entryScriptAssetPath = $"{entryFolderAssetPath}/{entryClassName}.cs";
            string subwindowScriptAssetPath = $"{entryFolderAssetPath}/{subwindowClassName}.cs";
            string uxmlAssetPath = $"{entryFolderAssetPath}/{tabUxmlFileName}";
            string styleAssetPath = $"{entryFolderAssetPath}/{styleFileName}";
            string subwindowUxmlAssetPath = $"{entryFolderAssetPath}/{subwindowUxmlFileName}";
            string subwindowStyleAssetPath = $"{entryFolderAssetPath}/{subwindowStyleFileName}";

            // Verify no conflicts
            if (!VerifyNoConflicts(out errorMessage, tabScriptAssetPath, entryScriptAssetPath,
                subwindowScriptAssetPath, uxmlAssetPath, styleAssetPath,
                subwindowUxmlAssetPath, subwindowStyleAssetPath))
            {
                return false;
            }

            // Ensure directories exist
            EnsureParentDirectoryExists(styleAssetPath);
            EnsureParentDirectoryExists(uxmlAssetPath);
            EnsureParentDirectoryExists(tabScriptAssetPath);
            EnsureParentDirectoryExists(entryScriptAssetPath);
            EnsureParentDirectoryExists(subwindowScriptAssetPath);
            EnsureParentDirectoryExists(subwindowStyleAssetPath);
            EnsureParentDirectoryExists(subwindowUxmlAssetPath);

            // Generate tab style and UXML
            CopyTextFile(baseStyleAssetPath, styleAssetPath);
            AssetDatabase.ImportAsset(styleAssetPath, ImportAssetOptions.ForceSynchronousImport);
            string styleGuid = AssetDatabase.AssetPathToGUID(styleAssetPath);

            CopyTextFile(baseUxmlAssetPath, uxmlAssetPath);
            PatchUxml(uxmlAssetPath, displayName, styleAssetPath, styleGuid, baseStyleAssetPath);

            // Generate subwindow style and UXML
            CopyTextFile(baseSubwindowStyleAssetPath, subwindowStyleAssetPath);
            AssetDatabase.ImportAsset(subwindowStyleAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            string subwindowStyleGuid = AssetDatabase.AssetPathToGUID(subwindowStyleAssetPath);

            CopyTextFile(baseSubwindowUxmlAssetPath, subwindowUxmlAssetPath);
            PatchUxml(subwindowUxmlAssetPath, displayName, subwindowStyleAssetPath,
                subwindowStyleGuid, baseSubwindowStyleAssetPath);

            // Build resource paths for scripts
            string tabUxmlResourcePath = BuildResourcesLoadPath(uxmlAssetPath);
            string subwindowUxmlResourcePath = BuildResourcesLoadPath(subwindowUxmlAssetPath);

            // Generate C# scripts
            WriteTextFile(tabScriptAssetPath,
                BuildTabScript(tabClassName, displayName, tabUxmlResourcePath));
            WriteTextFile(entryScriptAssetPath,
                BuildEntryScript(entryClassName, tabClassName, subwindowClassName, displayName));
            WriteTextFile(subwindowScriptAssetPath,
                BuildSubwindowScript(subwindowClassName, entryClassName, subwindowUxmlResourcePath));

            AssetDatabase.Refresh();

            return true;
        }

        #region Helper Methods
        private static string FindAssetPathFromResources(string resourcePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(resourcePath);
            string extension = resourcePath.Contains("/Uxml/") ? ".uxml" : ".uss";
            string filterExtension = extension == ".uxml" ?
                "VisualTreeAsset" :
                "StyleSheet";
            string filter = $"{fileName} t:{filterExtension}";
            string[] guids = AssetDatabase.FindAssets(filter);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Contains("/Resources/") && assetPath.EndsWith(fileName + extension))
                {
                    return assetPath;
                }
            }

            return null;
        }

        private static bool VerifyNoConflicts(out string errorMessage, params string[] assetPaths)
        {
            errorMessage = null;
            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i];
                if (File.Exists(ToFullPath(assetPath)))
                {
                    errorMessage = $"A file already exists at '{assetPath}'. " +
                        $"Choose a different name.";
                    return false;
                }
            }

            return true;
        }

        private static void PatchUxml(string assetPath, string displayName,
            string styleAssetPath, string styleGuid, string baseStyleAssetPath)
        {
            string content = File.ReadAllText(ToFullPath(assetPath), Encoding.UTF8);
            string styleFileName = Path.GetFileNameWithoutExtension(styleAssetPath);
            string styleReference = $"project://database/{styleAssetPath}?fileID=743344113259" +
                $"7879392&amp;guid={styleGuid}&amp;type=3#{styleFileName}";

            string baseStyleGuid = AssetDatabase.AssetPathToGUID(baseStyleAssetPath);
            string baseStyleFileName = Path.GetFileNameWithoutExtension(baseStyleAssetPath);
            string baseStyleReference = $"project://database/{baseStyleAssetPath}?fileID=7433441132597879392&amp;guid={baseStyleGuid}&amp;type=3#{baseStyleFileName}";

            content = content.Replace(baseStyleReference, styleReference);
            content = content.Replace("text=\"New Text\"", $"text=\"{displayName}\"");

            File.WriteAllText(ToFullPath(assetPath), content, new UTF8Encoding(false));
        }

        private static void CopyTextFile(string sourceAssetPath, string destinationAssetPath)
        {
            string sourceFullPath = ToFullPath(sourceAssetPath);
            string destinationFullPath = ToFullPath(destinationAssetPath);
            File.Copy(sourceFullPath, destinationFullPath, overwrite: false);
        }

        private static void WriteTextFile(string assetPath, string content)
        {
            File.WriteAllText(ToFullPath(assetPath), content, new UTF8Encoding(false));
        }

        private static void EnsureParentDirectoryExists(string assetPath)
        {
            string directory = Path.GetDirectoryName(ToFullPath(assetPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("Could not resolve the project root.");
            }

            return Path.GetFullPath(Path.Combine(projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string BuildResourcesLoadPath(string assetPath)
        {
            const string ResourcesFolderPrefix = "Assets/Resources/";

            if (!assetPath.StartsWith(ResourcesFolderPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Asset path '{assetPath}' is not under Assets/Resources.");
            }

            string relativePath = assetPath.Substring(ResourcesFolderPrefix.Length);
            string extension = Path.GetExtension(relativePath);
            if (!string.IsNullOrEmpty(extension))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - extension.Length);
            }

            return relativePath.Replace('\\', '/');
        }

        private static string BuildCoreName(string input)
        {
            string sanitized = ToPascalCase(input);
            sanitized = StripSuffix(sanitized, "Tab");
            sanitized = StripSuffix(sanitized, "Entry");
            sanitized = StripSuffix(sanitized, "Style");

            if (!string.IsNullOrWhiteSpace(sanitized) &&
                !char.IsLetter(sanitized[0]) && sanitized[0] != '_')
            {
                sanitized = "Cp" + sanitized;
            }

            string result = string.IsNullOrWhiteSpace(sanitized) ?
                "NewControlPanel" :
                sanitized;
            return result;
        }

        private static string ToPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            _builder.Clear();
            _builder.Capacity = value.Length;
            bool capitalizeNext = true;

            foreach (char elem in value)
            {
                if (char.IsLetterOrDigit(elem))
                {
                    char whatToAppend = capitalizeNext ?
                        char.ToUpperInvariant(elem) :
                        elem;
                    _builder.Append(whatToAppend);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            return _builder.ToString();
        }

        private static readonly StringBuilder _builder = new StringBuilder();

        private static string StripSuffix(string value, string suffix)
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                value.Length > suffix.Length)
            {
                return value.Substring(0, value.Length - suffix.Length);
            }

            return value;
        }

        private static string BuildTabScript(string tabClassName, string displayName,
            string pathToUxml)
        {
            string template = LoadTemplate(TabTemplateResourcePath);
            if (string.IsNullOrEmpty(template))
            {
                Debug.LogError($"Failed to load tab template from {TabTemplateResourcePath}");
                return string.Empty;
            }

            return template
                .Replace("#SCRIPTNAME#", tabClassName)
                .Replace("#DISPLAYNAME#", displayName)
                .Replace($"Editor/Uxml/{tabClassName}", pathToUxml);
        }

        private static string BuildEntryScript(string entryClassName, string tabClassName,
            string subwindowClassName, string displayName)
        {
            string template = LoadTemplate(EntryTemplateResourcePath);
            if (string.IsNullOrEmpty(template))
            {
                Debug.LogError($"Failed to load entry template from {EntryTemplateResourcePath}");
                return string.Empty;
            }

            return template
                .Replace("#SCRIPTNAME#", entryClassName)
                .Replace("#DISPLAYNAME#", displayName)
                .Replace("#TABCLASSNAME#", tabClassName)
                .Replace("#SUBWINDOWCLASSNAME#", subwindowClassName);
        }

        private static string BuildSubwindowScript(string subwindowClassName,
            string entryClassName, string pathToUxml)
        {
            string template = LoadTemplate(SubwindowTemplateResourcePath);
            if (string.IsNullOrEmpty(template))
            {
                Debug.LogError($"Failed to load subwindow template from " +
                    $"{SubwindowTemplateResourcePath}");
                return string.Empty;
            }

            return template
                .Replace("#SCRIPTNAME#", subwindowClassName)
                .Replace("#ENTRYCLASSNAME#", entryClassName)
                .Replace("#UXMLPATH#", pathToUxml);
        }

        private static string LoadTemplate(string resourcePath)
        {
            var templateAsset = Resources.Load<TextAsset>(resourcePath);
            if (templateAsset == null)
            {
                return null;
            }

            return templateAsset.text;
        }
        #endregion
    }
}