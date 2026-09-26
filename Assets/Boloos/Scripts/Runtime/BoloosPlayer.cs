using System;
using System.Collections;
using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Un jugador de la partida: su personaje, su pista y su puntuacion.
    /// Cada jugador tiene una pista entera para el (bolos, foso y maquina de
    /// retorno incluidos), no comparte nada con los demas.
    /// </summary>
    public class BoloosPlayer : MonoBehaviour
    {
        [Tooltip("Identificador estable. Con red, el del cliente.")]
        public int playerId;

        public string playerName = "Jugador";
        public Color color = Color.white;

        [Tooltip("True solo en la maquina de quien juega: es quien tiene controles y camara.")]
        public bool isLocal;

        [Tooltip("Pista asignada. La monta BoloosMatch al entrar.")]
        public BowlingLane lane;

        [Tooltip("Tiempo maximo que se espera a que la jugada se pare, en segundos.")]
        public float rollTimeout = 18f;

        [Tooltip("Margen para que los bolos dejen de bailar antes de contarlos.")]
        public float settleTime = 2.2f;

        public BowlingScore Score { get { return m_score; } }

        /// <summary>Bola en juego ahora mismo, o null si no hay ninguna rodando.</summary>
        public BowlingBall ActiveBall { get { return m_ball; } }

        public bool IsRolling { get { return m_rolling; } }

        /// <summary>Se dispara al apuntar un tiro, con los bolos derribados.</summary>
        public event Action<BoloosPlayer, int> rolled;

        /// <summary>Se dispara al cerrarse la partida de este jugador.</summary>
        public event Action<BoloosPlayer> finished;

        readonly BowlingScore m_score = new BowlingScore();
        BowlingBall m_ball;
        bool m_rolling;

        /// <summary>Bolos en pie en la pista de este jugador.</summary>
        public int StandingPins
        {
            get { return lane != null ? lane.StandingPins : 0; }
        }

        /// <summary>Bolas disponibles en su estante.</summary>
        public int BallsInRack
        {
            get { return lane != null && lane.rack != null ? lane.rack.Count : 0; }
        }

        /// <summary>
        /// Lanza desde una posicion y con una velocidad, y se encarga de esperar
        /// a que la jugada acabe para apuntarla.
        /// </summary>
        public BowlingBall Roll(Vector3 position, Vector3 velocity, float spin)
        {
            if (lane == null || m_rolling || m_score.IsComplete) return null;
            return RollBall(lane.ThrowFrom(position, velocity, spin), position, velocity, spin);
        }

        /// <summary>
        /// Lanza una bola que el jugador ya tenia en la mano. Devuelve null y la
        /// deja donde esta si la jugada anterior no ha acabado.
        /// </summary>
        public BowlingBall RollBall(BowlingBall ball, Vector3 position, Vector3 velocity, float spin)
        {
            if (ball == null || lane == null || m_rolling || m_score.IsComplete) return null;

            ball.transform.position = position;
            ball.Launch(velocity, spin);

            m_ball = ball;
            StartCoroutine(Settle(ball));
            return ball;
        }

        /// <summary>Replanta los 10 y empieza de cero.</summary>
        public void ResetGame()
        {
            StopAllCoroutines();
            m_rolling = false;
            m_ball = null;
            m_score.Reset();
            if (lane != null) lane.ResetPins();
        }

        /// <summary>
        /// Espera a que la bola y los bolos se paren, cuenta lo derribado y
        /// prepara el siguiente tiro: replanta si el frame se cierra, y retira
        /// los caidos si queda tiro en el mismo frame.
        /// </summary>
        IEnumerator Settle(BowlingBall ball)
        {
            m_rolling = true;

            int before = StandingPins;
            int frameBefore = m_score.CurrentFrame;
            float deadline = Time.time + rollTimeout;

            // La jugada acaba cuando la bola se va al foso (la recoge la maquina)
            // o cuando deja de moverse encima de la pista.
            while (Time.time < deadline)
            {
                if (ball == null || ball.IsReturning || ball.IsRacked) break;

                Vector3 velocity = BowlingBall.GetVelocity(ball.Body);
                if (!ball.Body.isKinematic && velocity.sqrMagnitude < 0.04f) break;

                yield return null;
            }

            yield return new WaitForSeconds(settleTime);

            int knocked = Mathf.Clamp(before - StandingPins, 0, BowlingScore.AllPins);
            m_score.Roll(knocked);

            if (rolled != null) rolled(this, knocked);

            if (m_score.IsComplete)
            {
                if (finished != null) finished(this);
            }
            else if (m_score.CurrentFrame != frameBefore || m_score.CurrentBall == 1)
            {
                lane.ResetPins();
            }
            else if (lane.pins != null)
            {
                // Mismo frame: los derribados se retiran, los de pie se quedan.
                lane.pins.ClearDeadwood();
            }

            m_ball = null;
            m_rolling = false;
        }
    }
}
