using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Pr3Tools
{
    public class GeometryLoader
    {
        private static readonly XNamespace XmlNamespace = "{http://www.collada.org/2005/11/COLLADASchema}";

        private readonly List<KeyValuePair<string, XElement>> _meshes;
        private readonly List<NodeData> _sceneData;

        public GeometryLoader(XElement file)
        {
            _meshes = AssembleMeshes(file);
            _sceneData = AssembleScene(file);
        }

        private static List<KeyValuePair<string, XElement>> AssembleMeshes(XElement file)
        {
            return file
                .Element($"{XmlNamespace}library_geometries")
                .Elements()
                .Select(element => new KeyValuePair<string, XElement>(element.Attribute("name").Value, element.Element($"{XmlNamespace}mesh")))
                .ToList();
        }

        private static List<NodeData> AssembleScene(XElement file)
        {
            return file
                .Element($"{XmlNamespace}library_visual_scenes")
                .Element($"{XmlNamespace}visual_scene")
                .Elements()
                .Select(CreateNodeData)
                .ToList();
        }

        private static NodeData CreateNodeData(XElement element)
        {
            return new NodeData(element.Attribute("name").Value, ParseFloats(element.Element($"{XmlNamespace}matrix").Value),
                element
                    .Element($"{XmlNamespace}instance_geometry")
                    .Element($"{XmlNamespace}bind_material")
                    .Element($"{XmlNamespace}technique_common")
                    .Element($"{XmlNamespace}instance_material")
                    .Attribute("target").Value.Trim('#')
                );
        }

        private static List<float> ParseFloats(string input)
        {
            return input.Split(' ').Select(float.Parse).ToList();
        }

        private static List<int> ParseInts(string input)
        {
            return input.Split(' ').Select(int.Parse).ToList();
        }

        public List<Geometry> Load()
        {
            var objects = new List<Geometry>();
            foreach (var meshPair in _meshes)
            {
                var objectName = meshPair.Key;
                var mesh = meshPair.Value;

                var vertices = new List<Vertex>();
                var polyList = new List<int>();
                var normals = new List<Vector3>();
                var textures = new List<Vector3>();

                // Vertices
                var xVertices = mesh
                    .Element($"{XmlNamespace}vertices")
                    .Element($"{XmlNamespace}input")
                    .Attribute("source").Value.TrimStart('#');

                var polylist = ReadVecArray<Vector3>(mesh, xVertices);
                foreach (var poly in polylist)
                    vertices.Add(new Vertex(vertices.Count, poly));

                // Normals
                var xNormals = mesh
                    .Element($"{XmlNamespace}triangles")
                    .Elements($"{XmlNamespace}input").FirstOrDefault(x => x.Attribute("semantic").Value == "NORMAL");
                var normalId = xNormals.Attribute("source").Value.TrimStart('#');

                normals.AddRange(ReadVecArray<Vector3>(mesh, normalId));

                // Textures
                var xTexCoords = mesh
                    .Element($"{XmlNamespace}triangles")
                    .Elements($"{XmlNamespace}input").FirstOrDefault(x => x.Attribute("semantic").Value == "TEXCOORD");
                var texCoordId = xTexCoords.Attribute("source").Value.TrimStart('#');

                textures.AddRange(ReadVecArray<Vector2>(mesh, texCoordId).Select(v => new Vector3(v.X, v.Y, 0)));

                AssembleVertices(vertices, polyList, mesh);
                RemoveUnusedVertices(vertices);

                var geometry = ConvertBuffersToGeometry(polyList, vertices, normals, textures);
                var nodeData = _sceneData.First(data => data.ObjectName == objectName);

                geometry.Name = nodeData.ObjectName;
                geometry.TransformationMatrix = nodeData.Transformation;
                geometry.MaterialName = nodeData.MaterialName;

                objects.Add(geometry);
            }

            return objects;
        }

        private static List<T> ReadVecArray<T>(XElement mesh, string id)
        {
            var data = mesh
                .Elements($"{XmlNamespace}source").FirstOrDefault(x => x.Attribute("id").Value == id)
                .Element($"{XmlNamespace}float_array");

            var count = int.Parse(data.Attribute("count").Value);
            var array = ParseFloats(data.Value);
            var result = new List<T>();

            if (typeof(T) == typeof(Vector3))
                for (var i = 0; i < count / 3; i++)
                    result.Add((T)(object)new Vector3(
                        array[i * 3],
                        array[i * 3 + 1],
                        array[i * 3 + 2]
                    ));
            else if (typeof(T) == typeof(Vector2))
                for (var i = 0; i < count / 2; i++)
                    result.Add((T)(object)new Vector2(
                        array[i * 2],
                        array[i * 2 + 1]
                    ));

            return result;
        }

        private static void AssembleVertices(List<Vertex> vertices, List<int> polyList,  XElement mesh)
        {
            var poly = mesh.Element($"{XmlNamespace}triangles");
            var typeCount = poly.Elements($"{XmlNamespace}input").Count();
            var id = ParseInts(poly.Element($"{XmlNamespace}p").Value);

            for (var i = 0; i < id.Count / typeCount; i++)
            {
                var posIndex = id[i * typeCount + 0];
                var normalIndex = id[i * typeCount + 1];
                var textureIndex = id[i * typeCount + 2];

                ProcessVertex(vertices, polyList, posIndex, normalIndex, textureIndex);
            }
        }

        private static void ProcessVertex(List<Vertex> vertices, List<int> polyList, int posIndex, int normalIndex, int textureIndex)
        {
            var currentVertex = vertices[posIndex];

            if (!currentVertex.IsSet)
            {
                currentVertex.NormalIndex = normalIndex;
                currentVertex.TextureIndex = textureIndex;
                polyList.Add(posIndex);
            }
            else
            {
                HandleAlreadyProcessedVertex(vertices, polyList, currentVertex, normalIndex, textureIndex);
            }
        }

        private static void HandleAlreadyProcessedVertex(List<Vertex> vertices, List<int> polyList, Vertex previousVertex, int newNormalIndex, int newTextureIndex)
        {
            if (previousVertex.HasSameInformation(newNormalIndex, newTextureIndex))
            {
                polyList.Add(previousVertex.Index);
                return;
            }

            if (previousVertex.DuplicateVertex != null)
            {
                HandleAlreadyProcessedVertex(vertices, polyList, previousVertex.DuplicateVertex, newNormalIndex, newTextureIndex);
                return;
            }

            var duplicateVertex = new Vertex(vertices.Count, previousVertex.Position)
            {
                NormalIndex = newNormalIndex,
                TextureIndex = newTextureIndex
            };

            previousVertex.DuplicateVertex = duplicateVertex;

            vertices.Add(duplicateVertex);
            polyList.Add(duplicateVertex.Index);
        }

        private static void RemoveUnusedVertices(List<Vertex> vertices)
        {
            foreach (var vertex in vertices)
            {
                if (vertex.IsSet) continue;

                vertex.NormalIndex = 0;
                vertex.TextureIndex = 0;
            }
        }

        private static Geometry ConvertBuffersToGeometry(List<int> polyList, List<Vertex> vertices, List<Vector3> normals, List<Vector3> textures)
        {
            var verticesArray = new Vector3[vertices.Count];
            var normalsArray = new Vector3[vertices.Count];
            var texturesArray = new Vector3[vertices.Count];

            for (var i = 0; i < vertices.Count; i++)
            {
                var currentVertex = vertices[i];

                verticesArray[i] = currentVertex.Position;
                normalsArray[i] = normals[currentVertex.NormalIndex];
                texturesArray[i] = textures[currentVertex.TextureIndex];
            }

            return new Geometry(verticesArray, normalsArray, texturesArray, polyList.ToArray());
        }
    }
}
