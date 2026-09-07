# NJUPT-WebAutoLogin

南京邮电大学校园网自动登录工具

## 简介

NJUPT-WebAutoLogin 是一个用于南京邮电大学校园网 Web 认证门户的自动登录工具。无需每次手动打开浏览器输入账号密码，运行即可自动完成认证。

该工具使用IP直接认证，避免VPN导致的认证失败。

## 功能特性

- 自动检测登录状态，已登录则跳过
- 记住账号密码，下次自动登录
- 支持多运营商选择（校园网/电信/联通/移动）
- 支持交互式和非交互式模式
- 原生 AOT 编译，启动速度快

## 环境要求

- .NET 10.0 SDK 或更高版本
- 连接至南京邮电大学校园网

## 安装与运行

### 从源码构建

```bash
# 克隆仓库
git clone https://github.com/HKLab/NJUPT-WebAutoLogin.git
cd NJUPT-WebAutoLogin

# 运行
dotnet run --project AutoLogin/AutoLogin.csproj

# 发布为原生 AOT 可执行文件（推荐）
dotnet publish AutoLogin/AutoLogin.csproj -c Release -r win-x64
```

### 直接运行

```bash
AutoLogin
```

## 使用说明

### 首次运行

1. 运行程序后，选择网络运营商：

   | 选项 | 运营商 | 账号后缀 |
   |------|--------|----------|
   | 1 | 校园网（收费） | 无 |
   | 2 | 中国电信 | `@njxy` |
   | 3 | 中国联通 | `@njlt` |
   | 4 | 中国移动 | `@njyd` |

2. 输入学号（以 `B` 开头）
3. 输入智慧校园密码
4. 程序自动完成登录

### 后续运行

凭据保存在 `%LOCALAPPDATA%/njupt_webautologin/` 目录下，后续运行将自动使用已保存的凭据。

## 配置文件

配置文件存储路径：`%LOCALAPPDATA%/njupt_webautologin/`

```
njupt_webautologin/
├── userName.txt    # 学号
├── userPwd.txt     # 智慧校园密码
└── network.txt     # 网络运营商后缀
```

## 开机自启动（可选）

### 方法一：启动文件夹

按 `Win + R`，输入 `shell:startup`，将程序快捷方式放入打开的文件夹。

### 方法二：注册表

```csharp
using Microsoft.Win32;

string appName = "NJUPT-WebAutoLogin";
string appPath = "C:\\path\\to\\AutoLogin.exe";

using var key = Registry.CurrentUser.OpenSubKey(
    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
key.SetValue(appName, $"\"{appPath}\" --no-interact");
```

## 安全提示

> **警告**：凭据以明文形式存储在本地磁盘。请勿将配置文件分享给他人，也不要将 `%LOCALAPPDATA%/njupt_webautologin/` 目录提交到版本控制系统。

## 许可证

本项目基于 [GNU General Public License v3.0](LICENSE) 开源。
