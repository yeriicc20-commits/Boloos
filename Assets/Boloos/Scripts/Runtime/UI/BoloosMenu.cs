using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Boloos.UI
{
    /// <summary>
    /// Menu, ajustes, pausa y HUD, montados por codigo sobre un canvas escalado
    /// a 1920x1080. No hace falta prefab ni escena de interfaz: se pone este
    /// componente y sale todo.
    /// </summary>
    public class BoloosMenu : MonoBehaviour
    {
        public enum View { Main, Settings, Playing, Paused }

        [Tooltip("La partida. Si se deja vacio se busca en la escena.")]
        public BoloosMatch match;

        [Tooltip("Arrancar en el menu, en vez de directamente jugando.")]
        public bool openOnStart = true;

        public View Current { get; private set; }

        Canvas m_canvas;
        RectTransform m_main;
        RectTransform m_settings;
        RectTransform m_pause;
        RectTransform m_hud;

        Text m_lane, m_frame, m_score, m_hint, m_fps, m_pins;
        RectTransform m_power;
        RectTransform m_powerFill;

        Text m_sensitivityValue, m_volumeValue, m_qualityValue, m_fpsValue, m_vsyncValue, m_fullscreenValue;

        BoloosFps m_counter;
        BoloosPlayerController m_controller;
        View m_returnTo = View.Main;

        void Awake()
        {
            if (match == null) match = FindMatch();

            BoloosSettings.Apply();

            m_counter = GetComponent<BoloosFps>();
            if (m_counter == null) m_counter = gameObject.AddComponent<BoloosFps>();

            EnsureEventSystem();

            m_canvas = BoloosUIKit.CreateCanvas("Boloos UI", 100);
            m_canvas.transform.SetParent(transform, false);

            BuildHud(m_canvas.transform);
            BuildMainMenu(m_canvas.transform);
            BuildSettings(m_canvas.transform);
            BuildPause(m_canvas.transform);
        }

        void Start()
        {
            Show(openOnStart ? View.Main : View.Playing);
        }

        // ------------------------------------------------------------------
        // Navegacion
        // ------------------------------------------------------------------

        public void Show(View view)
        {
            Current = view;

            m_main.gameObject.SetActive(view == View.Main);
            m_settings.gameObject.SetActive(view == View.Settings);
            m_pause.gameObject.SetActive(view == View.Paused);
            m_hud.gameObject.SetActive(view == View.Playing);

            bool playing = view == View.Playing;
            Time.timeScale = playing ? 1f : 0f;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;

            if (m_controller == null) m_controller = FindLocalController();
            if (m_controller != null)
            {
                m_controller.enabled = playing;
                if (playing) ApplySensitivity();
            }
        }

        public void Play() { Show(View.Playing); }

        public void OpenSettings()
        {
            m_returnTo = Current == View.Paused ? View.Paused : View.Main;
            RefreshSettings();
            Show(View.Settings);
        }

        public void CloseSettings() { Show(m_returnTo); }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Current == View.Playing) Show(View.Paused);
                else if (Current == View.Paused) Show(View.Playing);
                else if (Current == View.Settings) CloseSettings();
            }
