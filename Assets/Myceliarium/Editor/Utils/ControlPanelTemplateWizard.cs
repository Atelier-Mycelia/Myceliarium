using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    internal sealed class ControlPanelTemplateWizard : EditorWindow
    {
        #region Configurables
        #region Paths To Assets
        private const string BaseUxmlResourcePath =
            "Editor/Uxml/_BaseTab";

        private const string BaseStyleResourcePath =
            "Editor/Stylesheets/_baseTabStyle";

        private const string BaseSubwindowUxmlResourcePath =
            "Editor/Uxml/_BaseSubwindow";

        private const string BaseSubwindowStyleResourcePath =
            "Editor/Stylesheets/_baseSubwindowStyle";

        private const string WizardUxmlPath =
            "Editor/Uxml/ControlPanelTemplateWizard";

        private const string TabTemplateResourcePath =
            "Editor/Templates/ControlPanelTabTemplate.cs";

        private const string EntryTemplateResourcePath =
            "Editor/Templates/ControlPanelEntryTemplate.cs";

        private const string SubwindowTemplateResourcePath =
            "Editor/Templates/ControlPanelSubwindowTemplate.cs";

        private const string GeneratedAssetsRootFolderName =
            "AtMycelia/Myceliarium/Editor";
        #endregion

        private const string Title = "Control Panel Template Wizard";
        #endregion

        [MenuItem("Window/Atelier Mycelia/Myceliarium/New Control Panel Entry", false, 2000)]
        private static void OpenWizard()
        {
            var window = CreateInstance<ControlPanelTemplateWizard>();
            window.titleContent = new GUIContent(Title);
            window.minSize = window.maxSize = WindowSize;
            window.ShowUtility();
        }

        private static readonly Vector2 WindowSize = new Vector2(500f, 250f);

        private void CreateGUI()
        {
            AddUiElems(out bool success);
            if (!success)
            {
                Close();
                return;
            }
            RegisterCallbacks();

            _nameField.Focus();
            _nameField.SelectAll();
            // ^So the user can start typing immediately to replace the default text.
        }

        private void AddUiElems(out bool success)
        {
            success = false;
            var vta = Resources.Load<VisualTreeAsset>(WizardUxmlPath);
            if (vta == null)
            {
                string message = $"Failed to load wizard UXML at {WizardUxmlPath}. " +
                    $"Ensure the UXML file exists and is located in a Resources folder.";
                if (this != null)
                {
                    Debug.LogError(message);
                    return;
                }
            }

            vta.CloneTree(rootVisualElement);
            RegisterVisualElements(out success);
        }

        private void RegisterVisualElements(out bool success)
        {
            success = false;
            _nameField = rootVisualElement.Q<TextField>("NameField");
            _createButton = rootVisualElement.Q<Button>("CreateButton");

            if (_nameField == null || _createButton == null)
            {
                string logMessage = $"Failed to find required UI elements in " +
                    $"wizard UXML at {WizardUxmlPath}.";
                Debug.LogError(logMessage);
                return;
            }

            success = true;
        }

        private TextField _nameField;
        private Button _createButton;

        private void RegisterCallbacks()
        {
            _nameField.RegisterValueChangedCallback(OnNameChanged);
            _createButton.clicked += CreateTemplate;
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            bool isNameValid = !string.IsNullOrWhiteSpace(evt.newValue);
            _createButton.SetEnabled(isNameValid);
        }

        private void CreateTemplate()
        {
            string templateName = _nameField.value;
            string coreName = BuildCoreName(templateName);
            if (string.IsNullOrWhiteSpace(coreName))
            {
                EditorUtility.DisplayDialog("Myceliarium",
                    "Please enter a valid name.",
                    "OK");
                return;
            }

            // Resolve base assets dynamically via Resources
            string baseUxmlAssetPath = FindAssetPathFromResources(BaseUxmlResourcePath);
            string baseStyleAssetPath = FindAssetPathFromResources(BaseStyleResourcePath);
            string baseSubwindowUxmlAssetPath = FindAssetPathFromResources(BaseSubwindowUxmlResourcePath);
            string baseSubwindowStyleAssetPath = FindAssetPathFromResources(BaseSubwindowStyleResourcePath);

            if (string.IsNullOrEmpty(baseUxmlAssetPath) || string.IsNullOrEmpty(baseStyleAssetPath))
            {
                EditorUtility.DisplayDialog("Myceliarium",
                    "Could not locate base template assets. Ensure _BaseTab.uxml and " +
                    "_baseTabStyle.uss are in a Resources folder.",
                    "OK");
                return;
            }

            if (string.IsNullOrEmpty(baseSubwindowUxmlAssetPath) || string.IsNullOrEmpty(baseSubwindowStyleAssetPath))
            {
                EditorUtility.DisplayDialog("Myceliarium",
                    "Could not locate base subwindow template assets. Ensure _BaseSubwindow.uxml and " +
                    "_baseSubwindowStyle.uss are in a Resources folder.",
                    "OK");
                return;
            }

            string displayName = ObjectNames.NicifyVariableName(coreName);
            string tabClassName = coreName + "Tab";
            string entryClassName = coreName + "Entry";
            string subwindowClassName = coreName + "Subwindow";
            string styleFileName = coreName + "Style.uss";
            string tabUxmlFileName = tabClassName + ".uxml";
            string subwindowStyleFileName = subwindowClassName + "Style.uss";
            string subwindowUxmlFileName = subwindowClassName + ".uxml";

            string generatedAssetsRoot = $"Assets/Resources/{GeneratedAssetsRootFolderName}";
            string entryFolderAssetPath = $"{generatedAssetsRoot}/{coreName}";

            string tabScriptAssetPath = $"{entryFolderAssetPath}/{tabClassName}.cs";
            string entryScriptAssetPath = $"{entryFolderAssetPath}/{entryClassName}.cs";
            string subwindowScriptAssetPath = $"{entryFolderAssetPath}/{subwindowClassName}.cs";
            string uxmlAssetPath = $"{entryFolderAssetPath}/{tabUxmlFileName}";
            string styleAssetPath = $"{entryFolderAssetPath}/{styleFileName}";
            string subwindowUxmlAssetPath = $"{entryFolderAssetPath}/{subwindowUxmlFileName}";
            string subwindowStyleAssetPath = $"{entryFolderAssetPath}/{subwindowStyleFileName}";
            string tabUxmlResourcePath = BuildResourcesLoadPath(uxmlAssetPath);
            string subwindowUxmlResourcePath = BuildResourcesLoadPath(subwindowUxmlAssetPath);

            if (!VerifyNoConflicts(tabScriptAssetPath, entryScriptAssetPath,
                subwindowScriptAssetPath, uxmlAssetPath, styleAssetPath,
                subwindowUxmlAssetPath, subwindowStyleAssetPath))
            {
                return;
            }

            EnsureParentDirectoryExists(styleAssetPath);
            EnsureParentDirectoryExists(uxmlAssetPath);
            EnsureParentDirectoryExists(tabScriptAssetPath);
            EnsureParentDirectoryExists(entryScriptAssetPath);
            EnsureParentDirectoryExists(subwindowScriptAssetPath);
            EnsureParentDirectoryExists(subwindowStyleAssetPath);
            EnsureParentDirectoryExists(subwindowUxmlAssetPath);

            // Copy and patch tab style
            CopyTextFile(baseStyleAssetPath, styleAssetPath);
            AssetDatabase.ImportAsset(styleAssetPath, ImportAssetOptions.ForceSynchronousImport);
            string styleGuid = AssetDatabase.AssetPathToGUID(styleAssetPath);

            // Copy and patch tab UXML
            CopyTextFile(baseUxmlAssetPath, uxmlAssetPath);
            PatchUxml(uxmlAssetPath, displayName, styleAssetPath, styleGuid, baseStyleAssetPath);

            // Copy and patch subwindow style
            CopyTextFile(baseSubwindowStyleAssetPath, subwindowStyleAssetPath);
            AssetDatabase.ImportAsset(subwindowStyleAssetPath, ImportAssetOptions.ForceSynchronousImport);
            string subwindowStyleGuid = AssetDatabase.AssetPathToGUID(subwindowStyleAssetPath);

            // Copy and patch subwindow UXML
            CopyTextFile(baseSubwindowUxmlAssetPath, subwindowUxmlAssetPath);
            PatchUxml(subwindowUxmlAssetPath, displayName, subwindowStyleAssetPath, 
                subwindowStyleGuid, baseSubwindowStyleAssetPath);

            WriteTextFile(tabScriptAssetPath,
                BuildTabScript(tabClassName, displayName, tabUxmlResourcePath));
            WriteTextFile(entryScriptAssetPath,
                BuildEntryScript(entryClassName, tabClassName, subwindowClassName, displayName));
            WriteTextFile(subwindowScriptAssetPath,
                BuildSubwindowScript(subwindowClassName, entryClassName, subwindowUxmlResourcePath));

            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();

            var createdAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entryFolderAssetPath);
            if (createdAsset == null)
            {
                createdAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tabScriptAssetPath);
            }

            if (createdAsset != null)
            {
                EditorGUIUtility.PingObject(createdAsset);
            }

            Close();
        }

        private static string FindAssetPathFromResources(string resourcePath)
        {
            // Resources.Load doesn't give us the asset path, so we use AssetDatabase.FindAssets
            string fileName = Path.GetFileNameWithoutExtension(resourcePath);
            string extension = resourcePath.Contains("/Uxml/") ? ".uxml" : ".uss";
            string[] guids = AssetDatabase.FindAssets($"{fileName} t:{(extension == ".uxml" ? "VisualTreeAsset" : "StyleSheet")}");

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                // Ensure it's in a Resources folder and matches the expected resource path structure
                if (assetPath.Contains("/Resources/") && assetPath.EndsWith(fileName + extension))
                {
                    return assetPath;
                }
            }

            return null;
        }

        private static bool VerifyNoConflicts(params string[] assetPaths)
        {
            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i];
                if (File.Exists(ToFullPath(assetPath)))
                {
                    EditorUtility.DisplayDialog(
                        "Myceliarium",
                        $"A file already exists at '{assetPath}'. Choose a different name.",
                        "OK");
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

            // Get the base style reference dynamically
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

            if (!assetPath.StartsWith(ResourcesFolderPrefix, System.StringComparison.Ordinal))
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
            if (value.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase) &&
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
                Debug.LogError($"Failed to load subwindow template from {SubwindowTemplateResourcePath}");
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
    }
}
