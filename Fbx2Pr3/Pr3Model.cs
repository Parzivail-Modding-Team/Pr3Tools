using System.Collections.Generic;
using System.IO;
using System.Xml;
using Fbx;

namespace Fbx2Pr3
{
    internal class Pr3Model
    {
        public List<Pr3Object> Objects { get; }

        private Pr3Model(List<Pr3Object> objects)
        {
            Objects = objects;
        }

        public static Pr3Model FromFbx(FbxDocument f)
        {
            var objects = CollectObjects(f);

            return new Pr3Model(objects);
        }

        private static List<Pr3Object> CollectObjects(FbxDocument f)
        {
            var l = new List<Pr3Object>();

            var objects = f["Objects"].Nodes;

            foreach (var obj in objects)
            {
                if (obj == null || obj.Name != "Geometry")
                    continue;

                var vertices = CollectVertices(obj);
                var faces = CollectFaces(obj);
                var normals = CollectNormals(obj);
                var uvs = CollectUvs(obj);
//                var info = CollectInfo(obj);
                l.Add(new Pr3Object(vertices, faces, normals, uvs));
            }

            return l;
        }

        private static Pr3ObjectInfo CollectInfo(FbxNode f)
        {
            var rotation = GetNodeByValue(f["Properties70"], "Lcl Rotation");
            var translation = GetNodeByValue(f["Properties70"], "Lcl Translation");
            return new Pr3ObjectInfo();
        }

        private static object GetNodeByValue(FbxNode fbxNode, string value)
        {
            foreach (var node in fbxNode.Nodes)
            {
                if (node == null)
                    continue;

                if ((string)node.Value == value)
                    return node;
            }

            return null;
        }

        private static List<Vector3> CollectVertices(FbxNodeList f)
        {
            var vertices = new List<Vector3>();
            var raw = (double[])f["Vertices"].Value;

            for (var i = 0; i < raw.Length; i += 3) vertices.Add(new Vector3(raw[i], raw[i + 1], raw[i + 2]));

            return vertices;
        }

        private static List<Vector3> CollectNormals(FbxNodeList f)
        {
            var normals = new List<Vector3>();
            var raw = (double[])f["LayerElementNormal"]["Normals"].Value;

            for (var i = 0; i < raw.Length; i += 3) normals.Add(new Vector3(raw[i], raw[i + 1], raw[i + 2]));

            return normals;
        }

        private static List<Vector3> CollectUvs(FbxNodeList f)
        {
            var uvs = new List<Vector3>();
            var raw = (double[])f["LayerElementUV"]["UV"].Value;
            var indices = (int[])f["LayerElementUV"]["UVIndex"].Value;

            for (var i = 0; i < indices.Length; i += 2) uvs.Add(new Vector3(raw[indices[i]], raw[indices[i + 1]], 0));

            return uvs;
        }

        private static List<FacePointer> CollectFaces(FbxNodeList f)
        {
            var faces = new List<FacePointer>();
            var indices = (int[])f["PolygonVertexIndex"].Value;

            var queue = new List<int>();

            foreach (var item in indices)
            {
                var index = item;
                if (index < 0)
                {
                    index = ~index;
                    queue.Add(index);

                    switch (queue.Count)
                    {
                        case 3:
                            faces.Add(new FacePointer(queue[0], queue[1], queue[2], 0, false));
                            break;
                        case 4:
                            faces.Add(new FacePointer(queue[0], queue[1], queue[2], queue[3], true));
                            break;
                        default:
                            throw new InvalidDataException($"Unsupported face polygon: {queue.Count} sides");
                    }

                    queue.Clear();
                }
                else
                    queue.Add(index);
            }

            return faces;
        }
    }

    internal class Pr3ObjectInfo
    {
    }

    internal struct FacePointer
    {
        public int A;
        public int B;
        public int C;
        public int D;

        public bool IsQuad;

        public FacePointer(int a, int b, int c, int d, bool isQuad)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            IsQuad = isQuad;
        }
    }

    internal struct Vector3
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    internal class Pr3Object
    {
        public List<Vector3> Vertices { get; }
        public List<FacePointer> Faces { get; }
        public List<Vector3> Normals { get; }
        public List<Vector3> Uvs { get; }

        public Pr3Object(List<Vector3> vertices, List<FacePointer> faces, List<Vector3> normals, List<Vector3> uvs)
        {
            Vertices = vertices;
            Faces = faces;
            Normals = normals;
            Uvs = uvs;
        }
    }
}