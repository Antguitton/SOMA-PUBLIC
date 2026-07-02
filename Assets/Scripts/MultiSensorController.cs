using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;
#endif

public class SentadillaSimulada : MonoBehaviour
{
    [System.Serializable]
    public class SensorBone
    {
        public string sensorId; 
        public Transform bone;
        
        [Header("🔴 AJUSTES PRINCIPALES")]
        [Tooltip("Ajusta la rotación base para que el brazo esté en T")]
        public Vector3 offsetPose = Vector3.zero;

        [Header("🎯 MODO DE TRACKING")]
        [Tooltip("NUEVO: HibridoMejorado detecta movimiento en cualquier dirección (arriba/adelante)")]
        public TrackingMode modo = TrackingMode.Acelerometro;

        [Header("🔵 INVERSIÓN DE EJES (Tiempo Real)")]
        [Tooltip("Invierte el eje X del sensor")]
        public bool invertirX = false;
        
        [Tooltip("Invierte el eje Y del sensor")]
        public bool invertirY = false;
        
        [Tooltip("Invierte el eje Z del sensor")]
        public bool invertirZ = false;

        [Header("🔄 SWAP DE EJES")]
        [Tooltip("Intercambia X con Y")]
        public bool swapXY = false;
        
        [Tooltip("Intercambia X con Z")]
        public bool swapXZ = false;
        
        [Tooltip("Intercambia Y con Z")]
        public bool swapYZ = false;

        [Header("⚙️ AJUSTES DE MOVIMIENTO")]
        [Tooltip("Multiplicador de movimiento. 1 = Real. >1 = Exagerado.")]
        [Range(0f, 5f)]
        public float sensibilidadFlexion = 1.0f;

        [Header("⚙️ Sensibilidad Giroscopio (Solo modo Giroscopio)")]
        [Range(0f, 10f)]
        public float sensibilidadX = 1.0f;
        [Range(0f, 10f)]
        public float sensibilidadY = 1.0f;
        [Range(0f, 10f)]
        public float sensibilidadZ = 1.0f;

        [Header("🎚️ Suavizado")]
        [Range(0.01f, 1f)]
        public float suavizado = 0.3f;

        [HideInInspector] public Quaternion initialBoneRotation;
        [HideInInspector] public Quaternion currentRotation; 
        [HideInInspector] public Vector3 calibrationGravity;
        [HideInInspector] public bool isCalibrated = false;
    }

    public enum TrackingMode
    {
        Acelerometro,      // Usa gravedad absoluta
        Giroscopio,        // Usa velocidad angular (bueno para rotaciones rápidas)
        HibridoMejorado    // CORREGIDO: Usa Delta de Gravedad 3D (Arriba, Abajo, Adelante, Atrás)
    }

    public List<SensorBone> sensorBones;

    [Header("📊 DEBUG")]
    public bool mostrarValoresGiro = false;
    public bool mostrarValoresAcelerometro = false;

    [Header("🔴 INSTRUCCIONES")]
    [TextArea(8, 14)]
    public string instrucciones = "NUEVA CONFIGURACIÓN:\n\n1. Selecciona modo 'HibridoMejorado' para el Codo/Brazo.\n2. Pon el brazo en POSE T (o neutra).\n3. Presiona 'C' para calibrar.\n4. Mueve el brazo hacia adelante o arriba.\n\nNOTA: Si el movimiento es inverso, usa los checkbox de 'Invertir X/Y/Z'.";

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
        // Inicialización del diccionario vacía (ya instanciado)
        
        Debug.Log(">>> 🎯 LISTO: Presiona C para calibrar posiciones.");
        
