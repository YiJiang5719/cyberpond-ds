# CyberPond 赛博鱼塘 — 技术规范

## 1. 技术栈

| 层次 | 技术选择 |
|------|----------|
| 引擎 | Godot 4.6（4.x 系列最新稳定版） |
| 语言 | C#（使用 Godot .NET 版本） |
| 渲染 | GL Compatibility（兼容移动端 OpenGL ES） |
| 存储 | 本地 JSON 文件写入（`user://` 目录） |
| UI | Godot Control 节点 + 自定义主题 |

## 2. 项目目录结构

```
CyberPond/
├── configs/                  # JSON 配置文件（数值、游戏参数）
│   ├── fish_types.json
│   ├── pond_config.json
│   ├── shop_config.json
│   ├── inventory_config.json
│   ├── game_config.json
│   └── islands.json
├── scripts/                  # C# 源码
│   ├── managers/             # 管理器（单例，控制核心逻辑）
│   │   ├── GameManager.cs    # 游戏主入口 + 游戏状态
│   │   ├── SaveManager.cs    # 存档读写
│   │   ├── EconomyManager.cs # 金币经济
│   │   ├── PondManager.cs    # 鱼塘操作
│   │   ├── IslandManager.cs  # 岛屿发现与访问状态
│   │   └── FishManager.cs    # 鱼类养成
│   ├── data/                 # 数据模型（纯数据结构）
│   │   ├── FishData.cs
│   │   ├── PondData.cs
│   │   └── PlayerData.cs
│   ├── ui/                   # UI 控制器（绑定场景节点）
│   │   ├── MainMapUI.cs
│   │   ├── PondDetailUI.cs
│   │   ├── ShopUI.cs
│   │   ├── InventoryUI.cs
│   │   ├── DiscoverUI.cs
│   │   └── FishingGame.cs
├── scenes/                   # Godot 场景文件 (.tscn)
│   ├── main_map.tscn
│   ├── pond_detail.tscn
│   ├── shop.tscn
│   ├── inventory.tscn
│   └── discover.tscn
├── assets/                   # 资源文件
│   ├── fish/                 # 鱼类美术（PNG/SVG）
│   ├── ui/                   # UI 素材（按钮、面板、图标）
│   └── icons/                # 通用图标
├── dev-logs/                 # 开发日志
├── docs/                     # 项目文档
│   ├── requirements.md       # 需求文档
│   ├── technical.md          # 技术规范（本文件）
│   ├── design.md             # 设计规范
│   ├── dev-plan.md           # 开发执行步骤
│   └── architecture.md       # 架构设计
├── project.godot             # Godot 项目配置
└── CLAUDE.md                 # Claude Code 工作指引
```

## 3. 架构原则

### 3.1 单例管理器模式
- 所有 Manager 类使用 Godot AutoLoad（全局单例）
- 管理器负责核心逻辑，不直接操作 UI
- UI 脚本从管理器读取数据并渲染

### 3.2 数据流方向

```
配置文件(JSON) → Manager(内存) → UI 展示
                             ↑
用户操作 → UI → Manager(处理) → SaveManager(持久化)
```

### 3.3 场景切换
- 使用场景树切换，不使用弹窗叠加（保持简单）
- 主地图为核心常驻场景
- 详情页/商店通过按钮导航切换

## 4. 编码规范

### 4.1 命名约定
- Godot 节点：snake_case（引擎默认）
- C# 类名：PascalCase
- C# 方法名：PascalCase
- C# 字段/属性：PascalCase
- JSON 字段：snake_case

### 4.2 文件组织
- 每个 C# 文件只放一个类
- 场景文件命名：screen_name.tscn（功能描述_用途）
- UI 脚本命名：ScreenNameUI.cs（与场景名对应）

### 4.3 简洁原则
- 函数尽量短小（不超过 30 行）
- 不使用过度抽象和设计模式
- 三个相同逻辑再考虑抽取公共方法
- 不做未计划的功能预留

## 5. 存储格式

### 5.1 存档文件（`user://save_data.json`）
```json
{
  "player": {
    "coins": 100
  },
  "ponds": [
    {
      "id": "uuid",
      "name": "我的鱼塘",
      "lat": 31.2304,
      "lon": 121.4737,
      "fishes": [
        {
          "fish_type": "common_carp",
          "spawned_at": "2026-05-19T10:00:00Z",
          "is_adult": true,
          "uncollected_roe": 4
        }
      ]
    }
  ],
  "inventory": {
    "unlocked_slots": 10,
    "fry": { "common_carp": 2 },
    "roe": { "common_carp": 5 }
  }
}
```

### 5.2 读取方式
- 游戏启动时读取一次，加载到内存
- 关键操作后自动保存（购买、投放、收获）
- 使用 Godot 的 `FileAccess` 类读写

## 6. 地图技术方案

### 6.1 OpenStreetMap 集成
- 使用 `slippy map` 瓦片模式
- 瓦片 URL 模板：`https://tile.openstreetmap.org/{z}/{x}/{y}.png`
- 使用 Godot 的 `HTTPRequest` 节点加载瓦片
- 瓦片拼接显示，缓存已加载瓦片

### 6.2 GPS 定位
- 通过 Godot 引擎的 GPS 相关功能获取经纬度
- 需要 Android 定位权限
- 定位更新频率：地图打开时实时，其他界面关闭

## 7. Android 配置要求

- 最低 Android 版本：API Level 24 (Android 7.0)
- 权限要求：
  - `ACCESS_FINE_LOCATION`（GPS 定位）
  - `INTERNET`（地图瓦片加载）
- 导出配置：`project.godot` 中启用 Android 平台
