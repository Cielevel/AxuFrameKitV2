using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Modules.LiveStream.Tests.Editor
{
    /// <summary>
    /// [Axu/Module/LiveStream] 操作数据模型与录制器 EditMode 测试（依赖框架家族试点）：
    /// 覆盖 LiveStreamOpData 三类操作的映射与还原、LiveStreamOpRecorder 的
    /// 录制 / 配置开关 / 停止恢复 / 序列化（SaveToFile → LoadFromFile → 回放分发）链路。
    /// 全部为同步纯逻辑路径（FlushAll 立即回放），临时文件写入系统 Temp 目录并自清理。
    /// </summary>
    [Category("Axu/Module/LiveStream")]
    public class LiveStreamOpRecorderTests
    {
        [Test]
        public void LiveStreamOpData_三类操作From与To_字段完整映射且类型正确()
        {
            // Arrange：三类用户操作数据
            var gift = new UserDataGift
            {
                UserId = "u-gift", Nickname = "礼物哥", AvatarUrl = "http://a/gift.png",
                GiftName = "火箭", GiftIndex = 3, GiftCount = 23
            };
            var comment = new UserDataComment
            {
                UserId = "u-cmt", Nickname = "潜水员", AvatarUrl = "http://a/cmt.png",
                Comment = "主播666"
            };
            var like = new UserDataLike
            {
                UserId = "u-like", Nickname = "点赞手", AvatarUrl = "http://a/like.png",
                LikeCount = 88
            };

            // Act：映射为可序列化数据，再还原为对应类型
            var giftOp = LiveStreamOpData.FromGift(gift);
            var commentOp = LiveStreamOpData.FromComment(comment);
            var likeOp = LiveStreamOpData.FromLike(like);
            var giftBack = giftOp.ToGift();
            var commentBack = commentOp.ToComment();
            var likeBack = likeOp.ToLike();

            // Assert：OpType 标记正确
            Assert.That(giftOp.OpType, Is.EqualTo(LiveStreamOpType.Gift));
            Assert.That(commentOp.OpType, Is.EqualTo(LiveStreamOpType.Comment));
            Assert.That(likeOp.OpType, Is.EqualTo(LiveStreamOpType.Like));

            // Assert：Gift 字段往返无损
            Assert.That(giftBack.UserId, Is.EqualTo(gift.UserId));
            Assert.That(giftBack.Nickname, Is.EqualTo(gift.Nickname));
            Assert.That(giftBack.AvatarUrl, Is.EqualTo(gift.AvatarUrl));
            Assert.That(giftBack.GiftName, Is.EqualTo("火箭"));
            Assert.That(giftBack.GiftIndex, Is.EqualTo(3));
            Assert.That(giftBack.GiftCount, Is.EqualTo(23));

            // Assert：Comment / Like 字段往返无损
            Assert.That(commentBack.UserId, Is.EqualTo(comment.UserId));
            Assert.That(commentBack.Comment, Is.EqualTo("主播666"));
            Assert.That(likeBack.UserId, Is.EqualTo(like.UserId));
            Assert.That(likeBack.LikeCount, Is.EqualTo(88));
        }

        [Test]
        public void LiveStreamOpRecorder_录制三类操作_序列化往返回放按类型分发()
        {
            // Arrange
            var recorder = new LiveStreamOpRecorder(new LiveStreamRecorderConfig());
            recorder.RecordGift(new UserDataGift
            {
                UserId = "u-gift", Nickname = "礼物哥", AvatarUrl = "http://a/gift.png",
                GiftName = "火箭", GiftIndex = 3, GiftCount = 23
            });
            recorder.RecordComment(new UserDataComment
            {
                UserId = "u-cmt", Nickname = "潜水员", AvatarUrl = "http://a/cmt.png",
                Comment = "主播666"
            });
            recorder.RecordLike(new UserDataLike
            {
                UserId = "u-like", Nickname = "点赞手", AvatarUrl = "http://a/like.png",
                LikeCount = 88
            });
            Assert.That(recorder.RecordedCount, Is.EqualTo(3), "录制阶段应累计 3 条");

            string path = Path.Combine(Path.GetTempPath(), $"axu-livestream-test-{Guid.NewGuid():N}.json");
            try
            {
                // Act：落盘 → 加载 → 接回放处理器 → 立即清空回放
                Assert.That(recorder.SaveToFile(path), Is.True, "SaveToFile 应成功");

                var player = LiveStreamOpRecorder.LoadFromFile(path);
                Assert.That(player, Is.Not.Null, "LoadFromFile 应返回播放器");
                Assert.That(player.TotalCount, Is.EqualTo(3), "往返后快照数量应保持 3");

                var handler = new LiveStreamOpPlaybackHandler();
                var gifts = new List<UserDataGift>();
                var comments = new List<UserDataComment>();
                var likes = new List<UserDataLike>();
                var timestamps = new List<float>();
                bool completed = false;
                handler.OnGiftPlayed += (g, t) => { gifts.Add(g); timestamps.Add(t); };
                handler.OnCommentPlayed += (c, t) => comments.Add(c);
                handler.OnLikePlayed += (l, t) => likes.Add(l);
                handler.OnComplete += () => completed = true;

                player.UseHandler(handler);
                player.FlushAll();

                // Assert：按类型各分发一条，字段无损，完成回调触发
                Assert.That(gifts, Has.Count.EqualTo(1), "Gift 分发数量");
                Assert.That(comments, Has.Count.EqualTo(1), "Comment 分发数量");
                Assert.That(likes, Has.Count.EqualTo(1), "Like 分发数量");
                Assert.That(completed, Is.True, "FlushAll 后应触发 OnComplete");

                Assert.That(gifts[0].GiftName, Is.EqualTo("火箭"));
                Assert.That(gifts[0].GiftIndex, Is.EqualTo(3));
                Assert.That(gifts[0].GiftCount, Is.EqualTo(23));
                Assert.That(gifts[0].Nickname, Is.EqualTo("礼物哥"));
                Assert.That(comments[0].Comment, Is.EqualTo("主播666"));
                Assert.That(likes[0].LikeCount, Is.EqualTo(88));
                Assert.That(timestamps, Has.Count.EqualTo(1));
                Assert.That(timestamps[0], Is.GreaterThanOrEqualTo(0f), "回放时间戳应非负");
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void LiveStreamOpRecorder_关闭Gift与Like录制_仅评论被记录()
        {
            // Arrange
            var config = new LiveStreamRecorderConfig
            {
                RecordGifts = false,
                RecordComments = true,
                RecordLikes = false
            };
            var recorder = new LiveStreamOpRecorder(config);

            // Act：三类操作各录一条
            recorder.RecordGift(new UserDataGift { UserId = "u1", GiftName = "火箭", GiftCount = 1 });
            recorder.RecordComment(new UserDataComment { UserId = "u2", Comment = "666" });
            recorder.RecordLike(new UserDataLike { UserId = "u3", LikeCount = 5 });

            // Assert：配置开关生效，仅评论入列
            Assert.That(recorder.RecordedCount, Is.EqualTo(1), "关闭 Gift/Like 后应只记录评论");
            Assert.That(recorder.GetSnapshots()[0].Data.OpType, Is.EqualTo(LiveStreamOpType.Comment));
            Assert.That(recorder.GetSnapshots()[0].Data.Comment, Is.EqualTo("666"));
        }

        [Test]
        public void LiveStreamOpRecorder_停止录制后Record_数量不变并输出警告()
        {
            // Arrange
            var recorder = new LiveStreamOpRecorder();
            recorder.RecordComment(new UserDataComment { UserId = "u1", Comment = "先录一条" });
            Assert.That(recorder.RecordedCount, Is.EqualTo(1));

            // Act：停止后继续 Record（先声明预期警告，LogAssert 用完整日志原文匹配）
            recorder.StopRecording();
            LogAssert.Expect(LogType.Warning,
                "[SnapshotRecorder] Recording is stopped. Call StartRecording() to resume.");
            recorder.RecordComment(new UserDataComment { UserId = "u2", Comment = "不应被记录" });

            // Assert：停止期间不入列；恢复录制后可继续
            Assert.That(recorder.RecordedCount, Is.EqualTo(1), "停止录制后 Record 应被忽略");
            Assert.That(recorder.IsRecording, Is.False);

            recorder.StartRecording();
            recorder.RecordLike(new UserDataLike { UserId = "u3", LikeCount = 1 });
            Assert.That(recorder.RecordedCount, Is.EqualTo(2), "恢复录制后应继续累计");
            Assert.That(recorder.GetSnapshots()[1].Data.OpType, Is.EqualTo(LiveStreamOpType.Like));
        }
    }
}
