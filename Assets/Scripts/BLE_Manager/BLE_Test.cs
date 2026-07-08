using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityCoreBluetooth;

public class BLE_Test : MonoBehaviour
{
    [Header("BLE Configuration")]
    public string peripheralName = "IMU_ARM";

    [Header("UI Texts (Assign in the Inspector)")]
    public Text textEpaule;  // Drag the UI Text here for ch 0
    public Text textCoude;   // Drag the UI Text here for ch 1
    public Text textPoignet; // Drag the UI Text here for ch 2

    private CoreBluetoothManager manager;
    private CoreBluetoothCharacteristic characteristic;
    
    // Buffer used to merge fragmented packets
    private string dataBuffer = "";
    private object dataLock = new object();

    // Stores the latest reading for each sensor (0, 1, and 2)
    private IMUData[] imuSensors = new IMUData[3]; 

    void Start()
    {
        // Initialize the array to avoid null-reference errors
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

        // Data-receive event: just append to the buffer
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

    // Process the buffer by looking for complete JSON blocks between braces { }
    void ProcessBuffer()
    {
        string textToProcess = "";

        // Safely extract the buffer contents
        lock (dataLock)
        {
            if (string.IsNullOrEmpty(dataBuffer)) return;
            textToProcess = dataBuffer;
            dataBuffer = ""; 
        }

        // Keep going while a closing brace '}' is present
        while (textToProcess.Contains("}"))
        {
            int endIndex = textToProcess.IndexOf("}");
            
            // Take the candidate JSON up to the closing brace
            string potentialJson = textToProcess.Substring(0, endIndex + 1);
            
            // Keep the remaining text for the next pass
            textToProcess = textToProcess.Substring(endIndex + 1);

            // Find where this JSON starts (the last opening brace '{')
            int startIndex = potentialJson.LastIndexOf("{");

            if (startIndex != -1)
            {
                // We have a clean block from { to }
                string cleanJson = potentialJson.Substring(startIndex);
                ParseJson(cleanJson);
            }
        }

        // If text remains, push the incomplete packet back into the buffer
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

            // Check that the channel is valid (0, 1, or 2)
            if (data.ch >= 0 && data.ch < 3)
            {
                imuSensors[data.ch] = data; // Store it in the matching slot
            }
        }
        catch (Exception e)
        {
            // Only log if it is a real format error, not OCR noise
            Debug.LogWarning($"JSON error: {e.Message} in string: {json}");
        }
    }

    void UpdateUI()
    {
        // Update each text field independently
        if (textEpaule != null) textEpaule.text = FormatData("SHOULDER", imuSensors[0]);
        if (textCoude != null)  textCoude.text  = FormatData("ELBOW", imuSensors[1]);
        if (textPoignet != null) textPoignet.text = FormatData("WRIST", imuSensors[2]);
    }

    string FormatData(string label, IMUData imu)
    {
        // If timestamp is 0, no data has been received for this sensor yet
        if (imu.ts == 0 && imu.ax == 0) return $"{label}:\nWaiting for data...";

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
