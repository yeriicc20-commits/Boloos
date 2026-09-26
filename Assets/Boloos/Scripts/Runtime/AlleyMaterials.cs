using UnityEngine;
using UnityEngine.Rendering;

namespace Boloos
{
    /// <summary>
    /// Crea materiales sin depender del pipeline: detecta Built-in, URP o HDRP
    /// y escribe las propiedades con el nombre que toque en cada uno.
    /// </summary>
    public static class AlleyMaterials
    {
        static Shader s_lit;

        /// <summary>Shader PBR del pipeline activo.</summary>
        public static Shader LitShader
        {
            get
            {
                if (s_lit != null) return s_lit;

                RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
                if (rp != null)
                {
                    string pipeline = rp.GetType().Name;
                    if (pipeline.Contains("Universal")) s_lit = Shader.Find("Universal Render Pipeline/Lit");
                    else if (pipeline.Contains("HD")) s_lit = Shader.Find("HDRP/Lit");
                }

                if (s_lit == null) s_lit = Shader.Find("Standard");
                if (s_lit == null) s_lit = Shader.Find("Universal Render Pipeline/Lit");
                if (s_lit == null) s_lit = Shader.Find("Diffuse");

                if (s_lit == null)
                {
                    // Shader.Find solo encuentra shaders incluidos en la build. Si
                    // los materiales se crean en tiempo de ejecucion, ningun asset
                    // referencia el shader, se queda fuera al compilar y todo sale
                    // rosa. Se arregla con Boloos > Incluir shaders en la build.
                    Debug.LogError("[Boloos] No se encuentra el shader del pipeline activo. " +
                                   "En una build hay que anadirlo a Always Included Shaders: " +
                                   "menu Boloos > Incluir shaders en la build.");
                }
                return s_lit;
            }
        }

        public static Material Create(string name, Color color, float smoothness = 0.35f, float metallic = 0f,
                                      Texture texture = null, Vector2? tiling = null)
        {
            var m = new Material(LitShader) { name = name };
            SetColor(m, color);
            if (texture != null) SetTexture(m, texture);
            if (tiling.HasValue) SetTiling(m, tiling.Value);
            SetFloatIfPresent(m, "_Smoothness", smoothness);
            SetFloatIfPresent(m, "_Glossiness", smoothness);
            SetFloatIfPresent(m, "_Metallic", metallic);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static void SetTexture(Material m, Texture t)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        }

        public static void SetTiling(Material m, Vector2 tiling)
        {
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
            if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling);
        }

        /// <summary>Material emisivo para las luces del local y los marcadores.</summary>
        public static Material CreateEmissive(string name, Color color, float intensity = 2f)
        {
            Material m = Create(name, color, 0.2f);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * intensity);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return m;
        }

        /// <summary>
        /// Un material se ve rosa cuando su shader es nulo o es el de error de
        /// Unity. Sirve para detectarlo desde el editor y desde el juego.
        /// </summary>
        public static bool IsBroken(Material material)
        {
            if (material == null) return true;
            if (material.shader == null) return true;
            return material.shader.name == "Hidden/InternalErrorShader";
        }

        static void SetFloatIfPresent(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }
    }
}
