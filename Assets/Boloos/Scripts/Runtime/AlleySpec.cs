using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Medidas reglamentarias de una bolera (USBC) convertidas a metros.
    /// Todo lo que genera Boloos sale de aqui, asi que pista, bolos, bola y
    /// maquina de retorno quedan siempre coherentes entre si.
    /// </summary>
    public static class AlleySpec
    {
        public const float Inch = 0.0254f;
        public const float Foot = 12f * Inch;

        // ---------- Pista ----------
        /// <summary>Ancho util de la pista: 41,5" (39 tablas).</summary>
        public const float LaneWidth = 41.5f * Inch;
        /// <summary>Distancia de la linea de falta al bolo 1: 60 pies.</summary>
        public const float FoulLineToHeadPin = 60f * Foot;
        /// <summary>Del bolo 1 al final de la plataforma: 2' 10 3/16".</summary>
        public const float HeadPinToLaneEnd = 34.1875f * Inch;
        public const float LaneLength = FoulLineToHeadPin + HeadPinToLaneEnd;
        public const float LaneThickness = 0.10f;
        public const int BoardCount = 39;
        public const float BoardWidth = LaneWidth / BoardCount;

        /// <summary>
        /// Longitud engrasada desde la falta. Los ultimos metros van secos, y es
        /// justo ahi donde la bola con efecto agarra y curva hacia los bolos.
        /// </summary>
        public const float OiledLength = 42f * Foot;

        // ---------- Canaletas ----------
        public const float GutterWidth = 9.25f * Inch;
        public const float GutterDepth = 1.875f * Inch;

        // ---------- Aproximacion ----------
        public const float ApproachLength = 16f * Foot;

        // ---------- Foso ----------
        public const float PitLength = 1.70f;
        public const float PitDrop = 0.32f;

        // ---------- Bolos ----------
        public const float PinHeight = 15f * Inch;
        public const float PinMaxDiameter = 4.766f * Inch;
        public const float PinBaseDiameter = 2.031f * Inch;
        /// <summary>Separacion entre centros de bolos: 12".</summary>
        public const float PinSpacing = 12f * Inch;
        /// <summary>Peso reglamentario: 3 lb 6 oz.</summary>
        public const float PinMass = 1.53f;
        /// <summary>Separacion entre filas: 12" * sin(60 grados).</summary>
        public static readonly float PinRowSpacing = PinSpacing * 0.8660254f;

        // ---------- Bola ----------
        /// <summary>Diametro reglamentario: 8,5" (27" de circunferencia).</summary>
        public const float BallDiameter = 8.5f * Inch;
        public const float BallRadius = BallDiameter * 0.5f;
        /// <summary>Peso maximo: 16 lb.</summary>
        public const float BallMass = 7.26f;
        public const float FingerHoleDiameter = 1.0f * Inch;
        public const float ThumbHoleDiameter = 1.22f * Inch;
        public const float HoleDepth = 2.2f * Inch;

        // ---------- Mascara del fondo ----------
        /// <summary>
        /// Altura a la que arranca el panel que tapa el fondo. Por debajo pasan
        /// la bola, los bolos y el carril de retorno; por encima no se ve nada
        /// de la maquina.
        /// </summary>
        public const float MaskingBottom = 0.70f;
        public const float MaskingTop = 2.40f;

        // ---------- Distribucion del local ----------
        /// <summary>Hueco entre pistas donde va el retorno de bolas.</summary>
        public const float ReturnGap = 0.80f;
        public const float LanePitch = LaneWidth + 2f * GutterWidth + ReturnGap;

        /// <summary>
        /// Posicion de un bolo (1..10) en coordenadas locales de la pista:
        /// X lateral (0 = centro), Z a lo largo (0 = linea de falta), Y = 0 en la superficie.
        /// </summary>
        public static Vector3 PinPosition(int pinNumber)
        {
            int row, index;
            switch (pinNumber)
            {
                case 1: row = 0; index = 0; break;
                case 2: row = 1; index = 0; break;
                case 3: row = 1; index = 1; break;
                case 4: row = 2; index = 0; break;
                case 5: row = 2; index = 1; break;
                case 6: row = 2; index = 2; break;
                case 7: row = 3; index = 0; break;
                case 8: row = 3; index = 1; break;
                case 9: row = 3; index = 2; break;
                case 10: row = 3; index = 3; break;
                default: return Vector3.zero;
            }

            int pinsInRow = row + 1;
            float x = (index - (pinsInRow - 1) * 0.5f) * PinSpacing;
            float z = FoulLineToHeadPin + row * PinRowSpacing;
            return new Vector3(x, 0f, z);
        }

        /// <summary>Distancias de las 7 flechas de mira, a 12-16 pies de la falta.</summary>
        public static readonly float[] ArrowDistances =
        {
            12f * Foot, 13f * Foot, 14f * Foot, 15f * Foot, 14f * Foot, 13f * Foot, 12f * Foot
        };

        /// <summary>Las flechas van en las tablas 5, 10, 15, 20, 25, 30 y 35.</summary>
        public static float BoardCenterX(int board)
        {
            return -LaneWidth * 0.5f + (board - 0.5f) * BoardWidth;
        }
    }
}
