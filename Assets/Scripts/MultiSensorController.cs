using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;
#endif

public class MultiSensorController : MonoBehaviour
{
    [System.Serializable]
    public class SensorBone
    {
        public string sensorId; 
        public Transform bone;
        
        [Header("🔴 MAIN ADJUSTMENTS")]
        [Tooltip("Adjust the base rotation so the arm is aligned with the T-pose.")]
        public Vector3 offsetPose = Vector3.zero;

        [Header("🎯 TRACKING MODE")]
        [InspectorName("Tracking Mode")]
        [Tooltip("ImprovedHybrid detects movement in any direction, including upward and forward.")]
        public TrackingMode modo = TrackingMode.Acelerometro;

        [Header("🔵 AXIS INVERSION (Real Time)")]
        [InspectorName("Invert X")]
        [Tooltip("Invert the sensor X axis.")]
        public bool invertirX = false;
        
        [InspectorName("Invert Y")]
        [Tooltip("Invert the sensor Y axis.")]
        public bool invertirY = false;
        
        [InspectorName("Invert Z")]
        [Tooltip("Invert the sensor Z axis.")]
        public bool invertirZ = false;

        [Header("🔄 AXIS SWAP")]
        [Tooltip("Swap X and Y.")]
        public bool swapXY = false;
        
        [Tooltip("Swap X and Z.")]
        public bool swapXZ = false;
        
        [Tooltip("Swap Y and Z.")]
        public bool swapYZ = false;

        [Header("⚙️ MOVEMENT SETTINGS")]
        [InspectorName("Movement Sensitivity")]
        [Tooltip("Movement multiplier. 1 = real movement. >1 = exaggerated movement.")]
        [Range(0f, 5f)]
        public float sensibilidadFlexion = 1.0f;

        [Header("⚙️ Gyroscope Sensitivity (Gyroscope Mode Only)")]
        [InspectorName("Gyro Sensitivity X")]
        [Range(0f, 10f)]
        public float sensibilidadX = 1.0f;
        [InspectorName("Gyro Sensitivity Y")]
        [Range(0f, 10f)]
        public float sensibilidadY = 1.0f;
        [InspectorName("Gyro Sensitivity Z")]
        [Range(0f, 10f)]
        public float sensibilidadZ = 1.0f;

        [Header("🎚️ SMOOTHING")]
        [InspectorName("Smoothing")]
        [Range(0.01f, 1f)]
        public float suavizado = 0.3f;

        [HideInInspector] public Quaternion initialBoneRotation;
        [HideInInspector] public Quaternion currentRotation; 
        [HideInInspector] public Vector3 calibrationGravity;
        [HideInInspector] public bool isCalibrated = false;
    }

    public enum TrackingMode
    {
        [InspectorName("Accelerometer")]
        Acelerometro,      // Uses absolute gravity
        [InspectorName("Gyroscope")]
        Giroscopio,        // Uses angular velocity (good for fast rotations)
        [InspectorName("Improved Hybrid")]
        HibridoMejorado    // Uses 3D gravity delta (up, down, forward, backward)
    }

    public List<SensorBone> sensorBones;

    [Header("📊 DEBUG")]
    [InspectorName("Show Gyro Values")]
    public bool mostrarValoresGiro = false;
    [InspectorName("Show Accelerometer Values")]
    public bool mostrarValoresAcelerometro = false;

    [Header("🔴 INSTRUCTIONS")]
    [InspectorName("Instructions")]
    [TextArea(8, 14)]
    public string instrucciones = "NEW SETUP:\n\n1. Select 'Improved Hybrid' mode for the elbow/arm.\n2. Put the arm in a T-pose (or neutral pose).\n3. Press 'C' to calibrate.\n4. Move the arm forward or upward.\n\nNOTE: If the movement is reversed, use the 'Invert X/Y/Z' checkboxes.";

    #if UNITY_EDITOR_OSX || UNITY_IOS
    private CoreBluetoothManager manager;
    private string peripheralName = "Soma.firmware"; 
    private string dataBuffer = "";
    private object dataLock = new object();
    private System.Collections.Generic.Dictionary<string, IMUData> latestSensorData = new System.Collections.Generic.Dictionary<string, IMUData>();
    #endif

