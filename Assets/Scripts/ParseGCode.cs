using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;

// 1 unity unit = 2 cm in real life
// home coordinates in Unity world space
// beam: Vector3(1.81139994,7.24909925,2.01740003)
// head: Vector3(-4.69999981,7.3499999,2.91009998)
// bed: Vector3(0,3.70597005,9.07999992)
public class ParseGCode : MonoBehaviour
{
    public Rigidbody[] rb;

    Rigidbody head;
    Rigidbody bed;
    Rigidbody beam;
    Rigidbody frame;
    Rigidbody origin;

    static Dictionary<string, Action<string[]>> gcodeHandlers = new Dictionary<string, Action<string[]>>
    {
        { "G0", HandleG1 },
        { "G1", HandleG1 },
        { "G2", HandleG2 },
        { "G90", HandleG90 },
        { "G91", HandleG91 },
        { "G92", HandleG92 },
        { "G4", HandleG4 },
        { "G28", HandleG28 }
        /** FUTURE DEVELOPMENT: MORE COMMANDS **/
    };

    static Dictionary<string, Func<float, Vector3>> commandAxis = new Dictionary<string, Func<float, Vector3>>
    {
        { "X", HandleX },
        { "Z", HandleY }, // gcode uses Z for Unity Y axis
        { "Y", HandleZ }  // gcode uses Y for Unity Z axis
    };

    // path to .gcode files for testing
    /** FUTURE DEVELOPMENT: allow user to select file **/
    //string path = "Assets/Scripts/Resources/sampleSharkFile.gcode";
    //string path = "Assets/Scripts/Resources/smallShark.gcode";
    //string path = "Assets/Scripts/Resources/heart.gcode";
    //string path = "Assets/Scripts/Resources/detailedHeart.gcode";
    //string path = "Assets/Scripts/Resources/isolated.gcode";
    //string path = "Assets/Scripts/Resources/Cube_Test.gcode";
    //string path = "Assets/Scripts/Resources/sample.txt";
    string path = "Assets/Scripts/Resources/reducedCubeTest.gcode";

    private Vector3 targetPosition;
    private float arriveThreshold = 0.01f;
    public List<MovementCommand> activeCommands = new List<MovementCommand>();

    protected StreamReader reader = null;
    protected string text; // allow first line to be read below

    private float moveSpeed = 2f;

    public static ParseGCode instance; // needed for static access
    public int syncIterate = 0; // track iteration of synced commands
    public bool isSynced = false; // track if the current command is synced
    public bool printingStatus = false;

    public struct MovementCommand
    {
        public int rbIndex;
        public Vector3 vector;
        public float speed;
        public int syncId;
        public bool printing;

        public MovementCommand(int rbIndex, Vector3 vector, float speed, int syncId, bool printing)
        {
            this.rbIndex = rbIndex;
            this.vector = vector;
            this.speed = speed;
            this.syncId = syncId;
            this.printing = printing; 
        }
    }

    Queue<MovementCommand> commandQueue = new Queue<MovementCommand>();

    private static bool isAbsolutePositioning = true;
    public GameObject filamentPrefab; 
    private Vector3 filamentShift = new Vector3(-4.6926f, 7.3616f, 2.9300f);

