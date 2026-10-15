using System;
using System.Diagnostics;
using System.IO;
using HarmonyLib;

namespace AutoEvent.Patches;

[HarmonyPatch(typeof(ProjectMER.ProjectMER), nameof(ProjectMER.ProjectMER.SchematicsDir), MethodType.Getter)]
public class SchematicMerPatch
{
    public static bool Prefix(ref string __result)
    {
        if (AutoEvent.Singleton is null)
            return true;

        StackFrame[] frames = new StackTrace().GetFrames();
        if (frames is null)
            return true;

        foreach (StackFrame frame in frames)
        {
            Type declaringType = frame.GetMethod()?.DeclaringType;
            if (declaringType is null || declaringType == typeof(SchematicMerPatch))
                continue;

            if (!declaringType.Assembly.GetName().Name.Contains("AutoEvent"))
                continue;
            __result = Path.Combine(AutoEvent.Singleton.Config.SchematicsDirectoryPath, "ProjectMER");
            return false;
        }

        return true;
    }
}