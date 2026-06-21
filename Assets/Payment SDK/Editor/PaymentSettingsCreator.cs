using GamePaymentSDK.Core;
using GamePaymentSDK.WebView.UniWebViewAdapter;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GamePaymentSDK.EditorTools
{
    /// <summary>
    /// Editor menu to create the default <see cref="PaymentSettings"/> asset under
    /// Assets/Resources/GamePayment so the runtime fallback loader can find it.
    /// </summary>
    public static class PaymentSettingsCreator
    {
        private const string CreateMenuPath = "Tools/KingKode/Game Payment SDK/Create Settings";
        private const string LocateMenuPath = "Tools/KingKode/Game Payment SDK/Locate Settings";
        private const string CreateBoostrapper = "Tools/KingKode/Game Payment SDK/Create Bootstrapper";
        private const string TargetFolder = "Assets/Resources/GamePayment";
        private const string AssetPath = TargetFolder + "/PaymentSettings.asset";

        [MenuItem(LocateMenuPath)]
        public static void LocatePaymentSettings()
        {
            PaymentSettings existing = AssetDatabase.LoadAssetAtPath<PaymentSettings>(AssetPath);

            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);

                return;
            }

            bool create = EditorUtility.DisplayDialog(
                "Payment Settings not found",
                $"No PaymentSettings asset exists at:\n{AssetPath}\n\n" +
                "The SDK loads it at runtime via Resources.Load, so it must live under a " +
                "'Resources' folder. Create it now?",
                "Create",
                "Cancel");

            if (create)
                CreatePaymentSettings();
        }

        [MenuItem(CreateMenuPath)]
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

        [MenuItem(CreateBoostrapper)]
        public static void CreatePaymentServiceObject()
        {
            // Root: PaymentService + PaymentBootstrap
            var root = new GameObject("PaymentService");
            Undo.RegisterCreatedObjectUndo(root, "Create Payment Service");
            Undo.AddComponent<PaymentBootstrap>(root);

            // Child: WebView + UniWebViewPaymentService
            var webView = new GameObject("WebView");
            Undo.RegisterCreatedObjectUndo(webView, "Create Payment Service");
            Undo.AddComponent<UniWebViewPaymentService>(webView);

            // Parent under the root (respects prefab/stage context and aligns transform)
            GameObjectUtility.SetParentAndAlign(webView, root);

            // Place the root sensibly in the active scene / current selection context
            GameObjectUtility.SetParentAndAlign(root, Selection.activeGameObject);

            // Select + ping, and mark the scene dirty so the change is saved
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
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
