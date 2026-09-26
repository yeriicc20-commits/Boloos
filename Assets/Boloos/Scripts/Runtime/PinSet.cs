using UnityEngine;

namespace Boloos
{
    /// <summary>Los 10 bolos de una pista: contarlos, retirarlos y replantarlos.</summary>
    public class PinSet : MonoBehaviour
    {
        [Tooltip("Bolos 1 a 10, en orden.")]
        public BowlingPin[] pins = new BowlingPin[10];

        public int StandingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < pins.Length; i++)
                {
                    if (pins[i] != null && !pins[i].IsDown) n++;
                }
                return n;
            }
        }

        /// <summary>Replanta los 10 bolos en sus marcas.</summary>
        public void ResetAll()
        {
            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i] != null) pins[i].ResetToSpot();
            }
        }

        /// <summary>Retira los derribados y deja de pie los que sigan en pie.</summary>
        public void ClearDeadwood()
        {
            for (int i = 0; i < pins.Length; i++)
            {
                if (pins[i] != null && pins[i].IsDown) pins[i].gameObject.SetActive(false);
            }
        }
    }
}