    void Awake()
    {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (rb == null || rb.Length < 5)
        {
            Debug.LogError("Rigidbody array not assigned or wrong size");
            Debug.Log("Rb assignments: 0 = head (x-axis), 1 = bed (z-axis), 2 = moving beam (y-axis), 3 = printer frame, 4 = origin");
            return;
        }

        head = rb[0];
        bed = rb[1];
        beam = rb[2];
        frame = rb[3];
        origin = rb[4];

        // 0 = head (x-axis), 1 = bed (z-axis), 2 = beam (y-axis), 3 = frame, 4 = origin
        head.useGravity = false;
        head.isKinematic = true;
        bed.useGravity = false;
        bed.isKinematic = true;
        beam.useGravity = false;
        beam.isKinematic = true;
        frame.useGravity = false;
        frame.isKinematic = true;
        origin.useGravity = false;
        origin.isKinematic = false;

        if (File.Exists(path))
        {
            Debug.Log("Reading file");
            //Debug.Log("File exists");
            reader = new StreamReader(path);
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
            Debug.Log("End of file reached.");
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

    // need to run multiple commands at once
    void FixedUpdate()
    {
        if (activeCommands.Count == 0 && commandQueue.Count > 0)
        {
            activeCommands = DequeueNextCommandGroup(commandQueue);
        }

        if (activeCommands.Count > 0)
        {
            List<MovementCommand> completed = new List<MovementCommand>();

            foreach (var cmd in activeCommands)
            {
                int rbIndex = cmd.rbIndex;
                Vector3 target = cmd.vector;
                float speed = cmd.speed;
                Rigidbody body = rb[rbIndex];
                Debug.Log(body.name);

                // move towards the target position
                if (body.name == "beam")
                {
                    // Special handling for the head to follow the beam
                    Vector3 headPosition = rb[0].position;

                    if (headPosition.y < (body.position.y + 0.11f))
                    {
                        headPosition.y = body.position.y;
                    }

                    Vector3 target2 = new Vector3(headPosition.x, body.position.y, headPosition.z);
                    rb[0].MovePosition(Vector3.MoveTowards(rb[0].position, target2, speed * Time.fixedDeltaTime));
                }
                else if (body.name == "head")
                {
                    // Special handling for the beam to follow the head
                    Vector3 beamPosition = rb[2].position;
                    Vector3 target2 = new Vector3(beamPosition.x, rb[0].position.y, beamPosition.z);
                    rb[2].MovePosition(Vector3.MoveTowards(rb[2].position, target2, speed * Time.fixedDeltaTime));
                }

                Vector3 newPos = Vector3.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
                body.MovePosition(newPos);
                Debug.Log($"Moving {body.position} to {target}");

                if (Vector3.Distance(newPos, target) < arriveThreshold)
                {
                    body.MovePosition(target); 
                    completed.Add(cmd);
                }
            }

            // clear completed commands
            foreach (var cmd in completed)
            {
                activeCommands.Remove(cmd);
            }
        }
    }

    static void HandleG1(string[] parts)
    {
        Debug.Log("Handling G1 command");
        if (parts[0] == "G0")
        {
            Debug.Log("Handling G0 command");
            SetFeedRate(300);
        }
        foreach (var part in parts)
        {
            Debug.Log(part);
        }
        for (int i = parts.Length - 1; i >= 1; i--)
        {
            string commandAxis = getCommandLetter(parts[i]);
            if (commandAxis == "X")
            {
                //Debug.Log("X axis: Unity");
                instance.targetPosition = HandleX(parseCommand(parts[i]));
                //Debug.Log($"Move: {instance.targetPosition}, Speed: {instance.moveSpeed * Time.fixedDeltaTime}");
                instance.commandQueue.Enqueue(new MovementCommand(0, instance.targetPosition, instance.moveSpeed, instance.syncIterate, instance.printingStatus));
                instance.isSynced = true; // mark as synced command
            }
            else if (commandAxis == "Y")
            {
                //Debug.Log("Z axis: Unity");
                instance.targetPosition = HandleZ(parseCommand(parts[i]));
                //Debug.Log($"Move: {instance.targetPosition}, Speed: {instance.moveSpeed * Time.fixedDeltaTime}");
                instance.commandQueue.Enqueue(new MovementCommand(1, instance.targetPosition, instance.moveSpeed, instance.syncIterate, instance.printingStatus));
                instance.isSynced = true; // mark as synced command
            }
            else if (commandAxis == "Z")
            {
                //Debug.Log("Y axis: Unity");
                instance.targetPosition = HandleY(parseCommand(parts[i]));
                //Debug.Log($"Move: {instance.targetPosition}, Speed: {instance.moveSpeed * Time.fixedDeltaTime}");
                instance.commandQueue.Enqueue(new MovementCommand(2, instance.targetPosition, instance.moveSpeed, instance.syncIterate, instance.printingStatus));
                instance.isSynced = true; // mark as synced command
            }
            // need to calculate first to be stored to the action (i flipped the order of command parts addressed)
            else if (commandAxis == "F")
            {
                Debug.Log("Set Feed Rate");
                float feedRate = parseCommand(parts[i]);
                SetFeedRate(feedRate);
            }
            else if (commandAxis == "E")
            {
                float value = parseCommand(parts[i]);
                if (value <= 0)
                {
                    Debug.Log("Not Printing");
                    instance.printingStatus = false;
                }
                else
                {
                    Debug.Log("Printing");
                    instance.printingStatus = true;
                }
            }
            else
            {
                Debug.Log($"Non-axis command: {commandAxis}");
            }
        }

        if (instance.isSynced)   // mark this command group as synced
        {
            instance.syncIterate++;
        }
        instance.isSynced = false; // reset for next command group
        
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

    static void HandleG90(string[] parts)
    {
        Debug.Log("Handling G90 command: ABSOLUTE");
        isAbsolutePositioning = true;
    }

    static void HandleG91(string[] parts)
    {
        Debug.Log("Handling G91 command: RELATIVE");
        isAbsolutePositioning = false;
    }

    static void HandleG92(string[] parts)
    {
        Debug.Log("Handling G92 command: SET POSITION");
        bool currentState = isAbsolutePositioning;
        isAbsolutePositioning = true; // force absolute positioning for G92
        for (int i = parts.Length - 1; i >= 1; i--)
        {
            string commandAxis = getCommandLetter(parts[i]);
            if (commandAxis == "X")
            {
                instance.head.position = HandleX(parseCommand(parts[i]));
                instance.commandQueue.Enqueue(new MovementCommand(0, instance.head.position, instance.moveSpeed, instance.syncIterate, instance.printingStatus));
                instance.isSynced = true; // mark as synced command
            }
            else if (commandAxis == "Y")
            {
                instance.bed.position = HandleZ(parseCommand(parts[i]));
                instance.commandQueue.Enqueue(new MovementCommand(1, instance.head.position, instance.moveSpeed, instance.syncIterate, instance.printingStatus));
                instance.isSynced = true;
            }
            else if (commandAxis == "Z")
            {
                instance.beam.position = HandleY(parseCommand(parts[i]));
                instance.commandQueue.Enqueue(new MovementCommand(2, instance.head.position, instance.moveSpeed, instance.syncIterate, instance.printingStatus));
                instance.isSynced = true;
            }
            else if (commandAxis == "E")
            {
                if (parseCommand(parts[i]) <= 0)
                {
                    Debug.Log("Not Printing");
                    instance.printingStatus = false;
                }
                else
                {
                    Debug.Log("Printing");
                    instance.printingStatus = true;
                }
            }
            else
            {
                Debug.Log($"Unknown axis in G92: {commandAxis}");
            }
        }
        isAbsolutePositioning = currentState; // restore previous state
        if (instance.isSynced)   // mark this command group as synced
        {
            instance.syncIterate++;
        }
        instance.isSynced = false; // reset for next command group

    }

    static void HandleG4(string[] parts)
    {
        Debug.Log("Handling G4 command");
        // dont need to actively handle G4, since raspberry pi sends next command after the delay
    }

    static void HandleG28(string[] parts)
    {
        Debug.Log("Handling G28 command: HOMING");
        instance.commandQueue.Enqueue(new MovementCommand(0, new Vector3(-4.69999981f, 7.3499999f, 2.91009998f), instance.moveSpeed, instance.syncIterate, instance.printingStatus));
        instance.commandQueue.Enqueue(new MovementCommand(1, new Vector3(0f, 3.70597005f, 9.07999992f), instance.moveSpeed, instance.syncIterate, instance.printingStatus));
        instance.commandQueue.Enqueue(new MovementCommand(2, new Vector3(1.81139994f, 7.24909925f, 2.01740003f), instance.moveSpeed, instance.syncIterate, instance.printingStatus));
        instance.syncIterate++;
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
        // convert for Unity 2cm per unit
        float valueUnity = value / 20f;
        float targetX = isAbsolutePositioning ? instance.origin.position.x + valueUnity : instance.head.position.x + valueUnity;
        Vector3 response = new Vector3(targetX, instance.head.position.y, instance.head.position.z);
        Debug.Log($"HandleX: {response}");
        return response;
    }

    static Vector3 HandleY(float value) // value is .gcode Z-axis (vertical)
    {
        // convert for Unity 2cm per unit
        float valueUnity = value / 20f;
        float offsetY = 2f; // offset for the printer head clipping the bed
        float targetY = isAbsolutePositioning ? instance.origin.position.y + valueUnity + offsetY : instance.beam.position.y + valueUnity;
        Vector3 response = new Vector3(instance.beam.position.x, targetY, instance.beam.position.z);
        Debug.Log($"HandleY: {response}");
        return response;
    }

    static Vector3 HandleZ(float value) // value is .gcode Y-axis
    {
        // convert for Unity 2cm per unit
        float valueUnity = value / 20f;
        float targetZ = isAbsolutePositioning ? instance.origin.position.z + valueUnity : instance.bed.position.z + valueUnity;
        Vector3 response = new Vector3(instance.bed.position.x, instance.bed.position.y, targetZ);
        Debug.Log($"HandleZ: {response}");
        return response;
    }

    static void SetFeedRate(float value)
    {
        // mm/min to Unity units (2cm) per second
        float mmPerSec = value / 60f;
        float cmPerSec = mmPerSec / 20f;
        instance.moveSpeed = cmPerSec;
        //instance.moveSpeed = 10000f;

        //Debug.Log($"Adjusted Speed: {instance.moveSpeed}");
        return;
    }

    static List<MovementCommand> DequeueNextCommandGroup(Queue<MovementCommand> queue)
    {
        if (queue.Count == 0) return new List<MovementCommand>();

        // Peek the next group ID
        int nextSyncId = queue.Peek().syncId;

        List<MovementCommand> group = new List<MovementCommand>();
        while (queue.Count > 0 && queue.Peek().syncId == nextSyncId)
        {
            group.Add(queue.Dequeue());
        }
        Debug.Log($"DequeueNextCommandGroup: {group.Count} commands with sync ID {nextSyncId}");
        return group;
    }

    public static bool IsCurrentlyPrintingHead()
    {
        if (instance == null) return false;

        foreach (var cmd in instance.activeCommands)
        {
            if (cmd.printing)
            {
                Debug.Log("PRINTING SHOULD DISPLAY");
                return true;
            }
        }
        Debug.Log("PRINTING SHOULD NOT DISPLAY");
        return false;
    }
}
