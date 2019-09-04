using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Fbx2Pr3
{
    internal class Pr3Model
    {
        public List<Pr3Object> Objects { get; }

        private Pr3Model(List<Pr3Object> objects)
        {
            Objects = objects;
        }

        public static Pr3Model FromCollada(List<Geometry> geometry)
        {
            var objects = CollectObjects(geometry);

            return new Pr3Model(objects);
        }

        private static List<Pr3Object> CollectObjects(List<Geometry> geometry)
        {
            return geometry
                .Select(obj => new Pr3Object(obj.Name, obj.Vertices.ToList(), CreateFaces(obj), obj.Normals.ToList(), obj.Uvs.ToList().ToList(), obj.TransformationMatrix, obj.MaterialName))
                .ToList();
        }

        private static List<Pr3FacePointer> CreateFaces(Geometry geometry)
        {
            var faces = new List<Pr3FacePointer>();

            for (var i = 0; i < geometry.Indices.Length; i += 3)
                faces.Add(new Pr3FacePointer(geometry.Indices[i], geometry.Indices[i + 1], geometry.Indices[i + 2]));

            return faces;
        }
    }

    internal class Pr3ObjectInfo
    {
    }
}