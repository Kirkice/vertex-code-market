# AssetStudio MCP Server

一个独立的 MCP (Model Context Protocol) Server 插件，为 AssetStudio 提供 AI 大模型交互能力。通过 stdio 传输协议，让大模型能够加载、浏览、搜索、分析和导出 Unity 资产文件。

## 快速开始

### 1. 配置 MCP 客户端

在 AgentProject (Vertex Code) 的 MCP 设置中添加：

```json
{
  "mcpServers": {
    "asset-studio": {
      "type": "stdio",
      "command": "H:\\Project\\asset_studio_mcp\\AssetStudio.McpServer.exe",
      "args": []
    }
  }
}
```

在 Claude Desktop 的 `claude_desktop_config.json` 中添加：

```json
{
  "mcpServers": {
    "asset-studio": {
      "command": "H:\\Project\\asset_studio_mcp\\AssetStudio.McpServer.exe",
      "args": []
    }
  }
}
```

### 2. 使用

配置完成后，大模型会自动发现所有 22 个工具。直接用自然语言描述你的需求即可。

---

## 完整 Tool 列表（22个）

### 基础工具（8个）

| Tool | 功能 | 参数 |
|------|------|------|
| `load_assets` | 加载 Unity 资产文件或文件夹 | `paths: string[]` |
| `load_apk` | 从 APK 文件中提取并加载 Unity 资产 | `apkPath: string`, `subDirectory?: string` |
| `list_assets` | 列出已加载的资产 | `type?: string`, `includeAll?: bool` |
| `get_asset_info` | 获取指定资产的详细信息 | `pathId: long`, `sourceFile: string` |
| `search_assets` | 按名称搜索资产 | `query: string`, `type?: string`, `maxResults?: int` |
| `export_asset` | 导出资产到文件 | `pathId: long`, `sourceFile: string`, `outputPath: string`, `format?: string` |
| `extract_bundle` | 解压 AssetBundle/WebFile | `inputPath: string`, `outputPath: string` |
| `get_type_tree` | 获取资产的 TypeTree 序列化结构 | `pathId: long`, `sourceFile: string` |

### 图形/性能分析工具（10个）

| Tool | 功能 | 参数 |
|------|------|------|
| `analyze_shader` | 解析 Shader 的 SubShader/Pass/Tags/渲染管线/GPU 程序类型 | `pathId: long`, `sourceFile: string` |
| `analyze_texture` | 分析纹理格式/尺寸/MipMap/VRAM 占用，识别性能问题 | `pathId: long`, `sourceFile: string` |
| `analyze_mesh` | 分析 Mesh 顶点数/三角形数/UV 通道/骨骼/BlendShape | `pathId: long`, `sourceFile: string` |
| `analyze_animation` | 分析 AnimationClip 的时长/帧数/曲线数/压缩方式 | `pathId: long`, `sourceFile: string` |
| `get_material_info` | 获取 Material 的 Shader 引用/纹理绑定/属性 | `pathId: long`, `sourceFile: string` |
| `get_renderer_info` | 获取 Renderer 的材质/阴影/合批信息 | `pathId: long`, `sourceFile: string` |
| `get_hierarchy` | 获取 GameObject 场景层级树 | `maxDepth?: int`, `sourceFile?: string` |
| `get_asset_stats` | 统计资产类型分布、数量、大小 | 无参数 |
| `get_memory_profile` | 估算所有资产的运行时内存占用 | 无参数 |
| `batch_texture_report` | 批量生成纹理性能审计报告 | `maxDetails?: int` |

### 高级工具（4个）

| Tool | 功能 | 参数 |
|------|------|------|
| `decompile_shader` | 反编译 Shader 为可读源码 | `pathId: long`, `sourceFile: string` |
| `get_bundle_dependencies` | 分析 AssetBundle 依赖关系 | 无参数 |
| `dump_monobehaviour` | 反序列化 MonoBehaviour 为 JSON | `pathId: long`, `sourceFile: string`, `assemblyDir?: string` |
| `clear_assets` | 清空已加载资产，释放内存 | 无参数 |

---

## 使用示例

### 示例 1：分析本地 Bundle

```
用户：帮我分析 H:/MyGame/Bundles/characters.bundle 里的资产

大模型自动调用：
1. load_assets(paths: ["H:/MyGame/Bundles/characters.bundle"])
2. get_asset_stats()
3. list_assets(type: "Texture2D")
4. analyze_texture(pathId: 12345, sourceFile: "characters.bundle")
```

### 示例 2：分析 APK

```
用户：分析 H:/games/mygame.apk 的纹理性能

大模型自动调用：
1. load_apk(apkPath: "H:/games/mygame.apk")
2. batch_texture_report()
3. get_memory_profile()
```

