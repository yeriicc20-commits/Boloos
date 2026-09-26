using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Pieza que gira mientras la maquina trabaja: el acelerador del foso y las
    /// ruedas del elevador. Es lo que hace que el retorno parezca una maquina
    /// y no una caja quieta.
    /// </summary>
    public class SpinningPart : MonoBehaviour
    {
        public Vector3 axis = Vector3.right;
        public float rpm = 220f;
        public bool spinning;

        [Tooltip("Segundos que tarda en arrancar y en pararse.")]
        public float spinUp = 0.35f;

        float m_current;

        void Update()
        {
            float target = spinning ? rpm : 0f;
            m_current = Mathf.MoveTowards(m_current, target, rpm / Mathf.Max(spinUp, 0.01f) * Time.deltaTime);
            if (m_current > 0.01f)
            {
                transform.Rotate(axis.normalized, m_current * 6f * Time.deltaTime, Space.Self);
            }
        }
    }
}
