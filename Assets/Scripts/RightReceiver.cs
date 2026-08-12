using System.IO.Ports;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class RightReceiver : MonoBehaviour
{
    // Serial information
    SerialPort serial = new("COM12", 115200);

    // To avoid running serial on main thread
    private Thread thread;
    bool running = false;

    // Variables for Unity
    float currentX, currentY = 0f;
    float targetX, targetY = 0f;
    float tolerance = 0.2f;
    float velX, velY = 0f;

    void Start()
    {
        serial.Open();
        thread = new Thread(ReadSerialPort);
        thread.Start();
        running = true;
    }

    void ReadSerialPort()
    {
        while (running)
        {
            try
            {
                string incomingData = serial.ReadLine();
                string[] parsedData = incomingData.Split(',');
                float.TryParse(parsedData[0], out float x);
                float.TryParse(parsedData[1], out float y);
                targetX = x;
                targetY = y;
            }
            catch (System.TimeoutException) { }
            catch { break; }
        }
    }

    void Update()
    {

        if (Mathf.Abs(currentX - targetX) > tolerance)
        {
            currentX = Mathf.SmoothDamp(currentX, targetX, ref velX, 0.08f);
        }
        if (Mathf.Abs(currentY - targetY) > tolerance)
        {
            currentY = Mathf.SmoothDamp(currentY, targetY, ref velY, 0.08f);
        }

        transform.rotation = Quaternion.Euler(-currentY, -currentX, 0);
    }

    private void OnDestroy()
    {
        running = false;

        thread?.Join(200);

        if (serial.IsOpen)
        {
            serial.Close();
        }
    }
}