    void Start()
    {
        foreach (var sb in sensorBones)
        {
            if (sb.bone != null) 
            {
                sb.initialBoneRotation = sb.bone.localRotation;
                sb.currentRotation = Quaternion.identity;
                sb.isCalibrated = false;
            }
        }
        // Dictionary initialization is already handled at declaration.
        
        Debug.Log(">>> 🎯 READY: Press C to calibrate positions.");
        
        #if UNITY_EDITOR_OSX || UNITY_IOS
        StartBluetooth();
        #endif
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) 
        {
            CalibrateAll();
            Debug.Log(">>> ✅ Calibration saved");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var sb in sensorBones)
            {
                sb.currentRotation = Quaternion.identity;
                sb.bone.localRotation = sb.initialBoneRotation;
            }
            Debug.Log(">>> 🔄 Rotations reset");
        }

        #if UNITY_EDITOR_OSX || UNITY_IOS
        ProcessBuffer();

        foreach (var sb in sensorBones)
        {
            // Only apply Bluetooth updates to the bones selected in the UI isolation mode.
            if (SOMAFakeDataTester.Instance != null && SOMAFakeDataTester.Instance.currentMode != SOMAFakeDataTester.TestMode.None)
            {
                if (!SOMAFakeDataTester.Instance.ShouldAnimate(sb.sensorId)) continue;
            }

            if (latestSensorData.TryGetValue(sb.sensorId, out IMUData data))
            {
                if (data.ts != 0) ApplyRotation(sb, data);
            }
        }
        #endif
    }

    void CalibrateAll()
    {
        Debug.Log(">>> ♻️ Recalibrating all sensors...");
        foreach (var sb in sensorBones) 
        {
            sb.isCalibrated = false;
            sb.currentRotation = Quaternion.identity;
        }
    }

    void ApplyRotation(SensorBone sb, IMUData data)
    {
        if (sb.modo == TrackingMode.Acelerometro)
        {
            ApplyAccelerometerTracking(sb, data);
        }
        else if (sb.modo == TrackingMode.Giroscopio)
        {
            ApplyGyroscopeTracking(sb, data);
        }
        else // Improved hybrid (3D-corrected)
        {
            ApplyImprovedHybridTracking(sb, data);
        }
    }

    void ApplyAccelerometerTracking(SensorBone sb, IMUData data)
    {
        float x = data.ax;
        float y = data.ay;
        float z = data.az;
        
        if (sb.swapXY) { float temp = x; x = y; y = temp; }
        if (sb.swapXZ) { float temp = x; x = z; z = temp; }
        if (sb.swapYZ) { float temp = y; y = z; z = temp; }
        
        if (sb.invertirX) x = -x;
        if (sb.invertirY) y = -y;
        if (sb.invertirZ) z = -z;
        
        Vector3 currentGravity = new Vector3(x, y, z).normalized;
        if (currentGravity == Vector3.zero) return;

        if (!sb.isCalibrated)
        {
            sb.calibrationGravity = currentGravity;
            sb.isCalibrated = true;
            Debug.Log($">>> ✅ {sb.sensorId} calibrated [ACCEL]");
            return;
        }

        Quaternion sensorDelta = Quaternion.FromToRotation(sb.calibrationGravity, currentGravity);
        Quaternion correctedBasePose = sb.initialBoneRotation * Quaternion.Euler(sb.offsetPose);
        Quaternion targetRotation = correctedBasePose * sensorDelta;
        
        sb.bone.localRotation = Quaternion.Slerp(sb.bone.localRotation, targetRotation, 0.1f);
    }

    void ApplyGyroscopeTracking(SensorBone sb, IMUData data)
    {
        float gx = data.gx;
        float gy = data.gy;
        float gz = data.gz;
        
        if (sb.swapXY) { float temp = gx; gx = gy; gy = temp; }
        if (sb.swapXZ) { float temp = gx; gx = gz; gz = temp; }
        if (sb.swapYZ) { float temp = gy; gy = gz; gz = temp; }
        
        if (sb.invertirX) gx = -gx;
        if (sb.invertirY) gy = -gy;
        if (sb.invertirZ) gz = -gz;

        if (!sb.isCalibrated)
        {
            sb.currentRotation = Quaternion.identity;
            sb.isCalibrated = true;
            return;
        }

        Vector3 angularVelocity = new Vector3(
            gx * sb.sensibilidadX,
            gy * sb.sensibilidadY,
            gz * sb.sensibilidadZ
        );

        float scaleFactor = 0.001f;
        Quaternion deltaRotation = Quaternion.Euler(angularVelocity * Time.deltaTime * scaleFactor);
        sb.currentRotation *= deltaRotation;

        Quaternion correctedBasePose = sb.initialBoneRotation * Quaternion.Euler(sb.offsetPose);
        Quaternion targetRotation = correctedBasePose * sb.currentRotation;
        
        sb.bone.localRotation = Quaternion.Slerp(sb.bone.localRotation, targetRotation, sb.suavizado);
    }

    // ---------------------------------------------------------
    //  Main fix lives here
    // ---------------------------------------------------------
    void ApplyImprovedHybridTracking(SensorBone sb, IMUData data)
    {
        // 1. Read raw data
        float x = data.ax;
        float y = data.ay;
        float z = data.az;
        
        // 2. Apply axis swaps
        if (sb.swapXY) { float temp = x; x = y; y = temp; }
        if (sb.swapXZ) { float temp = x; x = z; z = temp; }
        if (sb.swapYZ) { float temp = y; y = z; z = temp; }
        
        // 3. Apply inversions
        if (sb.invertirX) x = -x;
        if (sb.invertirY) y = -y;
        if (sb.invertirZ) z = -z;
        
        Vector3 currentGravity = new Vector3(x, y, z).normalized;
        if (currentGravity == Vector3.zero) return;

        if (mostrarValoresAcelerometro)
        {
            Debug.Log($"{sb.sensorId} [HYBRID] -> x:{x:F0} y:{y:F0} z:{z:F0}");
        }

        // 4. Initial calibration
        if (!sb.isCalibrated)
        {
            sb.calibrationGravity = currentGravity;
            sb.isCalibrated = true;
            Debug.Log($">>> ✅ {sb.sensorId} calibrated [3D HYBRID]");
            return;
        }

        // 5. Full 3D rotation calculation
        // Instead of computing a single angle, compute the full delta rotation
        // needed to move from calibrated gravity to current gravity.
        // This allows the arm to move up, down, forward, or backward.
        Quaternion rotationDelta = Quaternion.FromToRotation(sb.calibrationGravity, currentGravity);

        // 6. Apply sensitivity (amplify or reduce movement)
        if (sb.sensibilidadFlexion != 1.0f)
        {
            // SlerpUnclamped can go beyond 100% to amplify motion.
            rotationDelta = Quaternion.SlerpUnclamped(Quaternion.identity, rotationDelta, sb.sensibilidadFlexion);
        }

        // 7. Apply the result to the bone
        Quaternion correctedBasePose = sb.initialBoneRotation * Quaternion.Euler(sb.offsetPose);
        
        // Apply the calculated delta on top of the base pose.
        Quaternion targetRotation = correctedBasePose * rotationDelta;
        
        sb.bone.localRotation = Quaternion.Slerp(sb.bone.localRotation, targetRotation, sb.suavizado);
    }

    #if UNITY_EDITOR_OSX || UNITY_IOS
    void StartBluetooth() 
    { 
        manager = CoreBluetoothManager.Shared; 
        manager.OnUpdateState((s) => { 
            if(s=="poweredOn") {
                Debug.Log(">>> 🔵 Bluetooth is on, scanning...");
                manager.StartScan(); 
            }
        }); 
        manager.OnDiscoverPeripheral((p) => { 
            if(p.name!=peripheralName) return; 
            Debug.Log($">>> 🎯 Found: {peripheralName}");
            manager.StopScan(); 
            manager.ConnectToPeripheral(p); 
        }); 
        manager.OnConnectPeripheral((p) => {
            Debug.Log(">>> ✅ Connected");
            p.discoverServices();
        }); 
        manager.OnDiscoverService((s) => s.discoverCharacteristics()); 
        manager.OnDiscoverCharacteristic((c) => { 
            foreach(var u in c.Propertis) 
                if(u=="notify") c.SetNotifyValue(true); 
        }); 
        manager.OnUpdateValue((c,d) => { 
            string t = System.Text.Encoding.UTF8.GetString(d); 
            lock(dataLock){ dataBuffer+=t; } 
        }); 
        manager.Start(); 
    }
    
    void ProcessBuffer() 
    { 
        string t=""; 
        lock(dataLock){ 
            if(string.IsNullOrEmpty(dataBuffer))return; 
            t=dataBuffer; 
            dataBuffer=""; 
        } 
        while(t.Contains("}")){ 
            int e=t.IndexOf("}"); 
            string j=t.Substring(0,e+1); 
            t=t.Substring(e+1); 
            int s=j.LastIndexOf("{"); 
            if(s!=-1) ParseJson(j.Substring(s)); 
        } 
        lock(dataLock){ dataBuffer=t+dataBuffer; } 
    }
    
    void ParseJson(string j) 
    { 
        try{ 
            IMUData d=JsonUtility.FromJson<IMUData>(j); 
            string sensorId = GetSensorIdFromMuxCh(d.mux, d.ch);
            if (!string.IsNullOrEmpty(sensorId)) {
                latestSensorData[sensorId] = d;
            }
        }catch{} 
    }

    private string GetSensorIdFromMuxCh(int mux, int ch)
    {
        if (mux == 0) {
            if (ch == 0) return "knee_l";
            if (ch == 1) return "hip_l";
            if (ch == 2) return "ankle_l";
        } else if (mux == 1) {
            if (ch == 0) return "hip_r";
            if (ch == 1) return "knee_r";
            if (ch == 2) return "ankle_r";
        } else if (mux == 2) {
            if (ch == 0) return "elbow_l";
            if (ch == 1) return "wrist_l";
            if (ch == 2) return "shoulder_l";
        } else if (mux == 3) {
            if (ch == 0) return "neck";
            if (ch == 1) return "elbow_r";
            if (ch == 2) return "wrist_r";
            if (ch == 3) return "shoulder_r";
        }
        return null;
    }
    #endif
    
    [System.Serializable] 
    public class IMUData 
    { 
        public int ts, mux, ch, ax, ay, az, gx, gy, gz;
    }
}
