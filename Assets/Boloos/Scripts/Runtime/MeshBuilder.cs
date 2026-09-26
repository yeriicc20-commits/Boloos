using System.Collections.Generic;
using UnityEngine;

namespace Boloos
{
    /// <summary>
    /// Generador de mallas procedurales. Evita depender de modelos importados:
    /// la pista, los bolos, la bola y la maquina se construyen aqui desde cero.
    ///
    /// Convenio de winding de Unity: la normal frontal de un triangulo (v0,v1,v2)
    /// es Cross(v1 - v0, v2 - v0). Todos los metodos de abajo estan ordenados
    /// segun ese convenio, por eso las caras salen hacia fuera.
    /// </summary>
    public static class MeshBuilder
    {
        // ------------------------------------------------------------------
        // Caja
        // ------------------------------------------------------------------

        /// <summary>Caja centrada en el origen, con UV 0..1 por cara.</summary>
        public static Mesh Box(Vector3 size, string name = "Box")
        {
            return Box(size, Vector2.one, false, name);
        }

        /// <summary>
        /// Caja centrada en el origen. <paramref name="uvTiling"/> multiplica las UV
        /// y <paramref name="swapTopUV"/> intercambia los ejes UV de la cara superior
        /// (util para que una textura de tablas cruce la pista a lo ancho).
        /// </summary>
        public static Mesh Box(Vector3 size, Vector2 uvTiling, bool swapTopUV, string name = "Box")
        {
            Vector3 h = size * 0.5f;
            var verts = new List<Vector3>(24);
            var norms = new List<Vector3>(24);
            var uvs = new List<Vector2>(24);
            var tris = new List<int>(36);

            Vector3 ex = new Vector3(size.x, 0f, 0f);
            Vector3 ey = new Vector3(0f, size.y, 0f);
            Vector3 ez = new Vector3(0f, 0f, size.z);

            // origen, direccion U, direccion V  (el orden da la normal correcta)
            AddQuad(verts, norms, uvs, tris, new Vector3(h.x, -h.y, -h.z), ey, ez, Vector2.one, false);   // +X
            AddQuad(verts, norms, uvs, tris, new Vector3(-h.x, -h.y, -h.z), ez, ey, Vector2.one, false);  // -X
            AddQuad(verts, norms, uvs, tris, new Vector3(-h.x, h.y, -h.z), ez, ex, uvTiling, swapTopUV);  // +Y
            AddQuad(verts, norms, uvs, tris, new Vector3(-h.x, -h.y, -h.z), ex, ez, Vector2.one, false);  // -Y
            AddQuad(verts, norms, uvs, tris, new Vector3(-h.x, -h.y, h.z), ex, ey, Vector2.one, false);   // +Z
            AddQuad(verts, norms, uvs, tris, new Vector3(-h.x, -h.y, -h.z), ey, ex, Vector2.one, false);  // -Z

            return Build(name, verts, norms, uvs, tris);
        }

        /// <summary>Quad plano en el plano XZ, normal hacia +Y, centrado en el origen.</summary>
        public static Mesh Quad(float width, float length, Vector2 uvTiling, string name = "Quad")
        {
            var verts = new List<Vector3>(4);
            var norms = new List<Vector3>(4);
            var uvs = new List<Vector2>(4);
            var tris = new List<int>(6);
            AddQuad(verts, norms, uvs, tris,
                new Vector3(-width * 0.5f, 0f, -length * 0.5f),
                new Vector3(0f, 0f, length),
                new Vector3(width, 0f, 0f),
                uvTiling, true);
            return Build(name, verts, norms, uvs, tris);
        }

        /// <summary>Triangulo isosceles plano (XZ, normal +Y) apuntando hacia +Z.</summary>
        public static Mesh FlatTriangle(float width, float length, string name = "Triangle")
        {
            var verts = new Vector3[3];
            verts[0] = new Vector3(-width * 0.5f, 0f, 0f);
            verts[1] = new Vector3(0f, 0f, length);
            verts[2] = new Vector3(width * 0.5f, 0f, 0f);
            // Cross(v1-v0, v2-v0) = Cross((w/2,0,l), (w,0,0)) = (0, l*w, 0) -> +Y
            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0f) };
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------
        // Superficie de revolucion (bolos, bola, rodillos, tubos)
        // ------------------------------------------------------------------

