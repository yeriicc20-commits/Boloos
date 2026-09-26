using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Bolo con peso reglamentario y centro de masas bajo, que es lo que hace
    /// que caiga y ruede como un bolo de verdad en vez de como un palo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BowlingPin : MonoBehaviour
    {
        [Tooltip("Numero de bolo, 1 a 10.")]
        public int number = 1;

        [Tooltip("Inclinacion, en grados, a partir de la cual se considera derribado.")]
        public float fallAngle = 40f;

        [Tooltip("Desplazamiento, en metros, a partir del cual se considera derribado.")]
        public float fallDistance = 0.06f;

        Vector3 m_spot;
        Quaternion m_spotRotation;
        Rigidbody m_body;

        public Rigidbody Body
        {
            get
            {
                if (m_body == null) m_body = GetComponent<Rigidbody>();
                return m_body;
            }
        }

        /// <summary>Marca del bolo, en coordenadas de mundo.</summary>
        public Vector3 Spot { get { return m_spot; } }

        void Awake()
        {
            m_body = GetComponent<Rigidbody>();
            m_body.mass = AlleySpec.PinMass;
            m_body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_body.interpolation = RigidbodyInterpolation.Interpolate;
            // Centro de masas a un tercio de la altura: el bolo baila en vez de volcar seco.
            m_body.centerOfMass = new Vector3(0f, AlleySpec.PinHeight * 0.32f, 0f);

            if (m_spot == Vector3.zero) RememberSpot();
        }

        /// <summary>Guarda la posicion actual como marca a la que volver.</summary>
        public void RememberSpot()
        {
            m_spot = transform.position;
            m_spotRotation = transform.rotation;
        }

        public bool IsDown
        {
            get
            {
                if (!gameObject.activeInHierarchy) return true;
                float tilt = Vector3.Angle(transform.up, Vector3.up);
                if (tilt > fallAngle) return true;
                Vector3 flat = transform.position - m_spot;
                flat.y = 0f;
                return flat.magnitude > fallDistance;
            }
        }

        /// <summary>Vuelve a plantar el bolo en su marca, quieto.</summary>
        public void ResetToSpot()
        {
            gameObject.SetActive(true);
            Rigidbody rb = Body;
            rb.isKinematic = true;
            transform.SetPositionAndRotation(m_spot, m_spotRotation);
            rb.isKinematic = false;
            BowlingBall.SetVelocity(rb, Vector3.zero);
            rb.angularVelocity = Vector3.zero;
        }
    }
}
