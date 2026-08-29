# AxuIndie - Sprite To Animation

精灵序列帧转动画工具：将选中的 Sprite 序列快速整合为 AnimationClip，并可选创建 Animator Controller（可设为默认状态、循环播放等）。

## 使用

菜单：`AxuIndieModules/Sprite To Animation`

- 选择包含 Sprite 的文件夹或纹理资源，自动收集序列帧
- 可配置保存路径、帧率、Clip 名称、后缀（支持以文件夹名作为后缀）
- 可选创建 Animator Controller 并设为 Layer 默认状态

设置存储在 EditorPrefs（`SpriteToAnim_*` 键），升级迁移不影响原有配置。

## 说明

- 本包为**独立模块**，无任何依赖，仅含 Editor 代码（`Editor` 平台程序集 `AxuIndieModules.SpriteToAnimation`）。
- 本包为**嵌入式包**（Embedded Package），位于 `Packages/` 目录下，可直接修改源码并随项目进行版本管理。
- 样式文件 `CommonWindowStyles.uss` 由窗口按脚本相对路径加载，需与本窗口脚本位于同一目录。
