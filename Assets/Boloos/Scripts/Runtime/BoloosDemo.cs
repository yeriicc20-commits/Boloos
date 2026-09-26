using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Mando de prueba: coge una bola del estante, la lanza y deja ver el ciclo
    /// completo (bola -> bolos -> foso -> maquina -> estante) sin tener que
    /// montar antes nada de interfaz.
    /// </summary>
    public class BoloosDemo : MonoBehaviour
    {
        public BowlingLane[] lanes = new BowlingLane[0];

        [Tooltip("Pista sobre la que actuan los controles.")]
        public int activeLane;

        [Range(3f, 12f)] public float speed = 8f;

        [Tooltip("Desplazamiento lateral de salida, en metros.")]
        [Range(-0.45f, 0.45f)] public float aim;

        [Tooltip("Efecto lateral, en vueltas por segundo.")]
        [Range(-4f, 4f)] public float spin = 1.2f;

        public bool showControls = true;

        public BowlingLane Current
        {
            get
            {
                if (lanes == null || lanes.Length == 0) return null;
                return lanes[Mathf.Clamp(activeLane, 0, lanes.Length - 1)];
            }
        }

        public void Throw()
        {
            BowlingLane lane = Current;
            if (lane != null) lane.Throw(aim, speed, spin);
        }

        public void ResetPins()
        {
            BowlingLane lane = Current;
            if (lane != null) lane.ResetPins();
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) Throw();
            if (Input.GetKeyDown(KeyCode.R)) ResetPins();
            if (Input.GetKeyDown(KeyCode.Tab) && lanes.Length > 0)
            {
                activeLane = (activeLane + 1) % lanes.Length;
            }
        }
#endif

        void OnGUI()
        {
            if (!showControls) return;

            GUILayout.BeginArea(new Rect(12f, 12f, 260f, 210f), GUI.skin.box);
            GUILayout.Label("Boloos - prueba");

            BowlingLane lane = Current;
            GUILayout.Label(lane != null
                ? "Pista " + lane.laneNumber + "   bolos en pie: " + lane.StandingPins
                : "Sin pistas asignadas");

            GUILayout.Label("Velocidad: " + speed.ToString("0.0") + " m/s");
            speed = GUILayout.HorizontalSlider(speed, 3f, 12f);

            GUILayout.Label("Punteria: " + aim.ToString("0.00") + " m");
            aim = GUILayout.HorizontalSlider(aim, -0.45f, 0.45f);

            GUILayout.Label("Efecto: " + spin.ToString("0.0") + " rps");
            spin = GUILayout.HorizontalSlider(spin, -4f, 4f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Lanzar")) Throw();
            if (GUILayout.Button("Replantar")) ResetPins();
            GUILayout.EndHorizontal();

            if (lanes.Length > 1 && GUILayout.Button("Cambiar de pista"))
            {
                activeLane = (activeLane + 1) % lanes.Length;
            }

            GUILayout.EndArea();
        }
    }
}
