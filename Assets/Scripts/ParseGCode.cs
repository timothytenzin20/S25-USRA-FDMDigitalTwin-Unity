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
        /** FUTURE DEVELOPMENT: MORE COMMANDS **/
    };

    // path to .gcode file
    /** FUTURE DEVELOPMENT: allow user to select file **/
    string path = "Assets/Scripts/Resources/sampleSharkFile.gcode";
    //string path = "Assets/Scripts/Resources/sampleSharkFile.gcode";
    // initialize queue of processed commands
    Queue<string> q = new Queue<string>();

    protected StreamReader reader = null;
    protected string text = " "; // assigned to allow first line to be read below


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

    static void HandleG1(string[] parts)
    {
        Debug.Log("Handling G1 command");
        foreach (var part in parts)
        {
            Debug.Log(part);
        }
        Vector3 move = (new Vector3(parseCommand(parts[1]), 0, parseCommand(parts[2])));
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
        Debug.Log($"Parsing command: {command}");
        float number = float.Parse(command.Substring(1));
        return number;
    }
}



//using (reader)
//{


//    while ((trimmed = line.Trim()) != null)
//    {
        // Skip empty or whitespace-only lines
        //Debug.Log("Trimming line");
        //if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("("))
        //{
        //    continue;
        //}
//    string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//    string command = parts[0].ToUpper();

//    Debug.Log("Handling command");
//    if (gcodeHandlers.TryGetValue(command, out var handler))
//    {
//        handler(parts);
//    }
//    else
//    {
//        Debug.Log($"Unknown command: {command}");
//    }
//    }
//}