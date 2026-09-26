using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Une las piezas de una pista: los bolos, la maquina de retorno y el
    /// estante. El eje local +Z apunta hacia los bolos y el origen esta en la
    /// linea de falta.
    /// </summary>
    public class BowlingLane : MonoBehaviour
    {
        public int laneNumber = 1;
        public PinSet pins;
        public BallReturn ballReturn;
        public BallRack rack;

        [Tooltip("Punto de la aproximacion desde el que sale la bola.")]
        public Transform releasePoint;

        /// <summary>Bolos en pie ahora mismo.</summary>
        public int StandingPins { get { return pins != null ? pins.StandingCount : 0; } }

        public void ResetPins()
        {
            if (pins != null) pins.ResetAll();
        }

        /// <summary>
        /// Coge una bola del estante y la lanza.
        /// </summary>
        /// <param name="lateralOffset">Desplazamiento respecto al centro, en metros. Negativo = izquierda.</param>
        /// <param name="speed">Velocidad de salida, en m/s. Un lanzamiento normal ronda los 8.</param>
        /// <param name="spin">Efecto lateral, en vueltas por segundo.</param>
        public BowlingBall Throw(float lateralOffset = 0f, float speed = 8f, float spin = 0f)
        {
            BowlingBall ball = rack != null ? rack.Take() : null;
            if (ball == null) return null;

            Transform origin = releasePoint != null ? releasePoint : transform;
            Vector3 position = origin.position + transform.right * lateralOffset;
            position.y = transform.position.y + AlleySpec.BallRadius + 0.01f;

            ball.transform.position = position;
            ball.Launch(transform.forward * speed, spin);
            return ball;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 a = transform.TransformPoint(new Vector3(-AlleySpec.LaneWidth * 0.5f, 0.02f, 0f));
            Vector3 b = transform.TransformPoint(new Vector3(AlleySpec.LaneWidth * 0.5f, 0.02f, 0f));
            Gizmos.DrawLine(a, b);

            Gizmos.color = Color.green;
            for (int pin = 1; pin <= 10; pin++)
            {
                Vector3 p = transform.TransformPoint(AlleySpec.PinPosition(pin));
                Gizmos.DrawWireSphere(p, AlleySpec.PinMaxDiameter * 0.5f);
            }
        }
    }
}
