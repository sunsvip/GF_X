using System.Collections.Generic;
using UnityEngine;

namespace AnimationCompressor
{
    // Not acturally bone
    public class Bone
    {
        public Bone Parent { get; private set; }

        public string Path { get; private set; }

        public Dictionary<string, AnimationCurve> Curves { get; private set; }

        public Bone(string path)
        {
            Curves = new Dictionary<string, AnimationCurve>();

            Path = path;
        }

        public void SetCurve(string propertyName, AnimationCurve curve)
        {
            Curves[propertyName] = curve;
        }

        public void SetParent(Bone bone)
        {
            if (bone == null)
                return;

            Parent = bone;
        }

        public float Sample(string propertyName, float time)
        {
            var value = Curves[propertyName].Evaluate(time);

            if (Parent != null)
                value += Parent.Sample(propertyName, time);

            return value;
        }
    }
}