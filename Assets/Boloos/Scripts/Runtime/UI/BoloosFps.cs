using UnityEngine;

namespace Boloos.UI
{
    /// <summary>
    /// Contador de FPS suavizado. Mide con tiempo sin escalar, asi que sigue
    /// marcando bien con el juego en pausa o a camara lenta.
    /// </summary>
    public class BoloosFps : MonoBehaviour
    {
        [Tooltip("Segundos de media. Mas alto, lectura mas estable.")]
        public float window = 0.5f;

        public float Fps { get; private set; }
        public float MillisecondsPerFrame { get { return Fps > 0.01f ? 1000f / Fps : 0f; } }

        float m_elapsed;
        int m_frames;

        void Update()
        {
            m_frames++;
            m_elapsed += Time.unscaledDeltaTime;

            if (m_elapsed < window) return;

            Fps = m_frames / m_elapsed;
            m_frames = 0;
            m_elapsed = 0f;
        }

        /// <summary>Verde si va fino, ambar si flojea, rojo si va mal.</summary>
        public Color Tint
        {
            get
            {
                if (Fps >= 55f) return new Color(0.42f, 0.92f, 0.55f);
                if (Fps >= 30f) return new Color(0.98f, 0.78f, 0.30f);
                return new Color(0.96f, 0.40f, 0.40f);
            }
        }
    }
}
