using UnityEngine;

namespace AxuFrameKit.Modules.AITools
{
    [CreateAssetMenu(fileName = "AIConfig", menuName = "AxuFrameKit/AI-Tools/AI-Config")]
    public class AIConfig : ScriptableObject
    {
        [Header("Model Settings")]
        [Tooltip("Enter the exact model name, e.g. glm-4.7-flashx")]
        public string modelName = "glm-4.7-flashx";

        [Header("Connection Settings")]
        public string apiUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

        public string apiKey;

        [Header("Parameters")]
        [Range(0f, 1f)]
        public float temperature = 0.95f;

        public bool stream = false;

        [TextArea(3, 10)]
        public string systemPrompt = "You are a helpful AI assistant.";

        public string AuthToken => "Bearer " + apiKey;

        public string GetModelName()
        {
            return modelName;
        }
    }
}