### 示例 3：逆向 Shader

```
用户：帮我看看这个 Bundle 里的 Shader 是怎么实现的

大模型自动调用：
1. load_assets(paths: ["H:/game/data.bundle"])
2. list_assets(type: "Shader")
3. analyze_shader(pathId: 67890, sourceFile: "data.bundle")
4. decompile_shader(pathId: 67890, sourceFile: "data.bundle")
```

### 示例 4：导出资产

```
用户：把 Bundle 里所有纹理导出到 H:/export 目录

大模型自动调用：
1. load_assets(paths: ["H:/game/data.bundle"])
2. list_assets(type: "Texture2D")
3. export_asset(pathId: 111, sourceFile: "data.bundle", outputPath: "H:/export", format: "png")
4. export_asset(pathId: 222, sourceFile: "data.bundle", outputPath: "H:/export", format: "png")
...
```

### 示例 5：场景结构分析

```
用户：分析这个 Bundle 的场景层级结构

大模型自动调用：
1. load_assets(paths: ["H:/game/scene.bundle"])
2. get_hierarchy(maxDepth: 5)
3. get_asset_stats()
```

### 示例 6：内存优化分析

```
用户：帮我找出这个 Bundle 里最占内存的资产

大模型自动调用：
1. load_assets(paths: ["H:/game/data.bundle"])
2. get_memory_profile()
3. batch_texture_report()
```

---

## 支持的资产导出类型

| 资产类型 | 导出格式 |
|----------|----------|
| Texture2D | png, jpeg, bmp, tga |
| Sprite | png, jpeg, bmp, tga |
| AudioClip | wav（或原始格式） |
| Mesh | obj |
| Shader | .shader |
| TextAsset | .txt |
| MonoBehaviour | .json |
| Font | .ttf / .otf |
| VideoClip | 原始格式 |
| MovieTexture | .ogv |
| Animator | .fbx（需要 FBX SDK 原生库） |
| 其他 | .dat（原始二进制） |

---

## 原生 DLL 说明

本 MCP Server 依赖两个 C++ 原生库：

| DLL | 功能 | 影响的工具 |
|-----|------|-----------|
| `Texture2DDecoderNative.dll` | 解码 DXT/ASTC/ETC/PVRTC 等压缩纹理 | `export_asset`（纹理导出）、`analyze_texture` |
| `AssetStudioFBXNative.dll` | FBX 模型导出 | `export_asset`（Animator 导出为 FBX） |

### 获取方式

这两个 DLL 需要从 AssetStudio 的 C++ 项目编译获得：

1. 安装 [Visual Studio 2022](https://visualstudio.microsoft.com/) 并勾选 **C++ 桌面开发** 工作负载
2. 安装 [FBX SDK 2020.2.1](https://www.autodesk.com/developer-network/platform-technologies/fbx-sdk-2020-2-1)
3. 用 Visual Studio 打开 `H:\Project\AssetStudio\AssetStudio.sln`
4. 选择 **Release | x64** 配置，编译整个解决方案
5. 将以下文件复制到本目录：
   - `AssetStudioFBXNative\bin\x64\Release\AssetStudioFBXNative.dll`
   - `Texture2DDecoderNative\bin\x64\Release\Texture2DDecoderNative.dll`

或者从 [AssetStudio GitHub Releases](https://github.com/Perfare/AssetStudio/releases) 下载预编译版本，提取其中的原生 DLL。

### 没有原生 DLL 时的影响

- **20 个工具正常工作**：加载、列表、搜索、分析、统计等所有非导出功能完全不受影响
- **纹理导出受限**：`export_asset` 导出纹理时，如果纹理使用压缩格式（DXT/ASTC/ETC），需要原生解码库
- **FBX 导出不可用**：`export_asset` 导出 Animator 为 FBX 格式需要原生 FBX 库

---

## 重新构建

如果需要重新构建 MCP Server：

```bash
cd H:\Project\AssetStudio.McpServer
dotnet build -c Release
```

构建产物位于 `bin\Release\net6.0\`，将所有文件复制到本目录即可。

---

## 注意事项

1. **内存管理**：加载大型 Bundle 会占用较多内存，分析完一个 Bundle 后建议调用 `clear_assets()` 再加载下一个
2. **日志输出**：所有日志输出到 stderr，不会干扰 MCP 协议的 stdout 通信
3. **MonoBehaviour 反序列化**：`dump_monobehaviour` 需要提供游戏的 `Managed` 文件夹路径才能完整解析自定义脚本数据
4. **Unity 版本支持**：支持 Unity 3.4 - 2022.1
