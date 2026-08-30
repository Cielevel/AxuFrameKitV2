using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// axu_test_run 的运行上下文（启动前写入 <see cref="AxuTestReportPaths.ContextFilePath"/>，
    /// RunFinished 时由报告器读回后删除）。设计原因：域重载开启时 PlayMode 运行的回调发生在重载后的新域、
    /// 静态字段被清空，只有磁盘文件能把"运行参数"（mode / filter / report 开关 / out_path / startedAt）
    /// 跨域带给 <c>AxuTestReportWriter</c>；域重载关闭时同样成立（文件读写与域形态无关）。
    /// </summary>
    public sealed class AxuTestRunContext
    {
        /// <summary>上下文文件 schema 标识。</summary>
        public const string SchemaId = "axu-test-run-context/1";

        /// <summary>schema 标识，默认取 <see cref="SchemaId"/>。</summary>
        public string Schema { get; set; } = SchemaId;

        /// <summary>运行模式："editor" / "playmode" / "all"（分次运行时为当次子运行的模式）。</summary>
        public string Mode { get; set; }

        /// <summary>过滤条件描述。</summary>
        public FilterInfo Filter { get; set; }

        /// <summary>是否落盘报告 JSON。</summary>
        public bool Report { get; set; }

        /// <summary>报告文件路径覆盖（相对工程根或绝对路径）；空为默认目录。</summary>
        public string OutPath { get; set; }

        /// <summary>artifacts 目录（相对工程根或绝对路径）；空为默认目录。RunStarted 时据此恢复 AxuCapture 重定向（跨域重载后静态字段会丢失）。</summary>
        public string ArtifactsDirectory { get; set; }

        /// <summary>运行开始时间（UTC，ISO 8601）。</summary>
        public string StartedAtUtc { get; set; }

        /// <summary>本次实际运行的程序集清单（安全过滤后）。</summary>
        public List<string> Assemblies { get; set; }

        /// <summary>被安全过滤排除的程序集清单（当前仅 Unity.Pipeline.Tests*）。</summary>
        public List<string> ExcludedAssemblies { get; set; }

        /// <summary>写上下文文件（覆盖写）。IO 异常向上抛出，由调用方决定是否中止运行。</summary>
        public static void Write(AxuTestRunContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var fullPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.ContextFilePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(fullPath, JsonConvert.SerializeObject(context, Formatting.Indented));
        }

        /// <summary>读上下文文件；不存在或解析失败返回 null（不抛异常——报告器在无 axu_test_run 发起的运行中也要能工作）。</summary>
        public static AxuTestRunContext TryRead()
        {
            var fullPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.ContextFilePath);
            try
            {
                if (!File.Exists(fullPath))
                    return null;
                return JsonConvert.DeserializeObject<AxuTestRunContext>(File.ReadAllText(fullPath));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>删除上下文文件（不存在时静默）。</summary>
        public static void Delete()
        {
            try
            {
                var fullPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.ContextFilePath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception)
            {
                // 删除失败不影响主流程（下次启动会被覆盖写）
            }
        }
    }
}
