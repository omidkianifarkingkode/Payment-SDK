using GamePaymentSDK.Core;
using UnityEditor;
using UnityEngine;

namespace GamePaymentSDK.EditorTools
{
    /// <summary>
    /// Modern, sectioned inspector for <see cref="PaymentSettings"/> with a colored
    /// header, status pill, accent cards and a resolved-endpoint preview.
    /// </summary>
    [CustomEditor(typeof(PaymentSettings))]
    public sealed class PaymentSettingsEditor : UnityEditor.Editor
    {
        // ---- Palette ----
        private static readonly Color Accent = new Color(0.36f, 0.42f, 0.95f);   // indigo
        private static readonly Color AccentDark = new Color(0.24f, 0.28f, 0.70f);
        private static readonly Color Ok = new Color(0.18f, 0.70f, 0.45f);   // green
        private static readonly Color Warn = new Color(0.92f, 0.62f, 0.18f);   // amber
        private static readonly Color Sandbox = new Color(0.95f, 0.55f, 0.20f);   // orange
        private static readonly Color Production = new Color(0.18f, 0.70f, 0.45f);   // green

        private SerializedProperty _apiKey, _environment;
        private SerializedProperty _productionBaseUrl, _stagingBaseUrl;
        private SerializedProperty _productRoute, _requestRoute, _claimRoute;
        private SerializedProperty _requestTimeout, _webViewTimeout;
        private SerializedProperty _claimRetryCount, _pendingOrderCleanupDays, _processedTransactionHistoryDays;
        private SerializedProperty _logEnabled, _logLevel;

        private bool _showApiKey;

        private void OnEnable()
        {
            _apiKey = serializedObject.FindProperty("_apiKey");
            _environment = serializedObject.FindProperty("_environment");
            _productionBaseUrl = serializedObject.FindProperty("_productionBaseUrl");
            _stagingBaseUrl = serializedObject.FindProperty("_stagingBaseUrl");
            _productRoute = serializedObject.FindProperty("_productRoute");
            _requestRoute = serializedObject.FindProperty("_requestRoute");
            _claimRoute = serializedObject.FindProperty("_claimRoute");
            _requestTimeout = serializedObject.FindProperty("_requestTimeoutSeconds");
            _webViewTimeout = serializedObject.FindProperty("_webViewTimeoutSeconds");
            _claimRetryCount = serializedObject.FindProperty("_claimRetryCount");
            _pendingOrderCleanupDays = serializedObject.FindProperty("_pendingOrderCleanupDays");
            _processedTransactionHistoryDays = serializedObject.FindProperty("_processedTransactionHistoryDays");
            _logEnabled = serializedObject.FindProperty("_logEnabled");
            _logLevel = serializedObject.FindProperty("_logLevel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            DrawStatusPill();

            EditorGUILayout.Space(6);
            Card("Payment Service", Accent, () =>
            {
                DrawApiKeyField();
            });

            Card("Environment", EnvColor(), () =>
            {
                EditorGUILayout.PropertyField(_environment, new GUIContent("Environment"));
                EditorGUILayout.PropertyField(_productionBaseUrl, new GUIContent("Production Base URL"));
                EditorGUILayout.PropertyField(_stagingBaseUrl, new GUIContent("Staging Base URL"));
                EditorGUILayout.PropertyField(_productRoute, new GUIContent("Products Route"));
                EditorGUILayout.PropertyField(_requestRoute, new GUIContent("Request Route"));
                EditorGUILayout.PropertyField(_claimRoute, new GUIContent("Claim Route"));
            });

            Card("Timeouts (seconds)", Accent, () =>
            {
                EditorGUILayout.PropertyField(_requestTimeout, new GUIContent("Request Timeout"));
                EditorGUILayout.PropertyField(_webViewTimeout, new GUIContent("WebView Timeout"));
            });

            Card("Recovery / Cleanup", Accent, () =>
            {
                EditorGUILayout.PropertyField(_claimRetryCount, new GUIContent("Claim Retry Count"));
                EditorGUILayout.PropertyField(_pendingOrderCleanupDays, new GUIContent("Pending Order Cleanup (days)"));
                EditorGUILayout.PropertyField(_processedTransactionHistoryDays, new GUIContent("Processed History (days)"));
            });

            Card("Logging", Accent, () =>
            {
                EditorGUILayout.PropertyField(_logEnabled, new GUIContent("Enable Logs"));
                using (new EditorGUI.DisabledScope(!_logEnabled.boolValue))
                    EditorGUILayout.PropertyField(_logLevel, new GUIContent("Log Level"));
            });

            EditorGUILayout.Space(6);
            DrawFooterButtons();

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        // ---- Active environment helpers ----
        private bool IsProduction =>
            _environment.enumValueIndex == (int)PaymentEnvironment.Production;

        private SerializedProperty ActiveBaseUrlProp =>
            IsProduction ? _productionBaseUrl : _stagingBaseUrl;

        // ---- Header band ----
        private new void DrawHeader()
        {
            bool sandbox = _environment.enumValueIndex == (int)PaymentEnvironment.Staging;
            string text = sandbox ? "SANDBOX" : "PRODUCTION";

            Rect r = GUILayoutUtility.GetRect(0, 70, GUILayout.ExpandWidth(true));
            Rect strip = new Rect(r.x, r.y, r.width, r.height * 0.5f);
            EditorGUI.DrawRect(strip, sandbox ? Sandbox : Production);

            var title = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 15,
                padding = new RectOffset(12, 8, 6, 0),
                alignment = TextAnchor.MiddleLeft
            };

            GUI.Label(new Rect(r.x, r.y, r.width, 30), $"Game Payment SDK ({text})", title);
        }

        // ---- Status pill ----
        private void DrawStatusPill()
        {
            bool hasBaseUrl = !string.IsNullOrWhiteSpace(ActiveBaseUrlProp.stringValue);
            bool hasApiKey = !string.IsNullOrWhiteSpace(_apiKey.stringValue);
            bool complete = hasBaseUrl && hasApiKey;

            if (complete)
                return;

            string label = "!  Missing required fields";
            Color color = Warn;

            Rect r = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(color.r, color.g, color.b, 0.18f));
            EditorGUI.DrawRect(new Rect(r.x, r.y, 4, r.height), color);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color },
                padding = new RectOffset(12, 8, 4, 4)
            };
            GUI.Label(r, label, style);

