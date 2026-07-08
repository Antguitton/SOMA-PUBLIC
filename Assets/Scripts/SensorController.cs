using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorController : MonoBehaviour
{
    [System.Serializable]
    public class SensorData
    {
        public string id;
        public Vector3 rot;
    }

    public Transform targetBone; // Assign this from the editor
    public float updateInterval = 0.1f;

    private List<SensorData> simulatedData;
    private int currentIndex = 0;

    void Start()
    {
        // Simulated arm movement data
        simulatedData = new List<SensorData>
        {
            new SensorData {id = "arm_r", rot = new Vector3(0, 0, 0)},
            new SensorData {id = "arm_r", rot = new Vector3(10, 0, 0)},
            new SensorData {id = "arm_r", rot = new Vector3(20, 5, 0)},
            new SensorData {id = "arm_r", rot = new Vector3(30, 10, 0)},
            new SensorData {id = "arm_r", rot = new Vector3(40, 10, 5)},
            new SensorData {id = "arm_r", rot = new Vector3(50, 15, 10)},
            new SensorData {id = "arm_r", rot = new Vector3(40, 10, 5)},
            new SensorData {id = "arm_r", rot = new Vector3(30, 5, 0)},
            new SensorData {id = "arm_r", rot = new Vector3(20, 0, 0)},
            new SensorData {id = "arm_r", rot = new Vector3(10, 0, 0)},
            new SensorData {id = "arm_r", rot = new Vector3(0, 0, 0)}
        };

        StartCoroutine(PlaySimulation());
    }

    IEnumerator PlaySimulation()
    {
        while (true)
        {
            if (targetBone != null && currentIndex < simulatedData.Count)
            {
                SensorData data = simulatedData[currentIndex];
                Quaternion rotation = Quaternion.Euler(data.rot);
                targetBone.localRotation = rotation;
                currentIndex++;
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }
}
