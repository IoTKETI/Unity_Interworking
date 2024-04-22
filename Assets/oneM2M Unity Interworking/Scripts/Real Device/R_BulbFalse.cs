using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class RealBulbFalse : MonoBehaviour
{
    private string baseURL = "http://127.0.0.1:7579/Mobius/zigbee_smarthome/Bulb/binarySwitch";
    private float timeSinceLastPut = 0.0f;
    private float putCooldown = 1.5f;

    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider
    private void OnCollisionEnter(Collision collision)
    {
        // Prevent frequent PUT requests by checking the time since the last PUT
        if (Time.time - timeSinceLastPut > putCooldown)
        {
            timeSinceLastPut = Time.time;
            StartCoroutine(SendPutToMobius("false"));
        }
    }

    // Sends a PUT request to update the state of the binarySwitch to "false"
    private IEnumerator SendPutToMobius(string state)
    {
        // Here we're directly using the JSON structure you've provided as the body of the PUT request.
        string jsonData = $"{{\"hd:binSh\": {{\"powerSe\": \"{state}\"}}}}";

        using (UnityWebRequest request = UnityWebRequest.Put(baseURL, jsonData))
        {
            // Convert the JSON string to a byte array.
            byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            // Set the headers.
            SetRequestHeaders(request);

            // Send the request and wait for a response.
            yield return request.SendWebRequest();

            // Check for errors.
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[RealBulbFalse]SendPutToMobius - Error: {request.error}");
            }
            else
            {
                Debug.Log($"[RealBulbFalse]SendPutToMobius - Response: {request.downloadHandler.text}");
            }
        }
    }

    // Sets custom headers for the Mobius platform request
    private void SetRequestHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("X-M2M-RI", "12345");
        request.SetRequestHeader("X-M2M-Origin", "Szigbee_smarthome");
        request.SetRequestHeader("Content-Type", "application/vnd.onem2m-res+json;");
    }
}