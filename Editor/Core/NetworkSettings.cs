using UnityEditor;
using UnityEngine;
using System;

namespace Yueby.NcmLyrics.Editor.Windows
{
    [InitializeOnLoad]
    public class NetworkSettings
    {
        private const string CONFIG_KEY_AUTO_ENABLE_HTTP = "Yueby.NcmLyrics.AutoEnableHttp";

        static NetworkSettings()
        {
            // Check and set network security settings when editor starts
            if (EditorPrefs.GetBool(CONFIG_KEY_AUTO_ENABLE_HTTP, true))
            {
                CheckAndSetNetworkSettings(false);
            }
        }

        private static void CheckAndSetNetworkSettings(bool showSuccessMessage = true)
        {
            bool needsUpdate = false;

            // Check HTTP settings
            if (!PlayerSettings.insecureHttpOption.HasFlag(InsecureHttpOption.AlwaysAllowed))
            {
                needsUpdate = true;
            }

            // Check if downloading is allowed
            if (!SecurityPolicyUtils.IsDownloadingAllowed())
            {
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                Debug.Log("[NcmLyrics] Enabling HTTP connection support...");
                try
                {
                    PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                    SecurityPolicyUtils.EnableDownloading();
                    AssetDatabase.SaveAssets();
                    if (showSuccessMessage)
                    {
                        Debug.Log("[NcmLyrics] HTTP connection support enabled");
                        EditorUtility.DisplayDialog("Success", "HTTP connection support has been enabled", "OK");
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Failed to enable HTTP connection support: {ex.Message}\n\nPossible solutions:\n" +
                        "1. Ensure Unity Editor has sufficient permissions\n" +
                        "2. Try restarting Unity Editor\n" +
                        "3. Check if ProjectSettings file is writable";
                    
                    Debug.LogError($"[NcmLyrics] {errorMsg}");
                    
                    if (EditorUtility.DisplayDialog("Error", 
                        errorMsg + "\n\nWould you like to retry?", 
                        "Retry", "Cancel"))
                    {
                        CheckAndSetNetworkSettings(showSuccessMessage);
                    }
                }
            }
            else if (showSuccessMessage)
            {
                EditorUtility.DisplayDialog("Notice", "HTTP connection support is already enabled", "OK");
            }
        }

        [MenuItem("Tools/YuebyTools/NcmLyrics/Network Settings/Auto Enable HTTP", true)]
        private static bool ValidateAutoEnableHttp()
        {
            Menu.SetChecked("Tools/YuebyTools/NcmLyrics/Network Settings/Auto Enable HTTP", 
                EditorPrefs.GetBool(CONFIG_KEY_AUTO_ENABLE_HTTP, true));
            return true;
        }

        [MenuItem("Tools/YuebyTools/NcmLyrics/Network Settings/Auto Enable HTTP")]
        private static void ToggleAutoEnableHttp()
        {
            bool current = EditorPrefs.GetBool(CONFIG_KEY_AUTO_ENABLE_HTTP, true);
            EditorPrefs.SetBool(CONFIG_KEY_AUTO_ENABLE_HTTP, !current);
            Debug.Log($"[NcmLyrics] Auto enable HTTP support has been {(!current ? "enabled" : "disabled")}");

            // If auto-enable is on, check and set immediately
            if (!current) // Switch to enabled state
            {
                CheckAndSetNetworkSettings(true);
            }
        }

        [MenuItem("Tools/YuebyTools/NcmLyrics/Network Settings/Status")]
        private static void CheckHttpStatus()
        {
            bool isHttpEnabled = PlayerSettings.insecureHttpOption.HasFlag(InsecureHttpOption.AlwaysAllowed);
            bool isDownloadingEnabled = SecurityPolicyUtils.IsDownloadingAllowed();
            bool autoEnable = EditorPrefs.GetBool(CONFIG_KEY_AUTO_ENABLE_HTTP, true);
            
            if ((!isHttpEnabled || !isDownloadingEnabled) && autoEnable)
            {
                if (EditorUtility.DisplayDialog("HTTP Connection Status", 
                    $"HTTP Connection Support: {(isHttpEnabled ? "Enabled" : "Disabled")}\n" +
                    $"Download Support: {(isDownloadingEnabled ? "Enabled" : "Disabled")}\n" +
                    $"Auto Enable: Enabled\n\n" +
                    "HTTP connection support is not fully enabled. Would you like to enable it now?", 
                    "Enable", "Cancel"))
                {
                    CheckAndSetNetworkSettings();
                }
            }
            else
            {
                string message = $"HTTP Connection Support: {(isHttpEnabled ? "Enabled" : "Disabled")}\n" +
                    $"Download Support: {(isDownloadingEnabled ? "Enabled" : "Disabled")}\n" +
                    $"Auto Enable: {(autoEnable ? "Enabled" : "Disabled")}";

                if (!isHttpEnabled && !isDownloadingEnabled && !autoEnable)
                {
                    message += "\n\nTip: Enable auto-enable to automatically configure HTTP connection support";
                }

                EditorUtility.DisplayDialog("HTTP Connection Status", message, "OK");
            }
        }
    }

    internal static class SecurityPolicyUtils
    {
        private const string SECURITY_POLICY_KEY = "Security.Policy";
        private const string DOWNLOADING_ALLOWED_KEY = "Security.Policy.Downloading.Allowed";

        public static bool IsDownloadingAllowed()
        {
            return EditorPrefs.GetBool(DOWNLOADING_ALLOWED_KEY, false);
        }

        public static void EnableDownloading()
        {
            EditorPrefs.SetBool(DOWNLOADING_ALLOWED_KEY, true);
        }
    }
} 