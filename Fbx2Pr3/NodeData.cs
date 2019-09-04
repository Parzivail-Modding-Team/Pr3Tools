using System.Collections.Generic;

namespace Fbx2Pr3
{
    internal class NodeData
    {
        public string ObjectName { get; }
        public List<float> Transformation { get; }
        public string MaterialName { get; }

        public NodeData(string objectName, List<float> transformation, string materialName)
        {
            ObjectName = objectName;
            Transformation = transformation;
            MaterialName = materialName;
        }
    }
}