namespace UGF.EditorTools
{
    internal static class JenkinsBuildExecutionService
    {
        public static void ExecuteResourceBuild(JenkinsBuildResourceConfig config)
        {
            AppBuilderExecutionService.ExecuteJenkinsResourceBuild(config);
        }

        public static void ExecuteAppBuild(JenkinsBuildAppConfig config)
        {
            AppBuilderExecutionService.ExecuteJenkinsAppBuild(config);
        }
    }
}