            if (!hasBaseUrl) Bullet(IsProduction ? "Production Base URL" : "Staging Base URL");
            if (!hasApiKey) Bullet("API Key");
        }

        private static void Bullet(string text)
        {
            var s = new GUIStyle(EditorStyles.miniLabel) { padding = new RectOffset(20, 0, 0, 0) };
            EditorGUILayout.LabelField("• " + text, s);
        }

        private void DrawApiKeyField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_showApiKey)
                {
                    EditorGUILayout.PropertyField(_apiKey, new GUIContent("API Key"));
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    string masked = EditorGUILayout.PasswordField("API Key", _apiKey.stringValue);
                    if (EditorGUI.EndChangeCheck())
                        _apiKey.stringValue = masked;
                }

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = Accent;
                if (GUILayout.Button(_showApiKey ? "Hide" : "Show", EditorStyles.miniButton, GUILayout.Width(48)))
                    _showApiKey = !_showApiKey;
                GUI.backgroundColor = prev;
            }
        }

        private void DrawFooterButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = Accent;
                if (GUILayout.Button("Locate / Create Default Asset", GUILayout.Height(24)))
                    PaymentSettingsCreator.CreatePaymentSettings();
                GUI.backgroundColor = prev;

                if (GUILayout.Button("Ping", GUILayout.Height(24), GUILayout.Width(60)))
                    EditorGUIUtility.PingObject(target);
            }
        }

        private Color EnvColor()
        {
            return _environment.enumValueIndex == (int)PaymentEnvironment.Staging ? Sandbox : Production;
        }

        // ---- Accent card ----
        private static void Card(string title, Color accent, System.Action body)
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Rect tr = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(new Rect(tr.x, tr.y + 2, 3, 14), accent);

                var ts = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = accent },
                    padding = new RectOffset(10, 0, 0, 0)
                };
                GUI.Label(tr, title.ToUpperInvariant(), ts);

                EditorGUILayout.Space(2);
                EditorGUI.indentLevel++;
                body?.Invoke();
                EditorGUI.indentLevel--;
            }
        }
    }
}