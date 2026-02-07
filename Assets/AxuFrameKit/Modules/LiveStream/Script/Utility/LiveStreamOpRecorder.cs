using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Common.Utility.Auto.AutoSnapshot;
using AxuFrameKit.Common.Utility.Serializer;

namespace AxuFrameKit.Modules.LiveStream
{
    // ==========================================
    // 1. 操作类型枚举 (Operation Type Enum)
    // ==========================================

    /// <summary>
    /// LiveStream 操作类型
    /// </summary>
    public enum LiveStreamOpType
    {
        Gift,
        Comment,
        Like
    }

    // ==========================================
    // 2. 操作数据结构 (Operation Data Structures)
    // ==========================================

    /// <summary>
    /// LiveStream 操作基础数据 - 用于序列化
    /// </summary>
    [Serializable]
    public class LiveStreamOpData
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public LiveStreamOpType OpType;

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId;

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string Nickname;

        /// <summary>
        /// 头像URL (用于序列化，Sprite 无法序列化)
        /// </summary>
        public string AvatarUrl;

        /// <summary>
        /// 礼物相关 (仅 Gift 类型有效)
        /// </summary>
        public string GiftName;
        public int GiftIndex;
        public int GiftCount;

        /// <summary>
        /// 评论内容 (仅 Comment 类型有效)
        /// </summary>
        public string Comment;

        /// <summary>
        /// 点赞数量 (仅 Like 类型有效)
        /// </summary>
        public int LikeCount;

        public LiveStreamOpData() { }

        /// <summary>
        /// 从 UserDataGift 创建
        /// </summary>
        public static LiveStreamOpData FromGift(UserDataGift gift)
        {
            return new LiveStreamOpData
            {
                OpType = LiveStreamOpType.Gift,
                UserId = gift.UserId,
                Nickname = gift.Nickname,
                AvatarUrl = gift.AvatarUrl,
                GiftName = gift.GiftName,
                GiftIndex = gift.GiftIndex,
                GiftCount = gift.GiftCount
            };
        }

        /// <summary>
        /// 从 UserDataComment 创建
        /// </summary>
        public static LiveStreamOpData FromComment(UserDataComment comment)
        {
            return new LiveStreamOpData
            {
                OpType = LiveStreamOpType.Comment,
                UserId = comment.UserId,
                Nickname = comment.Nickname,
                AvatarUrl = comment.AvatarUrl,
                Comment = comment.Comment
            };
        }

        /// <summary>
        /// 从 UserDataLike 创建
        /// </summary>
        public static LiveStreamOpData FromLike(UserDataLike like)
        {
            return new LiveStreamOpData
            {
                OpType = LiveStreamOpType.Like,
                UserId = like.UserId,
                Nickname = like.Nickname,
                AvatarUrl = like.AvatarUrl,
                LikeCount = like.LikeCount
            };
        }

        /// <summary>
        /// 转换为 UserDataGift
        /// </summary>
        public UserDataGift ToGift()
        {
            return new UserDataGift
            {
                UserId = UserId,
                Nickname = Nickname,
                AvatarUrl = AvatarUrl,
                GiftName = GiftName,
                GiftIndex = GiftIndex,
                GiftCount = GiftCount
            };
        }

        /// <summary>
        /// 转换为 UserDataComment
        /// </summary>
        public UserDataComment ToComment()
        {
            return new UserDataComment
            {
                UserId = UserId,
                Nickname = Nickname,
                AvatarUrl = AvatarUrl,
                Comment = Comment
            };
        }

