using UnityEngine;

[ExecuteInEditMode]
public class SubCellularGenerator : MonoBehaviour
{
    [Header("输出设置")]
    public int resolution = 512;
    public FilterMode filterMode = FilterMode.Point; // 像素风推荐 Point
    
    // --- 第一部分：大色块配置 ---
    [System.Serializable]
    public struct MacroLayer
    {
        public int seed;
        [Range(0.001f, 0.05f)] public float frequency; // 频率越低，块越大
        public FastNoiseLite.CellularDistanceFunction distanceFunc;
        public FastNoiseLite.CellularReturnType returnType;
    }

    [Header("第一部分：父级大色块 (Macro)")]
    public MacroLayer macroSettings = new MacroLayer
    {
        seed = 1337,
        frequency = 0.01f,
        distanceFunc = FastNoiseLite.CellularDistanceFunction.EuclideanSq,
        returnType = FastNoiseLite.CellularReturnType.CellValue
    };

    // --- 第二部分：子色块配置 ---
    [System.Serializable]
    public struct MicroLayer
    {
        public int seed;
        [Range(0.01f, 0.2f)] public float frequency; // 频率越高，块越碎
        public float domainWarpStrength; // 坐标偏移强度，设大一点以保证完全错开
    }

    [Header("第二部分：子级细分 (Micro)")]
    public MicroLayer microSettings = new MicroLayer
    {
        seed = 42,
        frequency = 0.1f,
        domainWarpStrength = 5000.0f // 巨大的偏移量，保证边界断裂
    };

    [Header("预览")]
    public Renderer previewRenderer;

    // 缓存 Noise 对象
    private FastNoiseLite _noiseMacro;
    private FastNoiseLite _noiseMicro;

    private void OnEnable()
    {
        Generate();
    }

    [ContextMenu("生成贴图 (Generate)")]
    public void Generate()
    {
        InitNoise();

        Texture2D texture = new Texture2D(resolution, resolution);
        texture.filterMode = filterMode;

        Color[] colors = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // 1. 获取父级大色块的值 (Parent ID)
                // 这个值在大色块内部是恒定的，跨越边界时会突变
                float parentValue = _noiseMacro.GetNoise(x, y);

                // 2. 关键步骤：利用 Parent ID 计算坐标偏移
                // 每个大色块得到一个基于其 ID 的唯一偏移量
                // 这里的 43758.54 是一个伪随机的大数，用来打乱坐标
                float offsetX = parentValue * microSettings.domainWarpStrength + (parentValue * 12.34f);
                float offsetY = parentValue * microSettings.domainWarpStrength * 0.5f + (parentValue * 56.78f);

                // 3. 采样子级噪声，使用偏移后的坐标
                // 加上 x, y 是为了保证在同一个大色块内，纹理是连续变化的
                float childValue = _noiseMicro.GetNoise(x + offsetX, y + offsetY);

                // 4. 映射颜色
                // 为了看清效果，我们直接把 childValue 映射为灰度
                // 也可以混合 parentValue 来给不同区域上底色
                float normalizedColor = (childValue + 1f) * 0.5f; // 归一化到 0-1
                
                // 稍微叠加大色块的颜色，方便肉眼区分归属（可选）
                float finalGray = normalizedColor * 0.8f + (parentValue + 1f) * 0.1f; 

                colors[y * resolution + x] = new Color(finalGray, finalGray, finalGray, 1f);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        if (previewRenderer != null)
        {
            previewRenderer.sharedMaterial.mainTexture = texture;
        }
    }

    private void InitNoise()
    {
        // 初始化父级噪声 (大块)
        _noiseMacro = new FastNoiseLite();
        _noiseMacro.SetSeed(macroSettings.seed);
        _noiseMacro.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        _noiseMacro.SetFrequency(macroSettings.frequency);
        _noiseMacro.SetCellularDistanceFunction(macroSettings.distanceFunc);
        _noiseMacro.SetCellularReturnType(macroSettings.returnType);

        // 初始化子级噪声 (细分)
        _noiseMicro = new FastNoiseLite();
        _noiseMicro.SetSeed(microSettings.seed);
        _noiseMicro.SetNoiseType(FastNoiseLite.NoiseType.Cellular); // 子级也可以是 Cellular
        _noiseMicro.SetFrequency(microSettings.frequency);
        _noiseMicro.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.EuclideanSq);
        _noiseMicro.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);
    }
    
    // 当Inspector数值改变时自动刷新
    private void OnValidate()
    {
        // 延迟一帧或者在特定条件下刷新，为了简单起见这里不实时刷新，
        // 你可以点击组件右上角的三个点 -> Generate，或者手动点击按钮
    }
}