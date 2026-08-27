using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Myceliarium
{
    internal sealed class ControlPanelTemplateWizard : EditorWindow
    {
        #region Configurables
        private const string WizardUxmlPath = "Editor/Uxml/ControlPanelTemplateWizard";
        private const string DefaultTargetFolder = "Assets/Resources/AtMycelia/Myceliarium/Editor";
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

        private static readonly Vector2 WindowSize = new Vector2(580f, 320f);

        private void CreateGUI()
        {
            AddUiElems(out bool success);
            if (!success)
            {
                Close();
                return;
            }
            RegisterCallbacks();

            // Set default folder
            _selectedFolderPath = DefaultTargetFolder;
            _folderField.value = _selectedFolderPath;

            _nameField.Focus();
            _nameField.SelectAll();
            // ^So the user can start typing immediately to replace the default text.
        }

        private void AddUiElems(out bool success)
        {
            GetVta(out VisualTreeAsset vta, out success);
            if (!success)
            {
                return;
            }

            vta.CloneTree(rootVisualElement);
            RegisterVisualElements(out success);
        }

        private void GetVta(out VisualTreeAsset vta, out bool success)
        {
            success = false;
            vta = Resources.Load<VisualTreeAsset>(WizardUxmlPath);
            if (vta == null)
            {
                string message = $"Failed to load wizard UXML at {WizardUxmlPath}. " +
                    $"Ensure the UXML file exists and is located in a Resources folder.";
                Debug.LogError(message);
                return;
            }
            success = true;
        }

        private void RegisterVisualElements(out bool success)
        {
            success = false;
            _nameField = rootVisualElement.Q<TextField>("NameField");
            _folderField = rootVisualElement.Q<TextField>("FolderField");
            _browseButton = rootVisualElement.Q<Button>("BrowseButton");
            _createButton = rootVisualElement.Q<Button>("CreateButton");

            if (_nameField == null || _folderField == null || _browseButton == null || _createButton == null)
            {
                string logMessage = $"Failed to find required UI elements in " +
                    $"wizard UXML at {WizardUxmlPath}.";
                Debug.LogError(logMessage);
                return;
            }

            success = true;
        }

        private TextField _nameField;
        private TextField _folderField;
        private Button _browseButton;
        private Button _createButton;
        private string _selectedFolderPath;

        private void RegisterCallbacks()
        {
            _nameField.RegisterValueChangedCallback(OnNameChanged);
            _browseButton.clicked += OnBrowseClicked;
            _createButton.clicked += CreateTemplate;
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            bool isNameValid = !string.IsNullOrWhiteSpace(evt.newValue);
            _createButton.SetEnabled(isNameValid);
        }

        private void OnBrowseClicked()
        {
            string currentFolder = string.IsNullOrEmpty(_selectedFolderPath) 
                ? DefaultTargetFolder 
                : _selectedFolderPath;

            // Convert to absolute path for the folder panel
            string absolutePath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath),
                currentFolder);

            string selectedFolder = EditorUtility.OpenFolderPanel(
                "Select Target Folder for Control Panel Entry",
                absolutePath,
                string.Empty);

            if (string.IsNullOrEmpty(selectedFolder))
            {
                return; // User cancelled
            }

            string assetPath = ConvertToAssetPath(selectedFolder);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog(
                    "Myceliarium",
                    "The selected folder must be within the project's Assets folder.",
                    "OK");
                return;
            }

            _selectedFolderPath = assetPath;
            _folderField.value = _selectedFolderPath;
        }

        private void CreateTemplate()
        {
            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                EditorUtility.DisplayDialog(
                    "Myceliarium",
                    "Please select a target folder first.",
                    "OK");
                return;
            }

            string templateName = _nameField.value;
            _createButton.SetEnabled(false); // To prevent multiple clicks
            if (!ControlPanelTemplateGenerator.Generate(templateName,
                _selectedFolderPath, out string errorMessage))
            {
                EditorUtility.DisplayDialog("Myceliarium", errorMessage, "OK");
                return;
            }

            #region Show the created files in the Project window
            string coreName = BuildCoreName(templateName);
            string entryFolderAssetPath = $"{_selectedFolderPath}/{coreName}";
            
            EditorUtility.FocusProjectWindow();

            var createdAsset = AssetDatabase.LoadAssetAtPath<UnityObj>(entryFolderAssetPath);
            if (createdAsset == null)
            {
                string tabScriptAssetPath = $"{entryFolderAssetPath}/{coreName}Tab.cs";
                createdAsset = AssetDatabase.LoadAssetAtPath<UnityObj>(tabScriptAssetPath);
            }

            if (createdAsset != null)
            {
                EditorGUIUtility.PingObject(createdAsset);
            }
            #endregion

            Close();
        }

        private static string ConvertToAssetPath(string absolutePath)
        {
            string dataPath = Application.dataPath;
            
            if (!absolutePath.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string relativePath = "Assets" + absolutePath.Substring(dataPath.Length);
            return relativePath.Replace('\\', '/');
        }

        // Helper method to match the generator's naming logic
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

            return string.IsNullOrWhiteSpace(sanitized) ? "NewControlPanel" : sanitized;
        }

        private static string ToPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(value.Length);
            bool capitalizeNext = true;

            foreach (char elem in value)
            {
                if (char.IsLetterOrDigit(elem))
                {
                    char whatToAppend = capitalizeNext ? char.ToUpperInvariant(elem) : elem;
                    builder.Append(whatToAppend);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            return builder.ToString();
        }

        private static string StripSuffix(string value, string suffix)
        {
            if (value.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase) &&
                value.Length > suffix.Length)
            {
                return value.Substring(0, value.Length - suffix.Length);
            }

            return value;
        }
    }
}
