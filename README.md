# Hot Update Server

ARGuide 项目的热更新后端服务器，用于托管和管理 Unity AssetBundles 资源文件，实现移动端 AR 应用的动态内容更新。

## 项目简介

这是一个基于 ASP.NET Core 的轻量级静态文件服务器，专门为 ARGuide AR 应用提供热更新支持。通过该服务器，可以动态更新 Unity AssetBundles，无需重新发布应用即可更新 3D 模型、场景和其他资源。

## 核心功能

### 1. 静态文件托管
- 托管 Unity AssetBundles 及其 manifest 文件
- 支持自定义 MIME 类型（如 `.hash` 文件）
- 自动处理未知文件类型

### 2. 文件上传 API
- **接口地址**: `POST /api/upload`
- **功能**: 支持上传新的 AssetBundles 到服务器
- **特性**:
  - 支持大文件上传（最大 500MB）
  - 自动保存到 `wwwroot` 目录
  - 实时日志输出
  - 错误处理和友好提示

### 3. 高性能配置
- Kestrel 服务器优化配置
- 支持大请求体（500MB）
- 延长请求超时时间（10 分钟）
- 优化 Keep-Alive 设置

## 技术栈

- **.NET 10.0**: 最新的 .NET 平台
- **ASP.NET Core**: 高性能 Web 框架
- **Kestrel**: 跨平台 Web 服务器
- **Unity 6000.0.62f1**: AssetBundles 构建版本

## 项目结构

```
hot_update_server/
├── Program.cs              # 主程序入口和配置
├── appsettings.json        # 应用配置
├── hot_update_server.csproj # 项目文件
├── Properties/
│   └── launchSettings.json # 启动配置
└── wwwroot/                # 静态文件目录
    ├── AssetBundles        # Unity AssetBundle 主文件
    ├── AssetBundles.manifest
    ├── node0               # 场景/资源包 0 (~16MB)
    ├── node0.manifest
    ├── node1               # 场景/资源包 1 (~5MB)
    ├── node1.manifest
    ├── node2               # 场景/资源包 2 (~37MB)
    └── node2.manifest
```

## 快速开始

### 环境要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows / Linux / macOS

### 安装运行

1. **克隆项目**
```bash
git clone https://github.com/eneisyou/hot_update_server.git
cd hot_update_server
```

2. **运行服务器**
```bash
dotnet run
```

3. **访问服务器**
- HTTP: `http://localhost:5177`
- 状态检查: `http://localhost:5177/`（返回 "Server is running."）

### 配置说明

#### 修改端口

编辑 `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://0.0.0.0:5177"
    }
  }
}
```

#### 调整上传限制

编辑 `Program.cs` 中的配置:

```csharp
options.MultipartBodyLengthLimit = 524288000; // 500MB，可根据需要调整
```

## API 文档

### 1. 健康检查

**接口**: `GET /`

**响应**:
```
Server is running.
```

### 2. 文件上传

**接口**: `POST /api/upload`

**请求**:
- Content-Type: `multipart/form-data`
- 参数: `file` (文件字段)

**响应**:

成功 (200):
```json
{
  "message": "上传成功",
  "fileName": "node0",
  "size": 16577413
}
```

失败 (400/500):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred",
  "status": 500,
  "detail": "上传失败: ..."
}
```

**示例请求**:

使用 curl:
```bash
curl -X POST http://localhost:5177/api/upload \
  -F "file=@node0"
```

使用 PowerShell:
```powershell
$form = @{
    file = Get-Item -Path "node0"
}
Invoke-RestMethod -Uri "http://localhost:5177/api/upload" -Method Post -Form $form
```

### 3. 下载资源

**接口**: `GET /{filename}`

**示例**:
```bash
# 下载 AssetBundle
curl http://localhost:5177/node0 -o node0

# 下载 manifest
curl http://localhost:5177/node0.manifest -o node0.manifest
```

## AssetBundles 信息

当前托管的 Unity AssetBundles:

| Bundle 名称 | 大小 | 说明 |
|------------|------|------|
| AssetBundles | 1.1 KB | 主 manifest 文件 |
| node0 | 16.6 MB | 场景/资源包 0 |
| node1 | 5.1 MB | 场景/资源包 1 |
| node2 | 37.2 MB | 场景/资源包 2 |

**Unity 版本**: 6000.0.62f1

## 部署建议

### 开发环境
```bash
dotnet run
```

### 生产环境

1. **发布应用**
```bash
dotnet publish -c Release -o ./publish
```

2. **运行发布版本**
```bash
cd publish
dotnet hot_update_server.dll
```

3. **使用 systemd（Linux）**

创建服务文件 `/etc/systemd/system/hot-update-server.service`:

```ini
[Unit]
Description=Hot Update Server for ARGuide
After=network.target

[Service]
Type=notify
WorkingDirectory=/path/to/publish
ExecStart=/usr/bin/dotnet /path/to/publish/hot_update_server.dll
Restart=always
RestartSec=10
SyslogIdentifier=hot-update-server
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

启动服务:
```bash
sudo systemctl enable hot-update-server
sudo systemctl start hot-update-server
```

### 使用 Nginx 反向代理

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:5177;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # 大文件上传支持
        client_max_body_size 500M;
        proxy_read_timeout 600s;
        proxy_send_timeout 600s;
    }
}
```

## 安全建议

1. **生产环境配置**:
   - 禁用 `ServeUnknownFileTypes`
   - 添加身份验证到上传接口
   - 使用 HTTPS
   - 限制允许的文件类型

2. **文件验证**:
   - 验证上传文件的扩展名
   - 检查文件大小
   - 验证文件内容

3. **访问控制**:
   - 为上传接口添加 API Key
   - 使用 IP 白名单
   - 实现速率限制

## 相关项目

- **ARGuide**: 主 AR 应用项目（Unity）
- **AssetBundle 构建工具**: Unity Editor 扩展

## 许可证

MIT License

## 联系方式

- GitHub: [@eneisyou](https://github.com/eneisyou)
- Project: [hot_update_server](https://github.com/eneisyou/hot_update_server)

## 更新日志

### v1.0.0 (2025-12-09)
- 初始版本发布
- 支持静态文件托管
- 实现文件上传 API
- 配置大文件上传支持（500MB）
- 添加自定义 MIME 类型支持
