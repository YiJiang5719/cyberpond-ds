# CyberPond 赛博鱼塘 — 架构设计

## 1. 整体架构

```
┌──────────────────────────────────────────┐
│               UI Layer (Scenes)           │
│  main_map  pond_detail  shop  inventory  │
└──────────────┬───────────────────────────┘
               │ 读取数据 / 触发操作
               ▼
┌──────────────────────────────────────────┐
│           Manager Layer (AutoLoad)        │
│ GameManager  PondManager  FishManager    │
│ EconomyManager  SaveManager              │
└──────────────┬───────────────────────────┘
               │ 读写
               ▼
┌──────────────────────────────────────────┐
│           Data Layer (Models)             │
│ PlayerData  PondData  FishData           │
└──────────────┬───────────────────────────┘
               │ 持久化
               ▼
┌──────────────────────────────────────────┐
│         Storage (user://save_data.json)   │
└──────────────────────────────────────────┘
```

## 2. Manager 职责与接口

### 2.1 GameManager
- 职责：游戏启动入口，初始化所有 Manager，持有全局游戏状态
- 主要接口：
  - `Init()` — 启动时调用，加载配置和存档
  - `Quit()` — 退出前自动存档

### 2.2 SaveManager
- 职责：存档读写，JSON 序列化/反序列化
- 主要接口：
  - `SaveData SaveGame()` — 收集所有 Manager 状态，写入文件
  - `void LoadGame()` — 读取文件，恢复所有 Manager 状态
  - `void AutoSave()` — 关键操作后调用

### 2.3 EconomyManager
- 职责：金币管理，增减检查
- 主要接口：
  - `int GetCoins()` — 查询当前金币
  - `bool SpendCoins(int amount)` — 消费金币，余额不足返回 false
  - `void AddCoins(int amount)` — 增加金币

### 2.4 PondManager
- 职责：鱼塘增删改查，容量管理
- 主要接口：
  - `List<PondData> GetAllPonds()` — 获取所有鱼塘
  - `PondData GetPond(string id)` — 获取单个鱼塘
  - `bool CreatePond(string name, double lat, double lon)` — 创建鱼塘（含金币检查）
  - `int GetUnlockCost()` — 获取下一个鱼塘解锁价格
  - `int GetMaxFishCount(int pondIndex)` — 获取鱼塘最大养鱼数

### 2.6 IslandManager
- 职责：岛屿发现、访问状态、船票消耗
- 主要接口：
  - `bool IsOnIsland` — 当前是否在岛屿上
  - `string CurrentIslandId` — 当前访问的岛屿 ID
  - `Dictionary GetIslandConfig(string id)` — 获取岛屿配置
  - `Array GetAllIslands()` — 获取所有岛屿
  - `bool ConsumeTicket(string id)` — 消耗船票
  - `int GetTicketCount(string id)` — 查询持有船票数
  - `void LeaveIsland()` — 离开岛屿，清除状态

### 2.5 FishManager
- 职责：鱼的状态计算，成长/产籽逻辑
- 主要接口：
  - `bool AddFryToPond(string pondId, string fishType)` — 投放鱼苗
  - `void UpdateFishStates()` — 刷新所有鱼的状态（根据时间戳判断）
  - `int CollectRoe(string pondId)` — 收获鱼塘中所有鱼籽
  - `FishStatus GetStatus(FishData fish)` — 返回单条鱼的状态

## 3. 数据模型

### 3.1 PlayerData
```
class PlayerData {
    int coins;          // 当前金币数
}
```

### 3.2 InventoryData
```
class InventoryData {
    int unlockedSlots;              // 已解锁格子数
    Dictionary<string, int> fry;    // 鱼苗数量（key=fishType）
    Dictionary<string, int> roe;    // 鱼籽数量（key=fishType）
}
```

### 3.3 PondData
```
class PondData {
    string id;          // 唯一标识（Guid）
    string name;        // 自定义名称
    double lat;         // 纬度
    double lon;         // 经度
    List<FishData> fishes;  // 塘中鱼列表
}
```

### 3.4 FishData
```
class FishData {
    string id;              // 唯一标识（Guid）
    string fishType;        // 鱼的类型 key（对应 fish_types.json）
    DateTime spawnedAt;     // 投放时间
    int uncollectedRoe;     // 累积未收获鱼籽数
    DateTime lastRoeTime;   // 上次产籽时间（用于计算日产）
}
```

## 4. 场景树结构

```
Root (Node)
├── GameManager (AutoLoad, Node)
├── SaveManager (AutoLoad, Node)
├── EconomyManager (AutoLoad, Node)
├── PondManager (AutoLoad, Node)
├── InventoryManager (AutoLoad, Node)
├── FishManager (AutoLoad, Node)
├── IslandManager (AutoLoad, Node)
└── MainScene (Control)
    ├── TopBar (Panel - 金币显示)
    ├── ContentArea (Control - 子场景加载)
    │   ├── PondList (鱼塘列表/钓鱼视图)
    │   ├── PondDetail (鱼塘详情)
    │   ├── Discover (岛屿发现)
    │   ├── Shop (商店)
    │   └── Inventory (背包)
    └── BottomNav (Panel - 底部导航：鱼塘/发现/商店/背包)
```

## 5. 状态流转图

### 5.1 鱼的状态
```
[背包鱼苗] ──投放──▶ [成长中] ──到时间──▶ [已成年] ──每天──▶ [产出鱼籽]
                                                  │
                                                  ▼
                                          [鱼籽待收获] ──收获──▶ [背包鱼籽] ──出售──▶ [金币]
```

### 5.2 界面流转
```
[主地图] ──点击鱼塘──▶ [鱼塘详情] ──点击投放──▶ [背包选择鱼苗]
    │                      │
    ├──导航[商店]──▶ [商店]
    └──导航[背包]──▶ [背包]
```
