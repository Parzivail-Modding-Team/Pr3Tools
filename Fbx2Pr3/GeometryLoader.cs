using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fbx2Pr3
{
    public class GeometryLoader
    {
        private static readonly XNamespace XmlNamespace = "{http://www.collada.org/2005/11/COLLADASchema}";

        private readonly List<KeyValuePair<string, XElement>> _meshes;
        private readonly List<NodeData> _sceneData;
        private readonly List<Vertex> _vertices;
        private readonly List<int> _polyList;

        private List<Vector3> _normals;
        private List<Vector3> _textures;

        public GeometryLoader(XElement file)
        {
            _vertices = new List<Vertex>();
            _polyList = new List<int>();

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

                // Vertices
                var positionId = mesh
                    .Element($"{XmlNamespace}vertices")
                    .Element($"{XmlNamespace}input")
                    .Attribute("source").Value.TrimStart('#');

                var polylist = ReadVecArray<Vector3>(mesh, positionId);
                foreach (var poly in polylist)
                    _vertices.Add(new Vertex(_vertices.Count, poly));

                // Normals
                var normals = mesh
                    .Element($"{XmlNamespace}triangles")
                    .Elements($"{XmlNamespace}input").FirstOrDefault(x => x.Attribute("semantic").Value == "NORMAL");
                if (normals != null)
                {
                    var normalId = normals.Attribute("source").Value.TrimStart('#');

                    _normals = ReadVecArray<Vector3>(mesh, normalId);
                }

                // Textures
                var texCoords = mesh
                    .Element($"{XmlNamespace}triangles")
                    .Elements($"{XmlNamespace}input").FirstOrDefault(x => x.Attribute("semantic").Value == "TEXCOORD");
                if (texCoords != null)
                {
                    var texCoordId = texCoords.Attribute("source").Value.TrimStart('#');

                    _textures = ReadVecArray<Vector2>(mesh, texCoordId).Select(v => new Vector3(v.X, v.Y, 0)).ToList();
                }

                AssembleVertices(mesh);
                RemoveUnusedVertices();

                var geometry = ConvertBuffersToGeometry();
                var nodeData = _sceneData.First(data => data.ObjectName == objectName);

                geometry.Name = nodeData.ObjectName;
                geometry.TransformationMatrix = nodeData.Transformation;
                geometry.MaterialName = nodeData.MaterialName;

                objects.Add(geometry);

                _vertices.Clear();
                _polyList.Clear();

                _normals = null;
                _textures = null;
            }

            return objects;
        }

        private List<T> ReadVecArray<T>(XElement mesh, string id)
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

        private void AssembleVertices(XElement mesh)
        {
            var poly = mesh.Element($"{XmlNamespace}triangles");
            var typeCount = poly.Elements($"{XmlNamespace}input").Count();
            var id = ParseInts(poly.Element($"{XmlNamespace}p").Value);

            for (var i = 0; i < id.Count / typeCount; i++)
            {
                var textureIndex = -1;
                var index = 0;

                var posIndex = id[i * typeCount + index]; index++;
                var normalIndex = id[i * typeCount + index]; index++;

                if (_textures != null)
                {
                    textureIndex = id[i * typeCount + index];
                }

                ProcessVertex(posIndex, normalIndex, textureIndex);
            }
        }

        private void ProcessVertex(int posIndex, int normalIndex, int textureIndex)
        {
            var currentVertex = _vertices[posIndex];

            if (!currentVertex.IsSet)
            {
                currentVertex.NormalIndex = normalIndex;
                currentVertex.TextureIndex = textureIndex;
                _polyList.Add(posIndex);
            }
            else
            {
                HandleAlreadyProcessedVertex(currentVertex, normalIndex, textureIndex);
            }
        }

        private void HandleAlreadyProcessedVertex(Vertex previousVertex, int newNormalIndex, int newTextureIndex)
        {
            if (previousVertex.HasSameInformation(newNormalIndex, newTextureIndex))
            {
                _polyList.Add(previousVertex.Index);
                return;
            }

            if (previousVertex.DuplicateVertex != null)
            {
                HandleAlreadyProcessedVertex(previousVertex.DuplicateVertex, newNormalIndex, newTextureIndex);
                return;
            }

            var duplicateVertex = new Vertex(_vertices.Count, previousVertex.Position)
            {
                NormalIndex = newNormalIndex,
                TextureIndex = newTextureIndex
            };

            previousVertex.DuplicateVertex = duplicateVertex;

            _vertices.Add(duplicateVertex);
            _polyList.Add(duplicateVertex.Index);
        }

        private void RemoveUnusedVertices()
        {
            foreach (var vertex in _vertices)
            {
                if (vertex.IsSet) continue;

                vertex.NormalIndex = 0;
                vertex.TextureIndex = 0;
            }
        }

        private Geometry ConvertBuffersToGeometry()
        {
            var verticesArray = new Vector3[_vertices.Count];
            var normalsArray = new Vector3[_vertices.Count];

            Vector3[] texturesArray = null;

            if (_textures != null)
                texturesArray = new Vector3[_vertices.Count];

            for (var i = 0; i < _vertices.Count; i++)
            {
                var currentVertex = _vertices[i];

                verticesArray[i] = currentVertex.Position;
                normalsArray[i] = _normals[currentVertex.NormalIndex];

                if (texturesArray != null) texturesArray[i] = _textures[currentVertex.TextureIndex];
            }

            return new Geometry(verticesArray, normalsArray, texturesArray, _polyList.ToArray());
        }
    }
}
