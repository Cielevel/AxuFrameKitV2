# HtmlCommunicate - HTML 通信模块

## 功能概述

提供 Unity 与网页之间的通信桥梁，通过本地 HTTP 服务器实现双向数据交互。

## 主要特性

### 本地 Web 服务器
- **HttpListener 实现**：基于 .NET HttpListener 的轻量级服务器
- **默认端口**：8080（可配置）
- **跨域支持**：内置 CORS 处理

### 线程安全设计
- **独立线程**：HTTP 请求在独立线程中处理
- **并发队列**：通过 ConcurrentQueue 将请求传递到主线程
- **主线程执行**：确保 Unity API 在主线程调用

### 通信协议
- **HTTP GET/POST**：支持标准 HTTP 方法
- **JSON 数据**：请求和响应使用 JSON 格式
- **自定义路由**：可扩展的 URL 路由处理

## 使用场景

- Unity 与嵌入式 WebView 通信
- 本地网页工具与 Unity 编辑器交互
- 开发调试时的外部控制接口

## 示例

示例实现了礼物赠送功能，网页发送请求后 Unity 接收并处理。
