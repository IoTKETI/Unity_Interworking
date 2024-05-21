using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

// MobiusInitializer is a MonoBehaviour class that sets up the necessary structures in the Mobius IoT platform
public class V_MobiusInitializer : MonoBehaviour
{
    // Base URL for the Mobius platform
    private string baseURL = "http://127.0.0.1:7579/Mobius";

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(InitializeMobius());
    }

    // This coroutine initializes Mobius by creating an Application Entity (AE),
    // a container, and a subscription, in that order.
    private IEnumerator InitializeMobius()
    {
        yield return StartCoroutine(CreateAE());
        yield return StartCoroutine(CreateContainer_Bulb());
        yield return StartCoroutine(CreateContainer_Bulb_binarySwitch());
        yield return StartCoroutine(CreateSubscription());
    }

    // Coroutine to create an Application Entity (AE) on the Mobius platform
    private IEnumerator CreateAE()
    {
        string aeJsonData = @"
        {
            ""m2m:ae"": {
                ""rn"": ""Unity"",
                ""api"": ""SUnity"",
                ""rr"": true
            }
        }";
        yield return SendMobiusRequest(baseURL, aeJsonData, "AE Creation", "application/vnd.onem2m-res+json;ty=2");
    }

    // Coroutine to create a container on the Mobius platform under the AE
    private IEnumerator CreateContainer_Bulb()
    {
        string containerJsonData = @"
        {
            ""m2m:cnt"": {
                ""rn"": ""Bulb"",
                ""lbl"": [""Bulb""],
                ""mbs"": 16384
            }
        }";
        string containerURL = baseURL + "/Unity";
        yield return SendMobiusRequest(containerURL, containerJsonData, "Container Creation", "application/vnd.onem2m-res+json;ty=3");
    }

    // Coroutine to create a container on the Mobius platform under the AE
    private IEnumerator CreateContainer_Bulb_binarySwitch()
    {
        string containerJsonData = @"
        {
            ""m2m:cnt"": {
                ""rn"": ""binarySwitch"",
                ""lbl"": [""binarySwitch""],
                ""mbs"": 16384
            }
        }";
        string containerURL = baseURL + "/Unity/Bulb";
        yield return SendMobiusRequest(containerURL, containerJsonData, "Container Creation", "application/vnd.onem2m-res+json;ty=3");
    }

    private IEnumerator CreateSubscription()
    {
        // net(Notification Event Type)3 => Create of Resource
        string subscriptionJsonData = @"
        {
            ""m2m:sub"": {
                ""rn"": ""subVirtualBulbDevice"",
                ""enc"": {""net"": [3]},
                ""nu"": [""mqtt://127.0.0.1:1883/VirtualBulbDevice/binarySwitch?ct=json""]
            }
        }";
            string subscriptionURL = baseURL + "/Unity/Bulb/binarySwitch";
            yield return SendMobiusRequest(subscriptionURL, subscriptionJsonData, 
                "Subscription Creation", "application/vnd.onem2m-res+json;ty=23");
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
