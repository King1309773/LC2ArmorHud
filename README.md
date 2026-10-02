# 失落城堡2·盔甲叠层 HUD

面向《失落城堡2》BepInEx 6 IL2CPP 的局内 HUD 模组。它只读取已有的装备与叠层计数，不修改战斗逻辑、存档或游戏资源。

## 功能

- 仅在装备对应盔甲时显示 HUD。
- 恶魔君王甲壳显示魔能进度 `x/60`。
- 兽面吞头连环铠、黑魔法师长袍、守护者白袍、红石法师长袍显示叠层进度与紧凑倒计时。
- 屠魔者重甲显示攻击力百分比进度，不显示倒计时。
- 局内按 `F5` 可按盔甲分别调整启用、文字、倒计时、位置、尺寸与透明度。
- 倒计时仅从模组观测到计数归零后的后续叠层开始记录，避免为初始未知层数显示不准确的秒数。

## 安装

1. 安装与游戏版本匹配的 BepInEx 6 IL2CPP。
2. 从 [Releases](https://github.com/King1309773/LC2ArmorHud/releases) 下载 `LC2ArmorHud.dll`。
3. 将 DLL 放入：

   ```text
   Lost Castle 2/BepInEx/plugins/LC2ArmorHud/LC2ArmorHud.dll
   ```

4. 启动游戏；配置文件会生成在：

   ```text
   BepInEx/config/LC2ArmorHud/demon-shell-hud.json
   ```

## 使用

局内按 `F5` 打开配置面板。选择对应盔甲后，可用鼠标或方向键调整参数；限时叠层盔甲会额外显示 `倒计时` 开关。

| 盔甲 | HUD 显示 | 倒计时 |
| --- | --- | --- |
| 恶魔君王甲壳 | 魔能 `x/60` | 无 |
| 兽面吞头连环铠 | 生命值、暴击率 | 15 秒刷新 |
| 屠魔者重甲 | 攻击力 | 无 |
| 黑魔法师长袍 | 技能强度 | 独立 10 秒 |
| 守护者白袍 | 技能强度 | 独立 15 秒 |
| 红石法师长袍 | 技能强度 | 独立 10 秒 |

## 从源码构建

需要已安装的《失落城堡2》与 BepInEx 6 IL2CPP，以提供编译所需的互操作程序集。

```powershell
dotnet run --project .\LC2RuntimeProbe.Tests\LC2RuntimeProbe.Tests.csproj -c Release
dotnet run --project .\LC2ArmorHud.Tests\LC2ArmorHud.Tests.csproj -c Release
dotnet build .\LC2ArmorHud\LC2ArmorHud.csproj -c Release
```

若游戏不在默认路径，构建时传入 `GameRoot`：

```powershell
dotnet build .\LC2ArmorHud\LC2ArmorHud.csproj -c Release -p:GameRoot="X:\SteamLibrary\steamapps\common\Lost Castle 2"
```

## 声明

本项目不含任何游戏资源，与 Hunter Studio 或 BepInEx 团队无关联。
