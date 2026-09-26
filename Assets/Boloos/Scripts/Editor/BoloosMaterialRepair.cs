using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boloos.EditorTools
{
    /// <summary>
    /// El rosa fucsia de Unity no es una textura que falte: es un material cuyo
    /// shader es nulo o no compilo, y que se pinta con el shader de error.
    ///
    /// Pasa por dos motivos, y esta herramienta cubre los dos:
    ///
    /// 1. El material usa un shader de otro pipeline (por ejemplo "Standard" en
    ///    un proyecto URP). Se ve rosa tambien en el editor.
    /// 2. El material se creo en tiempo de ejecucion con Shader.Find. Como
    ///    ningun asset referencia ese shader, al compilar se queda fuera de la
    ///    build, Shader.Find devuelve null y solo se ve rosa en la build, no en
    ///    el editor. Esa es la pista para distinguirlos.
    /// </summary>
    public static class BoloosMaterialRepair
    {
        /// <summary>Shaders de Built-in que en URP o HDRP salen rosas.</summary>
        static readonly string[] BuiltInShaders =
        {
            "Standard", "Standard (Specular setup)", "Autodesk Interactive",
            "Legacy Shaders/Diffuse", "Legacy Shaders/Bumped Diffuse",
            "Legacy Shaders/Specular", "Legacy Shaders/Transparent/Diffuse",
            "Mobile/Diffuse", "Mobile/Bumped Diffuse", "Diffuse", "Specular"
        };

        [MenuItem("Boloos/Reparar materiales rosas")]
        public static void Repair()
        {
            Shader target = AlleyMaterials.LitShader;
            if (target == null)
            {
                Debug.LogError("[Boloos] No hay shader al que reparar: no se encuentra el Lit del pipeline activo.");
                return;
            }

            var broken = new List<Material>();
            var wrongPipeline = new List<Material>();

            foreach (Material material in Materials())
            {
                if (AlleyMaterials.IsBroken(material)) broken.Add(material);
                else if (IsForeignPipeline(material)) wrongPipeline.Add(material);
            }

            int repaired = 0;
            repaired += Retarget(broken, target);
            repaired += Retarget(wrongPipeline, target);

            var report = new StringBuilder();
            report.AppendLine("[Boloos] Reparacion de materiales");
            report.AppendLine("  Shader de destino: " + target.name);
            report.AppendLine("  Materiales sin shader valido (rosas): " + broken.Count);
            report.AppendLine("  Materiales de otro pipeline: " + wrongPipeline.Count);
            report.AppendLine("  Reparados: " + repaired);

            if (repaired == 0)
            {
                report.AppendLine();
                report.AppendLine("  Ningun material del proyecto esta roto. Si en la build");
                report.AppendLine("  sale rosa pero en el editor no, el shader se esta quedando");
                report.AppendLine("  fuera al compilar: usa Boloos > Incluir shaders en la build.");
            }

            Debug.Log(report.ToString());
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Anade el shader del pipeline activo a Always Included Shaders, que es
        /// lo que hace falta cuando los materiales se crean por codigo al
        /// ejecutar: si no, el shader no entra en la build.
        /// </summary>
        [MenuItem("Boloos/Incluir shaders en la build")]
        public static void AlwaysInclude()
        {
            Shader shader = AlleyMaterials.LitShader;
            if (shader == null)
            {
                Debug.LogError("[Boloos] No se encuentra el shader del pipeline activo.");
                return;
            }

            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (settings == null || settings.Length == 0)
            {
                Debug.LogError("[Boloos] No se ha podido abrir ProjectSettings/GraphicsSettings.asset.");
                return;
            }

            var graphics = new SerializedObject(settings[0]);
            SerializedProperty list = graphics.FindProperty("m_AlwaysIncludedShaders");
            if (list == null)
            {
                Debug.LogError("[Boloos] Esta version de Unity no expone m_AlwaysIncludedShaders.");
                return;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    Debug.Log("[Boloos] " + shader.name + " ya estaba incluido en la build.");
                    return;
                }
            }

            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            list.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            graphics.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log("[Boloos] " + shader.name + " anadido a Always Included Shaders. " +
                      "Vuelve a compilar la build.");
        }

        /// <summary>Lista los materiales del proyecto y los de la escena abierta.</summary>
        static IEnumerable<Material> Materials()
        {
            var seen = new HashSet<Material>();

            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && seen.Add(material)) yield return material;
            }

            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (EditorUtility.IsPersistent(renderer)) continue;
                if (!renderer.gameObject.scene.IsValid()) continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && seen.Add(material)) yield return material;
                }
            }
        }

        static bool IsForeignPipeline(Material material)
        {
            if (GraphicsSettings.currentRenderPipeline == null) return false;

            string name = material.shader.name;
            for (int i = 0; i < BuiltInShaders.Length; i++)
            {
                if (name == BuiltInShaders[i]) return true;
            }
            return false;
        }

        /// <summary>
        /// Cambia el shader conservando color, textura y acabado.
        ///
        /// Los valores se leen del objeto serializado y no del material vivo: un
        /// material con el shader roto no responde a HasProperty, asi que
        /// leerlo del material dejaria todo blanco. En m_SavedProperties siguen
        /// guardados aunque el shader no exista.
        /// </summary>
        static int Retarget(List<Material> materials, Shader target)
        {
            int count = 0;
            foreach (Material material in materials)
            {
                var saved = new SerializedObject(material);
                Color color = SavedColor(saved, "_BaseColor", "_Color");
                Texture texture = SavedTexture(saved, "_BaseMap", "_MainTex");
                Vector2 scale = SavedScale(saved, "_BaseMap", "_MainTex");
                float smoothness = SavedFloat(saved, "_Smoothness", "_Glossiness", 0.4f);
                float metallic = SavedFloat(saved, "_Metallic", "_Metallic", 0f);

                Undo.RecordObject(material, "Reparar material");
                material.shader = target;

                AlleyMaterials.SetColor(material, color);
                if (texture != null)
                {
                    AlleyMaterials.SetTexture(material, texture);
                    AlleyMaterials.SetTiling(material, scale);
                }
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);

                EditorUtility.SetDirty(material);
                count++;
            }
            return count;
        }

        /// <summary>Busca una entrada por nombre en una de las listas de m_SavedProperties.</summary>
        static SerializedProperty Entry(SerializedObject material, string list, string first, string second)
        {
            SerializedProperty entries = material.FindProperty("m_SavedProperties." + list);
            if (entries == null) return null;

            SerializedProperty fallback = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                SerializedProperty key = entry.FindPropertyRelative("first");
                if (key == null) continue;

                if (key.stringValue == first) return entry.FindPropertyRelative("second");
                if (key.stringValue == second) fallback = entry.FindPropertyRelative("second");
            }
            return fallback;
        }

        static Color SavedColor(SerializedObject material, string first, string second)
        {
            SerializedProperty value = Entry(material, "m_Colors", first, second);
            return value != null ? value.colorValue : Color.white;
        }

        static Texture SavedTexture(SerializedObject material, string first, string second)
        {
            SerializedProperty value = Entry(material, "m_TexEnvs", first, second);
            if (value == null) return null;

            SerializedProperty texture = value.FindPropertyRelative("m_Texture");
            return texture != null ? texture.objectReferenceValue as Texture : null;
        }

        static Vector2 SavedScale(SerializedObject material, string first, string second)
        {
            SerializedProperty value = Entry(material, "m_TexEnvs", first, second);
            if (value == null) return Vector2.one;

            SerializedProperty scale = value.FindPropertyRelative("m_Scale");
            return scale != null ? scale.vector2Value : Vector2.one;
        }

        static float SavedFloat(SerializedObject material, string first, string second, float fallback)
        {
            SerializedProperty value = Entry(material, "m_Floats", first, second);
            return value != null ? value.floatValue : fallback;
        }
    }
}
