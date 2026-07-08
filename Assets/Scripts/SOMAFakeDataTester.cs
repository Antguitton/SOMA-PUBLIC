using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SOMAFakeDataTester : MonoBehaviour
{
    public static SOMAFakeDataTester Instance;
    public enum TestMode { None, All, LeftArm, RightArm, LeftLeg, RightLeg }

    [Header("Preview")]
    public bool autoStartPreview = false;
    [Range(0.1f, 4f)] public float previewSpeed = 1.2f;
    [Range(0f, 45f)] public float previewAmplitude = 18f;

    [Header("Visibility")]
    public bool autoFrameMannequinOnStart = true;

    public TestMode currentMode = TestMode.None;
    private MultiSensorController[] controllers;
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

        if (autoStartPreview && currentMode == TestMode.None)
        {
            SetTestMode(TestMode.All);
        }
        else if (currentMode != TestMode.None)
        {
            SetTestMode(currentMode);
        }

        if (autoFrameMannequinOnStart)
        {
            Invoke(nameof(EnsureMannequinVisibility), 0.05f);
        }
    }

    public void SetTestMode(TestMode mode)
    {
        currentMode = mode;
        controllers = ResolveControllers();

        if (currentMode != TestMode.None)
        {
            Debug.Log($">>> 🟢 Fake Movement Started: {mode}");
        }
        else
        {
            ResetPreviewPose();
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
        if (currentMode == TestMode.None) return;

        if (controllers == null || controllers.Length == 0)
        {
            controllers = ResolveControllers();
            if (controllers == null || controllers.Length == 0) return;
        }

        ApplyPreviewMotion();
    }

    void ApplyPreviewMotion()
    {
        float cycle = Time.time * previewSpeed;

        foreach (var controller in controllers)
        {
            if (controller == null || controller.sensorBones == null) continue;

            foreach (var sensorBone in controller.sensorBones)
            {
                if (sensorBone == null || sensorBone.bone == null) continue;
                if (!ShouldAnimate(sensorBone.sensorId)) continue;

                Vector3 previewEuler = GetPreviewOffset(sensorBone.sensorId.ToLower(), cycle);
                Quaternion targetRotation = sensorBone.initialBoneRotation * Quaternion.Euler(previewEuler);
                sensorBone.bone.localRotation = Quaternion.Slerp(sensorBone.bone.localRotation, targetRotation, 0.08f);
            }
        }
    }

    void ResetPreviewPose()
    {
        if (controllers == null) return;

        foreach (var controller in controllers)
        {
            if (controller == null || controller.sensorBones == null) continue;

            foreach (var sensorBone in controller.sensorBones)
            {
                if (sensorBone == null || sensorBone.bone == null) continue;
                sensorBone.bone.localRotation = sensorBone.initialBoneRotation;
            }
        }
    }

    Vector3 GetPreviewOffset(string sensorId, float cycle)
    {
        float swing = Mathf.Sin(cycle) * previewAmplitude;
        float bend = Mathf.Sin(cycle + 0.8f) * previewAmplitude * 0.5f;
        bool isLeft = sensorId.EndsWith("_l") || sensorId.Contains("left");
        float side = isLeft ? -1f : 1f;

        if (sensorId.Contains("shoulder"))
            return new Vector3(-swing * 0.45f, 0f, side * swing * 0.2f);

        if (sensorId.Contains("elbow"))
            return new Vector3(-previewAmplitude * 0.35f - Mathf.Abs(bend), 0f, 0f);

        if (sensorId.Contains("wrist"))
            return new Vector3(-Mathf.Abs(bend) * 0.35f, 0f, side * swing * 0.15f);

        if (sensorId.Contains("hip"))
            return new Vector3(0f, 0f, -side * swing * 0.12f);

        if (sensorId.Contains("knee"))
            return new Vector3(Mathf.Abs(bend) * 0.4f, 0f, 0f);

        if (sensorId.Contains("ankle"))
            return new Vector3(-Mathf.Abs(bend) * 0.2f, 0f, 0f);

        return Vector3.zero;
    }

    void EnsureMannequinVisibility()
    {
        GameObject mannequinRoot = ResolveMannequinRoot();
        if (mannequinRoot == null)
        {
            Debug.LogWarning(">>> Unable to resolve the mannequin root from the active scene.");
            return;
        }

        EnsureHierarchyActive(mannequinRoot.transform);

        Renderer[] renderers = mannequinRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning($">>> Mannequin root '{mannequinRoot.name}' was found, but no renderers were detected under it.");
            return;
        }

        Bounds bounds = new Bounds(mannequinRoot.transform.position, Vector3.zero);
        bool hasValidBounds = false;

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;

            if (!hasValidBounds)
            {
                bounds = renderer.bounds;
                hasValidBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        Camera cam = Camera.main;
        if (cam == null || !hasValidBounds) return;

        Vector3 focusPoint = bounds.center + Vector3.up * Mathf.Max(bounds.extents.y * 0.15f, 0.2f);
        float radius = Mathf.Max(bounds.extents.magnitude, 0.8f);
        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = radius / Mathf.Tan(halfFov);

        cam.transform.position = focusPoint + new Vector3(0f, radius * 0.15f, -(distance + bounds.extents.z + 0.6f));
        cam.transform.rotation = Quaternion.LookRotation(focusPoint - cam.transform.position, Vector3.up);

        Debug.Log($">>> Mannequin framed from root '{mannequinRoot.name}'. Center={bounds.center} Size={bounds.size}");
    }

    GameObject ResolveMannequinRoot()
    {
        if (controllers == null || controllers.Length == 0)
            controllers = ResolveControllers();

        if (controllers != null)
        {
            foreach (var controller in controllers)
            {
                if (controller == null) continue;
                return controller.gameObject;
            }
        }

        Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (var animator in animators)
        {
            if (animator == null) continue;
            if (animator.GetComponentsInChildren<Renderer>(true).Length > 0)
                return animator.gameObject;
        }

        Animator[] hiddenAnimators = Resources.FindObjectsOfTypeAll<Animator>();
        foreach (var animator in hiddenAnimators)
        {
            if (!IsSceneObject(animator.gameObject)) continue;
            if (animator.GetComponentsInChildren<Renderer>(true).Length > 0)
                return animator.gameObject;
        }

        GameObject rendererRoot = ResolveRootFromSceneRenderers();
        if (rendererRoot != null)
            return rendererRoot;

        return null;
    }

    MultiSensorController[] ResolveControllers()
    {
        MultiSensorController[] activeControllers = Object.FindObjectsByType<MultiSensorController>(FindObjectsSortMode.None);
        if (activeControllers != null && activeControllers.Length > 0) return activeControllers;

        MultiSensorController[] allControllers = Resources.FindObjectsOfTypeAll<MultiSensorController>();
        if (allControllers == null || allControllers.Length == 0) return allControllers;

        int sceneCount = 0;
        foreach (var controller in allControllers)
        {
            if (controller != null && IsSceneObject(controller.gameObject))
                sceneCount++;
        }

        MultiSensorController[] sceneControllers = new MultiSensorController[sceneCount];
        int index = 0;
        foreach (var controller in allControllers)
        {
            if (controller == null || !IsSceneObject(controller.gameObject)) continue;
            sceneControllers[index++] = controller;
        }

        return sceneControllers;
    }

    bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null
            && gameObject.scene.IsValid()
            && gameObject.scene.isLoaded;
    }

    GameObject ResolveRootFromSceneRenderers()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded) return null;

        GameObject[] roots = activeScene.GetRootGameObjects();
        GameObject bestRoot = null;
        int bestRendererCount = 0;
        float bestVolume = 0f;

        foreach (GameObject root in roots)
        {
            if (root == null) continue;
            if (root == gameObject) continue;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) continue;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float volume = bounds.size.x * bounds.size.y * bounds.size.z;
            if (renderers.Length > bestRendererCount || (renderers.Length == bestRendererCount && volume > bestVolume))
            {
                bestRoot = root;
                bestRendererCount = renderers.Length;
                bestVolume = volume;
            }
        }

        // if (bestRoot == null)
        // {
        //     Debug.LogWarning($">>> No renderer-backed root found in scene '{activeScene.name}'. Roots: {string.Join(\", \", System.Array.ConvertAll(roots, root => root != null ? root.name : \"<null>\"))}");
        // }

        return bestRoot;
    }

    void EnsureHierarchyActive(Transform current)
    {
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }
}
