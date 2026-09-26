using System.Collections.Generic;
using Boloos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boloos.EditorTools
{
    /// <summary>
    /// Ventana que construye la bolera en la escena abierta y deja las mallas,
    /// materiales y texturas generadas guardadas como assets.
    /// </summary>
    public class BoloosBuilderWindow : EditorWindow
    {
        const string GeneratedFolder = "Assets/Boloos/Generated";

        AlleyBuildSettings m_settings = new AlleyBuildSettings();
        Vector2 m_scroll;

        [MenuItem("Boloos/Construir bolera")]
        public static void Open()
        {
            var window = GetWindow<BoloosBuilderWindow>(false, "Boloos", true);
            window.minSize = new Vector2(320f, 300f);
            window.Show();
        }

        void OnGUI()
        {
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

            EditorGUILayout.LabelField("Bolera", EditorStyles.boldLabel);
            m_settings.laneCount = EditorGUILayout.IntSlider("Pistas", m_settings.laneCount, 1, 16);
            m_settings.ballsPerLane = EditorGUILayout.IntSlider("Bolas por pista", m_settings.ballsPerLane, 0, 5);
            m_settings.seed = EditorGUILayout.IntField("Semilla", m_settings.seed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Que se construye", EditorStyles.boldLabel);
            m_settings.buildPins = EditorGUILayout.Toggle("Bolos", m_settings.buildPins);
            m_settings.buildBalls = EditorGUILayout.Toggle("Bolas", m_settings.buildBalls);
            m_settings.buildReturnMachine = EditorGUILayout.Toggle("Maquina de retorno", m_settings.buildReturnMachine);
            m_settings.buildHouse = EditorGUILayout.Toggle("Local (suelo y luces)", m_settings.buildHouse);
            m_settings.addDemoController = EditorGUILayout.Toggle("Controles de prueba", m_settings.addDemoController);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Medidas reglamentarias: pista de " + AlleySpec.LaneWidth.ToString("0.000") + " m de ancho y " +
                AlleySpec.LaneLength.ToString("0.00") + " m de largo, bola de " +
                (AlleySpec.BallDiameter * 100f).ToString("0.0") + " cm y bolos de " +
                (AlleySpec.PinHeight * 100f).ToString("0.0") + " cm.",
                MessageType.Info);

            EditorGUILayout.Space();
            if (GUILayout.Button("Construir en la escena abierta", GUILayout.Height(34f)))
            {
                BuildIntoScene();
            }

            EditorGUILayout.EndScrollView();
        }

        void BuildIntoScene()
        {
            AlleyBuildResult result = AlleyFactory.Build(m_settings);

            SaveGeneratedAssets(result);

            Undo.RegisterCreatedObjectUndo(result.root, "Construir bolera");
            Selection.activeGameObject = result.root;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("[Boloos] Bolera construida: " + m_settings.laneCount +
                      " pistas, " + result.assets.Count + " recursos generados en " + GeneratedFolder + ".", result.root);
        }

        /// <summary>
        /// Guarda mallas, materiales, texturas y materiales de fisica como assets.
        /// Sin esto se perderian al recargar la escena.
        /// </summary>
        static void SaveGeneratedAssets(AlleyBuildResult result)
        {
            EnsureFolder(GeneratedFolder);

            var saved = new HashSet<Object>();
            foreach (Object asset in result.assets)
            {
                if (asset == null || saved.Contains(asset)) continue;
                if (AssetDatabase.Contains(asset)) continue;

                saved.Add(asset);
                string file = SafeName(asset.name) + ExtensionFor(asset);
                string path = AssetDatabase.GenerateUniqueAssetPath(GeneratedFolder + "/" + file);
                AssetDatabase.CreateAsset(asset, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static string ExtensionFor(Object asset)
        {
            if (asset is Material) return ".mat";
#if UNITY_6000_0_OR_NEWER
            if (asset is PhysicsMaterial) return ".physicsMaterial";
#else
            if (asset is PhysicMaterial) return ".physicMaterial";
#endif
            return ".asset";
        }

        static string SafeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Boloos";
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
