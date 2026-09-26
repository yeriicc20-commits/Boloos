using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Personaje de jugador generado por codigo, con el mismo enfoque que el
    /// resto: nada de modelos importados. Cada jugador sale identico salvo el
    /// color, que es lo que los distingue en la bolera.
    /// </summary>
    public static class PlayerFactory
    {
        public const float Height = 1.75f;
        public const float EyeHeight = 1.62f;
        public const float Radius = 0.30f;

        static readonly Color Skin = new Color(0.86f, 0.68f, 0.55f);
        static readonly Color Trousers = new Color(0.17f, 0.19f, 0.26f);
        static readonly Color Shoes = new Color(0.12f, 0.12f, 0.14f);

        /// <summary>Colores de camiseta, uno por jugador que entra.</summary>
        public static readonly Color[] Colors =
        {
            new Color(0.85f, 0.25f, 0.22f), new Color(0.20f, 0.45f, 0.80f),
            new Color(0.25f, 0.65f, 0.35f), new Color(0.90f, 0.70f, 0.20f),
            new Color(0.60f, 0.30f, 0.70f), new Color(0.20f, 0.70f, 0.70f),
            new Color(0.90f, 0.45f, 0.15f), new Color(0.80f, 0.40f, 0.60f)
        };

        public static Color ColorFor(int index)
        {
            return Colors[Mathf.Abs(index) % Colors.Length];
        }

        /// <summary>
        /// Monta el personaje: piernas, tronco, brazos y cabeza, con el
        /// CharacterController ya puesto y la camara a la altura de los ojos.
        /// </summary>
        public static GameObject CreateCharacter(AlleyBuildResult assets, Color shirt, string name)
        {
            var root = new GameObject(name);

            var controller = root.AddComponent<CharacterController>();
            controller.height = Height;
            controller.radius = Radius;
            controller.center = new Vector3(0f, Height * 0.5f, 0f);

            var body = new GameObject("Cuerpo");
            body.transform.SetParent(root.transform, false);

            Material shirtMat = assets.Track(AlleyMaterials.Create("BoloosShirt", shirt, 0.25f));
            Material skinMat = assets.Track(AlleyMaterials.Create("BoloosSkin", Skin, 0.20f));
            Material trouserMat = assets.Track(AlleyMaterials.Create("BoloosTrousers", Trousers, 0.20f));
            Material shoeMat = assets.Track(AlleyMaterials.Create("BoloosShoes", Shoes, 0.35f));

            // piernas
            Mesh leg = assets.Track(MeshBuilder.Lathe(new[]
            {
                new Vector2(0.000f, 0.00f), new Vector2(0.075f, 0.00f),
                new Vector2(0.080f, 0.20f), new Vector2(0.090f, 0.55f),
                new Vector2(0.105f, 0.78f), new Vector2(0.095f, 0.88f),
                new Vector2(0.000f, 0.88f)
            }, 16, "BoloosLeg"));

            Mesh shoe = assets.Track(MeshBuilder.Box(new Vector3(0.12f, 0.07f, 0.26f), "BoloosShoe"));

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Piece(body.transform, "Pierna", new Vector3(side * 0.11f, 0f, 0f), leg, trouserMat);
                Piece(body.transform, "Zapato", new Vector3(side * 0.11f, 0.035f, 0.04f), shoe, shoeMat);
            }

            // tronco
            Mesh torso = assets.Track(MeshBuilder.Lathe(new[]
            {
                new Vector2(0.000f, 0.86f), new Vector2(0.140f, 0.86f),
                new Vector2(0.165f, 1.00f), new Vector2(0.185f, 1.18f),
                new Vector2(0.180f, 1.34f), new Vector2(0.150f, 1.44f),
                new Vector2(0.090f, 1.48f), new Vector2(0.000f, 1.48f)
            }, 20, "BoloosTorso"));
            Piece(body.transform, "Tronco", Vector3.zero, torso, shirtMat);

            // Brazos. El perfil va de la mano (abajo) al hombro (arriba): mas
            // ancho arriba, y el hombro se cierra dentro del tronco. Y se
            // inclinan metiendo la parte de arriba hacia dentro, o quedaria un
            // hueco entre el hombro y el tronco.
            Mesh arm = assets.Track(MeshBuilder.Lathe(new[]
            {
                new Vector2(0.000f, 0.00f), new Vector2(0.044f, 0.02f),
                new Vector2(0.042f, 0.10f), new Vector2(0.046f, 0.28f),
                new Vector2(0.055f, 0.44f), new Vector2(0.058f, 0.48f),
                new Vector2(0.000f, 0.52f)
            }, 14, "BoloosArm"));

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                GameObject limb = Piece(body.transform, "Brazo",
                    new Vector3(side * 0.190f, 0.90f, 0f), arm, shirtMat);
                limb.transform.localRotation = Quaternion.Euler(0f, 0f, side * 6f);
            }

            // cuello y cabeza
            Mesh neck = assets.Track(MeshBuilder.Lathe(new[]
            {
                new Vector2(0.000f, 1.44f), new Vector2(0.050f, 1.44f),
                new Vector2(0.048f, 1.52f), new Vector2(0.000f, 1.52f)
            }, 12, "BoloosNeck"));
            Piece(body.transform, "Cuello", Vector3.zero, neck, skinMat);

            Mesh head = assets.Track(MeshBuilder.Sphere(0.105f, 20, 14, "BoloosHead"));
            GameObject headGo = Piece(body.transform, "Cabeza", new Vector3(0f, 1.62f, 0f), head, skinMat);
            headGo.transform.localScale = new Vector3(1f, 1.15f, 1f);

            // punto de vista y de la mano que suelta la bola
            var eyes = new GameObject("Camara");
            eyes.transform.SetParent(root.transform, false);
            eyes.transform.localPosition = new Vector3(0f, EyeHeight, 0f);

            var hand = new GameObject("Mano");
            hand.transform.SetParent(root.transform, false);
            hand.transform.localPosition = new Vector3(0.24f, 0.95f, 0.30f);

            return root;
        }

        /// <summary>Busca el hijo por nombre sin depender del orden de la jerarquia.</summary>
        public static Transform Find(GameObject character, string childName)
        {
            Transform[] all = character.GetComponentsInChildren<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == childName) return all[i];
            }
            return null;
        }

        static GameObject Piece(Transform parent, string name, Vector3 position, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
