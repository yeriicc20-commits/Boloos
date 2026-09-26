using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Control en primera persona del jugador local: andar por la aproximacion,
    /// mirar, coger bola del estante de su pista y lanzarla cargando fuerza.
    ///
    /// Todo pasa por metodos publicos, asi que vale igual con el Input Manager
    /// clasico, con el Input System nuevo o con lo que ya tengas: basta con
    /// llamar a SetMove, SetLook, PickUp y BeginThrow/EndThrow.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class BoloosPlayerController : MonoBehaviour
    {
        public BoloosPlayer player;

        [Tooltip("Velocidad al andar, en m/s.")]
        public float walkSpeed = 3.0f;

        public float lookSensitivity = 2.4f;
        public float maxPitch = 80f;

        [Tooltip("Segundos de carga para llegar a la fuerza maxima.")]
        public float chargeTime = 1.1f;

        [Tooltip("Velocidad de salida de la bola, de minima a maxima, en m/s.")]
        public float minThrowSpeed = 5.5f;
        public float maxThrowSpeed = 11.5f;

        [Tooltip("Efecto lateral maximo, en vueltas por segundo.")]
        public float maxSpin = 3.5f;

        [Tooltip("No se puede pasar de la linea de falta.")]
        public bool enforceFoulLine = true;

        public Camera view { get; private set; }

        /// <summary>Carga actual del lanzamiento, de 0 a 1. Para pintar la barra.</summary>
        public float Charge { get { return Mathf.Clamp01(m_charge); } }

        public bool HasBall { get { return m_ball != null; } }

        CharacterController m_controller;
        Transform m_head;
        Transform m_hand;
        Vector2 m_move;
        Vector2 m_look;
        float m_pitch;
        float m_charge;
        float m_spin;
        bool m_charging;
        float m_fall;
        BowlingBall m_ball;

        void Awake()
        {
            m_controller = GetComponent<CharacterController>();
            m_head = PlayerFactory.Find(gameObject, "Camara");
            m_hand = PlayerFactory.Find(gameObject, "Mano");

            if (player == null) player = GetComponent<BoloosPlayer>();

            // Camara solo para quien juega en esta maquina: si no, con red cada
            // cliente renderizaria tambien por los ojos de los demas.
            if (m_head != null && player != null && player.isLocal)
            {
                view = m_head.GetComponent<Camera>();
                if (view == null) view = m_head.gameObject.AddComponent<Camera>();
                view.nearClipPlane = 0.05f;
            }
        }

        void Start()
        {
            // La camara y el cuerpo solo del jugador local: si no, cada cliente
            // veria por los ojos de todos a la vez.
            if (player != null && !player.isLocal)
            {
                enabled = false;
                if (view != null) view.enabled = false;
            }
        }

        // ---------------- entrada ----------------

        /// <summary>Movimiento, de -1 a 1 en cada eje.</summary>
        public void SetMove(Vector2 move) { m_move = move; }

        /// <summary>Giro de camara de este frame, en unidades de raton.</summary>
        public void SetLook(Vector2 look) { m_look = look; }

        /// <summary>
        /// Coge una bola del estante de su pista, si esta cerca, y se la pone en
        /// la mano. La bola sale del estante de verdad: deja su hueco libre y las
        /// de detras deslizan hacia delante.
        /// </summary>
        public bool PickUp()
        {
            if (m_ball != null || player == null || player.lane == null || player.lane.rack == null) return false;

            float distance = Vector3.Distance(transform.position, player.lane.rack.PickupPoint);
            if (distance > 2.2f) return false;

            BowlingBall ball = player.lane.rack.Take();
            if (ball == null) return false;

            ball.Freeze();
            ball.transform.SetParent(m_hand != null ? m_hand : transform, false);
            ball.transform.localPosition = Vector3.zero;

            m_ball = ball;
            return true;
        }

        public void BeginThrow()
        {
            if (m_ball == null) return;
            m_charging = true;
            m_charge = 0f;
        }

        /// <summary>Suelta la bola con la fuerza cargada y el efecto apuntado.</summary>
        public BowlingBall EndThrow()
        {
            if (!m_charging || m_ball == null || player == null) return null;

            m_charging = false;

            float speed = Mathf.Lerp(minThrowSpeed, maxThrowSpeed, Charge);
            Vector3 origin = m_hand != null ? m_hand.position : transform.position;

            // Se lanza hacia donde mira el jugador, aplanado: una bola no sale
            // hacia arriba por mucho que levantes la vista.
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;

            BowlingBall ball = m_ball;
            m_ball = null;
            m_charge = 0f;

            ball.transform.SetParent(null, true);
            return player.RollBall(ball, origin, forward * speed, m_spin);
        }

        /// <summary>Efecto lateral, de -1 a 1.</summary>
        public void SetSpin(float spin) { m_spin = Mathf.Clamp(spin, -1f, 1f) * maxSpin; }

        // ---------------- ciclo ----------------

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            ReadLegacyInput();
#endif
            Look();
            Move();

            if (m_charging) m_charge = Mathf.Min(1f, m_charge + Time.deltaTime / Mathf.Max(chargeTime, 0.01f));
        }

        void Look()
        {
            if (Mathf.Abs(m_look.x) > 0f)
            {
                transform.Rotate(Vector3.up, m_look.x * lookSensitivity, Space.World);
            }

            if (m_head != null)
            {
                m_pitch = Mathf.Clamp(m_pitch - m_look.y * lookSensitivity, -maxPitch, maxPitch);
                m_head.localRotation = Quaternion.Euler(m_pitch, 0f, 0f);
            }

            m_look = Vector2.zero;
        }

        void Move()
        {
            Vector3 step = transform.right * m_move.x + transform.forward * m_move.y;
            if (step.sqrMagnitude > 1f) step = step.normalized;

            m_fall = m_controller.isGrounded ? -1f : m_fall - 9.81f * Time.deltaTime;

            Vector3 velocity = step * walkSpeed;
            velocity.y = m_fall;
            m_controller.Move(velocity * Time.deltaTime);

            if (enforceFoulLine) HoldBehindFoulLine();
        }

        /// <summary>Frena al jugador en la linea de falta en vez de dejarle pisar la pista.</summary>
        void HoldBehindFoulLine()
        {
            if (player == null || player.lane == null) return;

            Vector3 local = player.lane.ToLaneSpace(transform.position);
            if (local.z <= -0.15f) return;

            local.z = -0.15f;
            Vector3 corrected = player.lane.transform.TransformPoint(local);
            corrected.y = transform.position.y;
            transform.position = corrected;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        void ReadLegacyInput()
        {
            SetMove(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));
            SetLook(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")));

            if (Input.GetKeyDown(KeyCode.E)) PickUp();

            if (Input.GetMouseButtonDown(0))
            {
                if (!HasBall) PickUp();
                BeginThrow();
            }
            if (Input.GetMouseButtonUp(0)) EndThrow();

            if (Input.GetKey(KeyCode.Q)) SetSpin(-1f);
            else if (Input.GetKey(KeyCode.R)) SetSpin(1f);
            else SetSpin(0f);
        }
#endif
    }
}
