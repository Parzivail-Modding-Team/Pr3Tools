using System;
using System.Linq;
using Assimp;

namespace Fbx2Pr3
{
    class Program
    {
        static int Main(string[] args)
        {
            var importer = new AssimpContext();
            var scene = importer.ImportFile(args[0]);

            if (scene == null || !scene.HasMeshes)
                return -1;

            var objects = scene.RootNode;

            var armature = GetObjectByName(objects, "Armature");

            var meshParts = objects.Children.Where(node => node.Name != "Armature");

            foreach (var meshPart in meshParts)
            {
                var relatedBone = GetObjectByName(armature, meshPart.Name);
                Console.WriteLine($"Mesh {meshPart.Name} <-> Bone {relatedBone?.Name}");
            }

            return 0;
        }

        private static Node GetObjectByName(Node objects, string name)
        {
            foreach (var o in objects.Children)
            {
                var childrenResult = GetObjectByName(o, name);
                if (childrenResult != null)
                    return childrenResult;

                if (o.Name == name)
                    return o;
            }

            return null;
        }
    }
}