        /// <summary>
        /// Revoluciona un perfil alrededor del eje Y. Cada punto del perfil es
        /// (radio, altura) y deben ir ordenados de abajo a arriba.
        /// La costura no se duplica: la U salta de 1 a 0 en la ultima columna, lo
        /// cual es invisible con texturas que solo varian en V (las que usamos).
        /// </summary>
        public static Mesh Lathe(IList<Vector2> profile, int segments, string name = "Lathe")
        {
            int rings = profile.Count;
            float[] v = ProfileV(profile);

            var verts = new Vector3[rings * segments];
            var uvs = new Vector2[verts.Length];
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    float t = (float)s / segments;
                    float a = t * Mathf.PI * 2f;
                    int i = r * segments + s;
                    verts[i] = new Vector3(Mathf.Cos(a) * profile[r].x, profile[r].y, Mathf.Sin(a) * profile[r].x);
                    uvs[i] = new Vector2(t, v[r]);
                }
            }

            var tris = new List<int>((rings - 1) * segments * 6);
            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int s1 = (s + 1) % segments;
                    int a = r * segments + s;
                    int b = r * segments + s1;
                    int c = (r + 1) * segments + s;
                    int d = (r + 1) * segments + s1;
                    // (a,c,b) y (c,d,b) miran hacia fuera del eje
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(c); tris.Add(d); tris.Add(b);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Longitud acumulada normalizada de un perfil: la coordenada V del torneado.</summary>
        public static float[] ProfileV(IList<Vector2> profile)
        {
            var v = new float[profile.Count];
            float total = 0f;
            for (int i = 1; i < profile.Count; i++)
            {
                total += Vector2.Distance(profile[i - 1], profile[i]);
                v[i] = total;
            }
            if (total > 0f)
            {
                for (int i = 0; i < v.Length; i++) v[i] /= total;
            }
            return v;
        }

        /// <summary>
        /// Tubo recto a lo largo de Y, desde y=0 hacia arriba.
        /// Con <paramref name="inward"/> las caras miran al interior (agujeros de la bola).
        /// </summary>
        public static Mesh Tube(float radius, float height, int segments, bool inward, bool capBottom, string name = "Tube")
        {
            var profile = new[] { new Vector2(radius, 0f), new Vector2(radius, height) };
            Mesh mesh = Lathe(profile, segments, name);

            // Se construye siempre mirando hacia fuera y, si hace falta, se invierte
            // entero al final: asi la tapa acaba orientada hacia el lado correcto.
            if (capBottom) AppendDisc(mesh, radius, 0f, segments, Vector3.down);
            if (inward) FlipWinding(mesh);
            return mesh;
        }

        /// <summary>Esfera UV centrada en el origen. La costura en U no se duplica.</summary>
        public static Mesh Sphere(float radius, int segments, int rings, string name = "Sphere")
        {
            var verts = new Vector3[(rings + 1) * segments];
            var uvs = new Vector2[verts.Length];
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;      // 0 = polo norte
                float y = Mathf.Cos(phi) * radius;
                float rho = Mathf.Sin(phi) * radius;
                for (int s = 0; s < segments; s++)
                {
                    float t = (float)s / segments;
                    float a = t * Mathf.PI * 2f;
                    int i = r * segments + s;
                    verts[i] = new Vector3(Mathf.Cos(a) * rho, y, Mathf.Sin(a) * rho);
                    uvs[i] = new Vector2(t, 1f - (float)r / rings);
                }
            }

            var tris = new List<int>(rings * segments * 6);
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int s1 = (s + 1) % segments;
                    int a = r * segments + s;
                    int b = r * segments + s1;
                    int c = (r + 1) * segments + s;
                    int d = (r + 1) * segments + s1;
                    // el anillo r esta por encima del r+1, asi que el orden se invierte
                    // respecto al torneado
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------
        // Extrusion de una seccion (canaletas, carril de retorno)
        // ------------------------------------------------------------------

        /// <summary>
        /// Extruye una seccion 2D (X lateral, Y vertical) a lo largo de +Z.
        /// Con la seccion ordenada de -X a +X las caras miran hacia arriba,
        /// que es justo lo que quiere una canaleta o un carril en U.
        /// </summary>
        public static Mesh ExtrudeSection(IList<Vector2> section, float length, string name = "Extrusion")
        {
            int n = section.Count;
            var verts = new Vector3[n * 2];
            var uvs = new Vector2[verts.Length];
            float[] u = ProfileV(section);

            for (int i = 0; i < n; i++)
            {
                verts[i] = new Vector3(section[i].x, section[i].y, 0f);
                verts[i + n] = new Vector3(section[i].x, section[i].y, length);
                uvs[i] = new Vector2(u[i], 0f);
                uvs[i + n] = new Vector2(u[i], 1f);
            }

            var tris = new List<int>((n - 1) * 6);
            for (int i = 0; i < n - 1; i++)
            {
                int a = i, b = i + 1, c = i + n, d = i + 1 + n;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Seccion de una canaleta: arco circular de cuerda <paramref name="width"/>
        /// y flecha <paramref name="depth"/>, con los bordes a y=0.
        /// </summary>
        public static Vector2[] GutterSection(float width, float depth, int segments)
        {
            // Radio del circulo cuya cuerda mide "width" y cuya flecha mide "depth".
            float r = (width * width * 0.25f + depth * depth) / (2f * depth);
            float half = Mathf.Asin(Mathf.Clamp(width * 0.5f / r, -1f, 1f));
            float yc = r - depth;   // centro, para que el fondo quede en -depth y los bordes en 0

            var pts = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(-half, half, (float)i / segments);
                pts[i] = new Vector2(Mathf.Sin(a) * r, yc - Mathf.Cos(a) * r);
            }
            return pts;
        }

        /// <summary>
        /// Arco de circunferencia como seccion 2D, de <paramref name="startDegrees"/>
        /// a <paramref name="endDegrees"/>. Con 180 -> 0 sale una boveda ordenada
        /// de -X a +X, que es lo que pide ExtrudeSection.
        /// </summary>
        public static Vector2[] ArcSection(float radius, float startDegrees, float endDegrees, int segments)
        {
            var pts = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(startDegrees, endDegrees, (float)i / segments) * Mathf.Deg2Rad;
                pts[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
            }
            return pts;
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        /// <summary>Invierte el orden de los indices: las caras pasan a mirar al otro lado.</summary>
        public static void FlipWinding(Mesh mesh)
        {
            int[] tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i += 3)
            {
                int t = tris[i + 1];
                tris[i + 1] = tris[i + 2];
                tris[i + 2] = t;
            }
            mesh.triangles = tris;

            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++) normals[i] = -normals[i];
            mesh.normals = normals;
        }

        /// <summary>Anade un disco horizontal a una malla ya existente.</summary>
        public static void AppendDisc(Mesh mesh, float radius, float y, int segments, Vector3 facing)
        {
            var verts = new List<Vector3>(mesh.vertices);
            var uvs = new List<Vector2>(mesh.uv);
            var tris = new List<int>(mesh.triangles);

            int center = verts.Count;
            verts.Add(new Vector3(0f, y, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int s = 0; s < segments; s++)
            {
                float a = Mathf.PI * 2f * s / segments;
                verts.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
                uvs.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }

            bool up = facing.y > 0f;
            for (int s = 0; s < segments; s++)
            {
                int i0 = center + 1 + s;
                int i1 = center + 1 + (s + 1) % segments;
                if (up) { tris.Add(center); tris.Add(i1); tris.Add(i0); }
                else { tris.Add(center); tris.Add(i0); tris.Add(i1); }
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        static void AddQuad(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
                            Vector3 origin, Vector3 uDir, Vector3 vDir, Vector2 tiling, bool swapUV)
        {
            int b = verts.Count;
            Vector3 n = Vector3.Cross(uDir, vDir).normalized;

            verts.Add(origin);
            verts.Add(origin + uDir);
            verts.Add(origin + uDir + vDir);
            verts.Add(origin + vDir);
            for (int i = 0; i < 4; i++) norms.Add(n);

            var raw = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            for (int i = 0; i < 4; i++)
            {
                Vector2 uv = swapUV ? new Vector2(raw[i].y, raw[i].x) : raw[i];
                uvs.Add(new Vector2(uv.x * tiling.x, uv.y * tiling.y));
            }

            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
        }

        static Mesh Build(string name, List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
