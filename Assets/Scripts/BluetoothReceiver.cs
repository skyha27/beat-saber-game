using System.IO.Ports;
using UnityEngine;

public class BluetoothReceiver : MonoBehaviour
{
    // Serial information
    SerialPort serial = new("COM10", 115200);

    // Variables for Unity
    float currentX, currentY = 0f;
    float tolerance = 0.2f;
    float velX, velY = 0f;

    void Start()
    {
        serial.Open();
        serial.ReadTimeout = 100;
    }

    void Update()
    {
        try {
            string incomingData = serial.ReadLine();
            string[] parsedData = incomingData.Split(',');
            float.TryParse(parsedData[0], out float targetX);
            float.TryParse(parsedData[1], out float targetY);

            if (Mathf.Abs(currentX - targetX) > tolerance)
            {
                currentX = Mathf.SmoothDamp(currentX, targetX, ref velX, 0.08f);
            }
            if (Mathf.Abs(currentY - targetY) > tolerance)
            {
                currentY = Mathf.SmoothDamp(currentY, targetY, ref velY, 0.08f);
            }

            transform.rotation = Quaternion.Euler(currentX, currentY, 0);
            // position slight based off x rot
        }
        catch (System.TimeoutException) { }
    }
}
