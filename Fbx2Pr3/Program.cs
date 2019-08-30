using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Brotli;
using Fbx;
using Newtonsoft.Json;

namespace Fbx2Pr3
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
                return -1;

            var scene = FbxIO.ReadBinary(args[0]);
            var outputFile = args[1];

            if (scene == null || scene.Nodes.Count == 0)
                return -2;

//            using (var sw = new StreamWriter("fbx.txt"))
//                sw.Write(JsonConvert.SerializeObject(scene, Formatting.Indented));

            var model = Pr3Model.FromFbx(scene);

            WriteOutputFile(outputFile, model);

            return 0;
        }

        private static void WriteOutputFile(string outputFile, Pr3Model model)
        {
            var s = new StreamWriter(outputFile);
//            var bs = new BrotliStream(s.BaseStream, CompressionMode.Compress);
            using (var f = new BinaryWriter(s.BaseStream))
            {
                const string magic = "PR3";
                const int version = 1;

                var ident = magic.ToCharArray();

//                f.Write(ident);
//                f.Write(version);

                foreach (var pr3Object in model.Objects)
                {
                    WriteLengthCodedVectors(f, pr3Object.Vertices);
                    WriteLengthCodedVectors(f, pr3Object.Normals);
                    WriteLengthCodedVectors(f, pr3Object.Uvs);
                }
            }
        }

        private static void WriteLengthCodedVectors(BinaryWriter f, List<Vector3> v)
        {
//            f.Write(v.Count);
            foreach (var vector3 in v)
            {
                f.Write(vector3.X);
                f.Write(vector3.Y);
                f.Write(vector3.Z);
            }
        }
    }
}
