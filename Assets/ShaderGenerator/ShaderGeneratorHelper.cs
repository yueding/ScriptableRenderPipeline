namespace UnityEditor.Experimental.Rendering
{
    public class ShaderGeneratorHelper
    {
        // This function is a helper to follow unity convertion
        // when converting fooBar ro FOO_BAR
        public static string InsertUnderscore(string name)
        {
            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsLower(name[i - 1]) && char.IsUpper(name[i]))
                {
                    // case switch, insert underscore
                    name = name.Insert(i, "_");
                }
            }

            return name;
        }
    }
}