using System.Collections.Generic;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using PhysMat = UnityEngine.PhysicsMaterial;
#else
using PhysMat = UnityEngine.PhysicMaterial;
#endif

namespace Boloos
{
    /// <summary>Opciones de construccion de la bolera.</summary>
    [System.Serializable]
    public class AlleyBuildSettings
    {
        [Range(1, 16)] public int laneCount = 4;
        [Range(0, 6)] public int ballsPerLane = 3;
        public bool buildPins = true;
        public bool buildBalls = true;
        public bool buildReturnMachine = true;
        public bool buildHouse = true;
        public bool addDemoController = true;
        public int seed = 7;
    }

    /// <summary>Lo que devuelve la construccion: la jerarquia y los recursos generados.</summary>
    public class AlleyBuildResult
    {
        public GameObject root;
        public readonly List<BowlingLane> lanes = new List<BowlingLane>();
        /// <summary>Mallas, materiales y texturas creados: el editor los guarda como assets.</summary>
        public readonly List<Object> assets = new List<Object>();

        public T Track<T>(T asset) where T : Object
        {
            if (asset != null) assets.Add(asset);
            return asset;
        }
    }

    /// <summary>
    /// Construye la bolera entera por codigo: superficie de 39 tablas, canaletas,
    /// bolos torneados, bolas con sus tres agujeros y una maquina de retorno que
    /// de verdad recoge la bola del foso y la devuelve al estante.
    /// </summary>
    public static class AlleyFactory
    {
        // Altura del carril de retorno y profundidad de su canal.
        const float TrackTopY = 0.46f;
        const float TrackDepth = 0.07f;
        const float TrackWidth = 0.34f;
        static float BallTravelY { get { return TrackTopY - TrackDepth + AlleySpec.BallRadius; } }

        // Separacion entre los dos railes del estante, y altura a la que deben
        // quedar para que la bola descanse encima en vez de hundirse en el cuerpo.
        const float RailHalfGap = 0.085f;
        const float RailHeight = 0.06f;
        static float RailTopY
        {
            get
            {
                float r = AlleySpec.BallRadius;
                return BallTravelY - Mathf.Sqrt(r * r - RailHalfGap * RailHalfGap);
            }
        }

        static readonly Color[] BallColors =
        {
            new Color(0.10f, 0.13f, 0.20f),
            new Color(0.62f, 0.10f, 0.14f),
            new Color(0.09f, 0.34f, 0.55f),
            new Color(0.13f, 0.42f, 0.24f),
            new Color(0.48f, 0.16f, 0.52f),
            new Color(0.85f, 0.52f, 0.08f)
        };

        public static AlleyBuildResult Build(AlleyBuildSettings settings)
        {
            if (settings == null) settings = new AlleyBuildSettings();
            var result = new AlleyBuildResult();

            result.root = new GameObject("Bolera");
            Palette palette = CreatePalette(result, settings.seed);

            if (settings.buildHouse) BuildHouse(result, palette, settings);

            for (int i = 0; i < settings.laneCount; i++)
            {
                BuildLane(result, palette, settings, i);
            }

            if (settings.addDemoController)
            {
                var demo = result.root.AddComponent<BoloosDemo>();
                demo.lanes = result.lanes.ToArray();
            }

            return result;
        }

        // ==================================================================
        // Materiales compartidos
        // ==================================================================

        class Palette
        {
            public Material lane, approach, gutter, pin, pit, metal, machine, rubber, foulLine, arrow, carpet, wall, neon;
            public PhysMat lanePhysics, ballPhysics, pinPhysics, gutterPhysics;
            public Mesh pinMesh, pinColliderMesh;
            public Vector2[] pinProfile;
        }

