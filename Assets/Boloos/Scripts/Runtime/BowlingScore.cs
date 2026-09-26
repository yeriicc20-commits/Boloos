using System.Collections.Generic;

namespace Boloos
{
    /// <summary>
    /// Puntuacion de bolos de 10 bolos: 10 frames, dos tiros por frame salvo
    /// pleno, y hasta tres en el decimo. Un pleno suma 10 mas los dos tiros
    /// siguientes; un semipleno, 10 mas el siguiente.
    ///
    /// No es un MonoBehaviour a proposito: asi se puede probar y sincronizar por
    /// red sin arrastrar una escena detras.
    /// </summary>
    public class BowlingScore
    {
        public const int Frames = 10;
        public const int AllPins = 10;

        readonly List<int> m_rolls = new List<int>();

        /// <summary>Tiros registrados, en orden.</summary>
        public IList<int> Rolls { get { return m_rolls; } }

        /// <summary>Apunta un tiro. Devuelve false si la partida ya estaba acabada.</summary>
        public bool Roll(int pins)
        {
            if (IsComplete) return false;
            if (pins < 0) pins = 0;
            if (pins > AllPins) pins = AllPins;

            m_rolls.Add(pins);
            return true;
        }

        public void Reset()
        {
            m_rolls.Clear();
        }

        /// <summary>Puntuacion total con lo tirado hasta ahora.</summary>
        public int Total
        {
            get
            {
                int total = 0;
                for (int frame = 0; frame < Frames; frame++)
                {
                    int score = FrameScore(frame);
                    if (score < 0) break;
                    total += score;
                }
                return total;
            }
        }

        /// <summary>
        /// Puntos de un frame (0..9), o -1 si todavia faltan tiros para cerrarlo.
        /// Un pleno no puntua hasta que se conocen los dos tiros siguientes.
        /// </summary>
        public int FrameScore(int frame)
        {
            int i = FirstRollOf(frame);
            if (i < 0 || i >= m_rolls.Count) return -1;

            if (m_rolls[i] == AllPins)
            {
                if (i + 2 >= m_rolls.Count) return -1;
                return AllPins + m_rolls[i + 1] + m_rolls[i + 2];
            }

            if (i + 1 >= m_rolls.Count) return -1;

            if (m_rolls[i] + m_rolls[i + 1] == AllPins)
            {
                if (i + 2 >= m_rolls.Count) return -1;
                return AllPins + m_rolls[i + 2];
            }

            return m_rolls[i] + m_rolls[i + 1];
        }

        /// <summary>Frame en curso, de 1 a 10.</summary>
        public int CurrentFrame
        {
            get
            {
                if (IsComplete) return Frames;

                // El frame en curso es el primero sin cerrar, no el primero
                // empezado: con un solo tiro de un frame abierto, el frame sigue
                // siendo ese.
                int i = 0;
                for (int frame = 0; frame < Frames - 1; frame++)
                {
                    if (i >= m_rolls.Count) return frame + 1;

                    if (m_rolls[i] == AllPins)
                    {
                        i += 1;
                    }
                    else
                    {
                        if (i + 1 >= m_rolls.Count) return frame + 1;
                        i += 2;
                    }
                }
                return Frames;
            }
        }

        /// <summary>Tiro dentro del frame en curso: 1, 2 o 3 en el decimo.</summary>
        public int CurrentBall
        {
            get
            {
                if (IsComplete) return 1;
                int start = FirstRollOf(CurrentFrame - 1);
                if (start < 0) return 1;
                return m_rolls.Count - start + 1;
            }
        }

        /// <summary>Bolos que quedan en pie para el tiro que viene.</summary>
        public int PinsStanding
        {
            get
            {
                if (IsComplete) return AllPins;

                int start = FirstRollOf(CurrentFrame - 1);
                if (start < 0 || start >= m_rolls.Count) return AllPins;

                int standing = AllPins;
                for (int i = start; i < m_rolls.Count; i++)
                {
                    standing -= m_rolls[i];
                    // Un pleno o un semipleno replanta los 10, tambien en el decimo.
                    if (standing <= 0) standing = AllPins;
                }
                return standing;
            }
        }

        public bool IsComplete
        {
            get
            {
                int tenth = FirstRollOf(Frames - 1);
                if (tenth < 0 || tenth >= m_rolls.Count) return false;

                int rolled = m_rolls.Count - tenth;
                bool bonus = m_rolls[tenth] == AllPins ||
                             (rolled >= 2 && m_rolls[tenth] + m_rolls[tenth + 1] == AllPins);

                return rolled >= (bonus ? 3 : 2);
            }
        }

        /// <summary>Indice del primer tiro de un frame, o -1 si aun no se ha llegado.</summary>
        int FirstRollOf(int frame)
        {
            int i = 0;
            for (int f = 0; f < frame; f++)
            {
                if (i >= m_rolls.Count) return -1;
                i += m_rolls[i] == AllPins ? 1 : 2;
            }
            return i;
        }
    }
}
