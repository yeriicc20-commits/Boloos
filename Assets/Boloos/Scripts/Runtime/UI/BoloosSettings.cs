using UnityEngine;

namespace Boloos.UI
{
    /// <summary>
    /// Ajustes del juego, guardados en PlayerPrefs. Se aplican al leerlos, asi
    /// que la partida arranca ya con lo que dejo puesto el jugador.
    /// </summary>
    public static class BoloosSettings
    {
        const string KeyFps = "Boloos.ShowFps";
        const string KeySensitivity = "Boloos.Sensitivity";
        const string KeyVolume = "Boloos.Volume";
        const string KeyQuality = "Boloos.Quality";
        const string KeyVSync = "Boloos.VSync";
        const string KeyFullscreen = "Boloos.Fullscreen";

        public static bool ShowFps
        {
            get { return PlayerPrefs.GetInt(KeyFps, 0) == 1; }
            set { PlayerPrefs.SetInt(KeyFps, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Sensibilidad del raton, de 0,2 a 4.</summary>
        public static float Sensitivity
        {
            get { return PlayerPrefs.GetFloat(KeySensitivity, 1f); }
            set { PlayerPrefs.SetFloat(KeySensitivity, Mathf.Clamp(value, 0.2f, 4f)); PlayerPrefs.Save(); }
        }

        public static float Volume
        {
            get { return PlayerPrefs.GetFloat(KeyVolume, 0.8f); }
            set
            {
                float v = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyVolume, v);
                PlayerPrefs.Save();
                AudioListener.volume = v;
            }
        }

        public static int Quality
        {
            get { return PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel()); }
            set
            {
                int level = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
                PlayerPrefs.SetInt(KeyQuality, level);
                PlayerPrefs.Save();
                QualitySettings.SetQualityLevel(level, true);
            }
        }

        public static bool VSync
        {
            get { return PlayerPrefs.GetInt(KeyVSync, 1) == 1; }
            set
            {
                PlayerPrefs.SetInt(KeyVSync, value ? 1 : 0);
                PlayerPrefs.Save();
                QualitySettings.vSyncCount = value ? 1 : 0;
            }
        }

        public static bool Fullscreen
        {
            get { return PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1; }
            set
            {
                PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
                PlayerPrefs.Save();
                Screen.fullScreen = value;
            }
        }

        /// <summary>Deja el juego como el jugador lo dejo la ultima vez.</summary>
        public static void Apply()
        {
            AudioListener.volume = Volume;
            QualitySettings.SetQualityLevel(Quality, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Screen.fullScreen = Fullscreen;
        }
    }
}
