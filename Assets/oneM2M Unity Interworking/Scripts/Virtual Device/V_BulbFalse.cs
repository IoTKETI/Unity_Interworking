using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class V_BulbFalse : MonoBehaviour
{
    private string baseURL = "http://127.0.0.1:7579/Mobius/Unity/Bulb/binarySwitch";
    private float timeSinceLastPost = 0.0f;
    private float postCooldown = 1.5f;

    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider
    private void OnCollisionEnter(Collision collision)
    {
        // Prevent frequent POST requests by checking the time since the last POST
        if (Time.time - timeSinceLastPost > postCooldown)
        {
            timeSinceLastPost = Time.time;
            StartCoroutine(SendPostToMobius("false"));
        }
    }

    // Sends a POST request to update the state of the binarySwitch to "false"
    private IEnumerator SendPostToMobius(string state)
    {
        string jsonData = $"{{\"m2m:cin\": {{\"con\": \"{state}\"}}}}";

        using (UnityWebRequest request = new UnityWebRequest(baseURL, "POST"))
        {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            SetRequestHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[VirtualBulbFalse]SendPostToMobius - Error: {request.error}");
            }
            else
            {
                Debug.Log($"[VirtualBulbFalse]SendPostToMobius - Response: {request.downloadHandler.text}");
            }
        }
    }

    // Sets custom headers for the Mobius platform request
    private void SetRequestHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("X-M2M-RI", "12345");
        request.SetRequestHeader("X-M2M-Origin", "S");
        request.SetRequestHeader("Content-Type", "application/vnd.onem2m-res+json; ty=4");
    }
}