using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using System;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System.Collections.Concurrent;

public class RealDeviceMQTTManager : MonoBehaviour
{
    private MqttClient client;
    private float connectionCheckInterval = 10.0f;
    [SerializeField] private string brokerAddress = "127.0.0.1";
    [SerializeField] private string bulbTopic = "/Real/Bulb/state";
    [SerializeField] private string tempTopic = "/Real/Thermometer/value";
    [SerializeField] private int reconnectDelay = 5;

    private Material bulbMaterial;
    public Renderer targetBulb;
    public Light bulbLight;
    public TextMeshPro temperatureText;

    private bool? colorChangeRequest = null;
    private ConcurrentQueue<Tuple<string, string>> messageQueue = new ConcurrentQueue<Tuple<string, string>>();
    private int retryAttempt = 0;

    void Awake()
    {
        InitializeMqttClient();
    }

    void Start()
    {
        if (targetBulb == null || bulbLight == null || temperatureText == null)
        {
            Debug.LogError("[MQTT]inspector를 채워주세요");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }

        InvokeRepeating("CheckConnectionStatus", connectionCheckInterval, connectionCheckInterval);
        bulbMaterial = targetBulb.material;
        temperatureText.text = "N/A";
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
                client.Subscribe(new string[] { bulbTopic, tempTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
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
        messageQueue.Enqueue(new Tuple<string, string>(message, e.Topic));
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out Tuple<string, string> messageTuple))
        {
            ProcessReceivedMessage(messageTuple.Item1, messageTuple.Item2);
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

    private void ProcessReceivedMessage(string message, string topic)
    {
        try
        {
            JObject data = JObject.Parse(message);

            if (topic == bulbTopic)
            {
                JToken powerSeToken = data.SelectToken("pc.m2m:sgn.nev.rep['hd:binSh'].powerSe");

                if (powerSeToken != null)
                {
                    Debug.Log("[MQTT]powerSe value: " + powerSeToken.ToString().ToLower());

                    bool powerState = powerSeToken.ToObject<bool>();
                    colorChangeRequest = powerState; // Set color change request for bulb
                }

            }
            else if (topic == tempTopic)
            {
                // Extract the curT0 value from the thermometer message
                JToken curT0Token = data.SelectToken("pc.m2m:sgn.nev.rep['hd:tempe'].curT0");

                if (curT0Token != null)
                {
                    float temperature = curT0Token.ToObject<int>() / 100.0f;
                    Debug.Log("[MQTT]tempe value: " + temperature.ToString() + "°C");
                    UpdateTemperatureText(temperature);
                }
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
            bulbMaterial.SetColor("_EmissionColor", new Color(1, 225f / 255f, 74f / 255f));
            bulbLight.intensity = 2.0f;
            bulbLight.enabled = true;
        }
        else
        {
            bulbMaterial.SetColor("_EmissionColor", Color.black);
            bulbLight.intensity = 0.0f;
            bulbLight.enabled = false;
        }
    }

    private void UpdateTemperatureText(float temperature)
    {
        if (temperatureText != null)
        {
            temperatureText.text = temperature.ToString("F1") + "°C";
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