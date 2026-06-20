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

        private SerializedProperty _baseUrl, _apiKey, _playerId, _environment;
        private SerializedProperty _requestTimeout, _webViewTimeout;
        private SerializedProperty _claimRetryCount, _pendingOrderCleanupDays, _processedTransactionHistoryDays;
        private SerializedProperty _logEnabled, _logLevel;

        private bool _showApiKey;

        private void OnEnable()
        {
            _baseUrl = serializedObject.FindProperty("_baseUrl");
            _apiKey = serializedObject.FindProperty("_apiKey");
            _playerId = serializedObject.FindProperty("_playerId");
            _environment = serializedObject.FindProperty("_environment");
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
            EditorGUILayout.Space(8);
            DrawStatusPill();

            EditorGUILayout.Space(6);
            Card("Payment Service", Accent, () =>
            {
                EditorGUILayout.PropertyField(_baseUrl, new GUIContent("Base URL"));
                DrawApiKeyField();
                EditorGUILayout.PropertyField(_playerId, new GUIContent("Player Id"));
            });

            Card("Environment", EnvColor(), () =>
            {
                EditorGUILayout.PropertyField(_environment, new GUIContent("Environment"));
                DrawEnvBadge();
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
            DrawEndpointPreview();
            EditorGUILayout.Space(4);
            DrawFooterButtons();

            serializedObject.ApplyModifiedProperties();
        }

        // ---- Header band ----
        private void DrawHeader()
        {
            Rect r = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, AccentDark);

            Rect strip = new Rect(r.x, r.y, r.width, r.height * 0.5f);
            EditorGUI.DrawRect(strip, Accent);

            var title = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 15,
                padding = new RectOffset(12, 8, 6, 0),
                alignment = TextAnchor.UpperLeft
            };
            var sub = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.85f) },
                padding = new RectOffset(12, 8, 0, 6),
                alignment = TextAnchor.LowerLeft
            };

            GUI.Label(new Rect(r.x, r.y, r.width, 30), "💳  Game Payment SDK", title);
            GUI.Label(new Rect(r.x, r.y + 28, r.width, 22), "Payment settings & runtime tuning", sub);
        }

        // ---- Status pill ----
        private void DrawStatusPill()
        {
            bool hasBaseUrl = !string.IsNullOrWhiteSpace(_baseUrl.stringValue);
            bool hasApiKey = !string.IsNullOrWhiteSpace(_apiKey.stringValue);
            bool hasPlayerId = !string.IsNullOrWhiteSpace(_playerId.stringValue);
            bool complete = hasBaseUrl && hasApiKey && hasPlayerId;

            string label = complete ? "✓  Configuration complete" : "!  Missing required fields";
            Color color = complete ? Ok : Warn;

            Rect r = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(color.r, color.g, color.b, 0.18f));
            EditorGUI.DrawRect(new Rect(r.x, r.y, 4, r.height), color);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color },
                padding = new RectOffset(12, 8, 4, 4)
            };
            GUI.Label(r, label, style);

            if (!complete)
            {
                if (!hasBaseUrl) Bullet("Base URL");
                if (!hasApiKey) Bullet("API Key");
                if (!hasPlayerId) Bullet("Player Id");
            }
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

        private void DrawEnvBadge()
        {
            bool sandbox = _environment.enumValueIndex == (int)PaymentEnvironment.Sandbox;
            string text = sandbox ? "SANDBOX" : "PRODUCTION";
            Color color = sandbox ? Sandbox : Production;

            Rect r = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            Rect badge = new Rect(r.x, r.y, 110, r.height);
            EditorGUI.DrawRect(badge, new Color(color.r, color.g, color.b, 0.20f));
            EditorGUI.DrawRect(new Rect(badge.x, badge.y, 4, badge.height), color);

            var s = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = color },
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(badge, text, s);
        }

        private void DrawEndpointPreview()
        {
            string baseUrl = (_baseUrl.stringValue ?? string.Empty).TrimEnd('/');
            string P(string suffix) => string.IsNullOrEmpty(baseUrl) ? "—" : baseUrl + suffix;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var head = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = Accent } };
                EditorGUILayout.LabelField("◈  Resolved endpoints", head);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Products", P("/v1/products"));
                    EditorGUILayout.TextField("Request", P("/v1/payments/request"));
                    EditorGUILayout.TextField("Claim", P("/v1/payments/claim"));
                }
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
            return _environment.enumValueIndex == (int)PaymentEnvironment.Sandbox ? Sandbox : Production;
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
