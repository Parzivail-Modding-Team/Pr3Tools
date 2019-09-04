using System.Collections.Generic;

namespace Fbx2Pr3
{
    public class Geometry
    {
        public string Name { get; set; }
        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public Vector3[] Uvs { get; }
        public int[] Indices { get; }
        public List<float> TransformationMatrix { get; set; }
        public string MaterialName { get; set; }

        public Geometry(Vector3[] vertices, Vector3[] normals, Vector3[] uvs, int[] indices)
        {
            Vertices = vertices;
            Normals = normals;
            Uvs = uvs;
            Indices = indices;
        }
    }
}