using System.Collections;
using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// La maquina de retorno de bolas. El trigger de este objeto es el foso:
    /// cuando una bola cae dentro, la maquina la engancha, la sube por el
    /// elevador, la manda por el carril lateral y la deja en el estante.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BallReturn : MonoBehaviour
    {
        [Tooltip("Estante donde acaba la bola.")]
        public BallRack rack;

        [Tooltip("Recorrido: foso -> elevador -> carril -> estante.")]
        public Transform[] path = new Transform[0];

        [Tooltip("Velocidad por el carril horizontal, en m/s.")]
        public float trackSpeed = 6.5f;

        [Tooltip("Velocidad al subir por el elevador, en m/s.")]
        public float liftSpeed = 2.2f;

        [Tooltip("Lo que tarda el acelerador en enganchar la bola.")]
        public float grabDelay = 0.45f;

        [Tooltip("Ruedas y rodillos que giran mientras la maquina trabaja.")]
        public SpinningPart[] movingParts = new SpinningPart[0];

        int m_inTransit;

        /// <summary>Bolas que la maquina lleva ahora mismo dentro.</summary>
        public int InTransit { get { return m_inTransit; } }

        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            if (box != null) box.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponentInParent<BowlingBall>();
            if (ball == null || ball.IsReturning || ball.IsRacked) return;
            if (path.Length == 0) return;

            StartCoroutine(Carry(ball));
        }

        IEnumerator Carry(BowlingBall ball)
        {
            ball.IsReturning = true;
            ball.Freeze();
            ball.transform.SetParent(null, true);

            m_inTransit++;
            SetPartsSpinning(true);

            yield return new WaitForSeconds(grabDelay);

            for (int i = 0; i < path.Length; i++)
            {
                Transform target = path[i];
                if (target == null) continue;

                while (ball != null && Vector3.Distance(ball.transform.position, target.position) > 0.01f)
                {
                    Vector3 from = ball.transform.position;
                    Vector3 dir = target.position - from;

                    // Los tramos verticales son el elevador y van mas despacio.
                    bool vertical = Mathf.Abs(dir.normalized.y) > 0.6f;
                    float speed = vertical ? liftSpeed : trackSpeed;
                    float step = speed * Time.deltaTime;

                    ball.transform.position = Vector3.MoveTowards(from, target.position, step);

                    // Rodadura: la bola gira mientras viaja, no se desliza tiesa.
                    Vector3 travel = ball.transform.position - from;
                    Vector3 flat = new Vector3(travel.x, 0f, travel.z);
                    if (flat.sqrMagnitude > 1e-8f)
                    {
                        Vector3 axis = Vector3.Cross(Vector3.up, flat.normalized);
                        float degrees = flat.magnitude / Mathf.Max(ball.radius, 0.0001f) * Mathf.Rad2Deg;
                        ball.transform.Rotate(axis, degrees, Space.World);
                    }

                    yield return null;
                }
            }

            m_inTransit--;
            if (m_inTransit <= 0)
            {
                m_inTransit = 0;
                SetPartsSpinning(false);
            }

            if (ball == null) yield break;

            if (rack == null || !rack.Store(ball))
            {
                // Estante lleno: se suelta donde acaba el carril en vez de perderla.
                ball.IsReturning = false;
                ball.Release();
            }
        }

        void SetPartsSpinning(bool on)
        {
            for (int i = 0; i < movingParts.Length; i++)
            {
                if (movingParts[i] != null) movingParts[i].spinning = on;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (path == null) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == null) continue;
                Gizmos.DrawWireSphere(path[i].position, 0.09f);
                if (i > 0 && path[i - 1] != null) Gizmos.DrawLine(path[i - 1].position, path[i].position);
            }
        }
    }
}
