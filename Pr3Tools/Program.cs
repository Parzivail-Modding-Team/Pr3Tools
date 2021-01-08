using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Pr3Tools
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length < 3)
				return -1;

			var collada = XElement.Parse(File.ReadAllText(args[0]));

			var scene = new GeometryLoader(collada).Load();

			if (scene == null)
				return -2;

			var model = Pr3Model.FromCollada(scene);

			WriteModelFile(args[1], model);
			WriteRiggingFile(args[2], model);

			return 0;
		}

		private static void WriteModelFile(string outputFile, Pr3Model model)
		{
			using var f = new BinaryWriter(File.Open(outputFile, FileMode.Create));

			const string magic = "PR3";
			const int version = 2;

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

		private static void WriteRiggingFile(string outputFile, Pr3Model model)
		{
			using var f = new BinaryWriter(File.Open(outputFile, FileMode.Create));

			const string magic = "PR3R";
			const int version = 1;

			var ident = magic.ToCharArray();

			f.Write(ident);
			f.Write(version);

			f.Write(model.Objects.Count);

			foreach (var pr3Object in model.Objects)
			{
				f.WriteNtString(pr3Object.Name);
				WriteMatrix4(f, pr3Object.TransformationMatrix);
			}
		}

		private static void WriteLengthCodedFaces(BinaryWriter f, List<Pr3FacePointer> faces)
		{
			f.Write7BitEncodedInt(faces.Count);
			foreach (var face in faces)
			{
				f.Write7BitEncodedInt(face.A);
				f.Write7BitEncodedInt(face.B);
				f.Write7BitEncodedInt(face.C);
			}
		}

		private static void WriteMatrix4(BinaryWriter f, List<float> matrix)
		{
			foreach (var value in matrix) f.Write(value);
		}

		private static void WriteLengthCodedVectors(BinaryWriter f, List<Vector3> v)
		{
			f.Write7BitEncodedInt(v.Count);
			foreach (var vector3 in v)
			{
				WriteAsHalf(f, vector3.X);
				WriteAsHalf(f, vector3.Y);
				WriteAsHalf(f, vector3.Z);
			}
		}

		private static void WriteAsHalf(BinaryWriter bw, float f)
		{
			var half = new HalfFloat(f);
			bw.Write(half.GetHalfPrecisionAsShort());
		}
	}
}