using System.IO.Ports;
using UnityEngine;

public class BluetoothReceiver : MonoBehaviour
{
    SerialPort serial = new("COM10", 115200);
    void Start()
    {
        serial.Open();
        serial.ReadTimeout = 100;
    }

    // Update is called once per frame
    void Update()
    {
        try {
            string incomingData = serial.ReadLine();
            string[] parsedData = incomingData.Split(',');
            bool success = float.TryParse(parsedData[0], out float res);
            Debug.Log(res);
        }
        catch (System.TimeoutException) { }
    }
}