        #if UNITY_EDITOR_OSX || UNITY_IOS
        StartBluetooth();
        #endif
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) 
        {
            CalibrateAll();
            Debug.Log(">>> ✅ Calibración guardada");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var sb in sensorBones)
            {
                sb.currentRotation = Quaternion.identity;
                sb.bone.localRotation = sb.initialBoneRotation;
            }
            Debug.Log(">>> 🔄 Rotaciones reseteadas");
        }

        #if UNITY_EDITOR_OSX || UNITY_IOS
        ProcessBuffer();

        foreach (var sb in sensorBones)
        {
            // Aislar aplicando únicamente actualizaciones de Bluetooth a los huesos seleccionados en UI:
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
        Debug.Log(">>> ♻️ RECALIBRANDO todos los sensores...");
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
        else // HibridoMejorado (Corregido para 3D)
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
            Debug.Log($">>> ✅ {sb.sensorId} calibrado [ACCEL]");
            return;
        }

        Quaternion sensorDelta = Quaternion.FromToRotation(sb.calibrationGravity, currentGravity);
        Quaternion poseBaseCorregida = sb.initialBoneRotation * Quaternion.Euler(sb.offsetPose);
        Quaternion targetRotation = poseBaseCorregida * sensorDelta;
        
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

        Quaternion poseBaseCorregida = sb.initialBoneRotation * Quaternion.Euler(sb.offsetPose);
        Quaternion targetRotation = poseBaseCorregida * sb.currentRotation;
        
        sb.bone.localRotation = Quaternion.Slerp(sb.bone.localRotation, targetRotation, sb.suavizado);
    }

    // ---------------------------------------------------------
    //  👇 AQUÍ ESTÁ LA CORRECCIÓN PRINCIPAL 👇
    // ---------------------------------------------------------
    void ApplyImprovedHybridTracking(SensorBone sb, IMUData data)
    {
        // 1. Obtener datos crudos
        float x = data.ax;
        float y = data.ay;
        float z = data.az;
        
        // 2. Aplicar Swaps (Intercambio de ejes)
        if (sb.swapXY) { float temp = x; x = y; y = temp; }
        if (sb.swapXZ) { float temp = x; x = z; z = temp; }
        if (sb.swapYZ) { float temp = y; y = z; z = temp; }
        
        // 3. Aplicar Inversiones
        if (sb.invertirX) x = -x;
        if (sb.invertirY) y = -y;
        if (sb.invertirZ) z = -z;
        
        Vector3 currentGravity = new Vector3(x, y, z).normalized;
        if (currentGravity == Vector3.zero) return;

        if (mostrarValoresAcelerometro)
        {
            Debug.Log($"{sb.sensorId} [HIBRIDO] → x:{x:F0} y:{y:F0} z:{z:F0}");
        }

        // 4. Calibración Inicial
        if (!sb.isCalibrated)
        {
            sb.calibrationGravity = currentGravity;
            sb.isCalibrated = true;
            Debug.Log($">>> ✅ {sb.sensorId} calibrado [HIBRIDO 3D]");
            return;
        }

        // 5. CALCULO DE ROTACIÓN 3D (CORREGIDO)
        // En lugar de calcular un ángulo simple, calculamos la rotación "delta" completa
        // necesaria para ir desde la gravedad calibrada hasta la gravedad actual.
        // Esto permite mover el brazo arriba, abajo, adelante o atrás.
        Quaternion rotationDelta = Quaternion.FromToRotation(sb.calibrationGravity, currentGravity);

        // 6. Aplicar Sensibilidad (Amplificar o Reducir movimiento)
        if (sb.sensibilidadFlexion != 1.0f)
        {
            // SlerpUnclamped permite ir más allá del 100% (amplificar movimiento)
            rotationDelta = Quaternion.SlerpUnclamped(Quaternion.identity, rotationDelta, sb.sensibilidadFlexion);
        }

        // 7. Aplicar al hueso
        Quaternion poseBaseCorregida = sb.initialBoneRotation * Quaternion.Euler(sb.offsetPose);
        
        // Sumamos la rotación calculada a la base
        Quaternion targetRotation = poseBaseCorregida * rotationDelta;
        
        sb.bone.localRotation = Quaternion.Slerp(sb.bone.localRotation, targetRotation, sb.suavizado);
    }

    #if UNITY_EDITOR_OSX || UNITY_IOS
    void StartBluetooth() 
    { 
        manager = CoreBluetoothManager.Shared; 
        manager.OnUpdateState((s) => { 
            if(s=="poweredOn") {
                Debug.Log(">>> 🔵 Bluetooth encendido, escaneando...");
                manager.StartScan(); 
            }
        }); 
        manager.OnDiscoverPeripheral((p) => { 
            if(p.name!=peripheralName) return; 
            Debug.Log($">>> 🎯 Encontrado: {peripheralName}");
            manager.StopScan(); 
            manager.ConnectToPeripheral(p); 
        }); 
        manager.OnConnectPeripheral((p) => {
            Debug.Log(">>> ✅ Conectado");
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