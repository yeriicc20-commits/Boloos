using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Texturas generadas por codigo. Asi la bolera se ve como una bolera
    /// aunque el proyecto no tenga ni un solo asset de arte importado.
    /// </summary>
    public static class ProceduralTextures
    {
        /// <summary>
        /// Duela de pista: tablas verticales con veta, juntas oscuras y variacion
        /// de tono entre tablas. La U cruza la pista a lo ancho (39 tablas) y la V
        /// corre a lo largo, asi que la textura se repite en V sin costura visible.
        /// </summary>
        public static Texture2D LaneWood(int width = 512, int height = 1024, int boards = AlleySpec.BoardCount,
                                         Color? light = null, Color? dark = null, int seed = 7)
        {
            Color lo = dark ?? new Color(0.36f, 0.20f, 0.10f);
            Color hi = light ?? new Color(0.80f, 0.56f, 0.31f);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            tex.name = "BoloosLaneWood";
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float bf = u * boards;
                    int board = Mathf.FloorToInt(bf);
                    float inBoard = bf - board;

                    // tono base distinto por tabla
                    float tint = Mathf.PerlinNoise(board * 4.7f + seed, 0.37f);

                    // veta: ruido muy estirado a lo largo de la tabla
                    float grain =
                        Mathf.PerlinNoise(board * 13.1f + inBoard * 3f + seed, v * 34f) * 0.6f +
                        Mathf.PerlinNoise(board * 27.3f + inBoard * 9f, v * 150f) * 0.4f;

                    // Junta oscura entre tablas: una linea fina en el 5% de los
                    // bordes. Mas ancha y la pista parece chapa ondulada.
                    float edge = Mathf.Min(inBoard, 1f - inBoard) / 0.05f;
                    float seam = Mathf.SmoothStep(0.40f, 1f, Mathf.Clamp01(edge));

                    Color c = Color.Lerp(lo, hi, Mathf.Clamp01(tint * 0.70f + grain * 0.30f));
                    c *= seam;
                    c.a = 1f;
                    px[y * width + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>
        /// Bolo: blanco con el cuello coloreado y las dos franjas rojas.
        /// Solo varia en V, que es la coordenada que recorre el perfil torneado.
        /// </summary>
        public static Texture2D Pin(float stripeA, float stripeB, float stripeWidth,
                                    int width = 32, int height = 512)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            tex.name = "BoloosPin";
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[width * height];

            Color body = new Color(0.94f, 0.93f, 0.90f);
            Color stripe = new Color(0.78f, 0.09f, 0.11f);
            Color scuff = new Color(0.86f, 0.85f, 0.82f);

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                Color c = body;

                if (Mathf.Abs(v - stripeA) < stripeWidth * 0.5f) c = stripe;
                if (Mathf.Abs(v - stripeB) < stripeWidth * 0.5f) c = stripe;

                for (int x = 0; x < width; x++)
                {
                    float wear = Mathf.PerlinNoise(x * 0.35f, v * 60f);
                    Color o = Color.Lerp(c, c == body ? scuff : c * 0.9f, wear * 0.35f);
                    o.a = 1f;
                    px[y * width + x] = o;
                }
            }

            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>Bola: color solido con un jaspeado suave, como una bola de resina.</summary>
        public static Texture2D Ball(Color baseColor, int size = 256, int seed = 3)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = "BoloosBall";
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n =
                        Mathf.PerlinNoise(x * 0.018f + seed, y * 0.018f) * 0.6f +
                        Mathf.PerlinNoise(x * 0.07f, y * 0.07f + seed) * 0.4f;
                    Color c = Color.Lerp(baseColor * 0.65f, Color.Lerp(baseColor, Color.white, 0.25f), n);
                    c.a = 1f;
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>Moqueta de la zona de aproximacion y del local.</summary>
        public static Texture2D Carpet(Color baseColor, int size = 256, int seed = 11)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = "BoloosCarpet";
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.9f + seed, y * 0.9f) * 0.25f +
                              Mathf.PerlinNoise(x * 0.05f, y * 0.05f + seed) * 0.75f;
                    Color c = Color.Lerp(baseColor * 0.7f, baseColor * 1.15f, n);
                    c.a = 1f;
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }
    }
}