        static Palette CreatePalette(AlleyBuildResult r, int seed)
        {
            var p = new Palette();

            Texture2D wood = r.Track(ProceduralTextures.LaneWood(512, 1024, AlleySpec.BoardCount, null, null, seed));
            Texture2D carpet = r.Track(ProceduralTextures.Carpet(new Color(0.16f, 0.11f, 0.20f), 256, seed));

            // La textura lleva las 39 tablas a lo ancho, asi que solo se repite a lo largo.
            float lengthRepeats = AlleySpec.LaneLength / 2.4f;
            p.lane = r.Track(AlleyMaterials.Create("BoloosLane", Color.white, 0.82f, 0f, wood, new Vector2(1f, lengthRepeats)));
            p.approach = r.Track(AlleyMaterials.Create("BoloosApproach", new Color(0.86f, 0.80f, 0.72f), 0.55f, 0f, wood,
                new Vector2(1f, AlleySpec.ApproachLength / 2.4f)));
            p.carpet = r.Track(AlleyMaterials.Create("BoloosCarpet", Color.white, 0.08f, 0f, carpet, new Vector2(8f, 8f)));

            p.gutter = r.Track(AlleyMaterials.Create("BoloosGutter", new Color(0.13f, 0.14f, 0.16f), 0.62f, 0.75f));
            p.pit = r.Track(AlleyMaterials.Create("BoloosPit", new Color(0.09f, 0.09f, 0.10f), 0.25f, 0.2f));
            p.metal = r.Track(AlleyMaterials.Create("BoloosMetal", new Color(0.55f, 0.57f, 0.60f), 0.70f, 0.9f));
            p.machine = r.Track(AlleyMaterials.Create("BoloosMachine", new Color(0.22f, 0.24f, 0.27f), 0.45f, 0.5f));
            p.rubber = r.Track(AlleyMaterials.Create("BoloosRubber", new Color(0.07f, 0.07f, 0.08f), 0.18f, 0f));
            p.foulLine = r.Track(AlleyMaterials.Create("BoloosFoulLine", new Color(0.92f, 0.22f, 0.16f), 0.4f, 0f));
            p.arrow = r.Track(AlleyMaterials.Create("BoloosArrow", new Color(0.24f, 0.13f, 0.06f), 0.5f, 0f));
            p.wall = r.Track(AlleyMaterials.Create("BoloosWall", new Color(0.13f, 0.12f, 0.17f), 0.15f, 0f));
            p.neon = r.Track(AlleyMaterials.CreateEmissive("BoloosNeon", new Color(0.35f, 0.65f, 1f), 3f));

            // Fisica: la pista va aceitada, la canaleta agarra un poco mas.
            p.lanePhysics = r.Track(NewPhysics("BoloosLanePhysics", 0.04f, 0.03f, 0f));
            p.ballPhysics = r.Track(NewPhysics("BoloosBallPhysics", 0.14f, 0.10f, 0.02f));
            p.pinPhysics = r.Track(NewPhysics("BoloosPinPhysics", 0.28f, 0.22f, 0.30f));
            p.gutterPhysics = r.Track(NewPhysics("BoloosGutterPhysics", 0.22f, 0.18f, 0.05f));

            // Bolo: se tornea una vez y se reutiliza en los 10 de cada pista.
            p.pinProfile = PinProfile();
            p.pinMesh = r.Track(MeshBuilder.Lathe(p.pinProfile, 28, "BoloosPinMesh"));
            p.pinColliderMesh = r.Track(MeshBuilder.Lathe(p.pinProfile, 12, "BoloosPinCollider"));

            float[] v = MeshBuilder.ProfileV(p.pinProfile);
            float stripeA = VAtHeight(p.pinProfile, v, 0.262f);
            float stripeB = VAtHeight(p.pinProfile, v, 0.292f);
            Texture2D pinTex = r.Track(ProceduralTextures.Pin(stripeA, stripeB, 0.030f));
            p.pin = r.Track(AlleyMaterials.Create("BoloosPin", Color.white, 0.68f, 0f, pinTex));

            return p;
        }

        static PhysMat NewPhysics(string name, float dynamicFriction, float staticFriction, float bounciness)
        {
            var m = new PhysMat(name);
            m.dynamicFriction = dynamicFriction;
            m.staticFriction = staticFriction;
            m.bounciness = bounciness;
            return m;
        }

        // ==================================================================
        // Pista
        // ==================================================================

        static void BuildLane(AlleyBuildResult r, Palette p, AlleyBuildSettings s, int index)
        {
            var laneGo = new GameObject("Pista " + (index + 1));
            laneGo.transform.SetParent(r.root.transform, false);
            laneGo.transform.localPosition = new Vector3(index * AlleySpec.LanePitch, 0f, 0f);

            var lane = laneGo.AddComponent<BowlingLane>();
            lane.laneNumber = index + 1;

            float half = AlleySpec.LaneWidth * 0.5f;
            float laneEnd = AlleySpec.LaneLength;

            // ---- superficie de juego: 39 tablas, de la falta al foso ----
            Mesh bedMesh = r.Track(MeshBuilder.Box(
                new Vector3(AlleySpec.LaneWidth, AlleySpec.LaneThickness, laneEnd),
                new Vector2(1f, 1f), true, "BoloosLaneBed"));
            GameObject bed = Piece("Superficie", laneGo.transform,
                new Vector3(0f, -AlleySpec.LaneThickness * 0.5f, laneEnd * 0.5f), bedMesh, p.lane);
            AddBox(bed, new Vector3(AlleySpec.LaneWidth, AlleySpec.LaneThickness, laneEnd), Vector3.zero, p.lanePhysics);

            // ---- canaletas a ambos lados ----
            Vector2[] section = MeshBuilder.GutterSection(AlleySpec.GutterWidth, AlleySpec.GutterDepth, 12);
            Mesh gutterMesh = r.Track(MeshBuilder.ExtrudeSection(section, laneEnd, "BoloosGutter"));
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                GameObject gutter = Piece(side < 0 ? "Canaleta izquierda" : "Canaleta derecha", laneGo.transform,
                    new Vector3(side * (half + AlleySpec.GutterWidth * 0.5f), 0f, 0f), gutterMesh, p.gutter);
                AddMeshCollider(gutter, gutterMesh, p.gutterPhysics);

                // pared exterior de la canaleta, para que la bola no se salga del mundo
                Mesh wall = r.Track(MeshBuilder.Box(new Vector3(0.05f, 0.34f, laneEnd), "BoloosGutterWall"));
                GameObject w = Piece("Pared", gutter.transform,
                    new Vector3(side * AlleySpec.GutterWidth * 0.5f, 0.15f, laneEnd * 0.5f), wall, p.machine);
                AddBox(w, new Vector3(0.05f, 0.34f, laneEnd), Vector3.zero, p.gutterPhysics);
            }

