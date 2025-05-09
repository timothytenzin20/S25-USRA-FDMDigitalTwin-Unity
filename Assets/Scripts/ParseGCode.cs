using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ParseGCode : MonoBehaviour
{
    public Rigidbody[] rb;

    static Dictionary<string, Action<string[]>> gcodeHandlers = new Dictionary<string, Action<string[]>>
    {
        { "G1", HandleG1 },
        { "G2", HandleG2 },
        { "M3", HandleM3 }

    };
    // path to .gcode file
    /** FUTURE DEVELOPMENT: allow user to select file **/
    string path = "Assets/Scripts/Resources/sample.txt";
    // initialize queue of commands
    Queue<string> q = new Queue<string>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                line = reader.ReadLine();
                string trimmed;

                while ((trimmed = line.Trim()) != null)
                {
                    // Skip empty or whitespace-only lines
                    Debug.Log("Trimming line");
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("("))
                    {
                        continue;
                    }
                    string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string command = parts[0].ToUpper();

                    Debug.Log("Handling command");
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
        else
        {
            Debug.Log("G-code file not found.");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    static void HandleG1(string[] parts)
    {
        Debug.Log("Handling G1 command");
    }

    static void HandleG2(string[] parts)
    {
        Debug.Log("Handling G2 command");
    }

    static void HandleM3(string[] parts)
    {
        Debug.Log("Handling M3 command");
    }
}
