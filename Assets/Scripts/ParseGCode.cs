using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;

public class ParseGCode : MonoBehaviour
{
    public Rigidbody[] rb;
    static Dictionary<string, Action<string[]>> gcodeHandlers = new Dictionary<string, Action<string[]>>
    {
        { "G1", HandleG1 },
        { "G2", HandleG2 },
        { "M3", HandleM3 }
        /** FUTURE DEVELOPMENT: MORE COMMANDS **/
    };

    static Dictionary<string, Func<float, Vector3>> commandAxis = new Dictionary<string, Func<float, Vector3>>
    {
        { "X", HandleX },
        { "Z", HandleY }, // gcode uses Z for Unity Y axis
        { "Y", HandleZ }  // gcode uses Y for Unity Z axis
    };

    // path to .gcode file
    /** FUTURE DEVELOPMENT: allow user to select file **/
    // string path = "Assets/Scripts/Resources/sampleSharkFile.gcode";
    // string path = "Assets/Scripts/Resources/sample.txt";
    string path = "Assets/Scripts/Resources/isolated.gcode";

    // initialize queue of processed commands
    Queue<Vector3> q = new Queue<Vector3>();

    protected StreamReader reader = null;
    protected string text = " "; // assigned to allow first line to be read below

    public float moveSpeed = 300f;

    public static ParseGCode instance; // needed for static access
    void Awake()
    {
        instance = this;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (rb == null || rb.Length < 4)
        {
            Debug.LogError("Rigidbody array not assigned or wrong size");
            return;
        }

        // 0 = head (x-axis), 1 = bed (z-axis), 2 = beam (y-axis), 3 = frame
        rb[0].useGravity = false;
        rb[0].isKinematic = true;
        rb[1].useGravity = false;
        rb[1].isKinematic = true;
        rb[2].useGravity = false;
        rb[2].isKinematic = true;
        rb[3].useGravity = false;
        rb[3].isKinematic = true;

        if (File.Exists(path))
        {
            Debug.Log("File exists");
            reader = new StreamReader(path);
            Debug.Log("Reading file");
        }
        else
        {
            Debug.Log("G-code file not found.");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (reader == null) {
            return;
        }

        text = reader.ReadLine();

        if (text == null)
        {
            Debug.Log("End of file reached or no more lines to read.");
            reader.Close();
            reader = null;
            return;
        }

        string trimmed = text.Trim();
        if (trimmed != null)
        {
            //Debug.Log("Trimming line");
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("("))
            {
                Debug.Log("Skipping empty line or comment");
            }
            else
            {
                int index = trimmed.IndexOf(";");
                if (index >= 0)
                {
                    trimmed = trimmed.Substring(0, index);
                }
                string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command = parts[0].ToUpper();
                //Debug.Log(command);
                //Debug.Log("Handling command");
                if (gcodeHandlers.TryGetValue(command, out var handler))
                {
                    handler(parts);
                }
                else
                {
                    Debug.Log($"Unknown command: {command}");
                }
            }
        }
    }

    void FixedUpdate()
    {

    }

    static void HandleG1(string[] parts)
    {
        Debug.Log("Handling G1 command");
        //foreach (var part in parts)
        //{
        //    Debug.Log(part);
        //}
        for (var i = 1; i < parts.Length; i++)
        {
            string commandAxis = getCommandLetter(parts[i]);
            if (commandAxis == "X")
            {
                Debug.Log("X axis: Unity");
                Vector3 move = HandleX(parseCommand(parts[i]));
                instance.rb[0].MovePosition(instance.rb[0].position + move * Time.fixedDeltaTime);
            }
            else if (commandAxis == "Y")
            {
                Debug.Log("Z axis: Unity");
                Vector3 move = HandleZ(parseCommand(parts[i]));
                instance.rb[1].MovePosition(instance.rb[1].position + move * Time.fixedDeltaTime);
            }
            else if (commandAxis == "Z")
            {
                Debug.Log("Y axis: Unity");
                Vector3 targetPosition = HandleY(parseCommand(parts[i]));
                var step = instance.moveSpeed * Time.fixedDeltaTime; // calculate distance to move
                Debug.Log($"Move: {targetPosition}, Speed: {step}");
                Vector3 newPosition = Vector3.MoveTowards(instance.rb[2].position, targetPosition, step);
                Debug.Log($"Go to: {newPosition}");
                instance.rb[2].MovePosition(newPosition);
            }

            else if (commandAxis == "F")
            {
                float feedRate = parseCommand(parts[i]);
                SetFeedRate(feedRate);
            }
            else if (commandAxis == "E")
            {
                // Handle extruder movement
            }
            else
            {
                Debug.Log($"Non-axis command: {commandAxis}");
            }
        }
        return;
    }

    static void HandleG2(string[] parts)
    {
        Debug.Log("Handling G2 command");
        foreach (var part in parts)
        {
            Debug.Log(part);
        }
    }

    static void HandleM3(string[] parts)
    {
        Debug.Log("Handling M3 command");
        foreach (var part in parts)
        {
            Debug.Log(part);
        }
    }

    static float parseCommand(string command)
    {
        //Debug.Log($"Parsing command: {command}");
        float number = float.Parse(command.Substring(1));
        return number;
    }

    static string getCommandLetter(string command)
    {
        //Debug.Log($"Parsing command: {command}");
        string character = command.Substring(0,1);
        //Debug.Log(character);
        return character;
    }

    static Vector3 HandleX(float value)
    {
        Vector3 targetPosition = (new Vector3(value,instance.rb[0].position.y, instance.rb[0].position.z));
        Debug.Log(targetPosition);
        return targetPosition;
    }

    static Vector3 HandleY(float value)
    {
        Vector3 targetPosition = (new Vector3(instance.rb[2].position.x, value, instance.rb[2].position.z));
        Debug.Log(targetPosition);
        return targetPosition;
    }

    static Vector3 HandleZ(float value)
    {
        Vector3 targetPosition = (new Vector3(instance.rb[1].position.x, instance.rb[1].position.y, value));
        Debug.Log(targetPosition);
        return targetPosition;
    }

    static void SetFeedRate(float value)
    {
        instance.moveSpeed = value/(60 * 1000);
        Debug.Log($"Adjusted Speed: {instance.moveSpeed}");
        return;
    }

}


