using UnityEngine;
using UnityEditor;
using LastDay.Dialogue;

namespace LastDay.Editor
{
    /// <summary>
    /// Editor window for switching the active LLM model.
    /// Open via: LastDay > Switch Model
    /// </summary>
    public class ModelSwitcherWindow : EditorWindow
    {
        private const string PREFS_KEY = "LastDay.SelectedModelIndex";

        private int selectedIndex;

        [MenuItem("LastDay/Switch Model")]
        public static void Open()
        {
            var window = GetWindow<ModelSwitcherWindow>(true, "Switch LLM Model", true);
            window.minSize = new Vector2(500, 320);
            window.maxSize = new Vector2(600, 400);
            window.selectedIndex = EditorPrefs.GetInt(PREFS_KEY, 0);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("LLM Model Selection", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Switching the model here persists the selection across Editor and Play Mode sessions. " +
                "The model file is downloaded automatically on first use from Hugging Face.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            var models = ModelDownloader.AvailableModels;

            for (int i = 0; i < models.Length; i++)
            {
                var model = models[i];
                bool isCurrent = (i == selectedIndex);

                DrawModelEntry(i, model, isCurrent);
                EditorGUILayout.Space(6);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Apply Selection", GUILayout.Width(160), GUILayout.Height(28)))
                ApplySelection();

            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(28)))
                Close();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            DrawPersistentDataPath();
        }

        private void DrawModelEntry(int index, ModelDownloader.ModelDefinition model, bool isCurrent)
        {
            var boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8)
            };

            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.BeginHorizontal();
            bool selected = EditorGUILayout.Toggle(isCurrent, GUILayout.Width(20));
            if (selected && !isCurrent)
                selectedIndex = index;

            EditorGUILayout.LabelField(model.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(model.SizeHint, EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(model.Description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"File: {model.Filename}", EditorStyles.miniLabel);

            string modelPath = System.IO.Path.Combine(
                Application.persistentDataPath, "Models", model.Filename);
            bool cached = System.IO.File.Exists(modelPath);
            EditorGUILayout.LabelField(
                cached ? "Status: Cached on disk" : "Status: Not yet downloaded",
                cached ? EditorStyles.miniLabel : EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void ApplySelection()
        {
            EditorPrefs.SetInt(PREFS_KEY, selectedIndex);

            // Also write to PlayerPrefs so runtime picks it up immediately
            PlayerPrefs.SetInt(PREFS_KEY, selectedIndex);
            PlayerPrefs.Save();

            var model = ModelDownloader.AvailableModels[selectedIndex];
            Debug.Log($"[ModelSwitcher] Selected: {model.Name} ({model.SizeHint}). " +
                      $"File: {model.Filename}. Will download on next Play if not cached.");

            EditorUtility.DisplayDialog(
                "Model Selected",
                $"Active model set to:\n\n{model.Name} ({model.SizeHint})\n\n" +
                "The model will be downloaded automatically when you enter Play Mode if it is not already cached.",
                "OK");

            Close();
        }

        private void DrawPersistentDataPath()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cache folder:", EditorStyles.miniLabel, GUILayout.Width(80));
            string cachePath = System.IO.Path.Combine(Application.persistentDataPath, "Models");
            EditorGUILayout.SelectableLabel(cachePath, EditorStyles.miniLabel, GUILayout.Height(16));
            if (GUILayout.Button("Reveal", EditorStyles.miniButton, GUILayout.Width(56)))
                EditorUtility.RevealInFinder(cachePath);
            EditorGUILayout.EndHorizontal();
        }
    }
}
