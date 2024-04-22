using Newtonsoft.Json.Linq;
using UnityEngine;
using System;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System.Collections.Concurrent;

public class VirtualDeviceMQTTManager : MonoBehaviour
{
    private MqttClient client;
    private float connectionCheckInterval = 10.0f; // Connection check interval in seconds
    [SerializeField] private string brokerAddress = "127.0.0.1";
    [SerializeField] private string topic = "/VirtualBulbDevice/binarySwitch";
    [SerializeField] private int reconnectDelay = 5; // Reconnect delay in seconds

    private Material bulbMaterial;
    public Renderer targetBulb; // Renderer for the target bulb
    public Light bulbLight; // Light component for the bulb

    private bool? colorChangeRequest = null; // A nullable boolean to track color change requests
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>(); // Thread-safe queue for MQTT messages
    private int retryAttempt = 0; // Number of reconnect attempts

    void Awake()
    {
        InitializeMqttClient();
    }

    void Start()
    {
        // Verify that the required components are assigned in the inspector
        if (targetBulb == null || bulbLight == null)
        {
            Debug.LogError("MQTTManager requires a 'targetBulb' and a 'bulbLight'. Please assign them in the inspector.");

            // This code is intended for use in the editor only. It stops the play mode if required components are not assigned.
            // WARNING: Do not use this approach in production code. It is meant for editor use only.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif

            return; // Early return to prevent further execution
        }

        InvokeRepeating("CheckConnectionStatus", connectionCheckInterval, connectionCheckInterval);
        bulbMaterial = targetBulb.material;
    }

    private void InitializeMqttClient()
    {
        if (client != null && client.IsConnected)
            return;

        Debug.Log("[MQTT]Starting MQTT Client");
        client = new MqttClient(brokerAddress);
        client.MqttMsgPublishReceived += OnMqttMessageReceived;

        string clientId = Guid.NewGuid().ToString();
        try
        {
            client.Connect(clientId);
            if (client.IsConnected)
            {
                Debug.Log("[MQTT]Client successfully connected");
                client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
                retryAttempt = 0;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[MQTT]MQTT Connect failed: " + ex.Message);
            ScheduleReconnect();
        }
    }

    private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Message);
        Debug.Log("[MQTT]Received: " + message);
        messageQueue.Enqueue(message); // Enqueue the message for processing
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessReceivedMessage(message);
        }

        if (colorChangeRequest.HasValue)
        {
            UpdateBulbColor(colorChangeRequest.Value);
            colorChangeRequest = null;
        }
    }

    private void ScheduleReconnect()
    {
        if (retryAttempt < int.MaxValue)
            retryAttempt++;

        int delay = Mathf.Min(reconnectDelay * (int)Mathf.Pow(2, retryAttempt), int.MaxValue);
        Debug.LogWarning($"[MQTT]Attempting to reconnect in {delay} seconds.");
        Invoke(nameof(InitializeMqttClient), delay);
    }

    private void CheckConnectionStatus()
    {
        if (!client.IsConnected)
        {
            Debug.LogWarning("[MQTT]MQTT client disconnected. Scheduling reconnect...");
            ScheduleReconnect();
        }
    }

    private void ProcessReceivedMessage(string message)
    {
        try
        {
            JObject data = JObject.Parse(message);
            JToken conToken = data.SelectToken("pc.m2m:sgn.nev.rep.m2m:cin.con");

            string content = conToken.ToString();

            Debug.Log($"[MQTT]value: {content}");

            if (content == "true")
            {
                colorChangeRequest = true;
            }
            else if (content == "false")
            {
                colorChangeRequest = false;
            }

        }
        catch (Exception ex)
        {
            Debug.LogError("[MQTT]Exception in ProcessReceivedMessage: " + ex.Message);
        }
    }

    private void UpdateBulbColor(bool turnOn)
    {
        if (turnOn)
        {
            bulbMaterial.SetColor("_EmissionColor", new Color(1, 225f / 255f, 74f / 255f)); // Set bulb emission color to indicate 'on' state
            bulbLight.intensity = 2.0f; // Increase light intensity to simulate a turned on bulb
            bulbLight.enabled = true;
        }
        else
        {
            bulbMaterial.SetColor("_EmissionColor", Color.black); // Disable bulb emission to indicate 'off' state
            bulbLight.intensity = 0.0f; // Set light intensity to zero to simulate a turned off bulb
            bulbLight.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Disconnect();
        }
    }
}