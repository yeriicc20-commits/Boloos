using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Boloos.EditorTools
{
    /// <summary>
    /// Deja el arreglo del rosa en automatico: no hay que ejecutar ningun menu.
    ///
    /// - Al abrir el proyecto o recompilar, repara una vez por sesion.
    /// - Al importar materiales nuevos, repara los que vengan rotos.
    /// - Al compilar una build, mete el shader en Always Included Shaders.
    ///
    /// Se puede desactivar en Boloos > Reparar materiales automaticamente.
    /// </summary>
    [InitializeOnLoad]
    public static class BoloosAutoRepair
    {
        const string MenuPath = "Boloos/Reparar materiales automaticamente";
        const string EnabledKey = "Boloos.AutoRepair.Enabled";
        const string SessionKey = "Boloos.AutoRepair.Done";

        static BoloosAutoRepair()
        {
            // La base de datos de assets no esta lista durante el constructor.
            EditorApplication.delayCall += RunOnce;
        }

        public static bool Enabled
        {
            get { return EditorPrefs.GetBool(EnabledKey, true); }
            set { EditorPrefs.SetBool(EnabledKey, value); }
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            Enabled = !Enabled;
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        static void RunOnce()
        {
            if (!Enabled) return;
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            // En silencio: solo escribe en consola si ha reparado algo.
            BoloosMaterialRepair.Repair(false);
        }
    }

    /// <summary>Repara los materiales que entran al proyecto ya rotos.</summary>
    public class BoloosMaterialImporter : AssetPostprocessor
    {
        static readonly List<string> s_pending = new List<string>();

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!BoloosAutoRepair.Enabled) return;

            for (int i = 0; i < imported.Length; i++)
            {
                if (imported[i].EndsWith(".mat")) s_pending.Add(imported[i]);
            }

            // Reparar dentro del propio import reentraria en el importador,
            // asi que se deja para el siguiente tick del editor.
            if (s_pending.Count > 0) EditorApplication.delayCall += Flush;
        }

        static void Flush()
        {
            if (s_pending.Count == 0) return;

            var materials = new List<Material>(s_pending.Count);
            for (int i = 0; i < s_pending.Count; i++)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(s_pending[i]);
                if (material != null) materials.Add(material);
            }
            s_pending.Clear();

            BoloosMaterialRepair.RepairSpecific(materials);
        }
    }

    /// <summary>
    /// Mete el shader del pipeline en la build antes de compilar, que es lo que
    /// hace falta cuando los materiales se crean por codigo al ejecutar.
    /// </summary>
    public class BoloosBuildShaders : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!BoloosAutoRepair.Enabled) return;
            BoloosMaterialRepair.AlwaysInclude(false);
        }
    }
}
