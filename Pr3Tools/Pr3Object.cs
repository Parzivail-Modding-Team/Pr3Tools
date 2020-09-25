using System.Collections.Generic;

namespace Pr3Tools
{
    internal class Pr3Object
    {
        public string Name { get; }
        public List<Vector3> Vertices { get; }
        public List<Pr3FacePointer> Faces { get; }
        public List<Vector3> Normals { get; }
        public List<Vector3> Uvs { get; }
        public List<float> TransformationMatrix { get; }
        public string MaterialName { get; }

        public Pr3Object(string name, List<Vector3> vertices, List<Pr3FacePointer> faces, List<Vector3> normals, List<Vector3> uvs, List<float> transformationMatrix, string materialName)
        {
            Name = name;
            Vertices = vertices;
            Faces = faces;
            Normals = normals;
            Uvs = uvs;
            TransformationMatrix = transformationMatrix;
            MaterialName = materialName;
        }
    }
}