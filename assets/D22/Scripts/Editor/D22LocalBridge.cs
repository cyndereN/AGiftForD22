using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;

namespace D22.Editor
{
    [InitializeOnLoad]
    public static class D22LocalBridge
    {
        static D22LocalBridge()
        {
            if (!Application.isBatchMode) EditorApplication.delayCall += Connect;
        }

        [MenuItem("D22/Connect Local MCP")]
        public static async void Connect()
        {
            // A loopback-only editor bridge; each collaborator runs their own server.
            EditorConfigurationCache.Instance.SetUseHttpTransport(false);
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            if (!MCPServiceLocator.Bridge.IsRunning)
                await MCPServiceLocator.Bridge.StartAsync();
        }
    }
}
