using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Bola de bolos: masa y radio reglamentarios, rodadura real y estado
    /// para que la maquina de retorno sepa cuando puede recogerla.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class BowlingBall : MonoBehaviour
    {
        [Tooltip("Radio en metros. Por defecto, los 8,5\" reglamentarios.")]
        public float radius = AlleySpec.BallRadius;

        [Tooltip("Mientras la maquina la lleva de vuelta, la bola no responde a la fisica.")]
        public bool IsReturning { get; internal set; }

        /// <summary>True cuando la bola esta parada en el rack, lista para usarse.</summary>
        public bool IsRacked { get; internal set; }

        Rigidbody m_body;

        public Rigidbody Body
        {
            get
            {
                if (m_body == null) m_body = GetComponent<Rigidbody>();
                return m_body;
            }
        }

        void Awake()
        {
            m_body = GetComponent<Rigidbody>();
            m_body.mass = AlleySpec.BallMass;
            // Una bola de 7 kg a 8 m/s atraviesa un bolo fino si la deteccion es discreta.
            m_body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>Lanza la bola con una velocidad y un efecto lateral (revoluciones).</summary>
        public void Launch(Vector3 velocity, float sideSpinRps = 0f)
        {
            IsRacked = false;
            IsReturning = false;
            transform.SetParent(null, true);

            Rigidbody rb = Body;
            rb.isKinematic = false;
            rb.useGravity = true;
            SetVelocity(rb, velocity);

            // Rodadura natural mas el efecto que hace curvar la bola.
            Vector3 roll = Vector3.Cross(Vector3.up, velocity) / Mathf.Max(radius, 0.0001f);
            rb.angularVelocity = roll + Vector3.up * sideSpinRps * Mathf.PI * 2f;
        }

        /// <summary>Congela la bola (la usa la maquina mientras la transporta).</summary>
        public void Freeze()
        {
            Rigidbody rb = Body;
            SetVelocity(rb, Vector3.zero);
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        /// <summary>Devuelve la bola a la fisica normal.</summary>
        public void Release()
        {
            Rigidbody rb = Body;
            rb.isKinematic = false;
            rb.useGravity = true;
            IsRacked = false;
        }

        internal static void SetVelocity(Rigidbody rb, Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        internal static Vector3 GetVelocity(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
    }
}