            // ---- linea de falta ----
            Mesh foul = r.Track(MeshBuilder.Box(new Vector3(AlleySpec.LaneWidth + AlleySpec.GutterWidth * 2f, 0.004f, 0.03f), "BoloosFoulLine"));
            Piece("Linea de falta", laneGo.transform, new Vector3(0f, 0.002f, 0f), foul, p.foulLine);

            // ---- flechas de mira ----
            var arrows = new GameObject("Flechas");
            arrows.transform.SetParent(laneGo.transform, false);
            Mesh arrowMesh = r.Track(MeshBuilder.FlatTriangle(0.075f, 0.34f, "BoloosArrow"));
            int[] arrowBoards = { 5, 10, 15, 20, 25, 30, 35 };
            for (int i = 0; i < arrowBoards.Length; i++)
            {
                Piece("Flecha " + (i + 1), arrows.transform,
                    new Vector3(AlleySpec.BoardCenterX(arrowBoards[i]), 0.0015f, AlleySpec.ArrowDistances[i]),
                    arrowMesh, p.arrow);
            }

            // ---- aproximacion ----
            float approachWidth = AlleySpec.LaneWidth + AlleySpec.GutterWidth * 2f + AlleySpec.ReturnGap;
            Mesh approachMesh = r.Track(MeshBuilder.Box(
                new Vector3(approachWidth, AlleySpec.LaneThickness, AlleySpec.ApproachLength),
                Vector2.one, true, "BoloosApproach"));
            GameObject approach = Piece("Aproximacion", laneGo.transform,
                new Vector3(AlleySpec.ReturnGap * 0.5f, -AlleySpec.LaneThickness * 0.5f, -AlleySpec.ApproachLength * 0.5f),
                approachMesh, p.approach);
            AddBox(approach, new Vector3(approachWidth, AlleySpec.LaneThickness, AlleySpec.ApproachLength), Vector3.zero, p.lanePhysics);

            var release = new GameObject("Punto de lanzamiento");
            release.transform.SetParent(laneGo.transform, false);
            release.transform.localPosition = new Vector3(0f, AlleySpec.BallRadius, -0.9f);
            lane.releasePoint = release.transform;

            // ---- paredes del pin deck y foso ----
            BuildPitArea(r, p, laneGo.transform, laneEnd);

            // ---- bolos ----
            if (s.buildPins) lane.pins = BuildPins(r, p, laneGo.transform);

            // ---- maquina de retorno ----
            if (s.buildReturnMachine)
            {
                BuildReturnMachine(r, p, s, laneGo.transform, lane, laneEnd);
            }

            r.lanes.Add(lane);
        }

        static void BuildPitArea(AlleyBuildResult r, Palette p, Transform parent, float laneEnd)
        {
            var pit = new GameObject("Foso");
            pit.transform.SetParent(parent, false);

            float width = AlleySpec.LaneWidth + AlleySpec.GutterWidth * 2f;

            // suelo del foso, por debajo del nivel de la pista
            Mesh floor = r.Track(MeshBuilder.Box(new Vector3(width, 0.08f, AlleySpec.PitLength), "BoloosPitFloor"));
            GameObject floorGo = Piece("Suelo del foso", pit.transform,
                new Vector3(0f, -AlleySpec.PitDrop, laneEnd + AlleySpec.PitLength * 0.5f), floor, p.pit);
            AddBox(floorGo, new Vector3(width, 0.08f, AlleySpec.PitLength), Vector3.zero, p.gutterPhysics);

            // colchon del fondo: para la bola y los bolos que salen disparados
            Mesh cushion = r.Track(MeshBuilder.Box(new Vector3(width, 0.9f, 0.12f), "BoloosPitCushion"));
            GameObject cushionGo = Piece("Colchon", pit.transform,
                new Vector3(0f, -AlleySpec.PitDrop + 0.45f, laneEnd + AlleySpec.PitLength), cushion, p.rubber);
            AddBox(cushionGo, new Vector3(width, 0.9f, 0.12f), Vector3.zero, null);

            // paredes laterales del pin deck (kickbacks)
            Mesh kick = r.Track(MeshBuilder.Box(new Vector3(0.06f, 0.75f, 2.4f), "BoloosKickback"));
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                GameObject k = Piece(side < 0 ? "Kickback izquierdo" : "Kickback derecho", pit.transform,
                    new Vector3(side * (width * 0.5f + 0.03f), 0.375f, laneEnd - 1.0f), kick, p.machine);
                AddBox(k, new Vector3(0.06f, 0.75f, 2.4f), Vector3.zero, p.pinPhysics);
            }

