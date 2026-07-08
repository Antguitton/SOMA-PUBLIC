using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public class SOMAScriptsTests
{
    [Test]
    public void SensorController_CanBeCreatedWithDefaultValues()
    {
        var go = new GameObject("SensorControllerTestObject");
        var controller = go.AddComponent<SensorController>();

        Assert.IsNotNull(controller);
        Assert.AreEqual(0.1f, controller.updateInterval);
        Assert.IsNull(controller.targetBone);
    }

    [Test]
    public void MultiSensorController_InitializesSensorBonesSafely()
    {
        var go = new GameObject("MultiSensorControllerTestObject");
        var controller = go.AddComponent<MultiSensorController>();

        Assert.IsNotNull(controller.sensorBones);
        Assert.AreEqual(0, controller.sensorBones.Count);
    }

    [Test]
    public void MultiSensorController_CanCreateSensorBoneConfiguration()
    {
        var go = new GameObject("MultiSensorControllerConfigTestObject");
        var controller = go.AddComponent<MultiSensorController>();

        controller.sensorBones = new System.Collections.Generic.List<MultiSensorController.SensorBone>
        {
            new MultiSensorController.SensorBone
            {
                sensorId = "elbow_l",
                bone = new GameObject("elbow_l_bone").transform,
                offsetPose = new Vector3(10f, 0f, 0f)
            }
        };

        Assert.AreEqual(1, controller.sensorBones.Count);
        Assert.AreEqual("elbow_l", controller.sensorBones[0].sensorId);
        Assert.AreEqual(10f, controller.sensorBones[0].offsetPose.x);
    }

    [Test]
    public void SOMAFakeDataTester_InitializesDefaults()
    {
        var go = new GameObject("SOMAFakeDataTesterTestObject");
        var tester = go.AddComponent<SOMAFakeDataTester>();

        Assert.IsFalse(tester.autoStartPreview);
        Assert.AreEqual(1.2f, tester.previewSpeed);
        Assert.AreEqual(18f, tester.previewAmplitude);
        Assert.IsTrue(tester.autoFrameMannequinOnStart);
        Assert.AreEqual(SOMAFakeDataTester.TestMode.None, tester.currentMode);
    }

    [Test]
    public void SOMAFakeDataTester_ShouldAnimateRespectsModeSelection()
    {
        var go = new GameObject("SOMAFakeDataTesterMotionTestObject");
        var tester = go.AddComponent<SOMAFakeDataTester>();

        tester.SetTestMode(SOMAFakeDataTester.TestMode.LeftArm);
        Assert.IsTrue(tester.ShouldAnimate("shoulder_l"));
        Assert.IsFalse(tester.ShouldAnimate("ankle_r"));

        tester.SetTestMode(SOMAFakeDataTester.TestMode.RightLeg);
        Assert.IsTrue(tester.ShouldAnimate("ankle_r"));
        Assert.IsFalse(tester.ShouldAnimate("elbow_l"));
    }

    [Test]
    public void SOMAFakeDataTester_ShouldAnimateReturnsFalseForEmptyId()
    {
        var go = new GameObject("SOMAFakeDataTesterEmptyIdTestObject");
        var tester = go.AddComponent<SOMAFakeDataTester>();

        Assert.IsFalse(tester.ShouldAnimate(string.Empty));
    }

    [Test]
    public void MultiSensorController_ImprovedHybridTrackingChangesRotation()
    {
        var go = new GameObject("MultiSensorControllerHybridTestObject");
        var controller = go.AddComponent<MultiSensorController>();

        var boneGo = new GameObject("hybrid_bone");
        boneGo.transform.SetParent(go.transform, false);

        controller.sensorBones = new System.Collections.Generic.List<MultiSensorController.SensorBone>
        {
            new MultiSensorController.SensorBone
            {
                sensorId = "elbow_l",
                bone = boneGo.transform,
                initialBoneRotation = Quaternion.identity,
                currentRotation = Quaternion.identity,
                calibrationGravity = Vector3.up,
                isCalibrated = true,
                modo = MultiSensorController.TrackingMode.HibridoMejorado,
                suavizado = 0.5f,
                sensibilidadFlexion = 1.0f
            }
        };

        var imuData = new MultiSensorController.IMUData { ts = 1, ax = 0, ay = 1, az = 0 };
        var method = typeof(MultiSensorController).GetMethod("ApplyImprovedHybridTracking", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        method.Invoke(controller, new object[] { controller.sensorBones[0], imuData });

        Assert.That(controller.sensorBones[0].bone.localRotation, Is.Not.EqualTo(Quaternion.identity));
    }

    [Test]
    public void SOMATelemetryViewer_CanBeCreatedWithoutSceneUI()
    {
        var go = new GameObject("SOMATelemetryViewerTestObject");
        var viewer = go.AddComponent<SOMATelemetryViewer>();

        Assert.IsNotNull(viewer);
    }

#if UNITY_EDITOR_OSX || UNITY_IOS
    [Test]
    public void BLETest_CanBeCreatedOnApplePlatforms()
    {
        var go = new GameObject("BLE_Test_Object");
        var bleTest = go.AddComponent<BLE_Test>();

        Assert.IsNotNull(bleTest);
    }

    [Test]
    public void MultiSensorController_ParseJsonStoresLatestSensorData()
    {
        var go = new GameObject("MultiSensorControllerParseJsonTestObject");
        var controller = go.AddComponent<MultiSensorController>();

        var parseMethod = typeof(MultiSensorController).GetMethod("ParseJson", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(parseMethod);

        parseMethod.Invoke(controller, new object[] { "{\"ts\":10,\"mux\":2,\"ch\":0,\"ax\":1000,\"ay\":0,\"az\":0,\"gx\":0,\"gy\":0,\"gz\":0}" });

        var latestField = typeof(MultiSensorController).GetField("latestSensorData", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(latestField);

        var latestSensorData = latestField.GetValue(controller) as System.Collections.Generic.Dictionary<string, MultiSensorController.IMUData>;
        Assert.IsNotNull(latestSensorData);
        Assert.IsTrue(latestSensorData.ContainsKey("elbow_l"));
    }
#endif

    [TearDown]
    public void TearDown()
    {
        var sensorControllers = Object.FindObjectsByType<SensorController>(FindObjectsSortMode.None);
        foreach (var controller in sensorControllers)
        {
            if (controller != null) Object.DestroyImmediate(controller.gameObject);
        }

        var multiControllers = Object.FindObjectsByType<MultiSensorController>(FindObjectsSortMode.None);
        foreach (var controller in multiControllers)
        {
            if (controller != null) Object.DestroyImmediate(controller.gameObject);
        }

        var fakeTester = Object.FindObjectOfType<SOMAFakeDataTester>();
        if (fakeTester != null) Object.DestroyImmediate(fakeTester.gameObject);

        var telemetryViewer = Object.FindObjectOfType<SOMATelemetryViewer>();
        if (telemetryViewer != null) Object.DestroyImmediate(telemetryViewer.gameObject);
    }
}
