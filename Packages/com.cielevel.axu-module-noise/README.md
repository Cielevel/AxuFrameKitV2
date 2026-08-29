# AxuFrameKit Modules - Noise

AxuFrameKit 家族的噪声生成模块，以独立 UPM 包形式提供。**不依赖框架本体**，可单独使用。

## 功能

- **FastNoiseLite**：基于 [Auburn/FastNoiseLite](https://github.com/Auburn/FastNoiseLite)（v1.1.1，MIT）的高性能噪声库，支持 OpenSimplex2、Cellular、Domain Warp、分形等。
- **SimpleNoise**：纯 C# 实现的简易噪声生成器（Perlin / FBM / 白噪声），不依赖 UnityEngine。

## 编辑器窗口

菜单路径：

- `AxuFrameKit/Modules/Noise/Noise Generator` —— SimpleNoise 噪声生成器（分辨率、分形参数、Alpha 剔除、导出 PNG）。
- `AxuFrameKit/Modules/Noise/FastNoiseLite Previewer` —— FastNoiseLite 参数预览器（实时预览、导出 PNG）。

## 安装

将本目录作为嵌入式包放在 `Packages/` 下，或通过 Package Manager 的 "Add package from disk" 指向本包的 `package.json`。

## 第三方声明

本包包含第三方组件 FastNoiseLite（MIT License），详见 [Third Party Notices.md](Third%20Party%20Notices.md)。
