namespace UGF.EditorTools
{
    internal static class StripLinkConfigRepository
    {
        public static string[] GetProjectAssemblyDlls()
        {
            return StripLinkConfigTool.GetProjectAssemblyDlls();
        }

        public static string[] GetSelectedAssemblyDlls()
        {
            return StripLinkConfigTool.GetSelectedAssemblyDlls();
        }

        public static string[] GetSelectedAotDlls()
        {
            return StripLinkConfigTool.GetSelectedAotDlls();
        }

        public static string[] GetSelectedNetframeworkDlls()
        {
            return StripLinkConfigTool.GetSelectedNetframeworkDlls();
        }

        public static bool SaveLinkConfig(string[] stripList)
        {
            return StripLinkConfigTool.Save2LinkFile(stripList);
        }

        public static bool SaveAotDllList(string[] dllNames)
        {
            return StripLinkConfigTool.Save2AotDllList(dllNames);
        }

        public static bool SaveNetstandard2NetFrameworkConfig(string[] dllNames)
        {
            return StripLinkConfigTool.SaveNetstandard2NetFrameworkConfig(dllNames);
        }
    }
}
