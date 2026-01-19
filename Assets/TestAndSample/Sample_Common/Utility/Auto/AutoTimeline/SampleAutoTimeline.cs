using System;
using UnityEngine;
using AxuFrameKit.Common.Utility.Auto.AutoTimeline;

namespace TestAndSample.Sample_Common.Utility.Auto.AutoTimeline
{
    /// <summary>
    /// AutoTimeline 示例
    /// 演示时间线记录器和播放器的基本用法
    /// </summary>
    public class SampleAutoTimeline : MonoBehaviour
    {
        [Header("记录设置")]
        [SerializeField] private TimeScaleMode _scaleMode = TimeScaleMode.Scaled;
        [SerializeField] private TimeRecordMode _recordMode = TimeRecordMode.RecordFromInit;
        [SerializeField] private float _recordInterval = 0.5f;

        [Header("回放设置")]
        [SerializeField] private bool _autoPlayOnLoad = true;

        [Header("调试信息")]
        [SerializeField] private int _recordedCount;
        [SerializeField] private bool _isRecording;
        [SerializeField] private bool _isPlaying;
        [SerializeField] private float _playbackProgress;

        // 内部状态
        private TimelineRecorder<PositionData> _recorder;
        private TimelinePlayer<PositionData> _player;
        private ITimelineDataSerializer _serializer;  // 只需实现数据项的序列化
        private float _lastRecordTime;
        private string _savedJson;

        // 用于可视化的回放对象
        private GameObject _playbackIndicator;

        private void Awake()
        {
            // 使用框架提供的默认序列化器（使用 Unity JsonUtility）
            // 你也可以使用自定义的 Newtonsoft 等实现
            _serializer = new UnityJsonDataSerializer();
        }

        private void Start()
        {
            // 创建记录器
            CreateRecorder();

            // 创建回放指示器
            CreatePlaybackIndicator();

            Debug.Log("[SampleAutoTimeline] 按键说明:");
            Debug.Log("  R - 开始/停止记录");
            Debug.Log("  S - 保存为 JSON");
            Debug.Log("  L - 从 JSON 加载");
            Debug.Log("  P - 开始/暂停回放");
            Debug.Log("  F - 立即输出所有快照");
            Debug.Log("  C - 清空记录");
        }

        private void CreateRecorder()
        {
            _recorder = TimelineFactory.CreateRecorder<PositionData>()
                .WithScaleMode(_scaleMode)
                .WithRecordMode(_recordMode)
                .WithCapacity(100)
                .Build();

            _isRecording = true;
            Debug.Log($"[SampleAutoTimeline] 记录器已创建 - 模式: {_scaleMode}, 记录方式: {_recordMode}");
        }

        private void CreatePlaybackIndicator()
        {
            _playbackIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _playbackIndicator.name = "PlaybackIndicator";
            _playbackIndicator.transform.localScale = Vector3.one * 0.5f;
            _playbackIndicator.GetComponent<Renderer>().material.color = Color.red;
            _playbackIndicator.SetActive(false);
        }

        private void Update()
        {
            HandleInput();
            UpdateRecording();
            UpdateDebugInfo();
        }

        private void HandleInput()
        {
            // R - 切换记录状态
            if (Input.GetKeyDown(KeyCode.R))
            {
                ToggleRecording();
            }

            // S - 保存为 JSON
            if (Input.GetKeyDown(KeyCode.S))
            {
                SaveToJson();
            }

            // L - 从 JSON 加载
            if (Input.GetKeyDown(KeyCode.L))
            {
                LoadFromJson();
            }

            // P - 切换回放状态
            if (Input.GetKeyDown(KeyCode.P))
            {
                TogglePlayback();
            }

            // F - 立即输出所有
            if (Input.GetKeyDown(KeyCode.F))
            {
                FlushAll();
            }

            // C - 清空
            if (Input.GetKeyDown(KeyCode.C))
            {
                ClearRecording();
            }
        }

        private void UpdateRecording()
        {
            if (!_isRecording || _recorder == null) return;

            // 按间隔记录位置
            if (Time.time - _lastRecordTime >= _recordInterval)
            {
                var data = new PositionData
                {
                    X = transform.position.x,
                    Y = transform.position.y,
                    Z = transform.position.z,
                    RotationY = transform.eulerAngles.y
                };

                _recorder.Record(data);
                _lastRecordTime = Time.time;
            }
        }

        private void UpdateDebugInfo()
        {
            _recordedCount = _recorder?.SnapshotCount ?? 0;
            _isRecording = _recorder?.IsRecording ?? false;
            _isPlaying = _player?.IsPlaying ?? false;
            _playbackProgress = _player?.Progress ?? 0f;
        }

        #region 操作方法

        private void ToggleRecording()
        {
            if (_recorder == null) return;

            if (_recorder.IsRecording)
            {
                _recorder.StopRecording();
                Debug.Log("[SampleAutoTimeline] 停止记录");
            }
            else
            {
                _recorder.StartRecording();
                Debug.Log("[SampleAutoTimeline] 开始记录");
            }
        }

