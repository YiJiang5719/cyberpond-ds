# CLAUDE.md — CyberPond 赛博鱼塘 项目工作指引

## 项目简介

CyberPond 是一款基于 GPS 地理位置、现实时间的 2D 休闲养成手游。
引擎：Godot 4.6（C# / .NET），平台：Android。

## 重要：项目标准文档

任何时候需要理解项目时，优先阅读以下文件：

| 文档 | 路径 | 内容 |
|------|------|------|
| 开发需求 | `docs/requirements.md` | 功能需求、数值配置总览 |
| 技术规范 | `docs/technical.md` | 技术栈、目录结构、编码规范、存储格式 |
| 设计规范 | `docs/design.md` | 色彩系统、字体、UI 布局、界面草案 |
| 开发步骤 | `docs/dev-plan.md` | 6 个 Phase 的执行步骤和验证标准 |
| 架构设计 | `docs/architecture.md` | Manager 职责、数据模型、场景树、状态流转 |

## 工作方式

### 开发节奏
- 严格按 `docs/dev-plan.md` 中的 Phase 顺序推进
- 每个 Phase 完成一个功能闭环，验证通过后再进入下一 Phase
- 每个 Phase 内的任务一步一步执行，不跳跃

### 代码规范
- 遵循 `docs/technical.md` 中的编码规范和架构原则
- 保持简洁：不过度抽象、不做功能预留、三个相同逻辑才抽取

### UI 规范
- 所有 UI 遵循 `docs/design.md` 的配色和布局规范
- 主色调淡蓝色系（#E3F2FD / #90CAF9 / #42A5F5 / #1E88E5）

### 配置文件
- 游戏数值全部在 `configs/` 目录的 JSON 文件中
- 修改数值不需要改代码，直接修改 JSON 文件即可

### 开发记录
- 每次开发完成后，在 `dev-logs/` 创建日志文件
- 日志文件命名格式：`YYYY-MM-DD.md`
- 日志内容包含：当日完成事项、当前待办事项、遇到的问题

## 存档文件位置

开发时存档位于 Godot 的 `user://` 路径：
- Windows: `%APPDATA%/Godot/app_userdata/CyberPond/`
- Android: 应用私有目录

## 启动/构建命令

- 在 Godot 编辑器中打开项目：Godot 编辑器 → 导入 → 选择 `project.godot`
- Android 导出：通过 Godot 编辑器 → 项目 → 导出 → Android
