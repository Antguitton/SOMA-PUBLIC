using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System;
using System.Reflection;

public class SOMA_UIGenerator : EditorWindow
{
    [MenuItem("Tools/Generar UI SOMA")]
    public static void GenerateUI()
    {
        // 1. Setup Camera Background
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.04f, 0.03f, 0.03f, 1f); 
            Undo.RecordObject(mainCam, "Change Camera Background");
            // Ajuste fino para centrar el maniquí a la izquierda de donde estaba. 
            mainCam.transform.position = new Vector3(1.4f, 1, -4);
        }

        // 2. Clear old Canvas to avoid overlaps
        GameObject oldCanvas = GameObject.Find("SOMA_Canvas");
        if (oldCanvas != null) DestroyImmediate(oldCanvas);

        // 3. Create fresh Canvas
        GameObject canvasObj = new GameObject("SOMA_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f; // Balance width & height
        
        canvasObj.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create SOMA_Canvas");

        // 4. Find or Create EventSystem
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        // Palette
        Color bgColor = new Color(0.02f, 0.02f, 0.02f, 0.95f); 
        Color panelColorOutline = new Color(0.1f, 0.05f, 0.05f, 1f);
        Color redAccent = new Color(0.9f, 0.15f, 0.15f, 1f); 

        // --- TOP BAR ---
        GameObject topBar = CreatePanel("TopPanel", canvasObj.transform, bgColor);
        RectTransform topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 80);
        topRect.anchoredPosition = Vector2.zero;

        // Top bar border
        GameObject topBorder = CreatePanel("TopBorder", topBar.transform, redAccent);
        RectTransform topBorderRect = topBorder.GetComponent<RectTransform>();
        topBorderRect.anchorMin = new Vector2(0, 0); topBorderRect.anchorMax = new Vector2(1, 0);
        topBorderRect.pivot = new Vector2(0.5f, 0);
        topBorderRect.sizeDelta = new Vector2(0, 1); // 1px line

        // SOMA Title
        GameObject titleText = CreateText(topBar.transform, "SOMA", redAccent, 32, true);
        RectTransform titleRt = titleText.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0.5f); titleRt.anchorMax = new Vector2(0, 0.5f);
        titleRt.pivot = new Vector2(0, 0.5f);
        titleRt.anchoredPosition = new Vector2(40, 0);
        titleRt.sizeDelta = new Vector2(150, 40);

        // SOMA Subtitle
        GameObject subText = CreateText(topBar.transform, "SMART OUTFIT FOR MOTION ANALYSIS\nv1.0.2 - Visualisation en temps réel", Color.white, 12, false);
        RectTransform subRt = subText.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0, 0.5f); subRt.anchorMax = new Vector2(0, 0.5f);
        subRt.pivot = new Vector2(0, 0.5f);
        subRt.anchoredPosition = new Vector2(180, 0);
        subRt.sizeDelta = new Vector2(400, 40);

        // --- LEFT MENU ---
        GameObject leftMenu = CreatePanel("LeftPanel", canvasObj.transform, bgColor);
        RectTransform leftRect = leftMenu.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0); leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 0.5f);
        leftRect.sizeDelta = new Vector2(300, -80); 
        leftRect.anchoredPosition = new Vector2(0, -40); 
        
        GameObject leftBorder = CreatePanel("LeftBorder", leftMenu.transform, redAccent);
        RectTransform leftBorderRect = leftBorder.GetComponent<RectTransform>();
        leftBorderRect.anchorMin = new Vector2(1, 0); leftBorderRect.anchorMax = new Vector2(1, 1);
        leftBorderRect.pivot = new Vector2(1, 0.5f);
        leftBorderRect.sizeDelta = new Vector2(1, 0);

        VerticalLayoutGroup leftLayout = leftMenu.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(40, 20, 40, 20);
        leftLayout.spacing = 15;
        leftLayout.childControlWidth = true; leftLayout.childControlHeight = false;

        CreateMenuText(leftMenu.transform, "SEGMENTS", Color.white, 22, true);
        CreateMenuText(leftMenu.transform, " ", Color.white, 10, false);
        
        CreateMenuText(leftMenu.transform, "HAUT DU CORPS", redAccent, 14, true);
        CreateMenuText(leftMenu.transform, "Tête", Color.gray, 18, false);
        CreateMenuText(leftMenu.transform, "Cou", Color.gray, 18, false);
        CreateMenuText(leftMenu.transform, " ", Color.white, 10, false);

        CreateMenuText(leftMenu.transform, "BRAS", redAccent, 14, true);
        CreateMenuText(leftMenu.transform, "Bras Gauche", Color.gray, 18, false);
        CreateMenuText(leftMenu.transform, "Avant-bras Gauche", Color.gray, 18, false);
        CreateMenuText(leftMenu.transform, "Bras Droit", Color.gray, 18, false);

        // --- RIGHT MENU ---
        GameObject rightMenu = CreatePanel("RightPanel", canvasObj.transform, bgColor);
        RectTransform rightRect = rightMenu.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0); rightRect.anchorMax = new Vector2(1, 1);
        rightRect.pivot = new Vector2(1, 0.5f);
        rightRect.sizeDelta = new Vector2(350, -80);
        rightRect.anchoredPosition = new Vector2(0, -40);

        VerticalLayoutGroup rightLayout = rightMenu.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(20, 20, 40, 20);
        rightLayout.spacing = 20;
        rightLayout.childControlWidth = true; rightLayout.childControlHeight = true;
        rightLayout.childForceExpandHeight = false;

        CreateMenuText(rightMenu.transform, "Capteur #0421-A", Color.white, 24, true);
        CreateMenuText(rightMenu.transform, "STATUS: CONNECTED", redAccent, 12, true);

        // Cards
        CreateCard(rightMenu.transform, "QUATERNION", redAccent, new string[] { "W\t\t\t\t\t0.999", "X\t\t\t\t\t0.281", "Y\t\t\t\t   -0.335", "Z\t\t\t\t\t0.130" }, 200);
        CreateCard(rightMenu.transform, "ANGLES D'EULER", redAccent, new string[] { "ROULIS\t\t  -2.933", "TANGAGE\t -22.818", "LACET\t\t   -32.688" }, 200);
        CreateCard(rightMenu.transform, "ACCÉLÉRATION", redAccent, new string[] { "AXE-X\t\t   -0.645", "AXE-Y\t\t\t3.139", "AXE-Z\t\t\t1.536" }, 200);

        // --- BUTTON TO OPEN POPUP ---
        CreateMenuText(rightMenu.transform, "TOOLS", redAccent, 14, true);
        GameObject openPopupBtn = new GameObject("Button_OpenSimPopup");
        openPopupBtn.transform.SetParent(rightMenu.transform, false);
        Image openBtnImg = openPopupBtn.AddComponent<Image>();
        openBtnImg.color = redAccent;
        openPopupBtn.AddComponent<Button>();
        LayoutElement openBtnLe = openPopupBtn.AddComponent<LayoutElement>();
        openBtnLe.minHeight = 40;
        
        GameObject openBtnTxt = CreateMenuText(openPopupBtn.transform, "OPEN SIMULATION MENU", Color.white, 14, true);
        RectTransform txtRt = openBtnTxt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        
        System.Type tmpTypeT = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpTypeT != null && openBtnTxt.GetComponent(tmpTypeT) != null) {
            Type alignEnum = tmpTypeT.Assembly.GetType("TMPro.TextAlignmentOptions");
            if (alignEnum != null) {
                object centerVal = Enum.Parse(alignEnum, "Center");
                PropertyInfo prop = tmpTypeT.GetProperty("alignment");
                if (prop != null) prop.SetValue(openBtnTxt.GetComponent(tmpTypeT), centerVal, null);
            }
        } else {
            Text legacyText = openBtnTxt.GetComponent<Text>();
            if (legacyText != null) legacyText.alignment = TextAnchor.MiddleCenter;
        }

        GameObject teleBtn = new GameObject("Button_ToggleTelemetry");
        teleBtn.transform.SetParent(rightMenu.transform, false);
        Image telBtnImg = teleBtn.AddComponent<Image>();
        telBtnImg.color = redAccent;
        teleBtn.AddComponent<Button>();
        LayoutElement telBtnLe = teleBtn.AddComponent<LayoutElement>();
        telBtnLe.minHeight = 40;
        
        GameObject telBtnTxt = CreateMenuText(teleBtn.transform, "OPEN TELEMETRY", Color.white, 14, true);
        RectTransform tRt = telBtnTxt.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
        
        if (tmpTypeT != null && telBtnTxt.GetComponent(tmpTypeT) != null) {
            Type alignEnum = tmpTypeT.Assembly.GetType("TMPro.TextAlignmentOptions");
            if (alignEnum != null) {
                object centerVal = Enum.Parse(alignEnum, "Center");
                PropertyInfo prop = tmpTypeT.GetProperty("alignment");
                if (prop != null) prop.SetValue(telBtnTxt.GetComponent(tmpTypeT), centerVal, null);
            }
        } else {
            Text legacyText = telBtnTxt.GetComponent<Text>();
            if (legacyText != null) legacyText.alignment = TextAnchor.MiddleCenter;
        }

        // --- POPUP PANEL IN CANVAS ---
        GameObject popupPanel = CreatePanel("Panel_SimulationPopup", canvasObj.transform, new Color(0.1f, 0.1f, 0.1f, 0.98f));
        RectTransform popRt = popupPanel.GetComponent<RectTransform>();
        popRt.anchorMin = new Vector2(0.5f, 1f); popRt.anchorMax = new Vector2(0.5f, 1f);
        popRt.pivot = new Vector2(0.5f, 1f);
        popRt.sizeDelta = new Vector2(390, 260);
        popRt.anchoredPosition = new Vector2(0, -80);
        
        Outline popOut = popupPanel.AddComponent<Outline>();
        popOut.effectColor = redAccent;
        popOut.effectDistance = new Vector2(2, -2);

        VerticalLayoutGroup popLayout = popupPanel.AddComponent<VerticalLayoutGroup>();
        popLayout.padding = new RectOffset(20, 20, 20, 20);
        popLayout.spacing = 15;
        popLayout.childControlWidth = true;
        popLayout.childForceExpandHeight = false;

        CreateMenuText(popupPanel.transform, "SIMULATION CONTROLS", redAccent, 18, true);

        GameObject gridObj = new GameObject("Grid_Buttons");
        gridObj.transform.SetParent(popupPanel.transform, false);
        GridLayoutGroup gridGroup = gridObj.AddComponent<GridLayoutGroup>();
        gridGroup.cellSize = new Vector2(110, 40);
        gridGroup.spacing = new Vector2(10, 10);
        LayoutElement gridLe = gridObj.AddComponent<LayoutElement>();
        gridLe.minHeight = 150;

        string[] btnNames = { "Button_FakeData_Stop", "Button_FakeData_All", "Button_FakeData_LArm", "Button_FakeData_RArm", "Button_FakeData_LLeg", "Button_FakeData_RLeg", "Button_CloseSimPopup" };
        string[] btnLabels = { "STOP\nISO", "ALL\nBODY", "L\nARM", "R\nARM", "L\nLEG", "R\nLEG", "CLOSE\nMENU" };

        for (int i = 0; i < btnNames.Length; i++)
        {
            GameObject popBtnObj = new GameObject(btnNames[i]);
            popBtnObj.transform.SetParent(gridObj.transform, false);
            Image btnImg = popBtnObj.AddComponent<Image>();
            
            if (i == 0) btnImg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // STOP
            else if (i == btnNames.Length - 1) btnImg.color = new Color(0.4f, 0.1f, 0.1f, 1f); // CLOSE
            else btnImg.color = redAccent;
            
            popBtnObj.AddComponent<Button>();
            LayoutElement btnLe = popBtnObj.AddComponent<LayoutElement>();
            btnLe.minHeight = 30;

            GameObject btnTxtObj = CreateMenuText(popBtnObj.transform, btnLabels[i], Color.white, 14, true);
            RectTransform pTxtRt = btnTxtObj.GetComponent<RectTransform>();
            pTxtRt.anchorMin = Vector2.zero; pTxtRt.anchorMax = Vector2.one;
            pTxtRt.sizeDelta = Vector2.zero;
            
            if (tmpTypeT != null && btnTxtObj.GetComponent(tmpTypeT) != null) {
                Type alignEnum = tmpTypeT.Assembly.GetType("TMPro.TextAlignmentOptions");
                if (alignEnum != null) {
                    object centerVal = Enum.Parse(alignEnum, "Center");
                    PropertyInfo prop = tmpTypeT.GetProperty("alignment");
                    if (prop != null) prop.SetValue(btnTxtObj.GetComponent(tmpTypeT), centerVal, null);
                }
            } else {
                Text legacyText = btnTxtObj.GetComponent<Text>();
                if (legacyText != null) legacyText.alignment = TextAnchor.MiddleCenter;
            }
        }

        // --- TELEMETRY PANEL IN CANVAS ---
        GameObject telePanel = CreatePanel("Panel_Telemetry", canvasObj.transform, new Color(0.1f, 0.1f, 0.1f, 0.85f));
        RectTransform teleRt = telePanel.GetComponent<RectTransform>();
        teleRt.anchorMin = new Vector2(0f, 0.5f); teleRt.anchorMax = new Vector2(0f, 0.5f);
        teleRt.pivot = new Vector2(0f, 0.5f);
        teleRt.sizeDelta = new Vector2(350, 450);
        teleRt.anchoredPosition = new Vector2(320, 0); // Position it to the right of the Left Menu
        
        Outline teleOut = telePanel.AddComponent<Outline>();
        teleOut.effectColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        teleOut.effectDistance = new Vector2(2, -2);

        VerticalLayoutGroup teleLayout = telePanel.AddComponent<VerticalLayoutGroup>();
        teleLayout.padding = new RectOffset(20, 20, 20, 20);
        teleLayout.spacing = 15;
        teleLayout.childControlWidth = true;
        teleLayout.childForceExpandHeight = false;
        
        // Close Telemetry button inside
        GameObject cTeleBtn = new GameObject("Button_CloseTelemetry");
        cTeleBtn.transform.SetParent(telePanel.transform, false);
        Image cTeleImg = cTeleBtn.AddComponent<Image>();
        cTeleImg.color = new Color(0.4f, 0.1f, 0.1f, 1f);
        cTeleBtn.AddComponent<Button>();
        LayoutElement cTeleLe = cTeleBtn.AddComponent<LayoutElement>();
        cTeleLe.minHeight = 30;
        
        GameObject cTeleTxt = CreateMenuText(cTeleBtn.transform, "CLOSE TELEMETRY", Color.white, 12, true);
        RectTransform cTRt = cTeleTxt.GetComponent<RectTransform>();
        cTRt.anchorMin = Vector2.zero; cTRt.anchorMax = Vector2.one;
        cTRt.sizeDelta = Vector2.zero;
        
        if (tmpTypeT != null && cTeleTxt.GetComponent(tmpTypeT) != null) {
            Type alignEnum = tmpTypeT.Assembly.GetType("TMPro.TextAlignmentOptions");
            if (alignEnum != null) {
                object centerVal = Enum.Parse(alignEnum, "Center");
                PropertyInfo prop = tmpTypeT.GetProperty("alignment");
                if (prop != null) prop.SetValue(cTeleTxt.GetComponent(tmpTypeT), centerVal, null);
            }
        } else {
            Text legacyText = cTeleTxt.GetComponent<Text>();
            if (legacyText != null) legacyText.alignment = TextAnchor.MiddleCenter;
        }

        // The actual Data Text
        GameObject teleDataTxt = CreateMenuText(telePanel.transform, "WAITING FOR DATA...", Color.white, 14, false);
        teleDataTxt.name = "Txt_Tele_Data";
        LayoutElement teleDataLe = teleDataTxt.AddComponent<LayoutElement>();
        teleDataLe.minHeight = 350;

        // Add tester components to root Canvas
        canvasObj.AddComponent<SOMAFakeDataTester>();
        canvasObj.AddComponent<SOMATelemetryViewer>();

        Debug.Log("SOMA UI Generada con estética mejorada y sin errores.");
    }

    private static void CreateCard(Transform parent, string title, Color accentColor, string[] items, float height)
    {
        GameObject card = CreatePanel("Card_" + title, parent, new Color(0.04f, 0.02f, 0.02f, 1f));
        Outline outline = card.AddComponent<Outline>(); 
        outline.effectColor = accentColor; 
        outline.effectDistance = new Vector2(1, -1);
        
        LayoutElement le = card.AddComponent<LayoutElement>(); 
        le.minHeight = height;
        le.preferredWidth = 350; 
        
        // Horizontal padding that isn't too tight
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 15;
        // The most critical change to make the box wrap the text dynamically without squishing:
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;

        CreateMenuText(card.transform, title, accentColor, 12, true);
        foreach(var item in items) 
        {
            GameObject txtObj = CreateMenuText(card.transform, item, Color.white, 14, false);
            LayoutElement textLe = txtObj.AddComponent<LayoutElement>();
            textLe.preferredHeight = 18; // Give text a preferred height so layout can calculate card bounds
            textLe.minHeight = 18;
        }
    }

    private static GameObject CreateMenuText(Transform parent, string textStr, Color color, int fontSize, bool isBold)
    {
        GameObject txt = CreateText(parent, textStr, color, fontSize, isBold);
        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, fontSize + 8);
        return txt;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        Image img = panelObj.AddComponent<Image>(); img.color = color;
        return panelObj;
    }

    private static GameObject CreateText(Transform parent, string textStr, Color color, int fontSize, bool isBold)
    {
        GameObject textObj = new GameObject("Text_" + textStr.Replace(" ", "").Replace("\n", ""));
        textObj.transform.SetParent(parent, false);
        
        System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            Component tmpComp = textObj.AddComponent(tmpType);
            dynamic dynamicTMP = tmpComp;
            dynamicTMP.text = textStr;
            dynamicTMP.color = color;
            dynamicTMP.fontSize = fontSize;
            
            // Safe reflection with Enum to avoid binder crash
            if (isBold) 
            {
                Type fontStylesEnum = tmpType.Assembly.GetType("TMPro.FontStyles");
                if (fontStylesEnum != null) {
                    object boldValue = Enum.Parse(fontStylesEnum, "Bold");
                    PropertyInfo prop = tmpType.GetProperty("fontStyle");
                    if (prop != null) prop.SetValue(tmpComp, boldValue, null);
                }
            }

            Type alignEnum = tmpType.Assembly.GetType("TMPro.TextAlignmentOptions");
            if (alignEnum != null) {
                object topLValue = Enum.Parse(alignEnum, "TopLeft");
                PropertyInfo prop = tmpType.GetProperty("alignment");
                if (prop != null) prop.SetValue(tmpComp, topLValue, null);
            }
        }
        else
        {
            Text legacyText = textObj.AddComponent<Text>();
            legacyText.text = textStr;
            legacyText.color = color;
            legacyText.fontSize = fontSize;
            legacyText.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
            legacyText.alignment = TextAnchor.UpperLeft;
            legacyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        return textObj;
    }
}
