using UnityEngine;
using UnityEngine.UI;

public class SOMAFakeDataTester : MonoBehaviour
{
    public static SOMAFakeDataTester Instance;
    public enum TestMode { None, All, LeftArm, RightArm, LeftLeg, RightLeg }
    public TestMode currentMode = TestMode.None;
    private SentadillaSimulada[] controllers;
    private GameObject popupPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Transform popupT = transform.Find("Panel_SimulationPopup");
        if (popupT != null) popupPanel = popupT.gameObject;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            string n = btn.gameObject.name;
            if (n == "Button_OpenSimPopup") btn.onClick.AddListener(() => { if (popupPanel!=null) popupPanel.SetActive(true); });
            else if (n == "Button_CloseSimPopup") btn.onClick.AddListener(() => { if (popupPanel!=null) popupPanel.SetActive(false); });
            else if (n == "Button_FakeData_Stop") btn.onClick.AddListener(() => SetTestMode(TestMode.None));
            else if (n == "Button_FakeData_All") btn.onClick.AddListener(() => SetTestMode(TestMode.All));
            else if (n == "Button_FakeData_LArm") btn.onClick.AddListener(() => SetTestMode(TestMode.LeftArm));
            else if (n == "Button_FakeData_RArm") btn.onClick.AddListener(() => SetTestMode(TestMode.RightArm));
            else if (n == "Button_FakeData_LLeg") btn.onClick.AddListener(() => SetTestMode(TestMode.LeftLeg));
            else if (n == "Button_FakeData_RLeg") btn.onClick.AddListener(() => SetTestMode(TestMode.RightLeg));
        }

        if (popupPanel != null) popupPanel.SetActive(false);
    }

    public void SetTestMode(TestMode mode)
    {
        currentMode = mode;
        if (currentMode != TestMode.None)
        {
            controllers = Object.FindObjectsByType<SentadillaSimulada>(FindObjectsSortMode.None);
            Debug.Log($">>> 🟢 Fake Movement Started: {mode}");
        }
        else
        {
            Debug.Log(">>> 🔴 Fake Movement Stopped");
        }
    }

    public bool ShouldAnimate(string sensorId)
    {
        if (string.IsNullOrEmpty(sensorId)) return false;
        sensorId = sensorId.ToLower();
        
        if (currentMode == TestMode.All) return true;
        
        bool isLeft = sensorId.EndsWith("_l") || sensorId.Contains("left");
        bool isRight = sensorId.EndsWith("_r") || sensorId.Contains("right");
        bool isArm = sensorId.Contains("shoulder") || sensorId.Contains("arm") || sensorId.Contains("elbow") || sensorId.Contains("wrist") || sensorId.Contains("hand");
        bool isLeg = sensorId.Contains("hip") || sensorId.Contains("leg") || sensorId.Contains("knee") || sensorId.Contains("ankle") || sensorId.Contains("foot");

        if (currentMode == TestMode.LeftArm && isLeft && isArm) return true;
        if (currentMode == TestMode.RightArm && isRight && isArm) return true;
        if (currentMode == TestMode.LeftLeg && isLeft && isLeg) return true;
        if (currentMode == TestMode.RightLeg && isRight && isLeg) return true;

        return false;
    }

    void Update()
    {
        // El movimiento FAKE ha sido removido.
        // Ahora este script solo actúa como "manager de estado de aislamiento" 
        // para que MultiSensorController.cs decida qué datos reales aplicar.
    }
}
