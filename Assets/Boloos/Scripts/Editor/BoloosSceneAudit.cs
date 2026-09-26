using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Boloos.EditorTools
{
    /// <summary>
    /// Revisa la escena abierta y senala lo que sigue siendo un placeholder:
    /// objetos con el cubo o la esfera por defecto de Unity, objetos sin malla
    /// y objetos con el material por defecto. Es lo que hay detras de que una
    /// maquina de retorno "no parezca una maquina": nunca se le puso el modelo.
    /// </summary>
    public static class BoloosSceneAudit
    {
        static readonly HashSet<string> PrimitiveMeshes = new HashSet<string>
        {
            "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad"
        };

        static readonly HashSet<string> DefaultMaterials = new HashSet<string>
        {
            "Default-Material", "Default-Diffuse", "Lit", "Default-Line", "Sprites-Default"
        };

        [MenuItem("Boloos/Auditar escena")]
        public static void Audit()
        {
            var primitives = new List<GameObject>();
            var missingMesh = new List<GameObject>();
            var defaultMaterial = new List<GameObject>();

            foreach (MeshFilter filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                if (!IsInOpenScene(filter)) continue;

                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    missingMesh.Add(filter.gameObject);
                    continue;
                }

                if (PrimitiveMeshes.Contains(mesh.name) && IsBuiltIn(mesh))
                {
                    primitives.Add(filter.gameObject);
                }
            }

            foreach (MeshRenderer renderer in Resources.FindObjectsOfTypeAll<MeshRenderer>())
            {
                if (!IsInOpenScene(renderer)) continue;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || DefaultMaterials.Contains(materials[i].name))
                    {
                        defaultMaterial.Add(renderer.gameObject);
                        break;
                    }
                }
            }

            var report = new StringBuilder();
            report.AppendLine("[Boloos] Auditoria de la escena");
            report.AppendLine("  Primitivas de Unity sin sustituir: " + primitives.Count);
            report.AppendLine("  Objetos sin malla asignada: " + missingMesh.Count);
            report.AppendLine("  Objetos con el material por defecto: " + defaultMaterial.Count);

            Append(report, "Primitivas", primitives);
            Append(report, "Sin malla", missingMesh);
            Append(report, "Material por defecto", defaultMaterial);

            Debug.Log(report.ToString());

            var selection = new List<Object>();
            selection.AddRange(primitives.ToArray());
            selection.AddRange(missingMesh.ToArray());
            Selection.objects = selection.ToArray();

            if (selection.Count > 0)
            {
                Debug.Log("[Boloos] Se han seleccionado los " + selection.Count +
                          " objetos que siguen siendo placeholders.");
            }
        }

        static void Append(StringBuilder report, string title, List<GameObject> objects)
        {
            if (objects.Count == 0) return;

            report.AppendLine();
            report.AppendLine("  " + title + ":");
            for (int i = 0; i < objects.Count && i < 40; i++)
            {
                report.AppendLine("    - " + Path(objects[i]));
            }
            if (objects.Count > 40) report.AppendLine("    ... y " + (objects.Count - 40) + " mas");
        }

        static string Path(GameObject go)
        {
            string path = go.name;
            Transform t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }

        static bool IsInOpenScene(Component component)
        {
            if (component == null) return false;
            if (EditorUtility.IsPersistent(component)) return false;
            return component.gameObject.scene.IsValid();
        }

        static bool IsBuiltIn(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) || path.StartsWith("Library/") || path.EndsWith("unity default resources");
        }
    }
}
