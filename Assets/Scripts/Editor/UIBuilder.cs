using ARSurvival.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Builds the whole "ocean night" cartoon UI canvas (portrait, 1080x1920 reference) and wires
    /// every screen's serialized references. Run Tools > AR Survival > Rebuild UI to regenerate it.
    /// </summary>
    public static class UIBuilder
    {
        const string CanvasName = "UI";
        enum Font { Title, Body, Bold }

        [MenuItem("Tools/AR Survival/Rebuild UI")]
        public static void RebuildMenu()
        {
            var existing = GameObject.Find(CanvasName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);
            BuildIfMissing();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        /// <summary>Batch-mode entry point: regenerate UI art, rebuild the UI in the game scene and save it.</summary>
        public static void RebuildAndSave()
        {
            UIAssets.GenerateAll();
            ProjectPaths.OpenGameScene();
            RebuildMenu();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        public static void BuildIfMissing()
        {
            if (Object.FindAnyObjectByType<UIManager>() != null)
                return;
            if (UIAssets.TitleFont == null || UIAssets.Pill == null)
                UIAssets.GenerateAll();

            var canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Build UI");
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var safe = Stretch(Node("SafeArea", canvasGo.transform));
            safe.gameObject.AddComponent<SafeArea>();

            var leaderboard = BuildLeaderboard(safe);
            var mainMenu = BuildMainMenu(safe, leaderboard);
            var placement = BuildPlacement(safe);
            var hud = BuildHud(safe);
            var gameOver = BuildGameOver(safe, leaderboard);
            leaderboard.transform.SetAsLastSibling(); // overlay draws on top

            var manager = canvasGo.AddComponent<UIManager>();
            Wire(manager, ("mainMenu", mainMenu), ("placement", placement), ("hud", hud),
                ("gameOver", gameOver), ("leaderboard", leaderboard));
        }

        // ------------------------------------------------------------------ Screens

        static MainMenuScreen BuildMainMenu(RectTransform parent, LeaderboardScreen leaderboard)
        {
            var root = ScreenRoot("MainMenuScreen", parent);
            Backdrop(root, 0.5f);
            Decor(root);

            var title = CenterLabel("Title", root, "AR SURVIVAL", 140, UIStyle.Accent, Font.Title);
            title.Place(TopCenter, new Vector2(0f, -320f), new Vector2(1000f, 170f));
            title.localRotation = Quaternion.Euler(0f, 0f, 3f);
            var subtitle = CenterLabel("Subtitle", root, "SHOOTER", 112, UIStyle.Pink, Font.Title, 4f);
            subtitle.Place(TopCenter, new Vector2(0f, -465f), new Vector2(1000f, 140f));
            subtitle.localRotation = Quaternion.Euler(0f, 0f, -3f);
            CenterLabel("Tagline", root, "Hold your ground against the invaders\nright in your own room!", 38, UIStyle.TextPrimary, Font.Body)
                .Place(TopCenter, new Vector2(0f, -600f), new Vector2(960f, 110f));
            WaveStrip("Wave", root, TopCenter, new Vector2(0f, -690f), new Vector2(620f, 28f), UIStyle.Teal);

            var card = Card("Card", root, UIStyle.Panel, new Vector2(0.5f, 0f), new Vector2(0f, 500f), new Vector2(920f, 700f));
            CenterLabel("DifficultyLabel", card, "PICK YOUR MODE", 34, UIStyle.Teal, Font.Bold, 6f).Place(TopCenter, new Vector2(0f, -60f), new Vector2(860f, 50f));
            var shallow = Pill("Shallows", card, "SHALLOWS", UIStyle.Accent, UIStyle.Navy, new Vector2(390f, 110f), 44, Font.Bold);
            shallow.Place(TopCenter, new Vector2(-205f, -155f));
            var deep = Pill("DeepSea", card, "DEEP SEA", UIStyle.ButtonAlt, UIStyle.TextPrimary, new Vector2(390f, 110f), 44, Font.Bold);
            deep.Place(TopCenter, new Vector2(205f, -155f));
            var hint = CenterLabel("DifficultyHint", card, "", 32, UIStyle.TextMuted, Font.Body);
            hint.Place(TopCenter, new Vector2(0f, -250f), new Vector2(860f, 50f));

            var start = Pill("StartButton", card, "START", UIStyle.Accent, UIStyle.TextPrimary, new Vector2(780f, 170f), 84, Font.Title);
            start.Place(TopCenter, new Vector2(0f, -405f));
            var board = Pill("LeaderboardButton", card, "LEADERBOARD", UIStyle.ButtonAlt, UIStyle.TextPrimary, new Vector2(780f, 120f), 46, Font.Bold);
            board.Place(TopCenter, new Vector2(0f, -590f));

            CenterLabel("Credit", root, "Made by NTARE GAMA Allan", 32, UIStyle.TextMuted, Font.Bold)
                .Place(new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(900f, 50f));

            var screen = root.gameObject.AddComponent<MainMenuScreen>();
            Wire(screen, ("startButton", start.GetComponent<Button>()), ("leaderboardButton", board.GetComponent<Button>()),
                ("difficultyHint", hint.GetComponent<TMP_Text>()), ("leaderboard", leaderboard));
            WireArray(screen, "difficultyButtons", shallow.GetComponent<Button>(), deep.GetComponent<Button>());
            return screen;
        }

        static PlacementScreen BuildPlacement(RectTransform parent)
        {
            var root = ScreenRoot("PlacementScreen", parent);

            var card = Card("Card", root, UIStyle.Panel, TopCenter, new Vector2(0f, -205f), new Vector2(1000f, 330f));
            CenterLabel("Title", card, "FIND A SPOT", 80, UIStyle.Accent, Font.Title, 2f).Place(TopCenter, new Vector2(0f, -80f), new Vector2(940f, 100f));
            var instruction = CenterLabel("Instruction", card, "", 40, UIStyle.TextPrimary, Font.Body);
            instruction.Place(TopCenter, new Vector2(0f, -190f), new Vector2(920f, 100f));
            var status = CenterLabel("Status", card, "SEARCHING...", 34, UIStyle.TextMuted, Font.Bold, 6f);
            status.Place(TopCenter, new Vector2(0f, -280f), new Vector2(920f, 50f));

            // Scanning reticle: glowing ring with a sky flower in the middle.
            var reticle = Node("Reticle", root);
            reticle.Place(Center, new Vector2(0f, -60f), new Vector2(480f, 480f));
            Img("Ring", reticle, WithAlpha(UIStyle.Teal, 0.85f), UIAssets.Ring).rectTransform.Place(Center, Vector2.zero, new Vector2(480f, 480f));
            Img("Flower", reticle, WithAlpha(UIStyle.Pink, 0.9f), UIAssets.SkyFlower).rectTransform.Place(Center, Vector2.zero, new Vector2(170f, 170f));

            var back = Pill("BackButton", root, "BACK", UIStyle.ButtonAlt, UIStyle.TextPrimary, new Vector2(280f, 110f), 42, Font.Bold);
            back.Place(BottomLeft, new Vector2(180f, 120f));

            var screen = root.gameObject.AddComponent<PlacementScreen>();
            Wire(screen, ("instruction", instruction.GetComponent<TMP_Text>()), ("status", status.GetComponent<TMP_Text>()),
                ("reticle", reticle), ("backButton", back.GetComponent<Button>()),
                ("planeManager", Object.FindAnyObjectByType<ARPlaneManager>()));
            return screen;
        }

        static HudScreen BuildHud(RectTransform parent)
        {
            var root = ScreenRoot("HudScreen", parent);
            var vignette = Img("DamageVignette", root, new Color(UIStyle.Danger.r, 0.1f, 0.15f, 0f), UIAssets.Vignette);
            Stretch(vignette.rectTransform);

            // Top-left: health, 10 pill segments.
            var health = Card("HealthPanel", root, UIStyle.Panel, TopLeft, new Vector2(220f, -95f), new Vector2(400f, 130f));
            LeftLabel("Label", health, "HEALTH", 28, UIStyle.Pink, Font.Bold, 4f).Place(TopLeft, new Vector2(125f, -34f), new Vector2(200f, 40f));
            var healthValue = RightLabel("Value", health, "20", 52, UIStyle.TextPrimary, Font.Title);
            healthValue.Place(TopRight, new Vector2(-85f, -38f), new Vector2(130f, 60f));
            var segments = new Image[10];
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = Img($"Seg_{i}", health, UIStyle.Pink, UIAssets.Pill);
                SlicedPill(segments[i], 26f);
                segments[i].rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(-166f + i * 37f, 36f), new Vector2(32f, 26f));
            }

            // Top-centre: time left.
            var timerCard = Card("TimerPanel", root, UIStyle.Panel, TopCenter, new Vector2(0f, -95f), new Vector2(220f, 130f));
            CenterLabel("Label", timerCard, "TIME LEFT", 24, UIStyle.Teal, Font.Bold, 3f).Place(TopCenter, new Vector2(0f, -28f), new Vector2(210f, 36f));
            var timer = CenterLabel("TimerText", timerCard, "1:30", 68, UIStyle.TextPrimary, Font.Title);
            timer.Place(new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(210f, 80f));

            // Top-right: score and kills.
            var scoreCard = Card("ScorePanel", root, UIStyle.Panel, TopRight, new Vector2(-210f, -95f), new Vector2(380f, 130f));
            LeftLabel("Label", scoreCard, "SCORE", 26, UIStyle.Accent, Font.Bold, 4f).Place(TopLeft, new Vector2(115f, -30f), new Vector2(180f, 36f));
            var score = LeftLabel("ScoreText", scoreCard, "0", 62, UIStyle.Accent, Font.Title);
            score.Place(BottomLeft, new Vector2(115f, 44f), new Vector2(180f, 72f));
            var kills = RightLabel("KillsText", scoreCard, "KILLS  0", 30, UIStyle.TextMuted, Font.Bold, 1f);
            kills.Place(BottomRight, new Vector2(-120f, 38f), new Vector2(200f, 44f));
            var exit = Pill("ExitButton", root, "EXIT", UIStyle.ButtonAlt, UIStyle.TextPrimary, new Vector2(180f, 76f), 32, Font.Bold);
            exit.Place(TopRight, new Vector2(-110f, -212f));

            // Centre feedback: hit marker, kill feed, round banner.
            var hitMarker = Img("HitMarker", root, Color.white, UIAssets.HitMarker);
            hitMarker.rectTransform.Place(Center, Vector2.zero, new Vector2(84f, 84f));
            var killFeed = CenterLabel("KillFeed", root, "", 56, UIStyle.TextPrimary, Font.Title, 2f);
            killFeed.Place(Center, new Vector2(0f, 360f), new Vector2(960f, 80f));
            var banner = Node("RoundBanner", root);
            banner.Place(Center, new Vector2(0f, 170f), new Vector2(1000f, 280f));
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerGroup.interactable = false;
            WaveStrip("WaveTop", banner, TopCenter, new Vector2(0f, -12f), new Vector2(560f, 24f), UIStyle.Teal);
            var bannerTitle = CenterLabel("Title", banner, "HERE THEY COME!", 116, UIStyle.Accent, Font.Title);
            bannerTitle.Place(Center, new Vector2(0f, 20f), new Vector2(1000f, 140f));
            bannerTitle.localRotation = Quaternion.Euler(0f, 0f, 2f);
            var bannerSub = CenterLabel("Subtitle", banner, "", 40, UIStyle.TextPrimary, Font.Bold, 4f);
            bannerSub.Place(Center, new Vector2(0f, -78f), new Vector2(1000f, 50f));
            WaveStrip("WaveBottom", banner, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(560f, 24f), UIStyle.Teal);

            // Twin sticks on a virtual gamepad: left = move, right = aim (fires while pushed).
            Joystick(root, "MoveStick", "MOVE", BottomLeft, new Vector2(230f, 270f), UIStyle.Accent, "<Gamepad>/leftStick", false);
            Joystick(root, "AimStick", "AIM + FIRE", BottomRight, new Vector2(-230f, 270f), UIStyle.Pink, "<Gamepad>/rightStick", true);

            var screen = root.gameObject.AddComponent<HudScreen>();
            Wire(screen, ("healthText", healthValue.GetComponent<TMP_Text>()), ("scoreText", score.GetComponent<TMP_Text>()),
                ("killsText", kills.GetComponent<TMP_Text>()), ("timerText", timer.GetComponent<TMP_Text>()),
                ("exitButton", exit.GetComponent<Button>()), ("damageVignette", vignette), ("hitMarker", hitMarker),
                ("killFeed", killFeed.GetComponent<TMP_Text>()), ("roundBanner", bannerGroup),
                ("roundBannerSubtitle", bannerSub.GetComponent<TMP_Text>()));
            WireArray(screen, "healthSegments", segments);
            return screen;
        }

        static GameOverScreen BuildGameOver(RectTransform parent, LeaderboardScreen leaderboard)
        {
            var root = ScreenRoot("GameOverScreen", parent);
            Backdrop(root, 0.6f);
            Decor(root);

            var title = CenterLabel("Title", root, "WIPED OUT!", 130, UIStyle.Danger, Font.Title);
            title.Place(TopCenter, new Vector2(0f, -300f), new Vector2(1040f, 160f));
            title.localRotation = Quaternion.Euler(0f, 0f, 2f);
            CenterLabel("Report", root, "ROUND REPORT", 38, UIStyle.Teal, Font.Bold, 8f).Place(TopCenter, new Vector2(0f, -420f), new Vector2(1000f, 50f));
            var subtitle = CenterLabel("Subtitle", root, "MODE: SHALLOWS", 32, UIStyle.TextMuted, Font.Bold, 3f);
            subtitle.Place(TopCenter, new Vector2(0f, -470f), new Vector2(1000f, 44f));

            var card = Card("Stats", root, UIStyle.Panel, TopCenter, new Vector2(0f, -770f), new Vector2(920f, 460f));
            var score = StatRow(card, "FINAL SCORE", -90f, true, UIStyle.Accent);
            var kills = StatRow(card, "ENEMIES DEFEATED", -230f, true, UIStyle.TextPrimary);
            var time = StatRow(card, "TIME SURVIVED", -370f, false, UIStyle.TextPrimary);

            var restart = Pill("RestartButton", root, "RESTART", UIStyle.Accent, UIStyle.TextPrimary, new Vector2(780f, 150f), 72, Font.Title);
            restart.Place(new Vector2(0.5f, 0f), new Vector2(0f, 520f));
            var board = Pill("LeaderboardButton", root, "LEADERBOARD", UIStyle.ButtonAlt, UIStyle.TextPrimary, new Vector2(780f, 115f), 44, Font.Bold);
            board.Place(new Vector2(0.5f, 0f), new Vector2(0f, 370f));
            var menu = Pill("MenuButton", root, "MAIN MENU", UIStyle.ButtonAlt, UIStyle.TextPrimary, new Vector2(780f, 115f), 44, Font.Bold);
            menu.Place(new Vector2(0.5f, 0f), new Vector2(0f, 235f));

            var screen = root.gameObject.AddComponent<GameOverScreen>();
            Wire(screen, ("titleText", title.GetComponent<TMP_Text>()), ("subtitleText", subtitle.GetComponent<TMP_Text>()),
                ("scoreText", score), ("killsText", kills), ("timeText", time),
                ("restartButton", restart.GetComponent<Button>()), ("leaderboardButton", board.GetComponent<Button>()),
                ("menuButton", menu.GetComponent<Button>()), ("leaderboard", leaderboard));
            return screen;
        }

        static LeaderboardScreen BuildLeaderboard(RectTransform parent)
        {
            var root = ScreenRoot("LeaderboardScreen", parent);
            Stretch(Img("Dim", root, WithAlpha(UIStyle.Navy, 0.88f), null, true).rectTransform); // blocks touches beneath

            var card = Card("Card", root, UIStyle.PanelSolid, Center, Vector2.zero, new Vector2(1000f, 1300f));
            CenterLabel("Title", card, "LEADERBOARD", 92, UIStyle.Accent, Font.Title, 2f).Place(TopCenter, new Vector2(0f, -110f), new Vector2(940f, 110f));
            CenterLabel("Subtitle", card, "YOUR LAST 5 ROUNDS", 34, UIStyle.Teal, Font.Bold, 6f).Place(TopCenter, new Vector2(0f, -195f), new Vector2(940f, 48f));

            float[] widths = { 250f, 170f, 150f, 130f, 140f };
            var header = Node("Header", card);
            header.Place(TopCenter, new Vector2(0f, -275f), new Vector2(840f, 50f));
            Columns(header, widths, 30, UIStyle.Pink, Font.Bold, "DATE", "MODE", "SCORE", "KILLS", "TIME");
            WaveStrip("HeaderWave", card, TopCenter, new Vector2(0f, -310f), new Vector2(880f, 16f), WithAlpha(UIStyle.Teal, 0.6f));

            var rows = new (GameObject root, TMP_Text[] cells)[5];
            for (int i = 0; i < rows.Length; i++)
            {
                var rowImage = Img($"Row_{i}", card, new Color(0.45f, 0.72f, 1f, i % 2 == 0 ? 0.16f : 0.08f), UIAssets.Pill);
                SlicedPill(rowImage, 100f);
                var row = rowImage.rectTransform;
                row.Place(TopCenter, new Vector2(0f, -380f - i * 115f), new Vector2(900f, 100f));
                rows[i] = (row.gameObject, Columns(row, widths, 36, UIStyle.TextPrimary, Font.Bold, "-", "-", "-", "-", "-"));
            }

            var empty = CenterLabel("EmptyText", card, "No rounds yet!\nPress START to make a splash.", 40, UIStyle.TextMuted, Font.Bold);
            empty.Place(TopCenter, new Vector2(0f, -600f), new Vector2(880f, 160f));

            var close = Pill("CloseButton", card, "CLOSE", UIStyle.ButtonAlt, UIStyle.TextPrimary, new Vector2(900f, 120f), 46, Font.Bold);
            close.Place(new Vector2(0.5f, 0f), new Vector2(0f, 110f));

            var screen = root.gameObject.AddComponent<LeaderboardScreen>();
            Wire(screen, ("emptyText", empty.GetComponent<TMP_Text>()), ("closeButton", close.GetComponent<Button>()));
            var so = new SerializedObject(screen);
            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = rows.Length;
            string[] cellNames = { "date", "mode", "score", "kills", "time" };
            for (int i = 0; i < rows.Length; i++)
            {
                var element = rowsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = rows[i].root;
                for (int c = 0; c < cellNames.Length; c++)
                    element.FindPropertyRelative(cellNames[c]).objectReferenceValue = rows[i].cells[c];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        // ------------------------------------------------------------------ Building blocks

        static readonly Vector2 TopLeft = new(0f, 1f), TopCenter = new(0.5f, 1f), TopRight = new(1f, 1f);
        static readonly Vector2 BottomLeft = new(0f, 0f), BottomRight = new(1f, 0f), Center = new(0.5f, 0.5f);

        static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

        static RectTransform ScreenRoot(string screenName, RectTransform parent)
        {
            var rt = Stretch(Node(screenName, parent));
            rt.gameObject.AddComponent<CanvasGroup>();
            return rt;
        }

        /// <summary>Tints the camera feed deep-sea navy with a darker edge vignette.</summary>
        static void Backdrop(RectTransform root, float tint)
        {
            Stretch(Img("Dim", root, WithAlpha(UIStyle.Navy, tint), null).rectTransform);
            Stretch(Img("Vignette", root, WithAlpha(UIStyle.Navy, 0.95f), UIAssets.Vignette).rectTransform);
        }

        /// <summary>Glowing sky flowers, animated by <see cref="FloatingDecor"/>.</summary>
        static void Decor(RectTransform root)
        {
            var decor = Stretch(Node("Decor", root));
            (Vector2 pos, float size, Color color)[] flowerSpots =
            {
                (new Vector2(150f, 1780f), 230f, UIStyle.Teal), (new Vector2(930f, 1700f), 300f, UIStyle.Purple),
                (new Vector2(520f, 1860f), 170f, UIStyle.Pink), (new Vector2(80f, 1250f), 190f, UIStyle.Purple),
                (new Vector2(1010f, 1130f), 210f, UIStyle.Teal),
            };
            var flowers = new RectTransform[flowerSpots.Length];
            for (int i = 0; i < flowerSpots.Length; i++)
            {
                var (pos, size, color) = flowerSpots[i];
                flowers[i] = Img($"Flower_{i}", decor, WithAlpha(color, 0.75f), UIAssets.SkyFlower).rectTransform;
                flowers[i].Place(BottomLeft, pos, new Vector2(size, size));
            }

            var animator = decor.gameObject.AddComponent<FloatingDecor>();
            WireArray(animator, "flowers", flowers);
        }

        /// <summary>Rounded navy panel with a soft glowing teal frame.</summary>
        static RectTransform Card(string cardName, Transform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = Img(cardName, parent, color, UIAssets.Panel);
            image.type = Image.Type.Sliced;
            var rt = image.rectTransform;
            rt.Place(anchor, position, size);
            var frame = Img("Frame", rt, WithAlpha(UIStyle.Teal, 0.55f), UIAssets.PanelFrame);
            frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);
            return rt;
        }

        static void WaveStrip(string stripName, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var image = Img(stripName, parent, color, UIAssets.Wave);
            image.type = Image.Type.Tiled;
            image.pixelsPerUnitMultiplier = 32f / size.y; // one wave height per strip
            image.rectTransform.Place(anchor, position, size);
        }

        /// <summary>Makes a pill-sprite image have fully round ends at the given height.</summary>
        static void SlicedPill(Image image, float height)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 63f / (height / 2f);
        }

        static TMP_Text StatRow(RectTransform card, string label, float y, bool separator, Color valueColor)
        {
            var row = Node(label.Replace(" ", ""), card);
            row.Place(TopCenter, new Vector2(0f, y), new Vector2(800f, 110f));
            Stretch(LeftLabel("Label", row, label, 38, UIStyle.TextMuted, Font.Bold, 3f));
            var value = RightLabel("Value", row, "0", 76, valueColor, Font.Title);
            Stretch(value);
            if (separator)
                Img("Line", row, WithAlpha(UIStyle.Teal, 0.3f), null)
                    .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, -15f), new Vector2(800f, 3f));
            return value.GetComponent<TMP_Text>();
        }

        static void Joystick(RectTransform parent, string stickName, string caption, Vector2 anchor, Vector2 position,
            Color knobColor, string controlPath, bool crosshair)
        {
            var baseRt = Node(stickName, parent);
            baseRt.Place(anchor, position, new Vector2(320f, 320f));
            Img("Fill", baseRt, WithAlpha(UIStyle.Navy, 0.45f), UIAssets.Disc).rectTransform.Place(Center, Vector2.zero, new Vector2(300f, 300f));
            Img("Ring", baseRt, WithAlpha(UIStyle.Teal, 0.8f), UIAssets.Ring).rectTransform.Place(Center, Vector2.zero, new Vector2(320f, 320f));
            CenterLabel("Caption", baseRt, caption, 30, UIStyle.TextPrimary, Font.Bold, 3f).Place(TopCenter, new Vector2(0f, 32f), new Vector2(320f, 44f));

            var knob = Img("Knob", baseRt, WithAlpha(knobColor, 0.95f), UIAssets.Disc, true).rectTransform;
            knob.Place(Center, Vector2.zero, new Vector2(140f, 140f));
            if (crosshair)
            {
                Img("CrossH", knob, WithAlpha(UIStyle.Navy, 0.7f), null).rectTransform.Place(Center, Vector2.zero, new Vector2(60f, 7f));
                Img("CrossV", knob, WithAlpha(UIStyle.Navy, 0.7f), null).rectTransform.Place(Center, Vector2.zero, new Vector2(7f, 60f));
            }
            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            SetString(stick, "m_ControlPath", controlPath);
            SetFloat(stick, "m_MovementRange", 110f);
        }

        static TMP_Text[] Columns(RectTransform row, float[] widths, float size, Color color, Font font, params string[] texts)
        {
            var cells = new TMP_Text[texts.Length];
            float total = 0f;
            foreach (var w in widths)
                total += w;
            float x = -total / 2f;
            for (int i = 0; i < texts.Length; i++)
            {
                var cell = CenterLabel($"Col_{i}", row, texts[i], size, color, font).GetComponent<TMP_Text>();
                cell.textWrappingMode = TextWrappingModes.NoWrap; // table cells shrink rather than wrap
                cell.enableAutoSizing = true;
                cell.fontSizeMax = size;
                cell.fontSizeMin = size * 0.5f;
                cell.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                cell.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                cell.rectTransform.pivot = new Vector2(0f, 0.5f);
                cell.rectTransform.anchoredPosition = new Vector2(x, 0f);
                cell.rectTransform.sizeDelta = new Vector2(widths[i], 0f);
                x += widths[i];
                cells[i] = cell;
            }
            return cells;
        }

        /// <summary>Rounded pill button with a centred label.</summary>
        static RectTransform Pill(string buttonName, Transform parent, string text, Color fill, Color textColor, Vector2 size,
            float fontSize, Font font)
        {
            var image = Img(buttonName, parent, fill, UIAssets.Pill, true);
            SlicedPill(image, size.y);
            image.rectTransform.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            var label = Label("Label", image.transform, text, fontSize, textColor, TextAlignmentOptions.Center, font, 3f);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(20f, 0f);
            label.rectTransform.offsetMax = new Vector2(-20f, 0f);
            return image.rectTransform;
        }

        static RectTransform LeftLabel(string n, Transform p, string t, float s, Color c, Font f, float sp = 0f) =>
            Label(n, p, t, s, c, TextAlignmentOptions.Left, f, sp).rectTransform;
        static RectTransform RightLabel(string n, Transform p, string t, float s, Color c, Font f, float sp = 0f) =>
            Label(n, p, t, s, c, TextAlignmentOptions.Right, f, sp).rectTransform;
        static RectTransform CenterLabel(string n, Transform p, string t, float s, Color c, Font f, float sp = 0f) =>
            Label(n, p, t, s, c, TextAlignmentOptions.Center, f, sp).rectTransform;

        static TMP_Text Label(string labelName, Transform parent, string text, float size, Color color,
            TextAlignmentOptions alignment, Font font, float spacing = 0f)
        {
            var label = Node(labelName, parent).gameObject.AddComponent<TextMeshProUGUI>();
            Stretch(label.rectTransform);
            // Bold labels use the cartoon title face without the outline, always in capitals.
            label.font = font == Font.Body ? UIAssets.BodyFont : UIAssets.TitleFont;
            if (font == Font.Bold)
                label.fontStyle = FontStyles.UpperCase;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.characterSpacing = spacing;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            if (font == Font.Title)
            {
                // Cartoon-logo headings: navy outline + drop shadow, one line, shrink to fit.
                label.fontSharedMaterial = UIAssets.TitleMaterial;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMax = size;
                label.fontSizeMin = size * 0.5f;
            }
            return label;
        }

        static RectTransform Node(string nodeName, Transform parent)
        {
            var go = new GameObject(nodeName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static void Place(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static void Place(this RectTransform rt, Vector2 anchor, Vector2 position) => rt.Place(anchor, position, rt.sizeDelta);

        static Image Img(string imageName, Transform parent, Color color, Sprite sprite, bool raycast = false)
        {
            var image = Node(imageName, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = raycast;
            return image;
        }

        static void Wire(Object target, params (string property, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (property, value) in refs)
                so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireArray(Object target, string property, params Object[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(property);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string property, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
