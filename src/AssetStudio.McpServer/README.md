# AssetStudio MCP Server

一个独立的 MCP (Model Context Protocol) Server 插件，为 AssetStudio 提供 AI 大模型交互能力。通过 stdio 传输协议，让大模型能够加载、浏览、搜索和导出 Unity 资产文件。

## 功能

| Tool | 功能 |
|------|------|
| `load_assets` | 加载 Unity 资产文件或文件夹 |
| `list_assets` | 列出已加载的资产（支持按类型过滤） |
| `get_asset_info` | 获取指定资产的详细信息 |
| `search_assets` | 按名称或类型搜索资产 |
| `export_asset` | 导出资产到文件（支持 Texture2D、AudioClip、Mesh、Shader 等） |
| `extract_bundle` | 解压 AssetBundle 到目录 |
| `get_type_tree` | 获取资产的 TypeTree 序列化结构 |

## 支持的资产类型

- **Texture2D** → png, jpeg, bmp, tga
- **Sprite** → png, jpeg, bmp, tga
- **AudioClip** → wav (或原始格式)
- **Mesh** → obj
- **Shader** → .shader
- **TextAsset** → .txt
- **MonoBehaviour** → .json
- **Font** → .ttf / .otf
- **VideoClip** → 原始格式
- **MovieTexture** → .ogv
- **Animator** → .fbx (需要 FBX SDK 原生库)

## 构建

### 前置条件

- .NET 6.0 SDK
- AssetStudio 源码（本项目通过 ProjectReference 引用）

### 构建命令

```bash
cd H:\Project\AssetStudio.McpServer
dotnet build -c Release
```

## 配置

### 在 AgentProject (Vertex Code) 中配置

在 MCP 设置中添加以下配置：

```json
{
  "mcpServers": {
    "asset-studio": {
      "type": "stdio",
      "command": "H:\\Project\\AssetStudio.McpServer\\bin\\Release\\net6.0\\AssetStudio.McpServer.exe",
      "args": []
    }
  }
}
```

### 在其他 MCP 客户端中配置

任何支持 MCP stdio 传输的客户端都可以使用，例如 Claude Desktop：

```json
{
  "mcpServers": {
    "asset-studio": {
      "command": "H:\\Project\\AssetStudio.McpServer\\bin\\Release\\net6.0\\AssetStudio.McpServer.exe",
      "args": []
    }
  }
}
```

## 使用示例

### 1. 加载资产

```
load_assets(paths: ["H:/UnityGame/AssetBundles/characters.bundle"])
```

### 2. 列出所有纹理

```
list_assets(type: "Texture2D")
```

### 3. 搜索资产

```
search_assets(query: "hero", type: "Sprite")
```

### 4. 导出资产

```
export_asset(pathId: 12345, sourceFile: "characters.bundle", outputPath: "H:/export", format: "png")
```

### 5. 解压 Bundle

```
extract_bundle(inputPath: "H:/game/data.bundle", outputPath: "H:/extracted")
```

## 项目结构

```
AssetStudio.McpServer/
├── AssetStudio.McpServer.csproj   # 项目文件
├── Program.cs                      # 入口点，MCP Server 初始化
├── Services/
│   ├── AssetManagerService.cs      # 资产管理服务（封装 AssetsManager）
│   └── ExportService.cs            # 导出服务（封装各类资产导出逻辑）
├── Tools/
│   ├── LoadAssetsTool.cs           # load_assets 工具
│   ├── ListAssetsTool.cs           # list_assets 工具
│   ├── GetAssetInfoTool.cs         # get_asset_info 工具
│   ├── SearchAssetsTool.cs         # search_assets 工具
│   ├── ExportAssetTool.cs          # export_asset 工具
│   ├── ExtractBundleTool.cs        # extract_bundle 工具
│   └── GetTypeTreeTool.cs          # get_type_tree 工具
└── README.md
```

## 架构

本项目是一个**独立插件**，不修改 AssetStudio 源码。通过 `ProjectReference` 引用 AssetStudio 核心库：

- `AssetStudio` — 资产解析核心
- `AssetStudioUtility` — 资产转换工具

MCP Server 使用 [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) 实现 stdio 传输。

## 许可证

与 AssetStudio 保持一致。
