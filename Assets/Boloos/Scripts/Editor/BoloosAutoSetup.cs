using System.Collections.Generic;
using Boloos.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boloos.EditorTools
{
    /// <summary>
    /// Deja una escena jugable montada sola, sin tener que arrastrar ni un
    /// componente: bolera, jugador, menu, HUD y contador de FPS.
    ///
    /// Se ejecuta al importar y no toca ninguna escena que ya exista. Si la
    /// escena abierta esta vacia y sin guardar, abre la nueva; si estabas
    /// trabajando en algo, la deja creada y te avisa, para no tirar tu trabajo.
    /// </summary>
    [InitializeOnLoad]
    public static class BoloosAutoSetup
    {
        public const string ScenePath = "Assets/Boloos/Scenes/Boloos.unity";
        const string MenuPath = "Boloos/Crear escena jugable";

        static BoloosAutoSetup()
        {
            EditorApplication.delayCall += RunOnce;
        }

        static void RunOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (System.IO.File.Exists(ScenePath)) return;   // ya se creo antes

            CreateScene(openIfSafe: true);
        }

        [MenuItem(MenuPath)]
        public static void CreateFromMenu()
        {
            CreateScene(openIfSafe: false);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// Monta la escena en memoria, la guarda y la deja registrada en las
        /// escenas de la build.
        /// </summary>
        public static void CreateScene(bool openIfSafe)
        {
            EnsureFolder("Assets/Boloos/Scenes");

            // Aditiva: la escena en la que estuvieras trabajando no se toca.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Populate(scene);

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[Boloos] No se ha podido guardar la escena en " + ScenePath + ".");
                return;
            }

            RegisterInBuild(ScenePath);

            bool safeToOpen = openIfSafe && IsActiveSceneEmpty();
            if (safeToOpen)
            {
                EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Debug.Log("[Boloos] Escena creada y abierta: " + ScenePath + ". Dale a Play.");
            }
            else
            {
                EditorSceneManager.CloseScene(scene, true);
                Debug.Log("[Boloos] Escena creada en " + ScenePath + ". No se ha abierto para no " +
                          "tocar la que tienes delante: abrela desde el Project, o usa el menu " +
                          MenuPath + ".");
            }
        }

        /// <summary>Lo que lleva dentro: camara, luz y el objeto que arranca todo.</summary>
        static void Populate(Scene scene)
        {
            // Camara de respaldo: mientras estas en el menu todavia no hay
            // jugador, y sin camara la pantalla saldria negra.
            var cameraGo = new GameObject("Camara del menu");
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            cameraGo.transform.position = new Vector3(1.2f, 1.7f, -6.5f);
            cameraGo.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.05f;
            camera.depth = -10f;             // por debajo de la del jugador
            cameraGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Luz general");
            SceneManager.MoveGameObjectToScene(lightGo, scene);
            lightGo.transform.rotation = Quaternion.Euler(48f, 32f, 0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            light.color = new Color(1f, 0.96f, 0.90f);

            // El objeto que lo arranca todo.
            var root = new GameObject("Boloos");
            SceneManager.MoveGameObjectToScene(root, scene);

            var match = root.AddComponent<BoloosMatch>();
            match.maxLanes = 8;
            match.startingLanes = 1;
            match.joinLocalPlayerOnStart = true;

            root.AddComponent<BoloosFps>();

            var menu = root.AddComponent<BoloosMenu>();
            menu.match = match;
            menu.openOnStart = true;
        }

        static bool IsActiveSceneEmpty()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty) return false;
            if (!string.IsNullOrEmpty(active.path)) return false;
            return active.rootCount == 0;
        }

        static void RegisterInBuild(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path) return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
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
