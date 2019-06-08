using Assimp;

namespace Fbx2Pr3
{
    internal struct Pr3Bone
    {
        public string Name;
        public Matrix4x4 Transformation;
        public int? AssociatedMesh;
        public string Parent;

        public Pr3Bone(string name, Matrix4x4 transformation, int? associatedMesh, string parent)
        {
            Name = name;
            Transformation = transformation;
            AssociatedMesh = associatedMesh;
            Parent = parent;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Pr3Bone[Name={Name}, Parent={Parent}, Transformation={Transformation}, AssociatedMesh={AssociatedMesh}]";
        }
    }
}