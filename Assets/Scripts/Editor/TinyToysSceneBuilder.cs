#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TinyToysSceneBuilder : EditorWindow
{
    // ── World palette ─────────────────────────────────────────────────────
    static readonly Color BG         = new Color(0.13f, 0.14f, 0.18f);
    static readonly Color MachineA   = new Color(0.24f, 0.52f, 0.80f);
    static readonly Color MachineB   = new Color(0.20f, 0.72f, 0.50f);
    static readonly Color DarkPanel  = new Color(0.10f, 0.11f, 0.15f);
    static readonly Color CyanScreen = new Color(0.05f, 0.80f, 0.90f, 0.9f);
    static readonly Color WorkerGold = new Color(0.95f, 0.75f, 0.20f);
    static readonly Color Belt       = new Color(0.28f, 0.30f, 0.34f);
    // ── UI palette ────────────────────────────────────────────────────────
    static readonly Color UI_Bg     = new Color(0.06f, 0.08f, 0.14f, 0.93f);
    static readonly Color UI_Blue   = new Color(0.24f, 0.55f, 0.90f);
    static readonly Color UI_Green  = new Color(0.18f, 0.72f, 0.46f);
    static readonly Color UI_Orange = new Color(0.90f, 0.45f, 0.08f);
    static readonly Color UI_Red    = new Color(0.85f, 0.20f, 0.15f);

    // ── Entry point ───────────────────────────────────────────────────────
    [MenuItem("TinyToys / Build Starter Scene")]
    public static void BuildScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeCamera();
        MakeBackground();
        MakeManagers();
        MakeFactory();
        MakeWorkers();
        MakeUI();

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
            "Assets/Scenes/GameScene.unity");
        Debug.Log("[TinyToysSceneBuilder] Scene built & saved!");
    }

    // ── Camera ────────────────────────────────────────────────────────────
    static void MakeCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        go.AddComponent<AudioListener>();
    }

    // ── Background ────────────────────────────────────────────────────────
    static void MakeBackground()
    {
        var root = new GameObject("[Background]");

        // Load the new grassy factory background
        var bgSp = GetSprite("Assets/Sprites/factory_bg.png");
        if (bgSp != null)
        {
            var bg = Spr("Background", root, new Vector3(0, 0, 5f), Vector3.one, Color.white);
            var sr = bg.GetComponent<SpriteRenderer>();
            sr.sprite = bgSp;

            float camHeight = 10f; // orthographic 5 * 2
            float camWidth = 10f * (1920f / 1080f);
            float scaleX = camWidth / sr.sprite.bounds.size.x;
            float scaleY = camHeight / sr.sprite.bounds.size.y;
            float scale = Mathf.Max(scaleX, scaleY);
            bg.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            // Fallback primitive background
            Spr("Grass",   root, new Vector3(0,-1f,1f),  new Vector3(18f,8f,1f),   new Color(0.35f, 0.60f, 0.25f)); // Green grass color
            Spr("Sky",     root, new Vector3(0,4.5f,1f), new Vector3(18f,0.4f,1f), new Color(0.60f, 0.85f, 0.95f)); // Sky color
        }
        
        // Removed conveyor entirely as per user request.
    }

    // ── Managers ──────────────────────────────────────────────────────────
    static void MakeManagers()
    {
        var root = new GameObject("[Managers]");
        var gm   = Mgr<GameManager>(root,        "GameManager");
        var rm   = Mgr<ResourceManager>(root,    "ResourceManager");
        var pm   = Mgr<ProductionManager>(root,  "ProductionManager");
        var om   = Mgr<OrderManager>(root,       "OrderManager");
        var pd   = Mgr<PressureDirector>(root,   "PressureDirector");
        
        // Add PlayerInteraction globally
        gm.gameObject.AddComponent<PlayerInteraction>();

        // Load GameplayConfig SO if it exists
        var cfg = AssetDatabase.LoadAssetAtPath<GameplayConfig>("Assets/ScriptableObjects/GameplayConfig.asset");
        if (cfg != null)
        {
            gm.config = cfg;
            om.config = cfg;
            pd.config = cfg;
            Debug.Log("[SceneBuilder] GameplayConfig wired to managers.");
        }
        else Debug.LogWarning("[SceneBuilder] GameplayConfig.asset not found at Assets/ScriptableObjects/GameplayConfig.asset — create it manually.");

        om.availableOrders = new List<OrderData>();
        foreach (var g in AssetDatabase.FindAssets("t:OrderData", new[]{"Assets/ScriptableObjects/Orders"}))
            om.availableOrders.Add(AssetDatabase.LoadAssetAtPath<OrderData>(AssetDatabase.GUIDToAssetPath(g)));

        pd.eventPool = new List<RandomEventData>();
        foreach (var g in AssetDatabase.FindAssets("t:RandomEventData", new[]{"Assets/ScriptableObjects/Events"}))
            pd.eventPool.Add(AssetDatabase.LoadAssetAtPath<RandomEventData>(AssetDatabase.GUIDToAssetPath(g)));
    }

    // ── Factory ───────────────────────────────────────────────────────────
    static void MakeFactory()
    {
        // Force Unity to load the transparent assets we just fixed with PowerShell
        AssetDatabase.ImportAsset("Assets/Sprites/assembly.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Sprites/paint.png", ImportAssetOptions.ForceUpdate);

        var root = new GameObject("[FactoryMap]");
        var mA = MachinePrefab("MachineA_Assembly", Machine.MachineType.AssemblyA, new Vector3(-6.5f, 2.5f, 0f), MachineA);
        var mB = MachinePrefab("MachineB_Paint",    Machine.MachineType.PaintPackB, new Vector3(-0.5f, -2.5f, 0f),  MachineB);
        mA.transform.SetParent(root.transform);
        mB.transform.SetParent(root.transform);
        var car = AssetDatabase.LoadAssetAtPath<ProductData>("Assets/ScriptableObjects/Products/Product_ToyCar.asset");
        if (car != null)
        {
            mA.GetComponent<Machine>().assignedProduct = car;
            mB.GetComponent<Machine>().assignedProduct = car;
        }
        else Debug.LogWarning("[SceneBuilder] Product_ToyCar not found — run Generate Starter Data first!");
    }

    // ── Workers ───────────────────────────────────────────────────────────
    static void MakeWorkers()
    {
        var root = new GameObject("[Workers]");
        float[] xs = {-1.3f, 0f, 1.3f};
        // (Removed static workers as requested)
    }

    // ── Full UI ───────────────────────────────────────────────────────────
    static void MakeUI()
    {
        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Canvas
        var cvGO = new GameObject("Canvas");
        var cv   = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 50;
        cv.pixelPerfect = false; // Tắt pixel perfect vì trên vài màn hình nó bị lỗi scale
        
        var csc = cvGO.AddComponent<CanvasScaler>();
        csc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        csc.referenceResolution = new Vector2(1920, 1080);
        csc.matchWidthOrHeight = 0.5f; // Khôi phục lại cài đặt gốc cân bằng màn hình ngang/dọc
        cvGO.AddComponent<GraphicRaycaster>();

        // HUD root (full screen)
        var hud = Rect("HUD", cvGO); Anch(hud, 0,0,1,1);

        // Timer (top centre)
        var timerPanel = Panel("TimerPanel",  hud, 0.40f, 0.90f, 0.60f, 0.98f);
        var timerText  = TMP("TimerText",  timerPanel, "03:00", 36, Color.white, TextAlignmentOptions.Center); Anch(timerText.gameObject,0,0.25f,1,1);
        var timerBar   = Img("TimerBar",   timerPanel, UI_Blue); Anch(timerBar.gameObject,0.04f,0.04f,0.96f,0.22f);
        timerBar.type = Image.Type.Filled; timerBar.fillMethod = Image.FillMethod.Horizontal; timerBar.fillAmount = 1f;

        // Resources (left)
        var resPanel   = Panel("ResPanel",  hud, 0.01f, 0.25f, 0.12f, 0.85f);
        var woodText   = TMP("WoodText",   resPanel, "Wood: 50/100",   14, new Color(0.8f,0.95f,0.55f)); Anch(woodText.gameObject,   0.05f,0.76f,0.95f,1f);
        var paintText  = TMP("PaintText",  resPanel, "Paint: 50/100",  14, new Color(0.95f,0.65f,0.90f)); Anch(paintText.gameObject,  0.05f,0.52f,0.95f,0.76f);
        var powerText  = TMP("PowerText",  resPanel, "Power: 100/100", 14, new Color(0.95f,0.90f,0.35f)); Anch(powerText.gameObject,  0.05f,0.28f,0.95f,0.52f);
        var workerText = TMP("WorkerText", resPanel, "Workers: 3/3",   14, new Color(0.55f,0.85f,1.0f));  Anch(workerText.gameObject, 0.05f,0.04f,0.95f,0.28f);

        // Economy (top right)
        var econPanel  = Panel("EconPanel", hud, 0.84f, 0.88f, 0.99f, 0.98f);
        var credText   = TMP("CreditsText", econPanel, "$200",    24, UI_Green,  TextAlignmentOptions.Right); Anch(credText.gameObject,   0.05f,0.52f,0.95f,0.96f);
        var repText    = TMP("RepText",     econPanel, "Rep: 100", 16, Color.white,TextAlignmentOptions.Right); Anch(repText.gameObject,    0.05f,0.06f,0.95f,0.50f);

        // Tier (right, below economy)
        var tierPanel = Panel("TierPanel", hud, 0.84f, 0.78f, 0.99f, 0.86f);
        var tierText  = TMP("TierText", tierPanel, "Pressure T1", 14, UI_Orange, TextAlignmentOptions.Center); Anch(tierText.gameObject, 0.05f,0.55f,0.95f,0.98f);
        var tierDots  = new Image[5];
        for (int i = 0; i < 5; i++) {
            var dot = Img($"Tier{i+1}", tierPanel, Color.gray);
            Anch(dot.gameObject, 0.1f+i*0.16f, 0.15f, 0.22f+i*0.16f, 0.45f);
            tierDots[i] = dot;
        }

        // Status bar (bottom centre)
        var statPanel = Panel("StatusPanel", hud, 0.25f, 0.01f, 0.75f, 0.08f);
        var procAText = TMP("ProcAText",  statPanel, "A: Idle",     16, UI_Blue,  TextAlignmentOptions.Left);   Anch(procAText.gameObject, 0.05f,0.1f,0.30f,0.9f);
        var bufText   = TMP("BufferText", statPanel, "Buffer: 0/5", 16, Color.white,TextAlignmentOptions.Center);Anch(bufText.gameObject,   0.35f,0.1f,0.65f,0.9f);
        var procBText = TMP("ProcBText",  statPanel, "B: Idle",     16, UI_Green, TextAlignmentOptions.Right);  Anch(procBText.gameObject, 0.70f,0.1f,0.95f,0.9f);

        // Event & Pause buttons
        var emergBtn = Btn("EmergencyBuyBtn", hud, "Emergency Buy", UI_Orange); Anch(emergBtn, 0.01f, 0.90f, 0.15f, 0.98f);
        var pauseBtn = Btn("PauseBtn",        hud, "Pause",          new Color(0.2f,0.2f,0.35f)); Anch(pauseBtn, 0.92f, 0.01f, 0.99f, 0.06f);

        // Win panel
        var winPanel  = Panel("WinPanel",  cvGO, 0f,0f,1f,1f, new Color(0.04f,0.18f,0.08f,0.96f));
        var winTitle  = TMP("WinTitle",    winPanel, "ORDER COMPLETE!", 70, UI_Green,  TextAlignmentOptions.Center); Anch(winTitle.gameObject,  0.1f,0.55f,0.9f,0.80f);
        var winCredTxt= TMP("WinCredText", winPanel, "Credits Earned: $0", 30, Color.white,TextAlignmentOptions.Center); Anch(winCredTxt.gameObject,0.1f,0.40f,0.9f,0.56f);
        winPanel.SetActive(false);

        // Lose panel
        var losePanel = Panel("LosePanel", cvGO, 0f,0f,1f,1f, new Color(0.18f,0.04f,0.04f,0.96f));
        var loseTxt   = TMP("LoseTitle",   losePanel, "FACTORY FAILED",   70, UI_Red,   TextAlignmentOptions.Center); Anch(loseTxt.gameObject,  0.1f,0.55f,0.9f,0.80f);
        var loseSubTxt= TMP("LoseSub",     losePanel, "Better luck next time!",28,Color.white,TextAlignmentOptions.Center); Anch(loseSubTxt.gameObject,0.1f,0.40f,0.9f,0.56f);
        losePanel.SetActive(false);

        // Event popup
        var popup     = Panel("EventPopup", cvGO, 0.30f, 0.30f, 0.70f, 0.70f, new Color(0.06f,0.08f,0.17f,0.97f));
        var evName    = TMP("EvName",  popup, "Event Name",  24, UI_Orange,  TextAlignmentOptions.Center); Anch(evName.gameObject,  0.04f,0.76f,0.96f,0.97f);
        var evDesc    = TMP("EvDesc",  popup, "Description", 14, Color.white, TextAlignmentOptions.Center); Anch(evDesc.gameObject,  0.04f,0.58f,0.96f,0.76f);
        var prioBtn   = Btn("PrioBtn",   popup, "Push Through",  UI_Orange); Anch(prioBtn,   0.05f,0.36f,0.30f,0.58f);
        var repairBtn = Btn("RepairBtn", popup, "Repair",          UI_Blue);  Anch(repairBtn, 0.37f,0.36f,0.63f,0.58f);
        var tradeBtn  = Btn("TradeBtn",  popup, "Spend Credits",  UI_Green); Anch(tradeBtn,  0.70f,0.36f,0.95f,0.58f);
        var prioDesc  = TMP("PrioDesc",  popup, "", 10, new Color(0.9f,0.8f,0.6f), TextAlignmentOptions.Center); Anch(prioDesc.gameObject,  0.05f,0.04f,0.30f,0.36f);
        var repDesc   = TMP("RepDesc",   popup, "", 10, new Color(0.7f,0.85f,1.0f), TextAlignmentOptions.Center); Anch(repDesc.gameObject,   0.37f,0.04f,0.63f,0.36f);
        var tradDesc  = TMP("TradDesc",  popup, "", 10, new Color(0.6f,0.95f,0.7f), TextAlignmentOptions.Center); Anch(tradDesc.gameObject,  0.70f,0.04f,0.95f,0.36f);
        popup.SetActive(false);

        // Wire UIManager
        var uiMgrGO = new GameObject("UIManager");
        uiMgrGO.transform.SetParent(cvGO.transform, false);
        var uim = uiMgrGO.AddComponent<UIManager>();
        uim.timerText          = timerText;
        uim.timerFillBar       = timerBar;
        uim.woodPlasticText    = woodText;
        uim.paintFabricText    = paintText;
        uim.powerText          = powerText;
        uim.workerText         = workerText;
        uim.creditsText        = credText;
        uim.reputationText     = repText;
        uim.tierText           = tierText;
        uim.tierIndicators     = tierDots;
        uim.processAStatusText = procAText;
        uim.processBStatusText = procBText;
        uim.bufferCountText    = bufText;
        uim.winPanel           = winPanel;
        uim.losePanel          = losePanel;
        uim.winCreditsText     = winCredTxt;
        uim.pauseButton        = pauseBtn.GetComponent<Button>();
        uim.emergencyBuyButton = emergBtn.GetComponent<Button>();

        // Wire EventPopupUI
        var evp = uiMgrGO.AddComponent<EventPopupUI>();
        evp.popupPanel       = popup;
        evp.eventNameText    = evName;
        evp.eventDescText    = evDesc;
        evp.prioritizeButton = prioBtn.GetComponent<Button>();
        evp.repairButton     = repairBtn.GetComponent<Button>();
        evp.tradeButton      = tradeBtn.GetComponent<Button>();
        evp.prioritizeLabel  = prioBtn.GetComponentInChildren<TextMeshProUGUI>();
        evp.repairLabel      = repairBtn.GetComponentInChildren<TextMeshProUGUI>();
        evp.tradeLabel       = tradeBtn.GetComponentInChildren<TextMeshProUGUI>();
        evp.prioritizeDesc   = prioDesc;
        evp.repairDesc       = repDesc;
        evp.tradeDesc        = tradDesc;

        // ── Order Board UI (right side HUD panel) ─────────────────────────
        var orderBoardPanel = Panel("OrderBoardPanel", hud, 0.78f, 0.14f, 0.99f, 0.76f, new Color(0.06f, 0.09f, 0.17f, 0.92f));
        var orderTitle = TMP("OrderBoardTitle", orderBoardPanel, "Active Orders", 14, UI_Orange, TextAlignmentOptions.Center);
        Anch(orderTitle.gameObject, 0f, 0.90f, 1f, 1f);
        
        // Pending orders sub-container
        var pendingLabelGO = Rect("PendingLabel", orderBoardPanel);
        Anch(pendingLabelGO, 0f, 0.82f, 1f, 0.90f);
        var pendingLbl = pendingLabelGO.AddComponent<TextMeshProUGUI>();
        pendingLbl.text = "Pending"; pendingLbl.fontSize = 11; pendingLbl.color = UI_Orange;
        pendingLbl.alignment = TextAlignmentOptions.Center;

        var pendingCont = Rect("PendingContainer", orderBoardPanel);
        Anch(pendingCont, 0.02f, 0.42f, 0.98f, 0.82f);
        var vlgP = pendingCont.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlgP.childForceExpandHeight = false; vlgP.spacing = 4f;
        vlgP.padding = new RectOffset(4, 4, 4, 4);

        // Active orders sub-container
        var activeLabelGO = Rect("ActiveLabel", orderBoardPanel);
        Anch(activeLabelGO, 0f, 0.34f, 1f, 0.42f);
        var activeLbl = activeLabelGO.AddComponent<TextMeshProUGUI>();
        activeLbl.text = "Active"; activeLbl.fontSize = 11; activeLbl.color = UI_Red;
        activeLbl.alignment = TextAlignmentOptions.Center;

        var activeCont = Rect("ActiveContainer", orderBoardPanel);
        Anch(activeCont, 0.02f, 0f, 0.98f, 0.34f);
        var vlgA = activeCont.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlgA.childForceExpandHeight = false; vlgA.spacing = 4f;
        vlgA.padding = new RectOffset(4, 4, 4, 4);

        var obUI = uiMgrGO.AddComponent<OrderBoardUI>();
        obUI.pendingContainer = pendingCont.transform;
        obUI.activeContainer  = activeCont.transform;

        // Generate and assign the slot prefabs
        GameObject pendingSlotPrefab = MakePendingSlotPrefab();
        GameObject activeSlotPrefab  = MakeActiveSlotPrefab();
        obUI.pendingSlotPrefab = pendingSlotPrefab;
        obUI.activeSlotPrefab  = activeSlotPrefab;

        // Toast Panel for Expired Orders
        var toastPanel = Panel("ExpiredToastPanel", cvGO, 0.35f, 0.82f, 0.65f, 0.88f, new Color(0.85f, 0.15f, 0.15f, 0.98f));
        var toastText = TMP("ToastText", toastPanel, "Đơn hàng đã hết hạn!", 16, Color.white, TextAlignmentOptions.Center);
        Anch(toastText.gameObject, 0.02f, 0.1f, 0.98f, 0.9f);
        toastPanel.SetActive(false);
        obUI.expiredToastPanel = toastPanel;
        obUI.expiredToastText = toastText;

        // ── Result Screen ─────────────────────────────────────────────────
        var resultPanel  = Panel("ResultPanel", cvGO, 0f, 0f, 1f, 1f, new Color(0.04f, 0.05f, 0.12f, 0.97f));
        var resOutcome   = TMP("ResOutcome",   resultPanel, "ORDER COMPLETE!",  64, UI_Green,  TextAlignmentOptions.Center); Anch(resOutcome.gameObject,   0.1f, 0.68f, 0.9f, 0.85f);
        var resCredits   = TMP("ResCredits",   resultPanel, "Credits Earned: $0", 28, Color.white,TextAlignmentOptions.Center); Anch(resCredits.gameObject,   0.1f, 0.57f, 0.9f, 0.68f);
        var resOrders    = TMP("ResOrders",    resultPanel, "Orders Completed: 0",24, Color.white,TextAlignmentOptions.Center); Anch(resOrders.gameObject,    0.1f, 0.48f, 0.9f, 0.58f);
        var resRep       = TMP("ResRep",       resultPanel, "Final Reputation: 0",24, Color.white,TextAlignmentOptions.Center); Anch(resRep.gameObject,       0.1f, 0.39f, 0.9f, 0.49f);
        var resHiScore   = TMP("ResHiScore",   resultPanel, "Best Run: $0 | Rep 0", 18, new Color(0.95f,0.80f,0.20f),TextAlignmentOptions.Center); Anch(resHiScore.gameObject, 0.1f,0.30f,0.9f,0.40f);
        var resRetryBtn  = Btn("ResRetryBtn",  resultPanel, "Retry",     UI_Green);  Anch(resRetryBtn,  0.15f, 0.10f, 0.45f, 0.26f);
        var resMenuBtn   = Btn("ResMenuBtn",   resultPanel, "Main Menu", UI_Blue);   Anch(resMenuBtn,   0.55f, 0.10f, 0.85f, 0.26f);
        resultPanel.SetActive(false);
        var rs = uiMgrGO.AddComponent<ResultScreen>();
        rs.panel           = resultPanel;
        rs.outcomeLabel    = resOutcome;
        rs.creditsLabel    = resCredits;
        rs.ordersLabel     = resOrders;
        rs.reputationLabel = resRep;
        rs.highScoreLabel  = resHiScore;
        rs.retryButton     = resRetryBtn.GetComponent<Button>();
        rs.mainMenuButton  = resMenuBtn .GetComponent<Button>();

        // ── Tutorial Overlay ──────────────────────────────────────────────
        var tutPanel  = Panel("TutorialPanel", cvGO, 0.15f, 0.20f, 0.85f, 0.80f, new Color(0.04f, 0.06f, 0.15f, 0.96f));
        var tutText   = TMP("TutStepText",   tutPanel, "Tutorial Step Text", 18, Color.white, TextAlignmentOptions.Center); Anch(tutText.gameObject,  0.04f, 0.30f, 0.96f, 0.90f);
        var tutCount  = TMP("TutCounter",    tutPanel, "1/3",                14, new Color(0.6f,0.8f,1f), TextAlignmentOptions.Center); Anch(tutCount.gameObject, 0.35f, 0.18f, 0.65f, 0.30f);
        var tutNext   = Btn("TutNextBtn",    tutPanel, "Next",    UI_Green);  Anch(tutNext,  0.55f, 0.04f, 0.90f, 0.18f);
        var tutSkip   = Btn("TutSkipBtn",    tutPanel, "Skip",      UI_Orange); Anch(tutSkip,  0.10f, 0.04f, 0.45f, 0.18f);
        var tut = uiMgrGO.AddComponent<Tutorial>();
        tut.tutorialPanel    = tutPanel;
        tut.stepText         = tutText;
        tut.stepCounterText  = tutCount;
        tut.nextButton       = tutNext.GetComponent<Button>();
        tut.skipButton       = tutSkip.GetComponent<Button>();

        // ── Upgrade Shop Panel ────────────────────────────────────────────
        var shopPanel    = Panel("UpgradeShopPanel", cvGO, 0.20f, 0.15f, 0.80f, 0.85f, new Color(0.05f, 0.08f, 0.18f, 0.95f));
        var shopTitle    = TMP("ShopTitle", shopPanel, "Upgrade Shop  [Tab]", 22, UI_Orange, TextAlignmentOptions.Center); Anch(shopTitle.gameObject, 0f, 0.88f, 1f, 1f);
        var wrkBtn       = Btn("WrkUpgradeBtn",   shopPanel, "+1 Worker\n$80",     UI_Blue);  Anch(wrkBtn,  0.04f, 0.55f, 0.32f, 0.86f);
        var bufBtn       = Btn("BufUpgradeBtn",   shopPanel, "+2 Buffer\n$60",     UI_Green); Anch(bufBtn,  0.36f, 0.55f, 0.64f, 0.86f);
        var pwrBtn       = Btn("PwrRestoreBtn",   shopPanel, "Power +50\n$40", UI_Orange); Anch(pwrBtn,  0.68f, 0.55f, 0.96f, 0.86f);
        
        var asmSpwnBtn   = Btn("BuyAsmBtn", shopPanel, "+1 Assembly\n$250", UI_Blue);  Anch(asmSpwnBtn, 0.15f, 0.15f, 0.45f, 0.45f);
        var pntSpwnBtn   = Btn("BuyPntBtn", shopPanel, "+1 Paint\n$350",   UI_Green); Anch(pntSpwnBtn, 0.55f, 0.15f, 0.85f, 0.45f);

        var shopToggleBtn = Btn("ShopToggleHUDBtn", hud, "Shop [Tab]", new Color(0.15f,0.20f,0.38f)); Anch(shopToggleBtn, 0.92f, 0.07f, 0.99f, 0.12f);
        shopPanel.SetActive(false);
        var upg = uiMgrGO.AddComponent<UpgradeShopUI>();
        upg.shopPanel          = shopPanel;
        upg.shopButton         = shopToggleBtn.GetComponent<Button>();
        upg.workerUpgradeBtn   = wrkBtn.GetComponent<Button>();
        upg.bufferUpgradeBtn   = bufBtn.GetComponent<Button>();
        upg.powerRestoreBtn    = pwrBtn.GetComponent<Button>();
        upg.workerUpgradeLabel = wrkBtn.GetComponentInChildren<TextMeshProUGUI>();
        upg.bufferUpgradeLabel = bufBtn.GetComponentInChildren<TextMeshProUGUI>();
        upg.powerRestoreLabel  = pwrBtn.GetComponentInChildren<TextMeshProUGUI>();
        
        upg.buyAssemblyBtn     = asmSpwnBtn.GetComponent<Button>();
        upg.buyPaintBtn        = pntSpwnBtn.GetComponent<Button>();
        upg.buyAssemblyLabel   = asmSpwnBtn.GetComponentInChildren<TextMeshProUGUI>();
        upg.buyPaintLabel      = pntSpwnBtn.GetComponentInChildren<TextMeshProUGUI>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FACTORY HELPERS
    // ═══════════════════════════════════════════════════════════════════════
    static GameObject MachinePrefab(string n, Machine.MachineType t, Vector3 pos, Color col)
    {
        var root = new GameObject(n); root.transform.position = pos;
        root.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        string spritePath = (t == Machine.MachineType.AssemblyA) ? "Assets/Sprites/assembly.png" : "Assets/Sprites/paint.png";
        var sp = GetSprite(spritePath, true); // Auto transparency!
        
        if (sp != null)
        {
            var body = Spr("Body", root, Vector3.zero, Vector3.one, Color.white);
            var sr = body.GetComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = 5;

            float targetHeight = 2.4f;
            float scaleM = targetHeight / sr.sprite.bounds.size.y;
            body.transform.localScale = new Vector3(scaleM, scaleM, 1f);
        }
        else
        {
            Spr("Body",   root, Vector3.zero,                new Vector3(1.8f,2.2f,1f),    col);
            Spr("Panel",  root, new Vector3(0,0.2f,-0.1f),  new Vector3(1.3f,1.2f,1f),    DarkPanel);
            Spr("Screen", root, new Vector3(0,0.35f,-0.2f), new Vector3(1.0f,0.65f,1f),   CyanScreen);
            Spr("Vent1",  root, new Vector3(-0.35f,-0.78f,-0.1f), new Vector3(0.45f,0.08f,1f), col*0.5f);
            Spr("Vent2",  root, new Vector3( 0.35f,-0.78f,-0.1f), new Vector3(0.45f,0.08f,1f), col*0.5f);
        }

        var light = Spr("StatusLight", root, new Vector3(0.65f,1.2f,-0.3f), new Vector3(0.22f,0.22f,1f), Color.green);
        root.AddComponent<BoxCollider2D>().size = new Vector2(1.8f,2.2f);
        var m = root.AddComponent<Machine>(); m.machineType = t; m.statusLight = light.GetComponent<SpriteRenderer>();

        // ── Machine UI (WorldSpace) ──
        var cvGO = new GameObject("MachineUI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster), typeof(MachineUI));
        cvGO.transform.SetParent(root.transform, false);
        var cv = cvGO.GetComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        cv.sortingOrder = 10;
        var cvRect = cvGO.GetComponent<RectTransform>();
        cvRect.sizeDelta = new Vector2(180f, 220f); 
        cvRect.localPosition = new Vector3(0, 0f, -0.5f);
        cvRect.localScale = new Vector3(0.01f, 0.01f, 1f);

        var mui = cvGO.GetComponent<MachineUI>();
        mui.machine = m;

        // Worker Button
        var wBtnGO = Btn("WorkerBtn", cvGO, "Worker: OFF", new Color(0.3f,0.3f,0.3f));
        Anch(wBtnGO, 0.15f, 0.84f, 0.85f, 0.96f);
        mui.workerButton = wBtnGO.GetComponent<Button>();
        mui.workerButtonImage = wBtnGO.GetComponent<Image>();
        mui.workerButtonText = wBtnGO.GetComponentInChildren<TextMeshProUGUI>();
        mui.workerButtonText.fontSize = 12;

        // Progress Bar
        var pContainer = Panel("ProgressContainer", cvGO, 0.15f, 0.04f, 0.85f, 0.12f, new Color(0.1f,0.1f,0.1f));
        var pFill = Img("ProgressFill", pContainer, Color.green);
        Anch(pFill.gameObject, 0f, 0f, 1f, 1f);
        pFill.type = Image.Type.Filled;
        pFill.fillMethod = Image.FillMethod.Horizontal;
        mui.progressBarContainer = pContainer;
        mui.progressBarFill = pFill;

        return root;
    }

    static GameObject WorkerPrefab(string n, Vector3 pos)
    {
        var root = new GameObject(n); root.transform.position = pos;
        Spr("Body", root, new Vector3(0,0.12f,0f),   new Vector3(0.34f,0.44f,1f), WorkerGold);
        Spr("Head", root, new Vector3(0,0.53f,0f),   new Vector3(0.27f,0.27f,1f), WorkerGold*1.1f);
        Spr("LegL", root, new Vector3(-0.1f,-0.2f,0f),new Vector3(0.12f,0.28f,1f),WorkerGold*0.8f);
        Spr("LegR", root, new Vector3(0.1f,-0.2f,0f), new Vector3(0.12f,0.28f,1f),WorkerGold*0.8f);
        Spr("Hat",  root, new Vector3(0,0.71f,-0.1f), new Vector3(0.38f,0.12f,1f),new Color(1f,0.35f,0.1f));
        return root;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UI HELPERS (Modified to explicitly use UI Layer 5)
    // ═══════════════════════════════════════════════════════════════════════
    static GameObject Rect(string n, GameObject parent) {
        var go = new GameObject(n, typeof(RectTransform));
        go.layer = 5; // explicitly set to UI
        if (parent) go.transform.SetParent(parent.transform, false);
        return go;
    }
    static GameObject Panel(string n, GameObject parent, float x0,float y0,float x1,float y1, Color? col=null) {
        var go = Rect(n,parent); Anch(go,x0,y0,x1,y1);
        var img = go.AddComponent<Image>(); img.color = col ?? UI_Bg;
        return go;
    }
    static TextMeshProUGUI TMP(string n, GameObject parent, string txt, int fs, Color col, TextAlignmentOptions align=TextAlignmentOptions.Left) {
        var go = Rect(n,parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = fs; t.color = col; t.alignment = align;
        return t;
    }
    static Image Img(string n, GameObject parent, Color col) {
        var go = Rect(n,parent);
        var img = go.AddComponent<Image>(); img.color = col;
        return img;
    }
    static GameObject Btn(string n, GameObject parent, string label, Color col) {
        var go = Rect(n,parent);
        var img = go.AddComponent<Image>(); img.color = col;
        go.AddComponent<Button>();
        var lblGO = Rect("Label",go); Anch(lblGO,0.05f,0.1f,0.95f,0.9f);
        var t = lblGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 14; t.color = Color.white; t.alignment = TextAlignmentOptions.Center;
        return go;
    }
    static void Anch(GameObject go, float x0,float y0,float x1,float y1) {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(x0,y0); r.anchorMax = new Vector2(x1,y1);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SPRITE HELPERS
    // ═══════════════════════════════════════════════════════════════════════
    static void FloodFillTransparent(string path)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool reimport = false;
            if (!importer.isReadable) { importer.isReadable = true; reimport = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; reimport = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; reimport = true; }
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; reimport = true; }
            
            if (reimport) importer.SaveAndReimport();
        }

        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Color[] pixels = tex.GetPixels();
        bool changed = false;

        // Failsafe aggressive sweep to ensure no artifacts remain on the boundaries
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            // Clear any light-colored backgrounds aggressively (white/off-white)
            if (c.r > 0.85f && c.g > 0.85f && c.b > 0.85f && c.a > 0.05f)
            {
                pixels[i] = new Color(c.r, c.g, c.b, 0f);
                changed = true;
            }
        }

        if (changed)
        {
            tex.SetPixels(pixels);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            Debug.Log($"[SceneBuilder] Aggressive background alpha applied for {path}");
        }
    }

    static Sprite GetSprite(string path, bool removeBg = false)
    {
        if (removeBg) FloodFillTransparent(path);

        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                Debug.Log($"[SceneBuilder] Converting {path} to Sprite type...");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }

        Debug.LogWarning($"[SceneBuilder] Missing sprite at {path}");
        return null;
    }

    static GameObject Spr(string n, GameObject parent, Vector3 localPos, Vector3 localScale, Color col) {
        var go = new GameObject(n);
        if (parent != null) { go.transform.SetParent(parent.transform); go.transform.localPosition = localPos; go.transform.localScale = localScale; }
        else { go.transform.position = localPos; go.transform.localScale = localScale; }
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = WhiteSprite(); sr.color = col;
        return go;
    }
    static T Mgr<T>(GameObject parent, string n) where T : Component {
        var go = new GameObject(n); go.transform.SetParent(parent.transform);
        return go.AddComponent<T>();
    }
    static Sprite _wsp;
    static Sprite WhiteSprite() {
        if (_wsp) return _wsp;
        var tex = new Texture2D(4,4); 
        tex.filterMode = FilterMode.Point; // Chống mờ sương (blur) cho ảnh
        var px = new Color[16];
        for (int i=0;i<16;i++) px[i]=Color.white; 
        tex.SetPixels(px); tex.Apply();
        return _wsp = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f), 4f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ORDER SLOT PREFAB GENERATION
    // ═══════════════════════════════════════════════════════════════════════

    static void EnsurePrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
    }

    /// <summary>
    /// Pending slot prefab — shown before the player accepts the order.
    /// Children looked up by name in OrderBoardUI.CreatePendingSlot():
    ///   OrderNameText, RequiredText, DeadlineHintText, AcceptButton
    /// </summary>
    static GameObject MakePendingSlotPrefab()
    {
        EnsurePrefabFolder();
        string filePath = "Assets/Prefabs/UI/PendingSlot.prefab";

        var root = new GameObject("PendingSlot", typeof(RectTransform));
        root.layer = 5;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 90);
        root.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.20f, 0.95f);

        var nameTxt = TMP("OrderNameText", root, "Order Name", 13, UI_Orange, TextAlignmentOptions.TopLeft);
        Anch(nameTxt.gameObject, 0.04f, 0.72f, 0.96f, 0.97f);

        var reqTxt = TMP("RequiredText", root, "  • Toy ×1", 11, Color.white, TextAlignmentOptions.Left);
        Anch(reqTxt.gameObject, 0.04f, 0.35f, 0.75f, 0.72f);

        var hintTxt = TMP("DeadlineHintText", root, "⏱ Hạn: 3:00", 10, new Color(0.6f, 0.85f, 1f), TextAlignmentOptions.Left);
        Anch(hintTxt.gameObject, 0.04f, 0.10f, 0.60f, 0.35f);

        var acceptBtn = Btn("AcceptButton", root, "Accept", UI_Green);
        Anch(acceptBtn, 0.66f, 0.12f, 0.96f, 0.88f);

        var le = root.AddComponent<UnityEngine.UI.LayoutElement>();
        le.minHeight = 90f; le.preferredHeight = 90f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, filePath);
        DestroyImmediate(root);
        Debug.Log($"[SceneBuilder] Created PendingSlot prefab at {filePath}");
        return prefab;
    }

    /// <summary>
    /// Active slot prefab — shown after the player accepts an order (deadline counting).
    /// Children looked up by name in OrderBoardUI.OnOrderStarted() / OnDeadlineTick():
    ///   OrderNameText, RequiredText, DeadlineBar, DeadlineText, QueueButton, DeliverButton
    /// </summary>
    static GameObject MakeActiveSlotPrefab()
    {
        EnsurePrefabFolder();
        string filePath = "Assets/Prefabs/UI/ActiveSlot.prefab";

        var root = new GameObject("ActiveSlot", typeof(RectTransform));
        root.layer = 5;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 100);
        root.AddComponent<Image>().color = new Color(0.10f, 0.14f, 0.22f, 0.97f);

        var nameTxt = TMP("OrderNameText", root, "Order Name", 13, UI_Orange, TextAlignmentOptions.TopLeft);
        Anch(nameTxt.gameObject, 0.04f, 0.78f, 0.96f, 0.98f);

        var reqTxt = TMP("RequiredText", root, "  ⏳ Toy  0/1", 10, Color.white, TextAlignmentOptions.Left);
        Anch(reqTxt.gameObject, 0.04f, 0.42f, 0.70f, 0.78f);

        // Deadline bar background
        var barBg = Img("DeadlineBarBg", root, new Color(0.08f, 0.08f, 0.12f));
        Anch(barBg.gameObject, 0.04f, 0.27f, 0.96f, 0.38f);

        // Fill bar (child of barBg so anchors work properly)
        var barFill = Img("DeadlineBar", barBg.gameObject, UI_Green);
        var barFillRT = barFill.gameObject.GetComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero; barFillRT.anchorMax = Vector2.one;
        barFillRT.offsetMin = barFillRT.offsetMax = Vector2.zero;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillAmount = 1f;

        var timeTxt = TMP("DeadlineText", root, "⏱ 3:00", 10, Color.white, TextAlignmentOptions.Right);
        Anch(timeTxt.gameObject, 0.50f, 0.10f, 0.96f, 0.26f);

        var queueBtn = Btn("QueueButton", root, "📋 Produce", UI_Orange);
        Anch(queueBtn, 0.04f, 0.06f, 0.46f, 0.27f);

        var deliverBtn = Btn("DeliverButton", root, "Deliver ✅", UI_Blue);
        Anch(deliverBtn, 0.50f, 0.42f, 0.96f, 0.80f);

        var le = root.AddComponent<UnityEngine.UI.LayoutElement>();
        le.minHeight = 100f; le.preferredHeight = 100f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, filePath);
        DestroyImmediate(root);
        Debug.Log($"[SceneBuilder] Created ActiveSlot prefab at {filePath}");
        return prefab;
    }
}
#endif
