using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AxuFrameKit.Common.Editor.Utility.Converter
{
    /// <summary>
    /// 编辑器工具：将 Unity Terrain 烘焙转换为静态 Mesh。
    ///
    /// 使用方式：
    ///   1. 菜单栏 Tools/Terrain 转 Mesh 转换器（可配置参数后转换）
    ///   2. 在 Hierarchy 中右键带 Terrain 的物体 → 「将 Terrain 转换为 Mesh」（用默认设置直接转换）
    ///
    /// 功能：
    ///   - 按指定精度采样高度图生成网格（超过 65535 顶点时自动使用 32 位索引）
    ///   - 可选将所有 Splat Layer 合并烘焙为一张地表贴图（PNG）
    ///   - 可选自动添加 MeshCollider、在场景中生成对应物体并赋予材质
    /// </summary>
    public class TerrainToMeshConverter : EditorWindow
    {
        private const string DefaultSaveFolder = "Assets/_TerrainTemp";
        private const int BakedTextureSize = 1024; // 烘焙地表贴图的分辨率
        private const int LayerSampleSize = 128;   // 每个 Splat Layer 贴图的预采样尺寸

        private static readonly int[] Resolutions = { 33, 65, 129, 257, 513 };
        private static readonly string[] ResolutionLabels = { "33", "65", "129", "257", "513" };

        private Terrain _terrain;
        private int _resolution = 129;             // 每边顶点数（IntPopup 直接存“值”而非索引）
        private bool _bakeSplatTexture = true;
        private bool _generateCollider = true;
        private bool _createSceneObject = true;
        private string _saveFolder = DefaultSaveFolder;

        // ---------------- 菜单 ----------------

        [MenuItem("AxuFrameKit/Misc/Terrain->Mesh")]
        private static void OpenWindow()
        {
            var win = GetWindow<TerrainToMeshConverter>("Terrain → Mesh");
            win.minSize = new Vector2(340, 400);
            var selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<Terrain>()
                : null;
            if (selected != null)
                win._terrain = selected;
        }

        // ---------------- 窗口 UI ----------------

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Terrain → Mesh 转换器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("把 Terrain 的高度烘焙为静态 Mesh，并可将 Splat 地表混合烘焙为一张贴图。", MessageType.Info);

            _terrain = (Terrain)EditorGUILayout.ObjectField("目标 Terrain", _terrain, typeof(Terrain), true);

            // 注意：IntPopup 返回的是被选中项的“值”（Resolutions 中的数值），不是索引
            _resolution = EditorGUILayout.IntPopup(
                "网格精度（每边顶点数）",
                _resolution, ResolutionLabels, Resolutions);

            if (_terrain != null)
            {
                int res = _resolution;
                long verts = (long)res * res;
                long tris = (long)(res - 1) * (res - 1) * 2;
                EditorGUILayout.LabelField($"预计顶点：{verts:N0}    三角面：{tris:N0}");
                if (verts > 65000)
                    EditorGUILayout.HelpBox("顶点数超过 65535，将自动改用 32 位索引格式。", MessageType.None);
            }

            EditorGUILayout.Space(6);
            _bakeSplatTexture = EditorGUILayout.ToggleLeft("烘焙 Splat 地表贴图（合并所有 Layer 为一张 PNG）", _bakeSplatTexture);
            _generateCollider = EditorGUILayout.ToggleLeft("为生成的物体添加 MeshCollider", _generateCollider);
            _createSceneObject = EditorGUILayout.ToggleLeft("在场景中创建对应的 Mesh 物体", _createSceneObject);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            _saveFolder = EditorGUILayout.TextField("保存目录", _saveFolder);
            if (GUILayout.Button("选择…", GUILayout.Width(48)))
            {
                PickSaveFolder();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            GUI.enabled = _terrain != null;
            if (GUILayout.Button("开始转换", GUILayout.Height(34)))
            {
                ConvertTerrain(_terrain, _resolution,
                    _bakeSplatTexture, _generateCollider, _createSceneObject, _saveFolder);
            }
            GUI.enabled = true;

            if (GUILayout.Button("转换场景中的全部 Terrain"))
            {
                var all = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (all.Length == 0)
                {
                    EditorUtility.DisplayDialog("Terrain → Mesh", "场景中没有找到 Terrain。", "确定");
                    return;
                }
                foreach (var t in all)
                {
                    ConvertTerrain(t, _resolution,
                        _bakeSplatTexture, _generateCollider, _createSceneObject, _saveFolder);
                }
            }
        }

        // ---------------- 核心转换流程 ----------------

        private static void ConvertTerrain(Terrain terrain, int resolution, bool bakeSplat,
            bool addCollider, bool createObject, string saveFolder)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("[TerrainToMesh] Terrain 或 TerrainData 为空。");
                return;
            }

            try
            {
                saveFolder = NormalizeFolder(saveFolder);
                EnsureFolder(saveFolder);

                string baseName = MakeSafeFileName(terrain.gameObject.name);

                // 1. 构建网格
                Mesh mesh = BuildMesh(terrain, resolution);
                if (mesh == null) // 用户取消
                {
                    Debug.Log("[TerrainToMesh] 已取消。");
                    return;
                }

                string meshPath = AssetDatabase.GenerateUniqueAssetPath($"{saveFolder}/{baseName}_Mesh.asset");
                AssetDatabase.CreateAsset(mesh, meshPath);

                // 2. 烘焙地表贴图
                string texturePath = null;
                if (bakeSplat)
                {
                    Texture2D baked = BakeSplatTexture(terrain, BakedTextureSize);
                    if (baked != null)
                    {
                        texturePath = AssetDatabase.GenerateUniqueAssetPath($"{saveFolder}/{baseName}_Splat.png");
                        File.WriteAllBytes(texturePath, baked.EncodeToPNG());
                        Object.DestroyImmediate(baked);
                        AssetDatabase.ImportAsset(texturePath);
                    }
                }

                // 3. 创建材质
                Material material = null;
                if (texturePath != null)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    material = new Material(shader);
                    material.mainTexture = tex;
                    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.05f);

                    string matPath = AssetDatabase.GenerateUniqueAssetPath($"{saveFolder}/{baseName}_Mat.mat");
                    AssetDatabase.CreateAsset(material, matPath);
                }

                // 4. 场景中生成物体
                if (createObject)
                {
                    var go = new GameObject(baseName + "_Mesh");
                    go.transform.SetPositionAndRotation(terrain.transform.position, terrain.transform.rotation);
                    go.isStatic = true;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    // 优先引用源 Terrain 的材质模板，其次才是烘焙贴图生成的材质
                    Material rendererMaterial = terrain.materialTemplate != null ? terrain.materialTemplate : material;
                    if (rendererMaterial != null)
                        renderer.sharedMaterial = rendererMaterial;
                    if (addCollider)
                        go.AddComponent<MeshCollider>().sharedMesh = mesh;

                    Undo.RegisterCreatedObjectUndo(go, "Convert Terrain To Mesh");
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(mesh);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[TerrainToMesh] 转换完成：{terrain.name}\n网格：{meshPath}" +
                          (texturePath != null ? $"\n贴图：{texturePath}" : ""));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>按 resolution 采样高度图构建网格；用户取消时返回 null。</summary>
        private static Mesh BuildMesh(Terrain terrain, int resolution)
        {
            TerrainData data = terrain.terrainData;
            Vector3 size = data.size; // 已包含高度缩放的实际尺寸

            int vertCount = resolution * resolution;
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            float invRes = 1f / (resolution - 1);

            for (int z = 0; z < resolution; z++)
            {
                float v = z * invRes;
                for (int x = 0; x < resolution; x++)
                {
                    float u = x * invRes;
                    int i = z * resolution + x;
                    vertices[i] = new Vector3(u * size.x, data.GetInterpolatedHeight(u, v), v * size.z);
                    normals[i] = data.GetInterpolatedNormal(u, v);
                    uvs[i] = new Vector2(u, v);
                }

                if ((z & 31) == 0 &&
                    EditorUtility.DisplayCancelableProgressBar(
                        "Terrain → Mesh", $"采样高度数据… ({z}/{resolution})", z / (float)resolution))
                {
                    return null;
                }
            }

            int quadCount = (resolution - 1) * (resolution - 1);
            var triangles = new int[quadCount * 6];
            int t = 0;
            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int i0 = z * resolution + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + resolution;
                    int i3 = i2 + 1;

                    // 顺时针绕序，法线朝上
                    triangles[t++] = i0; triangles[t++] = i2; triangles[t++] = i1;
                    triangles[t++] = i1; triangles[t++] = i2; triangles[t++] = i3;
                }
            }

            var mesh = new Mesh
            {
                name = terrain.gameObject.name + "_Mesh",
                indexFormat = vertCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        // ---------------- Splat 贴图烘焙 ----------------

        /// <summary>把所有 Splat Layer 按 Control Map 权重混合，烘焙成一张普通贴图。</summary>
        private static Texture2D BakeSplatTexture(Terrain terrain, int texSize)
        {
            TerrainData data = terrain.terrainData;
            TerrainLayer[] layers = data.terrainLayers;
            if (layers == null || layers.Length == 0)
            {
                Debug.LogWarning("[TerrainToMesh] 该 Terrain 没有 Splat Layer，跳过贴图烘焙。");
                return null;
            }

            float[,,] alpha = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            int aw = alpha.GetLength(0);
            int ah = alpha.GetLength(1);
            int layerCount = Mathf.Min(layers.Length, alpha.GetLength(2));

            // 预采样每层贴图到小图（通过 RenderTexture，兼容不可读贴图）
            var layerPixels = new Color[layerCount][];
            for (int l = 0; l < layerCount; l++)
            {
                var src = layers[l] != null ? layers[l].diffuseTexture : null;
                layerPixels[l] = src != null ? SampleTexturePixels(src, LayerSampleSize) : null;
            }

            var pixels = new Color[texSize * texSize];
            for (int y = 0; y < texSize; y++)
            {
                float v = y / (float)(texSize - 1);
                for (int x = 0; x < texSize; x++)
                {
                    float u = x / (float)(texSize - 1);

                    Color acc = Color.clear;
                    float total = 0f;
                    for (int l = 0; l < layerCount; l++)
                    {
                        float w = SampleAlphaWeight(alpha, aw, ah, l, u, v);
                        if (w <= 0f) continue;
                        total += w;

                        if (layerPixels[l] == null)
                        {
                            acc += w * Color.white;
                            continue;
                        }

                        Vector2 tile = layers[l].tileSize;
                        Vector2 offset = layers[l].tileOffset;
                        acc += w * SampleWrapped(layerPixels[l], LayerSampleSize,
                            u * tile.x + offset.x, v * tile.y + offset.y);
                    }

                    pixels[y * texSize + x] = total > 1e-5f ? acc / total : Color.white;
                }

                if ((y & 31) == 0 &&
                    EditorUtility.DisplayCancelableProgressBar(
                        "Terrain → Mesh", $"烘焙地表贴图… ({y}/{texSize})", y / (float)texSize))
                {
                    return null;
                }
            }

            var result = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
            result.SetPixels(pixels);
            result.Apply(true);
            return result;
        }

        /// <summary>通过 RenderTexture 把任意贴图采样为指定尺寸的像素数组（无需贴图可读）。</summary>
        private static Color[] SampleTexturePixels(Texture source, int size)
        {
            RenderTexture rt = RenderTexture.GetTemporary(
                size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, rt);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tmp = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tmp.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tmp.Apply(false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            Color[] pixels = tmp.GetPixels();
            Object.DestroyImmediate(tmp);
            return pixels;
        }

        /// <summary>对 Control Map 的某一层权重做双线性采样（uv 为 0~1 归一化坐标）。</summary>
        private static float SampleAlphaWeight(float[,,] alpha, int width, int height, int layer, float u, float v)
        {
            float fx = u * (width - 1);
            float fy = v * (height - 1);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(fy), 0, height - 1);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            float tx = fx - x0;
            float ty = fy - y0;

            float w00 = alpha[y0, x0, layer];
            float w10 = alpha[y0, x1, layer];
            float w01 = alpha[y1, x0, layer];
            float w11 = alpha[y1, x1, layer];

            return Mathf.Lerp(Mathf.Lerp(w00, w10, tx), Mathf.Lerp(w01, w11, tx), ty);
        }

        /// <summary>以 Repeat 平铺方式双线性采样一层贴图像素。</summary>
        private static Color SampleWrapped(Color[] pixels, int size, float u, float v)
        {
            u -= Mathf.Floor(u);
            v -= Mathf.Floor(v);

            float fu = u * size - 0.5f;
            float fv = v * size - 0.5f;
            int x0 = Mathf.FloorToInt(fu);
            int y0 = Mathf.FloorToInt(fv);
            float tx = fu - x0;
            float ty = fv - y0;
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            x0 = WrapCoord(x0, size); x1 = WrapCoord(x1, size);
            y0 = WrapCoord(y0, size); y1 = WrapCoord(y1, size);

            Color c00 = pixels[y0 * size + x0];
            Color c10 = pixels[y0 * size + x1];
            Color c01 = pixels[y1 * size + x0];
            Color c11 = pixels[y1 * size + x1];

            return Color.Lerp(Color.Lerp(c00, c10, tx), Color.Lerp(c01, c11, tx), ty);
        }

        private static int WrapCoord(int value, int size)
        {
            value %= size;
            return value < 0 ? value + size : value;
        }

        // ---------------- 工具函数 ----------------

        private static string NormalizeFolder(string folder)
        {
            folder = (folder ?? DefaultSaveFolder).Replace('\\', '/').TrimEnd('/');
            if (!folder.StartsWith("Assets"))
                folder = "Assets/" + folder.TrimStart('/');
            return folder;
        }

        private static void EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrEmpty(name) ? "Terrain" : name;
        }

        private static string MakeRelativePath(string absolutePath)
        {
            absolutePath = absolutePath.Replace('\\', '/');
            string dataRoot = Application.dataPath; // ends with /Assets
            if (!absolutePath.StartsWith(dataRoot))
            {
                EditorUtility.DisplayDialog("Terrain → Mesh", "所选目录不在当前工程 Assets 内，请重新选择。", "确定");
                return null;
            }
            return "Assets" + absolutePath.Substring(dataRoot.Length);
        }

        /// <summary>
        /// 弹出系统文件夹选择框，让用户选择保存目录，并自动转为 Assets 内相对路径。
        /// 若当前默认路径已存在则先指向该目录；否则指向工程 Assets 根目录。
        /// </summary>
        private void PickSaveFolder()
        {
            string startDir = Path.Combine(Application.dataPath, _saveFolder.TrimStart('/').Replace("Assets/", ""));
            if (!Directory.Exists(startDir))
                startDir = Application.dataPath;

            string selected = EditorUtility.OpenFolderPanel("选择保存目录", startDir, "");
            if (string.IsNullOrEmpty(selected))
                return;

            string relative = MakeRelativePath(selected);
            if (relative != null)
                _saveFolder = relative;
        }
    }
}