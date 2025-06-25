
**[English](README.md) | 中文**

# AutoHPMA - 哈利波特：魔法觉醒自动化工具

<div align=center>
  <h1 align="center">
  <img src="https://github.com/FelixChristian011226/AutoHPMA/blob/master/AutoHPMA/Assets/hpma.png" width=50%>
  <br/>
  <a href="https://autohpma-web.vercel.app/">AutoHPMA</a>
  </h1>
</div>

<div align=center>
  <img src="https://img.shields.io/badge/build-passing-brightgreen">
  <img src="https://img.shields.io/github/v/release/FelixChristian011226/AutoHPMA">
  <img src="https://img.shields.io/github/license/FelixChristian011226/AutoHPMA">
  <img src="https://img.shields.io/github/downloads/FelixChristian011226/AutoHPMA/total">
  <img src="https://img.shields.io/github/stars/FelixChristian011226/AutoHPMA">
</div>

**AutoHPMA**：一个基于C#开发的WPF工具，为哈利波特：魔法觉醒（HPMA）游戏设计，旨在为游戏玩家实现一些简单的自动化功能。

<br>

## 功能

- **自动社团答题**  
  状态机驱动的全流程自动答题，支持自动进入社团场景、OCR 识别题目与选项、题库对比精准作答，并通过 Windows 通知反馈结果。

- **自动禁林探索**  
  一键刷取指定次数的禁林探索，自动检测身份（队长/队员），自动点赞队友，流程全自动。

- **自动巫师烹饪**  
  支持多种菜谱的自动烹饪，用户可自定义或添加菜谱配置，自动完成食材、厨具、调料的拖放与烹饪流程。

- **自动甜蜜冒险**  
  针对限时活动“甜蜜冒险”实现自动化，自动匹配、自动推进回合、自动结算。

<br>

## 特性

- 日志系统：游戏窗口覆盖日志显示，同时支持本地日志记录
- 遮罩窗口：可自定义显示的实时遮罩窗口，显示匹配结果
- 热键绑定：用户可自定义热键，通过快捷键快速启用各项功能
- 通知功能：支持原生 Windows 通知，实时推送运行结果

<br>

## 安装说明

### 环境要求

- Windows 10 或更高版本
- .NET 8.0 或以上

### 安装步骤

1. 前往[Releases](https://github.com/YourGitHubUsername/AutoHPMA/releases)获取最新发布版本。
2. 下载并双击 `AutoHPMA-Setup.exe` 启动安装程序。
3. 按提示完成安装，可自定义安装路径和开机启动设置。
4. 若首次启动提示缺少 .NET 运行时，请前往 [Microsoft .NET 官网](https://dotnet.microsoft.com/download) 安装对应版本。

### 启动与基础用法

1. 启动 `AutoHPMA.exe`
2. 按照界面提示配置相关参数
3. 点击启动页面的"启动"按钮
4. 根据需要，前往其余页面启用相关功能

详细使用教程请参阅[使用说明](https://autohpma-web.vercel.app/document/usage.html)。

<br>

## 注意事项

- **仅适配 MuMu 模拟器**，推荐分辨率 1280×720，1600×900 可能会截图异常。
- 请将游戏画质设置为默认“标准”画质，避免更改影响画面的参数。
- 脚本执行期间请勿最小化游戏窗口或点击“显示桌面”，否则可能导致窗口异常置顶。若遇异常弹窗，可尝试多次切屏或重启电脑解决。
- 本工具为个人开发，仅供学习与交流，使用风险自负。

<br>

## 贡献

欢迎反馈和贡献！如果您有改进 AutoHPMA 的建议或希望贡献代码，请随时在 GitHub 页面上提出问题或创建拉取请求。

<br>

## 许可证

本项目根据 [GPL-3.0 许可证](https://github.com/FelixChristian011226/AutoHPMA/blob/master/LICENSE) 授权发布 - 有关详情，请参阅 LICENSE 文件。

---

如需更详细的功能说明、常见问题解答等，请上[官网](https://autohpma-web.vercel.app/)查询相关内容。
