using UnityEngine;
using System.IO.Ports;

public class SerialDebugger : MonoBehaviour
{
    SerialPort sp = new SerialPort("COM3", 115200); // arduino serial port

    // connect to the arduino serial port
    void Start()
    {
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


    // read gcode from serial port
    void Update()
    {
        if (sp.IsOpen)
        {
            try
            {
                string line = sp.ReadLine();
                Debug.Log("Received Command: " + line);
            }
            catch (System.Exception e) { }
        }
    }

    void OnApplicationQuit()
    {
        if (sp.IsOpen) sp.Close();
    }
}
