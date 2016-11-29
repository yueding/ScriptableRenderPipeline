using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public enum PackingRules
    {
        Exact,
        Aggressive
    };

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
    public class GenerateHLSL : System.Attribute
    {
        public PackingRules packingRules;
        public bool needAccessors; // Whether or not to generate the accessors
        public bool needParamDefines; // Wheter or not to generate define for each parameters of the struc
        public int paramDefinesStart; // Start of the generated define

        public GenerateHLSL(PackingRules rules = PackingRules.Exact, bool needAccessors = true, bool needParamDefines = false, int paramDefinesStart = 1)
        {
            packingRules = rules;
            this.needAccessors = needAccessors;
            this.needParamDefines = needParamDefines;
            this.paramDefinesStart = paramDefinesStart;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SurfaceDataAttributes : System.Attribute
    {
        public enum Semantic
        {
            None,
            Normal,
            Tangent,
            Opacity,
            AmbientOcclusion,
        }

        public string displayName { get; private set; }
        public int priority { get; private set; }
        public int[] filter { get; private set; }
        public Semantic semantic { get; private set; }

        public SurfaceDataAttributes(string _displayName = "", int _priority = 0, int[] _filter = null, Semantic _semantic = Semantic.None)
        {
            displayName = _displayName;
            priority = _priority;
            filter = _filter;
            semantic = _semantic;
        }
    }
}
