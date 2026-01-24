using UnityEngine;

namespace AxuFrameKit.Modules.AITools
{
    [CreateAssetMenu(fileName = "AIChatConfig", menuName = "AxuFrameKit/AI-Tools/Chat-Config")]
    public class AIChatConfig : ScriptableObject
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
        public string systemPrompt = "你是一个有用的AI助手。";

        public string AuthToken => "Bearer " + apiKey;

        // Helper to keep compatibility if needed, but better to use field directly.
        // I will update usage to use .modelName directly or keep this wrapper.
        // wrapper is safer for refactoring.
        public string GetModelName()
        {
            return modelName;
        }
    }
}
