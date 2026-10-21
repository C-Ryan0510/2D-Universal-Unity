using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NetworkMetro.Editor
{
    public static class MetroSampleSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/NetworkMetroM0.unity";
        private static readonly Color Ink = new Color32(232, 240, 248, 255);
        private static readonly Color Muted = new Color32(148, 169, 191, 255);

        [MenuItem("Network Metro/Create M0 Sample Scene")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log("Network Metro: opened existing M0 scene without overwriting it.");
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (font == null || circle == null)
                throw new InvalidOperationException("Required built-in UI resources are unavailable.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(12, 21, 34, 255);

            var canvasObject = new GameObject("Network Metro Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasObject.transform;

            Image background = Box(root, "Background", Vector2.zero, new Vector2(1280, 720),
                new Color32(12, 21, 34, 255));
            RectTransform bg = background.rectTransform;
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.offsetMin = Vector2.zero;
            bg.offsetMax = Vector2.zero;

            Text(root, font, "Kicker", "NETWORK METRO   /   UNITY 2D STUDY", -572, 293, 820, 26, 16, Muted);
            Text(root, font, "Title", "One route. Four stations.", -572, 234, 1000, 62, 42, Ink);
            Text(root, font, "Subtitle", "Git Push stages, shown as a metro map. Every value is a simulation.",
                -572, 178, 1130, 40, 21, Muted);

            Box(root, "Simulation badge", new Vector2(438, 292), new Vector2(268, 36),
                new Color32(23, 61, 65, 255));
            Text(root, font, "Simulation label", "SIMULATION / MANUAL PREVIEW", 310, 292, 256, 32, 14,
                new Color32(124, 229, 198, 255), TextAnchor.MiddleCenter);

            Box(root, "Map panel", new Vector2(0, -2), new Vector2(1144, 282),
                new Color32(20, 34, 51, 255));
            Text(root, font, "Route label", "01  /  GIT PUSH SUCCESS ROUTE", -540, 112, 800, 30, 16, Muted);

            // ### Self-made-Object/Scripts
            // 0. 建立參數assign common variable
            float[] xs = { -426, -142, 142, 426 };
            string[] names = { "Local", "Pack", "Remote", "Refs" };
            string[] details =
            {
                "Prepare local changes",
                "Package objects",
                "Receive simulated data",
                "Update remote reference"
            };
            // 1. 先把站生出來，存起來
            RectTransform stations = Rect(root, "Stations", Vector2.zero, new Vector2(1280, 720));
            MetroStationView[] stationViews = new MetroStationView[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform station = Rect(stations, "Station - " + names[i],
                    new Vector2(xs[i], 38), new Vector2(250, 200));
                Image halo = Box(station, "Lamp border", Vector2.zero, new Vector2(58, 58),
                    new Color32(57, 81, 105, 255));
                halo.sprite = circle;
                Image lamp = Box(station, "Lamp", Vector2.zero, new Vector2(42, 42), Color.gray);
                lamp.sprite = circle;
                UnityEngine.UI.Text title = Text(station, font, "Label", names[i], -125, -59, 250, 40,
                    28, Ink, TextAnchor.MiddleCenter);
                UnityEngine.UI.Text status = Text(station, font, "Status", "NOT STARTED", -125, -99,
                    250, 30, 16, Muted, TextAnchor.MiddleCenter);
                UnityEngine.UI.Text description = Text(station, font, "Description", details[i], -125,
                    -139, 250, 44, 16, Muted, TextAnchor.MiddleCenter);
                MetroStationView view = station.gameObject.AddComponent<MetroStationView>();
                view.Configure(names[i], details[i], lamp, title, status, description);
                stationViews[i] = view; // 存起來
            }
            // 2. 再生軌道 // ### 最後一位要 Configure
            RectTransform connections = Rect(root, "Connections", Vector2.zero, new Vector2(1280, 720));
            for (int i = 0; i < xs.Length - 1; i++)
            {
                Image trackImg = Box(connections, "Track " + (i+1), 
                    new Vector2((xs[i] + xs[i+1])/2, 38),
                    new Vector2(xs[i+1]-xs[i], 6), 
                    new Color32(57, 81, 105, 255));
                var connector = trackImg.gameObject.AddComponent<StationConnectorView>();
                connector.Configure(stationViews[i], stationViews[i+1], trackImg);
            }

            // RectTransform stations = Rect(root, "Stations", Vector2.zero, new Vector2(1280, 720));
            // string[] names = { "Local", "Pack", "Remote", "Refs" };
            // string[] details =
            // {
            //     "Prepare local changes",
            //     "Package objects",
            //     "Receive simulated data",
            //     "Update remote reference"
            // };
            // for (int i = 0; i < names.Length; i++)
            // {
            //     RectTransform station = Rect(stations, "Station - " + names[i],
            //         new Vector2(xs[i], 38), new Vector2(250, 200));
            //     Image halo = Box(station, "Lamp border", Vector2.zero, new Vector2(58, 58),
            //         new Color32(57, 81, 105, 255));
            //     halo.sprite = circle;
            //     Image lamp = Box(station, "Lamp", Vector2.zero, new Vector2(42, 42), Color.gray);
            //     lamp.sprite = circle;
            //     UnityEngine.UI.Text title = Text(station, font, "Label", names[i], -125, -59, 250, 40,
            //         28, Ink, TextAnchor.MiddleCenter);
            //     UnityEngine.UI.Text status = Text(station, font, "Status", "NOT STARTED", -125, -99,
            //         250, 30, 16, Muted, TextAnchor.MiddleCenter);
            //     UnityEngine.UI.Text description = Text(station, font, "Description", details[i], -125,
            //         -139, 250, 44, 16, Muted, TextAnchor.MiddleCenter);
            //     MetroStationView view = station.gameObject.AddComponent<MetroStationView>();
            //     view.Configure(names[i], details[i], lamp, title, status, description);
            // }

            Text(root, font, "Exercise heading", "YOUR FIRST INSPECTOR EXERCISE", -572, -208,
                1144, 28, 16, new Color32(124, 229, 198, 255));
            Text(root, font, "Exercise", "Select Stations > Station - Local in Hierarchy.\nChange State, Station Label or Description in Metro Station View.",
                -572, -256, 1144, 64, 21, Ink);
            Text(root, font, "Footer", "M0: static layout only. No event playback, real Git command or network request.",
                -572, -326, 1144, 28, 16, Muted);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = stations.GetChild(0).gameObject;
            Debug.Log("Network Metro M0 scene created: " + ScenePath);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Image image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // Text x is its left edge, while y is the vertical center, in reference canvas units.
        private static UnityEngine.UI.Text Text(Transform parent, Font font, string name, string value,
            float x, float y, float width, float height, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            RectTransform rect = Rect(parent, name, new Vector2(x + width / 2, y), new Vector2(width, height));
            UnityEngine.UI.Text text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
    }
}
