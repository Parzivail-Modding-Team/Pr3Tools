using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Assimp;
using Brotli;

namespace Fbx2Pr3
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
                return -1;

            var importer = new AssimpContext();
            var scene = importer.ImportFile(args[0]);
            var outputFile = args[1];

            if (scene == null || !scene.HasMeshes)
                return -2;

            var armature = scene.RootNode.FindNode("Armature");
            var meshes = scene.RootNode;
            meshes.Children.Remove(armature);

            var bones = CreateBones(meshes, armature, null);

            WriteOutputFile(outputFile, scene, bones);

            return 0;
        }

        private static void WriteOutputFile(string outputFile, Scene scene, List<Pr3Bone> bones)
        {
            var s = new StreamWriter(outputFile);
            var bs = new BrotliStream(s.BaseStream, CompressionMode.Compress);
            using (var f = new BinaryWriter(bs))
            {
                const string magic = "PR3";
                const int version = 1;

                const byte flagHasParent = 0b00000001;
                const byte flagHasMesh = 0b00000010;

                var ident = magic.ToCharArray();

                f.Write(ident);
                f.Write(version);
                
                f.Write(scene.Meshes.Count);
                f.Write(bones.Count);

                foreach (var mesh in scene.Meshes)
                {
                    f.Write(mesh.Vertices.Count);
                    f.Write(mesh.Normals.Count);
                    f.Write(mesh.TextureCoordinateChannels[0].Count);
                    f.Write(mesh.Faces.Count);

                    foreach (var vertex in mesh.Vertices)
                    {
                        f.Write(vertex.X);
                        f.Write(vertex.Y);
                        f.Write(vertex.Z);
                    }

                    foreach (var vertex in mesh.TextureCoordinateChannels[0])
                    {
                        f.Write(vertex.X);
                        f.Write(vertex.Y);
                    }

                    foreach (var vertex in mesh.Normals)
                    {
                        f.Write(vertex.X);
                        f.Write(vertex.Y);
                        f.Write(vertex.Z);
                    }

                    foreach (var face in mesh.Faces)
                    {
                        f.Write(face.Indices.Count);
                        foreach (var i in face.Indices) f.Write(i);
                    }
                }

                foreach (var bone in bones)
                {
                    WriteNtString(f, bone.Name);

                    var flags = (byte)0;

                    if (bone.Parent != null)
                        flags |= flagHasParent;
                    if (bone.AssociatedMesh.HasValue)
                        flags |= flagHasMesh;

                    f.Write(flags);

                    if (bone.Parent != null)
                        WriteNtString(f, bone.Parent);

                    if (bone.AssociatedMesh.HasValue)
                        f.Write(bone.AssociatedMesh.Value);

                    f.Write(bone.Transformation.A1);
                    f.Write(bone.Transformation.A2);
                    f.Write(bone.Transformation.A3);
                    f.Write(bone.Transformation.A4);
                    f.Write(bone.Transformation.B1);
                    f.Write(bone.Transformation.B2);
                    f.Write(bone.Transformation.B3);
                    f.Write(bone.Transformation.B4);
                    f.Write(bone.Transformation.C1);
                    f.Write(bone.Transformation.C2);
                    f.Write(bone.Transformation.C3);
                    f.Write(bone.Transformation.C4);
                    f.Write(bone.Transformation.D1);
                    f.Write(bone.Transformation.D2);
                    f.Write(bone.Transformation.D3);
                    f.Write(bone.Transformation.D4);
                }
            }
        }

        private static void WriteNtString(BinaryWriter f, string s)
        {
            var buffer = Encoding.UTF8.GetBytes(s);
            f.Write(buffer);
            f.Write((byte)0);
        }

        private static List<Pr3Bone> CreateBones(Node meshes, Node armature, Node parent)
        {
            var list = new List<Pr3Bone>();
            foreach (var child in armature.Children) list.AddRange(CreateBones(meshes, child, armature));

            var assocMesh = meshes.FindNode(armature.Name)?.MeshIndices[0];
            list.Add(new Pr3Bone(armature.Name, armature.Transform, assocMesh, parent?.Name));

            return list;
        }
    }
}
