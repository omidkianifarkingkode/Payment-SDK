using System.IO;
using GamePaymentSDK.Core;
using UnityEditor;
using UnityEngine;

namespace GamePaymentSDK.EditorTools
{
    /// <summary>
    /// Editor menu to create the default <see cref="PaymentSettings"/> asset under
    /// Assets/Resources/GamePayment so the runtime fallback loader can find it.
    /// </summary>
    public static class PaymentSettingsCreator
    {
        private const string MenuPath = "Tools/Game Payment SDK/Create Payment Settings";
        private const string TargetFolder = "Assets/Resources/GamePayment";
        private const string AssetPath = TargetFolder + "/PaymentSettings.asset";

        [MenuItem(MenuPath)]
        public static void CreatePaymentSettings()
        {
            PaymentSettings existing = AssetDatabase.LoadAssetAtPath<PaymentSettings>(AssetPath);

            if (existing == null)
            {
                // Also catch an asset that exists somewhere else in the project.
                string[] guids = AssetDatabase.FindAssets("t:PaymentSettings");
                if (guids.Length > 0)
                    existing = AssetDatabase.LoadAssetAtPath<PaymentSettings>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log(
                    $"[GamePaymentSDK] PaymentSettings already exists at " +
                    $"{AssetDatabase.GetAssetPath(existing)}. Selected it.",
                    existing);
                return;
            }

            EnsureFolder(TargetFolder);

            PaymentSettings asset = ScriptableObject.CreateInstance<PaymentSettings>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"[GamePaymentSDK] Created PaymentSettings at {AssetPath}", asset);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folder);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
