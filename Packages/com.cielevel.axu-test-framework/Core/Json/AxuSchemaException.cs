using System;

namespace AxuTestFramework.Core.Json
{
    /// <summary>
    /// 场景数据 schema 校验/解析失败异常（消息含来源路径、期望与实际 schema）。
    /// </summary>
    public class AxuSchemaException : Exception
    {
        /// <summary>数据来源（文件路径或 "(inline)"）。</summary>
        public string SourcePath { get; }

        /// <summary>期望的 schema。</summary>
        public string ExpectedSchema { get; }

        /// <summary>实际的 schema（字段缺失时为 "(missing)"）。</summary>
        public string ActualSchema { get; }

        public AxuSchemaException() { }

        public AxuSchemaException(string message) : base(message) { }

        public AxuSchemaException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>schema 不匹配专用构造（消息含路径/期望/实际）。</summary>
        public AxuSchemaException(string sourcePath, string expectedSchema, string actualSchema)
            : base($"场景数据 schema 不匹配：path={sourcePath}，期望 \"{expectedSchema}\"，实际 \"{actualSchema}\"")
        {
            SourcePath = sourcePath;
            ExpectedSchema = expectedSchema;
            ActualSchema = actualSchema;
        }
    }
}
