using UnityEditor;
using UnityEngine;
using System.Linq;

namespace LastDay.EditorTools
{
    /// <summary>
    /// Automatically adds/removes the LLMUNITY_AVAILABLE scripting define
    /// based on whether the LLMUnity package is installed.
    /// </summary>
    [InitializeOnLoad]
    public static class LLMUnityDefineSetup
    {
        private const string DEFINE = "LLMUNITY_AVAILABLE";

        static LLMUnityDefineSetup()
        {
            CheckAndSetDefine();
        }

        private static void CheckAndSetDefine()
        {
            bool llmUnityFound = System.AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "LLMUnity");

            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (target == BuildTargetGroup.Unknown)
                target = BuildTargetGroup.Standalone;

            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            var definesList = currentDefines.Split(';').Where(d => !string.IsNullOrEmpty(d)).ToList();
            bool hasDefine = definesList.Contains(DEFINE);

            if (llmUnityFound && !hasDefine)
            {
                definesList.Add(DEFINE);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", definesList));
                Debug.Log("[LastDay] LLMUnity detected. Added LLMUNITY_AVAILABLE define.");
            }
            else if (!llmUnityFound && hasDefine)
            {
                definesList.Remove(DEFINE);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", definesList));
                Debug.Log("[LastDay] LLMUnity not found. Removed LLMUNITY_AVAILABLE define.");
            }
        }
    }
}
