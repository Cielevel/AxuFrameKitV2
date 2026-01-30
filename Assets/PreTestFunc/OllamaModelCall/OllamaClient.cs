using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class OllamaClient : MonoBehaviour
{
    [SerializeField] private string _userPrompt;

    private string url = "http://localhost:11434/api/generate";

    // 定义请求数据结构
    [System.Serializable]
    public class OllamaRequest
    {
        public string model; // 这里填 "gemma3" 或 "gemma3:4b"
        public string prompt;
        public bool stream;   // 建议设为 false，方便初次调试
    }

    [System.Serializable]
    public class OllamaResponse
    {
        public string model;
        public string created_at;
        public string response; // 这就是你想要的纯文本信息
        public bool done;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CallGemma(_userPrompt);
        }
    }

    public void CallGemma(string userPrompt)
    {
        StartCoroutine(PostRequest(userPrompt));
    }

    IEnumerator PostRequest(string userPrompt)
    {
        var data = new OllamaRequest
        {
            model = "gemma3",
            prompt = userPrompt,
            stream = false
        };
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            // 在你的 Coroutine 中这样解析：
            if (www.result == UnityWebRequest.Result.Success)
            {
                string rawJson = www.downloadHandler.text;

                // 将 JSON 转换为对象
                OllamaResponse result = JsonUtility.FromJson<OllamaResponse>(rawJson);

                // 只提取并调试核心回复信息
                string cleanMessage = result.response;
                Debug.Log($"[Gemma 3]: {cleanMessage}");

                // 接下来你可以把 cleanMessage 直接显示在 Unity Label 或 InputField 
            }
        }
    }
}