#endif
            UpdateHud();
        }

        // ------------------------------------------------------------------
        // HUD
        // ------------------------------------------------------------------

        void BuildHud(Transform parent)
        {
            m_hud = BoloosUIKit.Node("HUD", parent);
            BoloosUIKit.Stretch(m_hud);

            // bloque de partida, arriba a la izquierda
            RectTransform block = BoloosUIKit.Node("Partida", m_hud);
            BoloosUIKit.Anchor(block, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -36f), new Vector2(460f, 160f));

            Image bar = BoloosUIKit.Box(block, "Acento", BoloosUIKit.Accent);
            bar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            bar.rectTransform.offsetMin = Vector2.zero;
            bar.rectTransform.offsetMax = new Vector2(4f, 0f);

            m_lane = BoloosUIKit.Label(block, "Pista", "PISTA 1", 30, TextAnchor.UpperLeft,
                BoloosUIKit.Text, FontStyle.Bold);
            BoloosUIKit.Anchor(m_lane.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -4f), new Vector2(420f, 40f));

            m_frame = BoloosUIKit.Label(block, "Frame", "Frame 1  ·  Tiro 1", 22, TextAnchor.UpperLeft, BoloosUIKit.TextDim);
            BoloosUIKit.Anchor(m_frame.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -44f), new Vector2(420f, 30f));

            m_score = BoloosUIKit.Label(block, "Puntos", "0", 62, TextAnchor.UpperLeft,
                BoloosUIKit.Accent, FontStyle.Bold);
            BoloosUIKit.Anchor(m_score.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -78f), new Vector2(420f, 76f));

            m_pins = BoloosUIKit.Label(block, "Bolos", "10 bolos en pie", 20, TextAnchor.UpperLeft, BoloosUIKit.TextDim);
            BoloosUIKit.Anchor(m_pins.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -148f), new Vector2(420f, 28f));

            // contador de FPS, arriba a la derecha
            m_fps = BoloosUIKit.Label(m_hud, "FPS", "", 24, TextAnchor.UpperRight, BoloosUIKit.TextDim, FontStyle.Bold);
            BoloosUIKit.Anchor(m_fps.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(240f, 34f));

            // punto de mira
            Image dot = BoloosUIKit.Box(m_hud, "Mira", new Color(1f, 1f, 1f, 0.75f));
            BoloosUIKit.Anchor(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));

            // barra de fuerza
            m_power = BoloosUIKit.Node("Fuerza", m_hud);
            BoloosUIKit.Anchor(m_power, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(520f, 16f));

            Image back = BoloosUIKit.Box(m_power, "Fondo", new Color(1f, 1f, 1f, 0.12f));
            BoloosUIKit.Stretch(back.rectTransform);

            Image fill = BoloosUIKit.Box(m_power, "Relleno", BoloosUIKit.Magenta);
            m_powerFill = fill.rectTransform;
            m_powerFill.anchorMin = Vector2.zero;
            m_powerFill.anchorMax = new Vector2(0f, 1f);
            m_powerFill.offsetMin = Vector2.zero;
            m_powerFill.offsetMax = Vector2.zero;

            m_hint = BoloosUIKit.Label(m_hud, "Aviso", "", 24, TextAnchor.LowerCenter, BoloosUIKit.TextDim);
            BoloosUIKit.Anchor(m_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(1200f, 34f));
        }

        void UpdateHud()
        {
            if (m_fps != null)
            {
                bool show = BoloosSettings.ShowFps;
                m_fps.enabled = show;
                if (show && m_counter != null)
                {
                    m_fps.text = Mathf.RoundToInt(m_counter.Fps) + " FPS   ·   " +
                                 m_counter.MillisecondsPerFrame.ToString("0.0") + " ms";
                    m_fps.color = m_counter.Tint;
                }
            }

            if (Current != View.Playing) return;

            BoloosPlayer player = LocalPlayer();
            if (player == null) return;

            if (player.lane != null) m_lane.text = "PISTA " + player.lane.laneNumber;

            BowlingScore score = player.Score;
            m_frame.text = score.IsComplete
                ? "Partida terminada"
                : "Frame " + score.CurrentFrame + "  ·  Tiro " + score.CurrentBall;
            m_score.text = score.Total.ToString();
            m_pins.text = player.StandingPins + " bolos en pie";

            if (m_controller == null) m_controller = FindLocalController();

            float charge = m_controller != null ? m_controller.Charge : 0f;
            bool charging = charge > 0.001f;
            m_power.gameObject.SetActive(charging);
            if (charging) m_powerFill.anchorMax = new Vector2(charge, 1f);

            if (player.IsRolling) m_hint.text = "";
            else if (m_controller != null && m_controller.HasBall) m_hint.text = "Manten el clic para cargar y suelta para lanzar  ·  Q y R para el efecto";
            else if (player.BallsInRack > 0) m_hint.text = "Acercate al estante y pulsa E para coger una bola";
            else m_hint.text = "Esperando a que vuelva la bola";
        }

        // ------------------------------------------------------------------
        // Menus
        // ------------------------------------------------------------------

        void BuildMainMenu(Transform parent)
        {
            m_main = BoloosUIKit.Node("Menu", parent);
            BoloosUIKit.Stretch(m_main);

            Image backdrop = BoloosUIKit.Box(m_main, "Fondo", BoloosUIKit.Backdrop);
            BoloosUIKit.Stretch(backdrop.rectTransform);

            RectTransform card = BoloosUIKit.Node("Tarjeta", m_main);
            BoloosUIKit.Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 720f));

            Image panel = BoloosUIKit.Box(card, "Panel", BoloosUIKit.Panel);
            BoloosUIKit.Stretch(panel.rectTransform);

            Image top = BoloosUIKit.Box(card, "Neon", BoloosUIKit.Accent);
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.offsetMin = new Vector2(0f, -5f);
            top.rectTransform.offsetMax = Vector2.zero;

            Text title = BoloosUIKit.Label(card, "Titulo", "BOLOOS", 104, TextAnchor.UpperCenter,
                BoloosUIKit.Text, FontStyle.Bold);
            BoloosUIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(600f, 120f));

            Text subtitle = BoloosUIKit.Label(card, "Subtitulo", "B O L E R A", 24, TextAnchor.UpperCenter,
                BoloosUIKit.Accent);
            BoloosUIKit.Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(600f, 32f));

            VerticalLayoutGroup column = BoloosUIKit.Column(card.transform, "Botones", 18f, new RectOffset(70, 70, 0, 0));
            BoloosUIKit.Anchor(column.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -70f), new Vector2(680f, 300f));

            Button play = BoloosUIKit.TextButton(column.transform, "JUGAR", Play, 32, true);
            BoloosUIKit.Size(play.gameObject, 0f, 78f, true);

            Button settings = BoloosUIKit.TextButton(column.transform, "AJUSTES", OpenSettings);
            BoloosUIKit.Size(settings.gameObject, 0f, 66f, true);

            Button quit = BoloosUIKit.TextButton(column.transform, "SALIR", Quit);
            BoloosUIKit.Size(quit.gameObject, 0f, 66f, true);

            Text footer = BoloosUIKit.Label(card, "Pie", "WASD para moverte  ·  E para coger bola  ·  Esc para pausa",
                20, TextAnchor.LowerCenter, BoloosUIKit.TextDim);
            BoloosUIKit.Anchor(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(620f, 28f));
        }

        void BuildPause(Transform parent)
        {
            m_pause = BoloosUIKit.Node("Pausa", parent);
            BoloosUIKit.Stretch(m_pause);

            Image backdrop = BoloosUIKit.Box(m_pause, "Fondo", BoloosUIKit.Backdrop);
            BoloosUIKit.Stretch(backdrop.rectTransform);

            RectTransform card = BoloosUIKit.Node("Tarjeta", m_pause);
            BoloosUIKit.Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 460f));

            Image panel = BoloosUIKit.Box(card, "Panel", BoloosUIKit.Panel);
            BoloosUIKit.Stretch(panel.rectTransform);

            Text title = BoloosUIKit.Label(card, "Titulo", "PAUSA", 56, TextAnchor.UpperCenter,
                BoloosUIKit.Text, FontStyle.Bold);
            BoloosUIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(520f, 70f));

            VerticalLayoutGroup column = BoloosUIKit.Column(card.transform, "Botones", 16f, new RectOffset(60, 60, 0, 0));
            BoloosUIKit.Anchor(column.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -44f), new Vector2(620f, 260f));

            BoloosUIKit.Size(BoloosUIKit.TextButton(column.transform, "REANUDAR", Play, 30, true).gameObject, 0f, 70f, true);
            BoloosUIKit.Size(BoloosUIKit.TextButton(column.transform, "AJUSTES", OpenSettings).gameObject, 0f, 62f, true);
            BoloosUIKit.Size(BoloosUIKit.TextButton(column.transform, "SALIR AL MENU", delegate { Show(View.Main); }).gameObject, 0f, 62f, true);
        }

        void BuildSettings(Transform parent)
        {
            m_settings = BoloosUIKit.Node("Ajustes", parent);
            BoloosUIKit.Stretch(m_settings);

            Image backdrop = BoloosUIKit.Box(m_settings, "Fondo", BoloosUIKit.Backdrop);
            BoloosUIKit.Stretch(backdrop.rectTransform);

            RectTransform card = BoloosUIKit.Node("Tarjeta", m_settings);
            BoloosUIKit.Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 760f));

            Image panel = BoloosUIKit.Box(card, "Panel", BoloosUIKit.Panel);
            BoloosUIKit.Stretch(panel.rectTransform);

            Image top = BoloosUIKit.Box(card, "Neon", BoloosUIKit.Accent);
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.offsetMin = new Vector2(0f, -5f);
            top.rectTransform.offsetMax = Vector2.zero;

            Text title = BoloosUIKit.Label(card, "Titulo", "AJUSTES", 48, TextAnchor.UpperLeft,
                BoloosUIKit.Text, FontStyle.Bold);
            BoloosUIKit.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -44f), new Vector2(600f, 62f));

            VerticalLayoutGroup column = BoloosUIKit.Column(card.transform, "Filas", 12f, new RectOffset(56, 56, 0, 0));
            BoloosUIKit.Anchor(column.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -132f), new Vector2(860f, 500f));

            RectTransform slot = BoloosUIKit.SettingRow(column.transform, "Sensibilidad del raton", out m_sensitivityValue);
            BoloosUIKit.SliderControl(slot, 0.2f, 4f, BoloosSettings.Sensitivity, delegate(float v)
            {
                BoloosSettings.Sensitivity = v;
                ApplySensitivity();
                RefreshSettings();
            });

            slot = BoloosUIKit.SettingRow(column.transform, "Volumen", out m_volumeValue);
            BoloosUIKit.SliderControl(slot, 0f, 1f, BoloosSettings.Volume, delegate(float v)
            {
                BoloosSettings.Volume = v;
                RefreshSettings();
            });

            slot = BoloosUIKit.SettingRow(column.transform, "Calidad", out m_qualityValue);
            HorizontalLayoutGroup quality = BoloosUIKit.Row(slot, "Selector", 10f);
            BoloosUIKit.Stretch(quality.GetComponent<RectTransform>());
            quality.childAlignment = TextAnchor.MiddleRight;
            BoloosUIKit.SmallButton(quality.transform, "<", 54f, delegate { CycleQuality(-1); });
            BoloosUIKit.SmallButton(quality.transform, ">", 54f, delegate { CycleQuality(1); });

            slot = BoloosUIKit.SettingRow(column.transform, "Mostrar FPS", out m_fpsValue);
            HorizontalLayoutGroup fps = BoloosUIKit.Row(slot, "Selector", 10f);
            BoloosUIKit.Stretch(fps.GetComponent<RectTransform>());
            fps.childAlignment = TextAnchor.MiddleRight;
            BoloosUIKit.SmallButton(fps.transform, "CAMBIAR", 150f, delegate
            {
                BoloosSettings.ShowFps = !BoloosSettings.ShowFps;
                RefreshSettings();
            });

            slot = BoloosUIKit.SettingRow(column.transform, "Sincronizacion vertical", out m_vsyncValue);
            HorizontalLayoutGroup vsync = BoloosUIKit.Row(slot, "Selector", 10f);
            BoloosUIKit.Stretch(vsync.GetComponent<RectTransform>());
            vsync.childAlignment = TextAnchor.MiddleRight;
            BoloosUIKit.SmallButton(vsync.transform, "CAMBIAR", 150f, delegate
            {
                BoloosSettings.VSync = !BoloosSettings.VSync;
                RefreshSettings();
            });

            slot = BoloosUIKit.SettingRow(column.transform, "Pantalla completa", out m_fullscreenValue);
            HorizontalLayoutGroup full = BoloosUIKit.Row(slot, "Selector", 10f);
            BoloosUIKit.Stretch(full.GetComponent<RectTransform>());
            full.childAlignment = TextAnchor.MiddleRight;
            BoloosUIKit.SmallButton(full.transform, "CAMBIAR", 150f, delegate
            {
                BoloosSettings.Fullscreen = !BoloosSettings.Fullscreen;
                RefreshSettings();
            });

            Button back = BoloosUIKit.TextButton(card, "VOLVER", CloseSettings, 30, true);
            BoloosUIKit.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 46f), new Vector2(740f, 70f));

            RefreshSettings();
        }

        void RefreshSettings()
        {
            if (m_sensitivityValue != null) m_sensitivityValue.text = BoloosSettings.Sensitivity.ToString("0.0") + "x";
            if (m_volumeValue != null) m_volumeValue.text = Mathf.RoundToInt(BoloosSettings.Volume * 100f) + "%";
            if (m_qualityValue != null)
            {
                string[] names = QualitySettings.names;
                int level = Mathf.Clamp(BoloosSettings.Quality, 0, names.Length - 1);
                m_qualityValue.text = names.Length > 0 ? names[level] : "-";
            }
            if (m_fpsValue != null) m_fpsValue.text = BoloosSettings.ShowFps ? "SI" : "NO";
            if (m_vsyncValue != null) m_vsyncValue.text = BoloosSettings.VSync ? "SI" : "NO";
            if (m_fullscreenValue != null) m_fullscreenValue.text = BoloosSettings.Fullscreen ? "SI" : "NO";
        }

        void CycleQuality(int direction)
        {
            int count = QualitySettings.names.Length;
            if (count == 0) return;

            int level = (BoloosSettings.Quality + direction + count) % count;
            BoloosSettings.Quality = level;
            RefreshSettings();
        }

        void ApplySensitivity()
        {
            if (m_controller == null) m_controller = FindLocalController();
            if (m_controller != null) m_controller.lookSensitivity = 2.4f * BoloosSettings.Sensitivity;
        }

        // ------------------------------------------------------------------
        // Busquedas
        // ------------------------------------------------------------------

        BoloosPlayer LocalPlayer()
        {
            if (match == null) return null;
            for (int i = 0; i < match.Players.Count; i++)
            {
                if (match.Players[i] != null && match.Players[i].isLocal) return match.Players[i];
            }
            return null;
        }

        BoloosPlayerController FindLocalController()
        {
            BoloosPlayer player = LocalPlayer();
            return player != null ? player.GetComponent<BoloosPlayerController>() : null;
        }

        static BoloosMatch FindMatch()
        {
            BoloosMatch[] all = Resources.FindObjectsOfTypeAll<BoloosMatch>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject.scene.IsValid()) return all[i];
            }
            return null;
        }

        static void EnsureEventSystem()
        {
            EventSystem[] existing = Resources.FindObjectsOfTypeAll<EventSystem>();
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].gameObject.scene.IsValid()) return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#else
            // Con solo el Input System nuevo, StandaloneInputModule casca: el
            // modulo lo pone el paquete de entrada del propio proyecto.
            Debug.Log("[Boloos] " + go.name + " creado sin modulo de entrada. Anade " +
                      "InputSystemUIInputModule si usas el Input System nuevo.", go);
#endif
        }
    }
}