            // capucha del pinsetter: la caja que tapa la maquina de bolos
            Mesh hood = r.Track(MeshBuilder.Box(new Vector3(width + 0.2f, 1.1f, AlleySpec.PitLength + 0.5f), "BoloosPinsetterHood"));
            Piece("Capucha del pinsetter", pit.transform,
                new Vector3(0f, 1.35f, laneEnd + AlleySpec.PitLength * 0.5f), hood, p.machine);
        }

        static PinSet BuildPins(AlleyBuildResult r, Palette p, Transform parent)
        {
            var setGo = new GameObject("Bolos");
            setGo.transform.SetParent(parent, false);
            var set = setGo.AddComponent<PinSet>();
            set.pins = new BowlingPin[10];

            for (int n = 1; n <= 10; n++)
            {
                var go = new GameObject("Bolo " + n);
                go.transform.SetParent(setGo.transform, false);
                go.transform.localPosition = AlleySpec.PinPosition(n);

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = p.pinMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = p.pin;

                var col = go.AddComponent<MeshCollider>();
                col.sharedMesh = p.pinColliderMesh;
                col.convex = true;
                col.sharedMaterial = p.pinPhysics;

                go.AddComponent<Rigidbody>();
                var pin = go.AddComponent<BowlingPin>();
                pin.number = n;

                set.pins[n - 1] = pin;
            }

            return set;
        }

        // ==================================================================
        // Maquina de retorno
        // ==================================================================

        static void BuildReturnMachine(AlleyBuildResult r, Palette p, AlleyBuildSettings s,
                                       Transform parent, BowlingLane lane, float laneEnd)
        {
            float x = AlleySpec.LaneWidth * 0.5f + AlleySpec.GutterWidth + AlleySpec.ReturnGap * 0.5f;

            var machine = new GameObject("Retorno de bolas");
            machine.transform.SetParent(parent, false);
            machine.transform.localPosition = new Vector3(x, 0f, 0f);

            // ---- estante de bolas ----
            Vector3 rackEntry;
            BallRack rack = BuildRack(r, p, machine.transform, out rackEntry);

            // ---- carril en U: del foso hasta donde empieza el estante ----
            float trackStart = rack.transform.localPosition.z + RackDepth + 0.02f;
            float trackLength = laneEnd + AlleySpec.PitLength - trackStart;
            Vector2[] channel = MeshBuilder.GutterSection(TrackWidth, TrackDepth, 10);
            Mesh trackMesh = r.Track(MeshBuilder.ExtrudeSection(channel, trackLength, "BoloosReturnTrack"));
            GameObject track = Piece("Carril", machine.transform,
                new Vector3(0f, TrackTopY, trackStart), trackMesh, p.metal);
            AddMeshCollider(track, trackMesh, p.gutterPhysics);

            // carcasa bajo el carril, para que no flote
            Mesh housing = r.Track(MeshBuilder.Box(new Vector3(TrackWidth + 0.08f, TrackTopY - TrackDepth, trackLength), "BoloosReturnHousing"));
            Piece("Carcasa", machine.transform,
                new Vector3(0f, (TrackTopY - TrackDepth) * 0.5f, trackStart + trackLength * 0.5f), housing, p.machine);

            // ---- elevador junto al foso ----
            float liftZ = laneEnd + AlleySpec.PitLength * 0.5f;
            Mesh liftBody = r.Track(MeshBuilder.Box(new Vector3(0.42f, 1.15f, 0.55f), "BoloosLiftBody"));
            GameObject lift = Piece("Elevador", machine.transform,
                new Vector3(0f, 0.1f, liftZ), liftBody, p.machine);

            var wheels = new List<SpinningPart>();
            Mesh wheelMesh = r.Track(MeshBuilder.Lathe(
                new[] { new Vector2(0f, -0.03f), new Vector2(0.13f, -0.03f), new Vector2(0.13f, 0.03f), new Vector2(0f, 0.03f) },
                20, "BoloosLiftWheel"));
            for (int i = 0; i < 3; i++)
            {
                GameObject wheel = Piece("Rueda " + (i + 1), lift.transform, new Vector3(0.22f, -0.3f + i * 0.35f, 0f), wheelMesh, p.rubber);
                // el torneado gira sobre Y, asi que se tumba para que ruede sobre X
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var spin = wheel.AddComponent<SpinningPart>();
                spin.axis = Vector3.up;   // eje local, ya tumbado
                spin.rpm = 180f;
                wheels.Add(spin);
            }

            // acelerador del foso: la rueda que empuja la bola hacia el elevador
            GameObject accel = Piece("Acelerador", machine.transform,
                new Vector3(-AlleySpec.ReturnGap * 0.5f - AlleySpec.GutterWidth, -AlleySpec.PitDrop + 0.18f, liftZ),
                wheelMesh, p.rubber);
            accel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var accelSpin = accel.AddComponent<SpinningPart>();
            accelSpin.axis = Vector3.up;
            accelSpin.rpm = 260f;
            wheels.Add(accelSpin);

            // ---- trigger del foso ----
            var pitTrigger = new GameObject("Foso (trigger)");
            pitTrigger.transform.SetParent(machine.transform, false);
            pitTrigger.transform.localPosition = new Vector3(
                -AlleySpec.ReturnGap * 0.5f - AlleySpec.GutterWidth - AlleySpec.LaneWidth * 0.5f,
                -AlleySpec.PitDrop + 0.35f,
                laneEnd + AlleySpec.PitLength * 0.5f);

            var trigger = pitTrigger.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(AlleySpec.LaneWidth + AlleySpec.GutterWidth * 2f, 0.7f, AlleySpec.PitLength);

            var ret = pitTrigger.AddComponent<BallReturn>();
            ret.rack = rack;
            ret.movingParts = wheels.ToArray();

            // ---- recorrido ----
            var pathRoot = new GameObject("Recorrido");
            pathRoot.transform.SetParent(machine.transform, false);

            float pitY = -AlleySpec.PitDrop + AlleySpec.BallRadius;
            Vector3[] points =
            {
                new Vector3(-AlleySpec.ReturnGap * 0.5f - AlleySpec.GutterWidth, pitY, liftZ), // salida del foso
                new Vector3(0f, pitY, liftZ),                                                  // pie del elevador
                new Vector3(0f, BallTravelY, liftZ),                                           // arriba del elevador
                new Vector3(0f, BallTravelY, laneEnd * 0.5f),                                  // carril
                new Vector3(0f, BallTravelY, 0f),                                              // carril, a la altura de la falta
                rackEntry                                                                      // entrada del estante
            };

            var transforms = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var wp = new GameObject("Punto " + i);
                wp.transform.SetParent(pathRoot.transform, false);
                wp.transform.localPosition = points[i];
                transforms[i] = wp.transform;
            }
            ret.path = transforms;

            lane.ballReturn = ret;
            lane.rack = rack;

            if (s.buildBalls) BuildBalls(r, p, s, rack);
        }

        /// <summary>Longitud del estante desde su origen hacia +Z.</summary>
        const float RackDepth = (AlleySpec.BallDiameter + 0.02f) * 4f + 0.15f;
        const float RackFront = -0.25f;

        static BallRack BuildRack(AlleyBuildResult r, Palette p, Transform parent, out Vector3 entryPoint)
        {
            var rackGo = new GameObject("Estante de bolas");
            rackGo.transform.SetParent(parent, false);
            rackGo.transform.localPosition = new Vector3(0f, 0f, -1.9f);

            var rack = rackGo.AddComponent<BallRack>();

            float slotSpacing = AlleySpec.BallDiameter + 0.02f;
            int slotCount = 5;
            float length = RackDepth - RackFront;
            float centerZ = (RackFront + RackDepth) * 0.5f;
            float bodyHeight = RailTopY - RailHeight;

            // cuna: dos railes sobre los que la bola apoya
            Mesh railMesh = r.Track(MeshBuilder.Box(new Vector3(0.05f, RailHeight, length), "BoloosRackRail"));
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Piece(side < 0 ? "Rail izquierdo" : "Rail derecho", rackGo.transform,
                    new Vector3(side * RailHalfGap, RailTopY - RailHeight * 0.5f, centerZ), railMesh, p.metal);
            }

            // cuerpo, por debajo de la bola para que no la tape
            Mesh body = r.Track(MeshBuilder.Box(new Vector3(0.42f, bodyHeight, length), "BoloosRackBody"));
            GameObject bodyGo = Piece("Cuerpo", rackGo.transform,
                new Vector3(0f, bodyHeight * 0.5f, centerZ), body, p.machine);
            AddBox(bodyGo, new Vector3(0.42f, bodyHeight, length), Vector3.zero, null);

            // capucha sobre la entrada: la bola sale de debajo y baja la fila
            Vector2[] arc = MeshBuilder.ArcSection(0.30f, 180f, 0f, 16);
            Mesh hoodMesh = r.Track(MeshBuilder.ExtrudeSection(arc, 0.55f, "BoloosRackHood"));
            Piece("Capucha", rackGo.transform,
                new Vector3(0f, RailTopY - 0.02f, RackDepth - 0.55f), hoodMesh, p.machine);

            // huecos: el 0 es el de delante
            rack.slots = new Transform[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                var slot = new GameObject("Hueco " + i);
                slot.transform.SetParent(rackGo.transform, false);
                slot.transform.localPosition = new Vector3(0f, BallTravelY, i * slotSpacing);
                rack.slots[i] = slot.transform;
            }

            // la maquina entrega la bola en el ultimo hueco, bajo la capucha
            entryPoint = rackGo.transform.localPosition + new Vector3(0f, BallTravelY, (slotCount - 1) * slotSpacing);
            return rack;
        }

        static void BuildBalls(AlleyBuildResult r, Palette p, AlleyBuildSettings s, BallRack rack)
        {
            int count = Mathf.Min(s.ballsPerLane, rack.slots.Length);
            for (int i = 0; i < count; i++)
            {
                Color color = BallColors[(i + s.seed) % BallColors.Length];
                BowlingBall ball = CreateBall(r, p, color, "Bola " + (i + 1));
                ball.transform.position = rack.slots[i].position;
                rack.Store(ball);
            }
        }

        /// <summary>
        /// Bola reglamentaria: esfera de 8,5", 7,26 kg y los tres agujeros
        /// perforados de verdad sobre la malla.
        /// </summary>
        public static BowlingBall CreateBall(AlleyBuildResult r, Color color, string name)
        {
            return CreateBall(r, null, color, name);
        }

        static BowlingBall CreateBall(AlleyBuildResult r, Palette p, Color color, string name)
        {
            var go = new GameObject(name);

            float radius = AlleySpec.BallRadius;
            float[] holeRadius =
            {
                AlleySpec.ThumbHoleDiameter * 0.5f,
                AlleySpec.FingerHoleDiameter * 0.5f,
                AlleySpec.FingerHoleDiameter * 0.5f
            };

            Mesh mesh = MeshBuilder.Sphere(radius, 64, 40, "BoloosBallMesh");
            DrillHoles(mesh, radius, GripHoles, holeRadius, AlleySpec.HoleDepth);
            r.Track(mesh);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            Texture2D tex = r.Track(ProceduralTextures.Ball(color));
            Material ballMat = r.Track(AlleyMaterials.Create("BoloosBall_" + name, Color.white, 0.92f, 0f, tex));
            Material boreMat = p != null ? p.rubber
                : r.Track(AlleyMaterials.Create("BoloosBore", new Color(0.05f, 0.05f, 0.06f), 0.2f, 0f));

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { ballMat, boreMat };

            var col = go.AddComponent<SphereCollider>();
            col.radius = radius;
            if (p != null) col.sharedMaterial = p.ballPhysics;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = AlleySpec.BallMass;

            var ball = go.AddComponent<BowlingBall>();
            ball.radius = radius;
            return ball;
        }

        /// <summary>
        /// Empunadura convencional: los dos dedos separados 2,4" entre si y el
        /// pulgar a 4,25", medido sobre la superficie de la bola.
        /// </summary>
        static readonly Vector3[] GripHoles =
        {
            HoleDirection(57.3f, 180f),
            HoleDirection(16.2f, -90f),
            HoleDirection(16.2f, 90f)
        };

        /// <summary>
        /// Perfora los agujeros de verdad, en vez de disimularlos: quita los
        /// triangulos que caen en la boca, cose el borde irregular que queda a un
        /// aro circular limpio y baja las paredes del taladro hasta el fondo.
        /// No hace falta ordenar el borde, cada arista frontera genera su propia
        /// banda, pared y trozo de tapa.
        ///
        /// Las paredes van al submalla 1 para poder darles un material oscuro: sin
        /// eso el agujero se ve del color de la bola y no parece un agujero.
        /// </summary>
        static void DrillHoles(Mesh mesh, float radius, Vector3[] directions, float[] holeRadius, float depth)
        {
            var verts = new List<Vector3>(mesh.vertices);
            var uvs = new List<Vector2>(mesh.uv);
            var surface = new List<int>(mesh.triangles);
            var bore = new List<int>();
            int surfaceCount = verts.Count;

            for (int h = 0; h < directions.Length; h++)
            {
                Vector3 d = directions[h];
                float rh = holeRadius[h];
                float cosMouth = Mathf.Cos(Mathf.Asin(Mathf.Clamp01(rh / radius)));

                // Vertices dentro de la boca. Solo los de la esfera original: los
                // que anaden los taladros anteriores no se tocan.
                var inside = new bool[verts.Count];
                for (int i = 0; i < surfaceCount; i++)
                {
                    inside[i] = Vector3.Dot(verts[i].normalized, d) > cosMouth;
                }

                var kept = new List<int>(surface.Count);
                var gone = new HashSet<long>();
                for (int t = 0; t < surface.Count; t += 3)
                {
                    int a = surface[t], b = surface[t + 1], c = surface[t + 2];
                    if (inside[a] || inside[b] || inside[c])
                    {
                        gone.Add(EdgeKey(a, b));
                        gone.Add(EdgeKey(b, c));
                        gone.Add(EdgeKey(c, a));
                    }
                    else
                    {
                        kept.Add(a); kept.Add(b); kept.Add(c);
                    }
                }

                if (gone.Count == 0)
                {
                    Debug.LogWarning("[Boloos] La boca de un agujero no llega a tocar la malla de la bola.");
                    continue;
                }

                float yRim = Mathf.Sqrt(Mathf.Max(radius * radius - rh * rh, 0f));
                var rimSurface = new Dictionary<int, int>();
                var rimBore = new Dictionary<int, int>();
                var bottom = new Dictionary<int, int>();

                int center = verts.Count;
                verts.Add(d * (yRim - depth));
                uvs.Add(SphericalUV(d));

                var band = new List<int>();
                for (int t = 0; t < kept.Count; t += 3)
                {
                    int a = kept[t], b = kept[t + 1], c = kept[t + 2];
                    Stitch(verts, uvs, band, bore, gone, rimSurface, rimBore, bottom, center, a, b, d, rh, yRim, depth);
                    Stitch(verts, uvs, band, bore, gone, rimSurface, rimBore, bottom, center, b, c, d, rh, yRim, depth);
                    Stitch(verts, uvs, band, bore, gone, rimSurface, rimBore, bottom, center, c, a, d, rh, yRim, depth);
                }

                kept.AddRange(band);
                surface = kept;
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(surface, 0);
            mesh.SetTriangles(bore, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>Cose una arista del borde del corte al aro y baja la pared.</summary>
        static void Stitch(List<Vector3> verts, List<Vector2> uvs, List<int> band, List<int> bore,
                           HashSet<long> gone, Dictionary<int, int> rimSurface, Dictionary<int, int> rimBore,
                           Dictionary<int, int> bottom, int center,
                           int u, int v, Vector3 d, float rh, float yRim, float depth)
        {
            // Frontera solo si al otro lado habia un triangulo que se ha quitado.
            if (!gone.Contains(EdgeKey(v, u))) return;

            int su = Rim(verts, uvs, rimSurface, u, d, rh, yRim, 0f);
            int sv = Rim(verts, uvs, rimSurface, v, d, rh, yRim, 0f);
            int bu = Rim(verts, uvs, rimBore, u, d, rh, yRim, 0f);
            int bv = Rim(verts, uvs, rimBore, v, d, rh, yRim, 0f);
            int fu = Rim(verts, uvs, bottom, u, d, rh, yRim, depth);
            int fv = Rim(verts, uvs, bottom, v, d, rh, yRim, depth);

            // banda del borde irregular al aro, con la cara hacia fuera
            band.Add(u); band.Add(su); band.Add(sv);
            band.Add(u); band.Add(sv); band.Add(v);

            // pared del taladro, mirando hacia dentro, y el fondo
            bore.Add(bu); bore.Add(fu); bore.Add(bv);
            bore.Add(bv); bore.Add(fu); bore.Add(fv);
            bore.Add(center); bore.Add(fv); bore.Add(fu);
        }

        /// <summary>
        /// Proyecta un vertice del borde sobre el circulo del agujero, a la
        /// profundidad pedida. Cada tabla guarda su copia para que el aro quede
        /// vivo y RecalculateNormals no lo redondee.
        /// </summary>
        static int Rim(List<Vector3> verts, List<Vector2> uvs, Dictionary<int, int> cache,
                       int index, Vector3 d, float rh, float yRim, float sink)
        {
            int existing;
            if (cache.TryGetValue(index, out existing)) return existing;

            Vector3 v = verts[index];
            Vector3 perp = v - d * Vector3.Dot(v, d);
            perp = perp.sqrMagnitude > 1e-12f ? perp.normalized : Vector3.Cross(d, Vector3.right).normalized;

            int created = verts.Count;
            verts.Add(perp * rh + d * (yRim - sink));
            uvs.Add(SphericalUV(perp * rh + d * yRim));
            cache[index] = created;
            return created;
        }

        static Vector2 SphericalUV(Vector3 v)
        {
            Vector3 n = v.normalized;
            return new Vector2(Mathf.Atan2(n.z, n.x) / (Mathf.PI * 2f) + 0.5f,
                               1f - Mathf.Acos(Mathf.Clamp(n.y, -1f, 1f)) / Mathf.PI);
        }

        static long EdgeKey(int a, int b)
        {
            return ((long)a << 32) | (uint)b;
        }

        /// <summary>Direccion de un agujero: inclinacion desde el polo y giro alrededor de el.</summary>
        static Vector3 HoleDirection(float tiltDegrees, float yawDegrees)
        {
            float t = tiltDegrees * Mathf.Deg2Rad;
            float y = yawDegrees * Mathf.Deg2Rad;
            float s = Mathf.Sin(t);
            return new Vector3(s * Mathf.Sin(y), Mathf.Cos(t), s * Mathf.Cos(y)).normalized;
        }

        // ==================================================================
        // Local
        // ==================================================================

        static void BuildHouse(AlleyBuildResult r, Palette p, AlleyBuildSettings s)
        {
            var house = new GameObject("Local");
            house.transform.SetParent(r.root.transform, false);

            float width = s.laneCount * AlleySpec.LanePitch + 4f;
            float length = AlleySpec.LaneLength + AlleySpec.PitLength + AlleySpec.ApproachLength + 8f;
            float centerX = (s.laneCount - 1) * AlleySpec.LanePitch * 0.5f;
            float centerZ = (AlleySpec.LaneLength + AlleySpec.PitLength - AlleySpec.ApproachLength - 6f) * 0.5f;

            Mesh floor = r.Track(MeshBuilder.Quad(width, length, new Vector2(width / 3f, length / 3f), "BoloosFloor"));
            GameObject floorGo = Piece("Suelo", house.transform,
                new Vector3(centerX, -AlleySpec.LaneThickness - 0.001f, centerZ), floor, p.carpet);
            AddBox(floorGo, new Vector3(width, 0.02f, length), new Vector3(0f, -0.01f, 0f), null);

            Mesh wall = r.Track(MeshBuilder.Box(new Vector3(width, 4.5f, 0.2f), "BoloosWall"));
            Piece("Pared del fondo", house.transform,
                new Vector3(centerX, 2.25f - AlleySpec.LaneThickness, AlleySpec.LaneLength + AlleySpec.PitLength + 1.2f), wall, p.wall);
            Piece("Pared trasera", house.transform,
                new Vector3(centerX, 2.25f - AlleySpec.LaneThickness, -AlleySpec.ApproachLength - 5.8f), wall, p.wall);

            // luces sobre las pistas
            for (int i = 0; i < s.laneCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var lightGo = new GameObject("Luz " + (i + 1) + "-" + (j + 1));
                    lightGo.transform.SetParent(house.transform, false);
                    lightGo.transform.localPosition = new Vector3(
                        i * AlleySpec.LanePitch + AlleySpec.ReturnGap * 0.5f, 3.6f, j * 6.5f - 1f);

                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 9f;
                    light.intensity = 1.4f;
                    light.color = new Color(1f, 0.95f, 0.88f);
                }
            }

            // marcador iluminado al fondo de cada pista
            Mesh screen = r.Track(MeshBuilder.Box(new Vector3(AlleySpec.LaneWidth, 0.7f, 0.06f), "BoloosScoreScreen"));
            for (int i = 0; i < s.laneCount; i++)
            {
                Piece("Marcador " + (i + 1), house.transform,
                    new Vector3(i * AlleySpec.LanePitch, 2.6f, AlleySpec.LaneLength + AlleySpec.PitLength + 1.05f),
                    screen, p.neon);
            }
        }

        // ==================================================================
        // Utilidades
        // ==================================================================

        static GameObject Piece(string name, Transform parent, Vector3 localPosition, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static void AddBox(GameObject go, Vector3 size, Vector3 center, PhysMat physics)
        {
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            col.center = center;
            if (physics != null) col.sharedMaterial = physics;
        }

        static void AddMeshCollider(GameObject go, Mesh mesh, PhysMat physics)
        {
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
            col.convex = false;
            if (physics != null) col.sharedMaterial = physics;
        }

        /// <summary>Perfil de un bolo reglamentario: (radio, altura) en metros.</summary>
        public static Vector2[] PinProfile()
        {
            return new[]
            {
                new Vector2(0.0000f, 0.000f),
                new Vector2(0.0258f, 0.000f),
                new Vector2(0.0272f, 0.006f),
                new Vector2(0.0330f, 0.020f),
                new Vector2(0.0430f, 0.045f),
                new Vector2(0.0530f, 0.075f),
                new Vector2(0.0590f, 0.100f),
                new Vector2(0.0605f, 0.120f),
                new Vector2(0.0596f, 0.140f),
                new Vector2(0.0550f, 0.170f),
                new Vector2(0.0470f, 0.200f),
                new Vector2(0.0380f, 0.228f),
                new Vector2(0.0300f, 0.252f),
                new Vector2(0.0250f, 0.272f),
                new Vector2(0.0228f, 0.290f),
                new Vector2(0.0240f, 0.305f),
                new Vector2(0.0275f, 0.322f),
                new Vector2(0.0300f, 0.338f),
                new Vector2(0.0295f, 0.352f),
                new Vector2(0.0255f, 0.366f),
                new Vector2(0.0170f, 0.376f),
                new Vector2(0.0000f, 0.381f)
            };
        }

        static float VAtHeight(Vector2[] profile, float[] v, float height)
        {
            for (int i = 1; i < profile.Length; i++)
            {
                if (profile[i].y >= height)
                {
                    float span = profile[i].y - profile[i - 1].y;
                    float t = span > 0.0001f ? (height - profile[i - 1].y) / span : 0f;
                    return Mathf.Lerp(v[i - 1], v[i], t);
                }
            }
            return v[v.Length - 1];
        }
    }
}
