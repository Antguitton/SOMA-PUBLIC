using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;

public class BLE_Test : MonoBehaviour
{
    [Header("Configuración BLE")]
    public string peripheralName = "IMU_ARM";

    [Header("UI Texts (Asignar en el Inspector)")]
    public Text textEpaule;  // Arrastra aquí el UI Text para ch 0
    public Text textCoude;   // Arrastra aquí el UI Text para ch 1
    public Text textPoignet; // Arrastra aquí el UI Text para ch 2

    private CoreBluetoothManager manager;
    private CoreBluetoothCharacteristic characteristic;
    
    // Buffer para unir paquetes fragmentados
    private string dataBuffer = "";
    private object dataLock = new object();

    // Array para guardar la última lectura de cada sensor (0, 1 y 2)
    private IMUData[] imuSensors = new IMUData[3]; 

    void Start()
    {
        // Inicializamos el array para evitar errores de nulos
        for(int i=0; i<3; i++) imuSensors[i] = new IMUData();

        manager = CoreBluetoothManager.Shared;

        manager.OnUpdateState((string state) =>
        {
            if (state != "poweredOn") return;
            manager.StartScan();
        });

        manager.OnDiscoverPeripheral((CoreBluetoothPeripheral peripheral) =>
        {
            if (peripheral.name != peripheralName) return;
            manager.StopScan();
            manager.ConnectToPeripheral(peripheral);
        });

        manager.OnConnectPeripheral((CoreBluetoothPeripheral peripheral) =>
        {
            peripheral.discoverServices();
        });

        manager.OnDiscoverService((CoreBluetoothService service) =>
        {
            service.discoverCharacteristics();
        });

        manager.OnDiscoverCharacteristic((CoreBluetoothCharacteristic ch) =>
        {
            characteristic = ch;
            foreach (var usage in ch.Propertis)
            {
                if (usage == "notify")
                    ch.SetNotifyValue(true);
            }
        });

        // Evento al recibir datos: Solo acumulamos en el buffer
        manager.OnUpdateValue((CoreBluetoothCharacteristic ch, byte[] data) =>
        {
            string receivedFragment = System.Text.Encoding.UTF8.GetString(data);
            lock(dataLock) 
            {
                dataBuffer += receivedFragment;
            }
        });

        manager.Start();
    }

    void Update()
    {
        ProcessBuffer();
        UpdateUI();
    }

    // Procesa el buffer buscando JSONs completos entre llaves { }
    void ProcessBuffer()
    {
        string textToProcess = "";

        // Extraemos el contenido del buffer de forma segura
        lock (dataLock)
        {
            if (string.IsNullOrEmpty(dataBuffer)) return;
            textToProcess = dataBuffer;
            dataBuffer = ""; 
        }

        // Mientras encontremos una llave de cierre '}'
        while (textToProcess.Contains("}"))
        {
            int endIndex = textToProcess.IndexOf("}");
            
            // Tomamos el posible JSON hasta la llave de cierre
            string potentialJson = textToProcess.Substring(0, endIndex + 1);
            
            // Actualizamos el texto restante para la siguiente vuelta
            textToProcess = textToProcess.Substring(endIndex + 1);

            // Buscamos dónde empieza este JSON (la última llave '{')
            int startIndex = potentialJson.LastIndexOf("{");

            if (startIndex != -1)
            {
                // Tenemos un bloque limpio de { a }
                string cleanJson = potentialJson.Substring(startIndex);
                ParseJson(cleanJson);
            }
        }

        // Si sobra texto (un paquete incompleto que llegó al final), lo devolvemos al buffer
        lock (dataLock)
        {
            dataBuffer = textToProcess + dataBuffer;
        }
    }

    void ParseJson(string json)
    {
        try
        {
            IMUData data = JsonUtility.FromJson<IMUData>(json);

            // Verificamos que el canal sea válido (0, 1 o 2)
            if (data.ch >= 0 && data.ch < 3)
            {
                imuSensors[data.ch] = data; // Guardamos en el slot correspondiente
            }
        }
        catch (Exception e)
        {
            // Solo imprimimos si es un error real de formato, no de OCR
            Debug.LogWarning($"Error JSON: {e.Message} en string: {json}");
        }
    }

    void UpdateUI()
    {
        // Actualizamos cada texto independientemente
        if (textEpaule != null) textEpaule.text = FormatData("EPAULE (Hombro)", imuSensors[0]);
        if (textCoude != null)  textCoude.text  = FormatData("COUDE (Codo)", imuSensors[1]);
        if (textPoignet != null) textPoignet.text = FormatData("POIGNET (Muñeca)", imuSensors[2]);
    }

    string FormatData(string label, IMUData imu)
    {
        // Si timestamp es 0, es que no hemos recibido datos para este sensor todavía
        if (imu.ts == 0 && imu.ax == 0) return $"{label}:\nEsperando datos...";

        float accelX = imu.ax / 16384f;
        float accelY = imu.ay / 16384f;
        float accelZ = imu.az / 16384f;

        float gyroX = imu.gx / 131f;
        float gyroY = imu.gy / 131f;
        float gyroZ = imu.gz / 131f;

        return $"<b>{label}</b>\n" +
               $"Gyro: X:{gyroX:F0} Y:{gyroY:F0} Z:{gyroZ:F0}\n" +
               $"Accel: X:{accelX:F2} Y:{accelY:F2} Z:{accelZ:F2}";
    }

    [System.Serializable]
    public class IMUData
    {
        public int ts;
        public int mux;
        public int mpu;
        public int ch; // 0, 1, 2
        public int id;
        public int ax;
        public int ay;
        public int az;
        public int gx;
        public int gy;
        public int gz;
    }
}
#endif