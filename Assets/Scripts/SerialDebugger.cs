using UnityEngine;
using System.IO.Ports;
using System.Linq;

public class SerialDebugger : MonoBehaviour
{
    SerialPort sp = new SerialPort("COM3", 115200); // arduino serial port
    public static string line;
    public static bool portExists = SerialPort.GetPortNames().Any(x => x == "COM3");
    // connect to the arduino serial port

    void Start()
    {
        if (!portExists)
        {
            Debug.LogError("Serial port COM3 does not exist. Please check your connection.");
            return;
        }
        else
        {
            Debug.Log("Serial port COM3 exists. Attempting to connect...");
            try
                {
                    sp.Open();
                    sp.ReadTimeout = 500;
                    Debug.Log("Serial port opened");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to connect: " + e.Message);
                }
        }
    }


    // read gcode from serial port
    void Update()
    {
        if (sp.IsOpen)
        {
            try
            {
                line = sp.ReadLine();
                Debug.Log("Received Command: " + line);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to recieve command: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (sp.IsOpen) sp.Close();
    }
}
