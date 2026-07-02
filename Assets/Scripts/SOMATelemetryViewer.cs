using UnityEngine;
using UnityEngine.UI;

public class SOMATelemetryViewer : MonoBehaviour
{
    private GameObject telemetryPanel;
    private SentadillaSimulada[] controllers;
    private object tmpText;
    private Text legacyText;
    private System.Reflection.PropertyInfo tmpTextProp;

    void Start()
    {
        Transform panelT = transform.Find("Panel_Telemetry");
        if (panelT != null) telemetryPanel = panelT.gameObject;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn.gameObject.name == "Button_ToggleTelemetry")
            {
                btn.onClick.AddListener(() => {
                    if (telemetryPanel != null) 
                        telemetryPanel.SetActive(!telemetryPanel.activeSelf);
                });
            }
            if (btn.gameObject.name == "Button_CloseTelemetry")
            {
                btn.onClick.AddListener(() => {
                    if (telemetryPanel != null) 
                        telemetryPanel.SetActive(false);
                });
            }
        }

        System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null) tmpTextProp = tmpType.GetProperty("text");

        if (telemetryPanel != null)
        {
            Transform txtTransform = telemetryPanel.transform.Find("Txt_Tele_Data");
            if (txtTransform != null)
            {
                if (tmpType != null && txtTransform.GetComponent(tmpType) != null)
                {
                    tmpText = txtTransform.GetComponent(tmpType);
                }
                else if (txtTransform.GetComponent<Text>() != null)
                {
                    legacyText = txtTransform.GetComponent<Text>();
                }
            }
            telemetryPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (telemetryPanel == null || !telemetryPanel.activeInHierarchy) return;

        if (controllers == null || controllers.Length == 0)
            controllers = Object.FindObjectsByType<SentadillaSimulada>(FindObjectsSortMode.None);

        string bodyText = "REAL-TIME TELEMETRY\n-------------------\n\n";
        bool hasData = false;

        if (controllers != null)
        {
            foreach (var controller in controllers)
            {
                foreach (var sb in controller.sensorBones)
                {
                    if (sb.bone != null)
                    {
                        hasData = true;
                        Vector3 euler = sb.bone.localEulerAngles;
                        float x = euler.x; if (x > 180) x -= 360;
                        float z = euler.z; if (z > 180) z -= 360;
                        
                        string paddedId = sb.sensorId.ToUpper().PadRight(15, ' ');
                        bodyText += $"{paddedId} X: {x, 6:F1}°   Z: {z, 6:F1}°\n";
                    }
                }
            }
        }

        if (!hasData) bodyText += "NO SENSOR BONES CONFIGURED.";

        if (tmpText != null && tmpTextProp != null)
            tmpTextProp.SetValue(tmpText, bodyText, null);
        else if (legacyText != null)
            legacyText.text = bodyText;
    }
}
