using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Boloos.UI
{
    /// <summary>
    /// Piezas de interfaz construidas por codigo, con una paleta unica para que
    /// menu, ajustes y HUD parezcan lo mismo. Todo se monta sobre un canvas
    /// escalado a 1920x1080, asi que se ve igual de nitido en cualquier
    /// resolucion.
    /// </summary>
    public static class BoloosUIKit
    {
        public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        public static readonly Color Backdrop = new Color(0.035f, 0.030f, 0.060f, 0.94f);
        public static readonly Color Panel = new Color(0.075f, 0.065f, 0.115f, 0.98f);
        public static readonly Color PanelSoft = new Color(0.11f, 0.10f, 0.17f, 1f);
        public static readonly Color Accent = new Color(0.21f, 0.91f, 1f);
        public static readonly Color AccentDim = new Color(0.21f, 0.91f, 1f, 0.22f);
        public static readonly Color Magenta = new Color(1f, 0.24f, 0.75f);
        public static readonly Color Text = new Color(0.93f, 0.94f, 0.97f);
        public static readonly Color TextDim = new Color(0.62f, 0.64f, 0.73f);

        static Font s_font;

        /// <summary>Fuente del sistema. El nombre cambio de version, se prueban los dos.</summary>
        public static Font Font
        {
            get
            {
                if (s_font != null) return s_font;
                s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (s_font == null) s_font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return s_font;
            }
        }

        // ------------------------------------------------------------------
        // Estructura
        // ------------------------------------------------------------------

        /// <summary>Canvas a pantalla completa, escalado a 1920x1080.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            // 0.5 reparte el ajuste entre ancho y alto: no se corta ni en 21:9 ni en 4:3.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            return canvas;
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Estira el elemento a todo el padre, con un margen opcional.</summary>
        public static RectTransform Stretch(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return rect;
        }

        /// <summary>Fija el elemento a una esquina o borde, con tamano propio.</summary>
        public static RectTransform Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot,
                                           Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Box(Transform parent, string name, Color color)
        {
            RectTransform rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Columna con separacion y margenes, para no colocar a mano.</summary>
        public static VerticalLayoutGroup Column(Transform parent, string name, float spacing, RectOffset padding)
        {
            RectTransform rect = Node(name, parent);
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding;
            group.childAlignment = TextAnchor.UpperCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }

        public static HorizontalLayoutGroup Row(Transform parent, string name, float spacing)
        {
            RectTransform rect = Node(name, parent);
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;
            return group;
        }

        public static LayoutElement Size(GameObject go, float width, float height, bool flexibleWidth = false)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            if (width > 0f) element.preferredWidth = width;
            if (height > 0f) element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth ? 1f : 0f;
            return element;
        }

        // ------------------------------------------------------------------
        // Contenido
        // ------------------------------------------------------------------

        public static Text Label(Transform parent, string name, string content, int size,
                                 TextAnchor anchor, Color color, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = Node(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Boton plano con barra de acento que se enciende al pasar por encima.</summary>
        public static Button TextButton(Transform parent, string content, UnityAction onClick,
                                        int fontSize = 30, bool primary = false)
        {
            RectTransform rect = Node("Boton " + content, parent);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = primary ? new Color(0.21f, 0.91f, 1f, 0.16f) : PanelSoft;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.5f, 1.5f, 1.5f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Image bar = Box(rect, "Acento", primary ? Accent : AccentDim);
            Anchor(bar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(4f, 0f));
            bar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            bar.rectTransform.offsetMin = new Vector2(0f, 0f);
            bar.rectTransform.offsetMax = new Vector2(4f, 0f);

            Text label = Label(rect, "Texto", content, fontSize, TextAnchor.MiddleLeft,
                               primary ? Accent : Text, FontStyle.Bold);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(26f, 0f);
            label.rectTransform.offsetMax = new Vector2(-26f, 0f);

            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>
        /// Fila de ajuste con su nombre a la izquierda y el control a la derecha.
        /// Devuelve el hueco donde va el control.
        /// </summary>
        public static RectTransform SettingRow(Transform parent, string title, out Text value)
        {
            HorizontalLayoutGroup row = Row(parent, "Ajuste " + title, 16f);
            Size(row.gameObject, 0f, 62f, true);

            Image background = Box(row.transform, "Fondo", new Color(1f, 1f, 1f, 0.03f));
            Stretch(background.rectTransform);
            background.transform.SetAsFirstSibling();
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;

            Text name = Label(row.transform, "Nombre", title, 26, TextAnchor.MiddleLeft, Text);
            Size(name.gameObject, 330f, 62f);

            RectTransform slot = Node("Control", row.transform);
            Size(slot.gameObject, 0f, 62f, true);

            value = Label(row.transform, "Valor", "", 26, TextAnchor.MiddleRight, Accent, FontStyle.Bold);
            Size(value.gameObject, 130f, 62f);

            return slot;
        }

        /// <summary>Barra deslizante sin adornos, pensada para leerse de un vistazo.</summary>
        public static Slider SliderControl(Transform parent, float min, float max, float value, UnityAction<float> onChanged)
        {
            RectTransform rect = Node("Slider", parent);
            Stretch(rect);

            Image track = Box(rect, "Riel", new Color(1f, 1f, 1f, 0.12f));
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.offsetMin = new Vector2(0f, -3f);
            track.rectTransform.offsetMax = new Vector2(0f, 3f);

            RectTransform fillArea = Node("Relleno", rect);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(0f, -3f);
            fillArea.offsetMax = new Vector2(0f, 3f);

            Image fill = Box(fillArea, "Barra", Accent);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            RectTransform handleArea = Node("Mango", rect);
            Stretch(handleArea);
            Image handle = Box(handleArea, "Tirador", Text);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(14f, 30f);

            var slider = rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        /// <summary>
        /// Interruptor y selector se hacen con botones en vez de con Toggle o
        /// Dropdown: se montan enteros por codigo y no dependen de plantillas.
        /// </summary>
        public static Button SmallButton(Transform parent, string content, float width, UnityAction onClick)
        {
            RectTransform rect = Node("Boton " + content, parent);
            Size(rect.gameObject, width, 44f);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = PanelSoft;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = Label(rect, "Texto", content, 24, TextAnchor.MiddleCenter, Text, FontStyle.Bold);
            Stretch(label.rectTransform);

            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }
    }
}
