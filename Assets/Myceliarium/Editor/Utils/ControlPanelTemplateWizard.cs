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
        private const string BaseUxmlSource = 
            "Assets/Myceliarium/Resources/Editor/Uxml/_BaseTab.uxml";

        private const string BaseStyleSource = 
            "Assets/Myceliarium/Resources/Editor/Stylesheets/_baseTabStyle.uss";

        private const string WizardUxmlPath = 
            "Editor/Uxml/ControlPanelTemplateWizard";

        private const string TabTemplateResourcePath = 
            "Editor/Templates/ControlPanelTabTemplate.cs";

        private const string EntryTemplateResourcePath = 
            "Editor/Templates/ControlPanelEntryTemplate.cs";

        private const string SubwindowTemplateResourcePath = 
            "Editor/Templates/ControlPanelSubwindowTemplate.cs";

        private const string TabScriptsFolder = 
            "Assets/Myceliarium/Editor/Scripts";

        private const string UxmlFolder = 
            "Assets/Myceliarium/Resources/Editor/Uxml";

        private const string StylesFolder = 
            "Assets/Myceliarium/Resources/Editor/Stylesheets";
        #endregion

        private const string Title = "Control Panel Template Wizard";
        #endregion

        [MenuItem("Assets/Create/Myceliarium/Control Panel Template", false, 2000)]
        private static void OpenWizard()
        {
            var window = CreateInstance<ControlPanelTemplateWizard>();
            window.titleContent = new GUIContent(Title);
            window.minSize = window.maxSize = WindowSize;
            window.ShowUtility();
        }

        private static readonly Vector2 WindowSize = new Vector2(420f, 150f);

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

            string displayName = ObjectNames.NicifyVariableName(coreName);
            string tabClassName = coreName + "Tab";
            string entryClassName = coreName + "Entry";
            string subwindowClassName = coreName + "Subwindow";
            string styleFileName = coreName + "Style.uss";
            string tabUxmlFileName = tabClassName + ".uxml";

            string tabScriptAssetPath = $"{TabScriptsFolder}/{tabClassName}.cs";
            string entryScriptAssetPath = $"{TabScriptsFolder}/{entryClassName}.cs";
            string subwindowScriptAssetPath = $"{TabScriptsFolder}/{subwindowClassName}.cs";
            string uxmlAssetPath = $"{UxmlFolder}/{tabUxmlFileName}";
            string styleAssetPath = $"{StylesFolder}/{styleFileName}";

            if (!VerifyNoConflicts(tabScriptAssetPath, entryScriptAssetPath, 
                subwindowScriptAssetPath, uxmlAssetPath, styleAssetPath))
            {
                return;
            }

            EnsureParentDirectoryExists(styleAssetPath);
            EnsureParentDirectoryExists(uxmlAssetPath);
            EnsureParentDirectoryExists(tabScriptAssetPath);
            EnsureParentDirectoryExists(entryScriptAssetPath);
            EnsureParentDirectoryExists(subwindowScriptAssetPath);

            CopyTextFile(BaseStyleSource, styleAssetPath);
            AssetDatabase.ImportAsset(styleAssetPath, ImportAssetOptions.ForceSynchronousImport);
            string styleGuid = AssetDatabase.AssetPathToGUID(styleAssetPath);

            CopyTextFile(BaseUxmlSource, uxmlAssetPath);
            PatchUxml(uxmlAssetPath, displayName, styleAssetPath, styleGuid);

            WriteTextFile(tabScriptAssetPath, BuildTabScript(tabClassName, displayName));
            WriteTextFile(entryScriptAssetPath, BuildEntryScript(entryClassName, tabClassName, subwindowClassName, displayName));
            WriteTextFile(subwindowScriptAssetPath, BuildSubwindowScript(subwindowClassName, entryClassName));

            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            var createdAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tabScriptAssetPath);
            if (createdAsset != null)
            {
                EditorGUIUtility.PingObject(createdAsset);
            }

            Close();
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
            string styleAssetPath, string styleGuid)
        {
            string content = File.ReadAllText(ToFullPath(assetPath), Encoding.UTF8);
            string styleFileName = Path.GetFileNameWithoutExtension(styleAssetPath);
            string styleReference = $"project://database/{styleAssetPath}?fileID=743344113259" +
                $"7879392&amp;guid={styleGuid}&amp;type=3#{styleFileName}";

            content = content.Replace(
                "project://database/Assets/Myceliarium/Resources/Editor/Stylesheets/" +
                "baseTabStyle.uss?fileID=7433441132597879392&amp;guid=e529783e7fb76b44ab" +
                "24de2f7649c171&amp;type=3#baseTabStyle",
                styleReference);
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

            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
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

        private static string BuildTabScript(string tabClassName, string displayName)
        {
            string template = LoadTemplate(TabTemplateResourcePath);
            if (string.IsNullOrEmpty(template))
            {
                Debug.LogError($"Failed to load tab template from {TabTemplateResourcePath}");
                return string.Empty;
            }

            return template
                .Replace("#SCRIPTNAME#", tabClassName)
                .Replace("#DISPLAYNAME#", displayName);
        }

        private static string BuildEntryScript(string entryClassName, string tabClassName, string subwindowClassName, string displayName)
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

        private static string BuildSubwindowScript(string subwindowClassName, string entryClassName)
        {
            string template = LoadTemplate(SubwindowTemplateResourcePath);
            if (string.IsNullOrEmpty(template))
            {
                Debug.LogError($"Failed to load subwindow template from {SubwindowTemplateResourcePath}");
                return string.Empty;
            }

            return template
                .Replace("#SCRIPTNAME#", subwindowClassName)
                .Replace("#ENTRYCLASSNAME#", entryClassName);
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
