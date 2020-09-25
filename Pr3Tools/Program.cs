using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Pr3Tools
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
                return -1;

            var collada = XElement.Parse(File.ReadAllText(args[0]));

            var scene = new GeometryLoader(collada).Load();
            var outputFile = args[1];

            if (scene == null)
                return -2;

            var model = Pr3Model.FromCollada(scene);

            WriteOutputFile(outputFile, model);

            return 0;
        }

        private static void WriteOutputFile(string outputFile, Pr3Model model)
        {
	        using var f = new BinaryWriter(File.Open(outputFile, FileMode.Create));

	        const string magic = "PR3";
	        const int version = 1;

	        var ident = magic.ToCharArray();

	        f.Write(ident);
	        f.Write(version);

	        f.Write(model.Objects.Count);

	        foreach (var pr3Object in model.Objects)
	        {
		        f.WriteNtString(pr3Object.Name);
		        f.WriteNtString(pr3Object.MaterialName);
		        WriteMatrix4(f, pr3Object.TransformationMatrix);
		        WriteLengthCodedVectors(f, pr3Object.Vertices);
		        WriteLengthCodedVectors(f, pr3Object.Normals);
		        WriteLengthCodedVectors(f, pr3Object.Uvs);
		        WriteLengthCodedFaces(f, pr3Object.Faces);
	        }
        }

        private static void WriteLengthCodedFaces(BinaryWriter f, List<Pr3FacePointer> faces)
        {
            f.Write(faces.Count);
            foreach (var face in faces)
            {
                f.Write(face.A);
                f.Write(face.B);
                f.Write(face.C);
            }
        }

        private static void WriteMatrix4(BinaryWriter f, List<float> matrix)
        {
            foreach (var value in matrix) f.Write(value);
        }

        private static void WriteLengthCodedVectors(BinaryWriter f, List<Vector3> v)
        {
            f.Write(v.Count);
            foreach (var vector3 in v)
            {
                f.Write(vector3.X);
                f.Write(vector3.Y);
                f.Write(vector3.Z);
            }
        }
    }
}
