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
        /// <summary>Shaders de Built-in que dentro de URP o HDRP salen rosas.</summary>
        static readonly string[] BuiltInShaders =
        {
            "Standard", "Standard (Specular setup)", "Autodesk Interactive",
            "Diffuse", "Specular", "Bumped Diffuse", "Bumped Specular", "VertexLit"
        };

        /// <summary>Familias enteras de Built-in que tampoco sobreviven al cambio.</summary>
        static readonly string[] BuiltInFamilies =
        {
            "Legacy Shaders/", "Mobile/", "Nature/", "Reflective/", "Transparent/", "Self-Illumin/"
        };

        /// <summary>
        /// Estas funcionan igual en los tres pipelines: tocarlas seria romper
        /// interfaz, sprites, cielo o texto.
        /// </summary>
        static readonly string[] Untouchable =
        {
            "UI/", "Sprites/", "TextMeshPro/", "Skybox/", "Hidden/", "GUI/", "Particles/Standard"
        };

        [MenuItem("Boloos/Reparar materiales rosas")]
        public static void RepairFromMenu()
        {
            Repair(true);
        }

        /// <summary>Repara y devuelve cuantos materiales ha tocado.</summary>
        public static int Repair(bool verbose)
        {
            // Antes de tocar un solo material: si el proyecto es URP o HDRP y
            // simplemente ha perdido la referencia al asset del pipeline, todo
            // sale rosa y convertir los materiales a Standard seria cargarse el
            // proyecto. En ese caso se asigna el pipeline y no hay nada mas que
            // reparar.
            if (EnsurePipelineAssigned(verbose)) return 0;

            Shader target = AlleyMaterials.LitShader;
            if (target == null)
            {
                Debug.LogError("[Boloos] No hay shader al que reparar: no se encuentra el Lit del pipeline activo.");
                return 0;
            }

            var broken = new List<Material>();
            var wrongPipeline = new List<Material>();

            foreach (Material material in Materials())
            {
                if (!IsEditable(material)) continue;
                if (AlleyMaterials.IsBroken(material)) broken.Add(material);
                else if (IsForeignPipeline(material)) wrongPipeline.Add(material);
            }

            int repaired = 0;
            repaired += Retarget(broken, target);
            repaired += Retarget(wrongPipeline, target);

            if (!verbose && repaired == 0) return 0;

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
                report.AppendLine("  fuera al compilar: se arregla solo al compilar, o a mano");
                report.AppendLine("  con Boloos > Incluir shaders en la build.");
            }

            Debug.Log(report.ToString());
            if (repaired > 0) AssetDatabase.SaveAssets();
            return repaired;
        }

        /// <summary>Repara una lista concreta de materiales, para el importador.</summary>
        public static int RepairSpecific(IEnumerable<Material> materials)
        {
            Shader target = AlleyMaterials.LitShader;
            if (target == null) return 0;

            var broken = new List<Material>();
            foreach (Material material in materials)
            {
                if (material == null || !IsEditable(material)) continue;
                if (AlleyMaterials.IsBroken(material) || IsForeignPipeline(material)) broken.Add(material);
            }

            int repaired = Retarget(broken, target);
            if (repaired > 0)
            {
                Debug.Log("[Boloos] " + repaired + " material(es) recien importados reparados al shader del pipeline activo.");
                AssetDatabase.SaveAssets();
            }
            return repaired;
        }

        /// <summary>
        /// Anade el shader del pipeline activo a Always Included Shaders, que es
        /// lo que hace falta cuando los materiales se crean por codigo al
        /// ejecutar: si no, el shader no entra en la build.
        /// </summary>
        [MenuItem("Boloos/Incluir shaders en la build")]
        public static void AlwaysIncludeFromMenu()
        {
            AlwaysInclude(true);
        }

        public static void AlwaysInclude(bool verbose)
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
                    if (verbose) Debug.Log("[Boloos] " + shader.name + " ya estaba incluido en la build.");
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

        /// <summary>
        /// Comprueba si el rosa viene de que no hay pipeline asignado teniendo el
        /// proyecto uno. Se decide con evidencia: si los materiales del proyecto
        /// usan sobre todo shaders de URP o HDRP, lo que falta es asignar el
        /// asset, no convertir nada.
        /// </summary>
        static bool EnsurePipelineAssigned(bool verbose)
        {
            if (GraphicsSettings.currentRenderPipeline != null) return false;

            RenderPipelineAsset candidate = null;
            foreach (string guid in AssetDatabase.FindAssets("t:RenderPipelineAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
                if (asset != null) { candidate = asset; break; }
            }
            if (candidate == null) return false;

            int scriptable = 0, builtIn = 0;
            foreach (Material material in Materials())
            {
                if (!IsEditable(material) || material.shader == null) continue;

                string name = material.shader.name;
                if (name.StartsWith("Universal Render Pipeline/") || name.StartsWith("HDRP/") ||
                    name.StartsWith("Shader Graphs/")) scriptable++;
                else if (name == "Standard" || name.StartsWith("Legacy Shaders/")) builtIn++;
            }

            if (scriptable <= builtIn) return false;

            GraphicsSettings.defaultRenderPipeline = candidate;
            QualitySettings.renderPipeline = candidate;
            AlleyMaterials.ResetShaderCache();
            AssetDatabase.SaveAssets();

            Debug.Log("[Boloos] El proyecto no tenia pipeline asignado y sus materiales son de " +
                      "render pipeline (" + scriptable + " frente a " + builtIn + " de Built-in). " +
                      "Asignado " + candidate.name + " en Graphics y Quality Settings: esa era la causa del rosa. " +
                      "No se ha convertido ningun material.");
            return true;
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
            if (material.shader == null) return false;

            string name = material.shader.name;

            for (int i = 0; i < Untouchable.Length; i++)
            {
                if (name.StartsWith(Untouchable[i])) return false;
            }
            for (int i = 0; i < BuiltInShaders.Length; i++)
            {
                if (name == BuiltInShaders[i]) return true;
            }
            for (int i = 0; i < BuiltInFamilies.Length; i++)
            {
                if (name.StartsWith(BuiltInFamilies[i])) return true;
            }
            return false;
        }

        /// <summary>
        /// Solo se tocan materiales del proyecto o de la escena. Los de Packages
        /// y los internos de Unity son de solo lectura.
        /// </summary>
        static bool IsEditable(Material material)
        {
            string path = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(path)) return true;           // material de escena
            return path.StartsWith("Assets/");
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