        private void SaveToJson()
        {
            if (_recorder == null || _recorder.SnapshotCount == 0)
            {
                Debug.LogWarning("[SampleAutoTimeline] 没有数据可保存");
                return;
            }

            // 序列化：框架自动处理元数据（ScaleMode, RecordMode, CreatedAtUtc, InitTime）
            // 用户序列化器只需处理 PositionData 类型
            _savedJson = _recorder.Serialize(_serializer);
            Debug.Log($"[SampleAutoTimeline] 已保存 {_recorder.SnapshotCount} 条记录");
            Debug.Log($"[SampleAutoTimeline] JSON 内容:\n{_savedJson}");
        }

        private void LoadFromJson()
        {
            if (string.IsNullOrEmpty(_savedJson))
            {
                Debug.LogWarning("[SampleAutoTimeline] 没有保存的 JSON 数据");
                return;
            }

            // 停止当前回放
            StopPlayback();

            // 从 JSON 创建播放器：框架自动处理元数据反序列化
            // 用户序列化器只需处理 PositionData 类型的反序列化
            _player = TimelinePlayer<PositionData>.FromJson(_savedJson, _serializer);

            // 设置回调
            _player
                .OnSnapshotOutput(OnSnapshotOutput)
                .OnPlaybackComplete(OnPlaybackComplete);

            Debug.Log($"[SampleAutoTimeline] 已加载 {_player.TotalCount} 条记录");

            if (_autoPlayOnLoad)
            {
                StartPlayback();
            }
        }

        private void TogglePlayback()
        {
            if (_player == null)
            {
                Debug.LogWarning("[SampleAutoTimeline] 请先加载数据 (按 L)");
                return;
            }

            if (_player.IsPlaying)
            {
                _player.Pause();
                Debug.Log("[SampleAutoTimeline] 暂停回放");
            }
            else
            {
                StartPlayback();
            }
        }

        private void StartPlayback()
        {
            if (_player == null) return;

            _playbackIndicator.SetActive(true);
            _player.Play(this);
            Debug.Log("[SampleAutoTimeline] 开始回放");
        }

        private void StopPlayback()
        {
            if (_player == null) return;

            _player.Stop(this);
            _playbackIndicator.SetActive(false);
        }

        private void FlushAll()
        {
            if (_player == null)
            {
                Debug.LogWarning("[SampleAutoTimeline] 请先加载数据 (按 L)");
                return;
            }

            _player.FlushAll();
        }

        private void ClearRecording()
        {
            _recorder?.Clear();
            Debug.Log("[SampleAutoTimeline] 已清空记录");
        }

        #endregion

        #region 回调

        private void OnSnapshotOutput(PositionData data, float timestamp)
        {
            // 更新回放指示器位置
            _playbackIndicator.transform.position = new Vector3(data.X, data.Y, data.Z);

            Debug.Log($"[回放] 时间: {timestamp:F2}s | 位置: ({data.X:F2}, {data.Y:F2}, {data.Z:F2}) | 旋转Y: {data.RotationY:F1}°");
        }

        private void OnPlaybackComplete()
        {
            _playbackIndicator.SetActive(false);
            Debug.Log("[SampleAutoTimeline] 回放完成!");
        }

        #endregion

        private void OnDestroy()
        {
            StopPlayback();

            if (_playbackIndicator != null)
            {
                Destroy(_playbackIndicator);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"<b>AutoTimeline 示例</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(5);
            GUILayout.Label($"记录模式: {_scaleMode} / {_recordMode}");
            GUILayout.Label($"已记录: {_recordedCount} 条");
            GUILayout.Label($"正在记录: {_isRecording}");
            GUILayout.Label($"正在回放: {_isPlaying}");
            GUILayout.Label($"回放进度: {_playbackProgress:P0}");
            GUILayout.Space(10);
            GUILayout.Label("R-记录 S-保存 L-加载 P-回放 C-清空");
            GUILayout.EndArea();
        }
    }

    // ==========================================
    // 示例数据结构
    // ==========================================

    /// <summary>
    /// 位置数据 - 用于记录的示例数据结构
    /// 只需要是可序列化的类即可，无需包装器
    /// </summary>
    [Serializable]
    public class PositionData
    {
        public float X;
        public float Y;
        public float Z;
        public float RotationY;
    }

    // ==========================================
    // 自定义序列化器示例 (可选，使用 Newtonsoft.Json)
    // ==========================================

    /*
    /// <summary>
    /// 使用 Newtonsoft.Json 的序列化器示例
    /// 只需实现数据项的序列化，元数据由框架自动处理
    /// </summary>
    public class NewtonsoftDataSerializer : ITimelineDataSerializer
    {
        public string SerializeItem<T>(T item)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(item);
        }

        public T DeserializeItem<T>(string data)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data);
        }
    }
    */
}
