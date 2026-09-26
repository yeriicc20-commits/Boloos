using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// La partida: reparte pistas entre los jugadores. Cuando entra alguien se
    /// le da una pista libre y, si no hay, se construye una nueva ahi mismo con
    /// la misma paleta y las mismas medidas, asi que le sale identica a las
    /// demas: superficie, canaletas, flechas, los 10 bolos, el foso y su maquina
    /// de retorno con sus bolas.
    ///
    /// No sabe nada de red a proposito. Una capa de red (Netcode, Mirror,
    /// Photon) solo tiene que llamar a Join cuando un cliente se conecta y a
    /// Leave cuando se va, y replicar la posicion del personaje y los tiros.
    /// </summary>
    public class BoloosMatch : MonoBehaviour
    {
        [Tooltip("Como se construye cada pista. Todas salen de aqui, por eso son iguales.")]
        public AlleyBuildSettings settings = new AlleyBuildSettings();

        [Tooltip("Tope de pistas, y por tanto de jugadores a la vez.")]
        [Range(1, 24)] public int maxLanes = 8;

        [Tooltip("Pistas ya montadas al arrancar, antes de que entre nadie.")]
        [Range(0, 8)] public int startingLanes = 1;

        [Tooltip("Mete un jugador local al arrancar, para poder jugar sin red.")]
        public bool joinLocalPlayerOnStart = true;

        [Tooltip("Da control y camara al jugador local.")]
        public bool spawnControllerForLocal = true;

        public event Action<BoloosPlayer> playerJoined;
        public event Action<BoloosPlayer> playerLeft;

        public IList<BoloosPlayer> Players { get { return m_players; } }
        public int LaneCount { get { return m_lanes.Count; } }

        readonly List<BoloosPlayer> m_players = new List<BoloosPlayer>();
        readonly List<BowlingLane> m_lanes = new List<BowlingLane>();
        readonly Dictionary<BowlingLane, BoloosPlayer> m_occupied = new Dictionary<BowlingLane, BoloosPlayer>();

        AlleyBuildResult m_alley;
        AlleyFactory.Palette m_palette;
        int m_nextPlayerId;

        void Awake()
        {
            BuildAlley();
        }

        void Start()
        {
            if (joinLocalPlayerOnStart) Join("Jugador 1", true);
        }

        /// <summary>
        /// Mete un jugador: le asigna pista (construyendola si hace falta), le
        /// planta el personaje en su aproximacion y le da su color.
        /// Devuelve null si ya no quedan pistas.
        /// </summary>
        public BoloosPlayer Join(string playerName, bool isLocal)
        {
            BowlingLane lane = ClaimLane();
            if (lane == null)
            {
                Debug.LogWarning("[Boloos] No queda pista libre: la partida esta al tope de " + maxLanes + ".");
                return null;
            }

            int index = m_nextPlayerId++;
            Color color = PlayerFactory.ColorFor(index);

            GameObject character = PlayerFactory.CreateCharacter(m_alley, color, "Jugador " + (index + 1));
            character.transform.SetParent(transform, true);
            PlaceOnApproach(character.transform, lane);

            var player = character.AddComponent<BoloosPlayer>();
            player.playerId = index;
            player.playerName = string.IsNullOrEmpty(playerName) ? "Jugador " + (index + 1) : playerName;
            player.color = color;
            player.isLocal = isLocal;
            player.lane = lane;

            if (isLocal && spawnControllerForLocal)
            {
                var controller = character.AddComponent<BoloosPlayerController>();
                controller.player = player;
            }

            m_players.Add(player);
            m_occupied[lane] = player;

            if (playerJoined != null) playerJoined(player);
            return player;
        }

        /// <summary>Saca a un jugador y libera su pista para el siguiente.</summary>
        public void Leave(BoloosPlayer player)
        {
            if (player == null) return;

            if (player.lane != null) m_occupied.Remove(player.lane);
            m_players.Remove(player);

            if (playerLeft != null) playerLeft(player);
            Destroy(player.gameObject);
        }

        public BoloosPlayer PlayerOn(BowlingLane lane)
        {
            BoloosPlayer player;
            return lane != null && m_occupied.TryGetValue(lane, out player) ? player : null;
        }

        /// <summary>Busca pista libre y, si no hay, construye la siguiente.</summary>
        BowlingLane ClaimLane()
        {
            for (int i = 0; i < m_lanes.Count; i++)
            {
                if (!m_occupied.ContainsKey(m_lanes[i])) return m_lanes[i];
            }

            if (m_lanes.Count >= maxLanes) return null;
            return BuildLane(m_lanes.Count);
        }

        void BuildAlley()
        {
            if (m_alley != null) return;

            // El local se dimensiona para el tope de pistas, para que las que se
            // anadan luego tengan suelo debajo. Las pistas las crea este
            // componente una a una, asi que laneCount no se usa aqui.
            settings.houseLanes = Mathf.Max(settings.houseLanes, maxLanes);

            m_alley = new AlleyBuildResult();
            m_alley.root = new GameObject("Bolera");
            m_alley.root.transform.SetParent(transform, false);

            m_palette = AlleyFactory.CreatePalette(m_alley, settings.seed);
            if (settings.buildHouse) AlleyFactory.BuildHouse(m_alley, m_palette, settings);

            int initial = Mathf.Clamp(startingLanes, 0, maxLanes);
            for (int i = 0; i < initial; i++) BuildLane(i);
        }

        BowlingLane BuildLane(int index)
        {
            BowlingLane lane = AlleyFactory.BuildLane(m_alley, m_palette, settings, index);
            m_lanes.Add(lane);
            return lane;
        }

        /// <summary>Deja al personaje de pie en la aproximacion, mirando a los bolos.</summary>
        static void PlaceOnApproach(Transform character, BowlingLane lane)
        {
            Vector3 spot = lane.transform.TransformPoint(new Vector3(0f, 0f, -2.6f));
            character.SetPositionAndRotation(spot, lane.transform.rotation);
        }
    }
}
