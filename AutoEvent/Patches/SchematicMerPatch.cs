using System.Diagnostics;
using System.IO;
using HarmonyLib;

namespace AutoEvent.Patches;

[HarmonyPatch(typeof(ProjectMER.ProjectMER), nameof(ProjectMER.ProjectMER.SchematicsDir), MethodType.Getter)]
public class SchematicMerPatch
{
    public static bool Prefix(ref string __result)
    {
        var stackTrace = new StackTrace();
        foreach (var frame in stackTrace.GetFrames())
        {
            var declaringType = frame.GetMethod().DeclaringType;
            var assemblyName = declaringType.Assembly.GetName().Name;

            if (!assemblyName.Contains("AutoEvent") || declaringType.Name == "SchematicMerPatch") 
                continue;
            __result = Path.Combine(AutoEvent.Singleton.Config.SchematicsDirectoryPath, "ProjectMER");
            return false;
        }
        return true;
    }
}