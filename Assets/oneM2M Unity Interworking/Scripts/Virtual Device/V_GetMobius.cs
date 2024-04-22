using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class V_GetMobius : MonoBehaviour
{
    private const string URL = "http://127.0.0.1:7579/Mobius/Unity/Bulb/binarySwitch/la";
    private Material cubeMaterial;
    private Material bulbMaterial;

    public Renderer targetBulb;
    public Light bulbLight;

    // Check components and initialize materials on start
    void Start()
    {
        if (!CheckComponent(targetBulb, "targetBulb") ||
            !CheckComponent(bulbLight, "bulbLight") ||
            !CheckComponent(GetComponent<Renderer>(), "Renderer"))
        {
            return;
        }

        cubeMaterial = GetComponent<Renderer>().material;
        bulbMaterial = targetBulb.material;
    }

    // Verifies if a component is assigned, logs error and stops play mode if not
    private bool CheckComponent<T>(T component, string componentName) where T : Component
    {
        if (component == null)
        {
            Debug.LogError($"{componentName} has not been assigned in the inspector.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
            return false;
        }
        return true;
    }

    // Initiates fetching state from Mobius when a collision is detected
    private void OnCollisionEnter(Collision collision)
    {
        StartCoroutine(GetFromMobius());
    }

    // Coroutine to send a GET request to Mobius and handle the response
    private IEnumerator GetFromMobius()
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(URL);
        SetRequestHeaders(webRequest);

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(webRequest.error);
        }
        else
        {
            HandleResponse(webRequest.downloadHandler.text);
        }
    }

    // Sets the necessary HTTP headers for the Mobius request
    private void SetRequestHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("X-M2M-RI", "12345");
        request.SetRequestHeader("X-M2M-Origin", "S");
        request.SetRequestHeader("Accept", "application/json");
    }

    // Processes the response from Mobius and updates the scene accordingly
    private void HandleResponse(string jsonResponse)
    {
        try
        {
            JObject response = JObject.Parse(jsonResponse);
            if (response.TryGetValue("m2m:cin", out JToken cin))
            {
                string con = (string)cin["con"];
                SetState(con == "true");
                Debug.Log($"[retrieval]value: {con}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("JSON parsing failed: " + e.Message);
        }
    }

    // Updates the material colors and light based on the switch state
    private void SetState(bool isOn)
    {
        if (isOn)
        {
            cubeMaterial.color = Color.green;
            bulbMaterial.SetColor("_EmissionColor", new Color(1, 225f / 255f, 74f / 255f));
            bulbLight.intensity = 2.0f;
        }
        else
        {
            cubeMaterial.color = Color.red;
            bulbMaterial.SetColor("_EmissionColor", Color.black);
            bulbLight.intensity = 0.0f;
        }
    }
}