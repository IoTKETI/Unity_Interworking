using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

// MobiusInitializer is a MonoBehaviour class that sets up the necessary structures in the Mobius IoT platform
public class R_MobiusInitializer : MonoBehaviour
{
    // Base URL for the Mobius platform
    private string baseURL = "http://127.0.0.1:7579/Mobius";
    private string AEName = "/zigbee_smarthome";

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(InitializeMobius());
    }

    // This coroutine initializes Mobius by creating an Application Entity (AE),
    // a container, and a subscription, in that order.
    private IEnumerator InitializeMobius()
    {
        yield return StartCoroutine(CreateSubscription_Bulb_binarySwitch());
        yield return StartCoroutine(CreateSubscription_Thermometer_temperature());
    }

    private IEnumerator CreateSubscription_Bulb_binarySwitch()
    {
        // net(Notification Event Type)1 => Update of Resource
        string subscriptionJsonData = @"
        {
            ""m2m:sub"": {
                ""rn"": ""subRealBulbDevice"",
                ""enc"": {""net"": [1]},
                ""nu"": [""mqtt://127.0.0.1:1883/Real/Bulb/state?ct=json""]
            }
        }";
            string subscriptionURL = baseURL + AEName + "/Bulb/binarySwitch";
            yield return SendMobiusRequest(subscriptionURL, subscriptionJsonData, "[MQTT]Real Bulb Device Subscription Creation", "application/vnd.onem2m-res+json;ty=23");
    }

    private IEnumerator CreateSubscription_Thermometer_temperature()
    {
        // net(Notification Event Type)1 => Update of Resource
        string subscriptionJsonData = @"
        {
            ""m2m:sub"": {
                ""rn"": ""subRealThermometerDevice"",
                ""enc"": {""net"": [1]},
                ""nu"": [""mqtt://127.0.0.1:1883/Real/Thermometer/value?ct=json""]
            }
        }";
        string subscriptionURL = baseURL + AEName + "/Thermometer/temperature";
        yield return SendMobiusRequest(subscriptionURL, subscriptionJsonData, "[MQTT]Thermometer Device Subscription Creation", "application/vnd.onem2m-res+json;ty=23");
    }

    // General method for sending requests to Mobius
    private IEnumerator SendMobiusRequest(string url, string jsonData, string logPrefix, string resourceType)
    {
        // Initialize a new UnityWebRequest with the given url and POST method
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer(); // Set up a download handler that will handle the response data

            SetRequestHeaders(request, resourceType); // Set custom headers for the request

            yield return request.SendWebRequest(); // Send the request and wait until it is done

            ProcessResponse(request, logPrefix); // Process the response
        }
    }

    // Helper method to set custom headers required by Mobius for the request
    private void SetRequestHeaders(UnityWebRequest request, string resourceType)
    {
        request.SetRequestHeader("X-M2M-RI", "12345");
        request.SetRequestHeader("X-M2M-Origin", "S");
        request.SetRequestHeader("Content-Type", resourceType);
    }

    // Helper method to process the response from Mobius after a request
    private void ProcessResponse(UnityWebRequest request, string logPrefix)
    {
        // Check if the request did not complete successfully
        if (request.result != UnityWebRequest.Result.Success)
        {
            // If the response code is 409, it indicates that the resource already exists
            if (request.responseCode == 409)
            {
                Debug.Log($"{logPrefix} Error: resource already exists");
            }
            else
            {
                Debug.LogError($"{logPrefix} Error: {request.error}");
            }
        }
        else
        {
            // Log the successful response from the server
            Debug.Log($"{logPrefix} Response: {request.downloadHandler.text}");
        }
    }
}
