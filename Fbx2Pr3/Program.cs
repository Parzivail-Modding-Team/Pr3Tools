using System;
using System.Collections.Generic;
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

            var armature = scene.RootNode.FindNode("Armature");
            var meshes = scene.RootNode;
            meshes.Children.Remove(armature);

            var bones = CreateBones(meshes, armature, null);

            return 0;
        }

        private static List<Pr3Bone> CreateBones(Node meshes, Node armature, Node parent)
        {
            var list = new List<Pr3Bone>();
            foreach (var child in armature.Children) list.AddRange(CreateBones(meshes, child, armature));

            var assocMesh = meshes.FindNode(armature.Name)?.MeshIndices[0];
            list.Add(new Pr3Bone(armature.Name, new Vector3D(0, 0, 0), assocMesh, parent?.Name));

            return list;
        }
    }

    internal struct Pr3Bone
    {
        public string Name;
        public Vector3D RotationPoint;
        public int? AssociatedMesh;
        public string Parent;

        public Pr3Bone(string name, Vector3D rotationPoint, int? associatedMesh, string parent)
        {
            Name = name;
            RotationPoint = rotationPoint;
            AssociatedMesh = associatedMesh;
            Parent = parent;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Pr3Bone[Name={Name}, Parent={Parent}, RotationPoint={RotationPoint}, AssociatedMesh={AssociatedMesh}]";
        }
    }
}