        /// <summary>
        /// 转换为 UserDataLike
        /// </summary>
        public UserDataLike ToLike()
        {
            return new UserDataLike
            {
                UserId = UserId,
                Nickname = Nickname,
                AvatarUrl = AvatarUrl,
                LikeCount = LikeCount
            };
        }
    }

    // ==========================================
    // 3. 操作录制器配置 (Recorder Configuration)
    // ==========================================

    /// <summary>
    /// LiveStream 操作录制器配置
    /// </summary>
    [Serializable]
    public class LiveStreamRecorderConfig
    {
        /// <summary>
        /// 时间缩放模式
        /// </summary>
        public TimeScaleMode ScaleMode = TimeScaleMode.Unscaled;

        /// <summary>
        /// 时间记录模式
        /// </summary>
        public TimeRecordMode RecordMode = TimeRecordMode.RecordFromInit;

        /// <summary>
        /// 初始容量
        /// </summary>
        public int InitialCapacity = 100;

        /// <summary>
        /// 是否记录礼物操作
        /// </summary>
        public bool RecordGifts = true;

        /// <summary>
        /// 是否记录评论操作
        /// </summary>
        public bool RecordComments = true;

        /// <summary>
        /// 是否记录点赞操作
        /// </summary>
        public bool RecordLikes = true;
    }

    // ==========================================
    // 4. LiveStream 操作录制器 (Operation Recorder)
    // ==========================================

    /// <summary>
    /// LiveStream 操作录制器
    /// 用于记录 LiveStreamSimulatorWindow 中发生的操作
    /// 此类可在运行时使用，打包后也可用
    /// </summary>
    public class LiveStreamOpRecorder
    {
        private readonly LiveStreamRecorderConfig _config;
        private readonly SnapshotRecorder<LiveStreamOpData> _recorder;
        private readonly ISerializer _serializer;

        /// <summary>
        /// 是否正在录制
        /// </summary>
        public bool IsRecording => _recorder.IsRecording;

        /// <summary>
        /// 已录制的操作数量
        /// </summary>
        public int RecordedCount => _recorder.SnapshotCount;

        /// <summary>
        /// 当前配置
        /// </summary>
        public LiveStreamRecorderConfig Config => _config;

        /// <summary>
        /// 创建 LiveStream 操作录制器
        /// </summary>
        /// <param name="config">录制配置</param>
        /// <param name="serializer">可选的自定义序列化器</param>
        public LiveStreamOpRecorder(LiveStreamRecorderConfig config = null, ISerializer serializer = null)
        {
            _config = config ?? new LiveStreamRecorderConfig();
            _serializer = serializer ?? new UnityJsonSerializer();

            _recorder = SnapshotFactory.CreateRecorder<LiveStreamOpData>()
                .WithScaleMode(_config.ScaleMode)
                .WithRecordMode(_config.RecordMode)
                .WithCapacity(_config.InitialCapacity)
                .Build();
        }

        /// <summary>
        /// 录制礼物操作
        /// </summary>
        public void RecordGift(UserDataGift gift)
        {
            if (!_config.RecordGifts) return;

            var opData = LiveStreamOpData.FromGift(gift);
            _recorder.Record(opData);
        }

        /// <summary>
        /// 批量录制礼物操作
        /// </summary>
        public void RecordGifts(IReadOnlyList<UserDataGift> gifts)
        {
            if (!_config.RecordGifts) return;

            foreach (var gift in gifts)
            {
                var opData = LiveStreamOpData.FromGift(gift);
                _recorder.Record(opData);
            }
        }

        /// <summary>
        /// 录制评论操作
        /// </summary>
        public void RecordComment(UserDataComment comment)
        {
            if (!_config.RecordComments) return;

            var opData = LiveStreamOpData.FromComment(comment);
            _recorder.Record(opData);
        }

        /// <summary>
        /// 批量录制评论操作
        /// </summary>
        public void RecordComments(IReadOnlyList<UserDataComment> comments)
        {
            if (!_config.RecordComments) return;

            foreach (var comment in comments)
            {
                var opData = LiveStreamOpData.FromComment(comment);
                _recorder.Record(opData);
            }
        }

        /// <summary>
        /// 录制点赞操作
        /// </summary>
        public void RecordLike(UserDataLike like)
        {
            if (!_config.RecordLikes) return;

            var opData = LiveStreamOpData.FromLike(like);
            _recorder.Record(opData);
        }

        /// <summary>
        /// 批量录制点赞操作
        /// </summary>
        public void RecordLikes(IReadOnlyList<UserDataLike> likes)
        {
            if (!_config.RecordLikes) return;

            foreach (var like in likes)
            {
                var opData = LiveStreamOpData.FromLike(like);
                _recorder.Record(opData);
            }
        }

        /// <summary>
        /// 开始录制
        /// </summary>
        public void StartRecording()
        {
            _recorder.StartRecording();
        }

        /// <summary>
        /// 停止录制
        /// </summary>
        public void StopRecording()
        {
            _recorder.StopRecording();
        }

        /// <summary>
        /// 清空所有录制数据
        /// </summary>
        public void Clear()
        {
            _recorder.Clear();
        }

        /// <summary>
        /// 获取所有录制的快照
        /// </summary>
        public IReadOnlyList<TimestampedSnapshot<LiveStreamOpData>> GetSnapshots()
        {
            return _recorder.GetSnapshots();
        }

        /// <summary>
        /// 导出录制数据
        /// </summary>
        public SnapshotData<LiveStreamOpData> ExportData()
        {
            return _recorder.ExportData();
        }

        /// <summary>
        /// 序列化为 JSON 字符串
        /// </summary>
        public string SerializeToJson()
        {
            return _recorder.Serialize(_serializer);
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否保存成功</returns>
        public bool SaveToFile(string filePath)
        {
            try
            {
                string json = SerializeToJson();
                System.IO.File.WriteAllText(filePath, json);
                Debug.Log($"[LiveStreamOpRecorder] 已保存 {RecordedCount} 条记录到: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LiveStreamOpRecorder] 保存失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从文件加载创建播放器
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="serializer">可选的自定义序列化器</param>
        /// <returns>播放器实例，失败返回 null</returns>
        public static SnapshotPlayer<LiveStreamOpData> LoadFromFile(string filePath, ISerializer serializer = null)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogError($"[LiveStreamOpRecorder] 文件不存在: {filePath}");
                    return null;
                }

                string json = System.IO.File.ReadAllText(filePath);
                var usedSerializer = serializer ?? new UnityJsonSerializer();
                var player = SnapshotPlayer<LiveStreamOpData>.FromJson(json, usedSerializer);
                Debug.Log($"[LiveStreamOpRecorder] 已加载 {player.TotalCount} 条记录从: {filePath}");
                return player;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LiveStreamOpRecorder] 加载失败: {e.Message}");
                return null;
            }
        }
    }

    // ==========================================
    // 5. LiveStream 操作回放处理器 (Playback Handler)
    // ==========================================

    /// <summary>
    /// LiveStream 操作回放处理器
    /// 用于将录制的操作回放并触发对应事件
    /// </summary>
    public class LiveStreamOpPlaybackHandler : ISnapshotPlaybackHandler<LiveStreamOpData>
    {
        /// <summary>
        /// 礼物操作回调
        /// </summary>
        public event Action<UserDataGift, float> OnGiftPlayed;

        /// <summary>
        /// 评论操作回调
        /// </summary>
        public event Action<UserDataComment, float> OnCommentPlayed;

        /// <summary>
        /// 点赞操作回调
        /// </summary>
        public event Action<UserDataLike, float> OnLikePlayed;

        /// <summary>
        /// 回放完成回调
        /// </summary>
        public event Action OnComplete;

        public void OnSnapshotOutput(LiveStreamOpData data, float timestamp)
        {
            switch (data.OpType)
            {
                case LiveStreamOpType.Gift:
                    OnGiftPlayed?.Invoke(data.ToGift(), timestamp);
                    break;

                case LiveStreamOpType.Comment:
                    OnCommentPlayed?.Invoke(data.ToComment(), timestamp);
                    break;

                case LiveStreamOpType.Like:
                    OnLikePlayed?.Invoke(data.ToLike(), timestamp);
                    break;
            }
        }

        public void OnPlaybackComplete()
        {
            OnComplete?.Invoke();
        }

        /// <summary>
        /// 清除所有回调
        /// </summary>
        public void ClearCallbacks()
        {
            OnGiftPlayed = null;
            OnCommentPlayed = null;
            OnLikePlayed = null;
            OnComplete = null;
        }
    }
}
