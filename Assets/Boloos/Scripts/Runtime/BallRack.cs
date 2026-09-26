using System.Collections.Generic;
using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Estante de bolas al final del retorno. Las bolas que llegan se colocan en
    /// fila y van deslizando hacia delante segun se van cogiendo.
    /// </summary>
    public class BallRack : MonoBehaviour
    {
        [Tooltip("Huecos de la fila. El 0 es el de delante, el que se coge.")]
        public Transform[] slots = new Transform[0];

        [Tooltip("Velocidad con la que una bola se desliza hasta su hueco, en m/s.")]
        public float slideSpeed = 1.1f;

        readonly List<BowlingBall> m_balls = new List<BowlingBall>();

        public int Count { get { return m_balls.Count; } }
        public bool HasBall { get { return m_balls.Count > 0; } }
        public bool IsFull { get { return m_balls.Count >= slots.Length; } }

        /// <summary>Mete una bola en la fila. Devuelve false si el estante esta lleno.</summary>
        public bool Store(BowlingBall ball)
        {
            if (ball == null || IsFull || m_balls.Contains(ball)) return false;

            ball.Freeze();
            ball.IsRacked = true;
            ball.IsReturning = false;
            ball.transform.SetParent(transform, true);
            m_balls.Add(ball);
            return true;
        }

        /// <summary>Saca la bola de delante y se la entrega al jugador.</summary>
        public BowlingBall Take()
        {
            if (m_balls.Count == 0) return null;

            BowlingBall ball = m_balls[0];
            m_balls.RemoveAt(0);
            ball.transform.SetParent(null, true);
            ball.IsRacked = false;
            ball.Release();
            return ball;
        }

        /// <summary>Posicion desde la que el jugador recoge la bola.</summary>
        public Vector3 PickupPoint
        {
            get { return slots.Length > 0 && slots[0] != null ? slots[0].position : transform.position; }
        }

        void Update()
        {
            for (int i = 0; i < m_balls.Count && i < slots.Length; i++)
            {
                BowlingBall ball = m_balls[i];
                if (ball == null || slots[i] == null) continue;

                ball.transform.position = Vector3.MoveTowards(
                    ball.transform.position, slots[i].position, slideSpeed * Time.deltaTime);
            }

            m_balls.RemoveAll(IsMissing);
        }

        static bool IsMissing(BowlingBall ball)
        {
            return ball == null;
        }
    }
}
