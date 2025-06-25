**English | [中文](README_ZH.md)**

# AutoHPMA - Harry Potter: Magic Awakened Automation Tool

<div align=center>
  <h1 align="center">
  <img src="https://github.com/FelixChristian011226/AutoHPMA/blob/master/AutoHPMA/Assets/hpma.png" width=50%>
  <br/>
  <a href="https://felixchristian011226.github.io/AutoHPMA-Web">AutoHPMA</a>
  </h1>
</div>

<div align=center>
  <img src="https://img.shields.io/badge/build-passing-brightgreen">
  <img src="https://img.shields.io/github/v/release/FelixChristian011226/AutoHPMA">
  <img src="https://img.shields.io/github/license/FelixChristian011226/AutoHPMA">
  <img src="https://img.shields.io/github/downloads/FelixChristian011226/AutoHPMA/total">
  <img src="https://img.shields.io/github/stars/FelixChristian011226/AutoHPMA">
</div>

**AutoHPMA** is a WPF tool developed in C# for the game *Harry Potter: Magic Awakened (HPMA)*, designed to automate various repetitive gameplay tasks for players.

<br/>

## Features

- **Auto Club Quiz**  
  Fully automated quiz answering powered by a state machine. Supports auto-entering club scene, OCR recognition of questions and options, database matching, and accurate answering. Results are pushed via Windows notifications.

- **Auto Forbidden Forest Exploration**  
  Automatically completes a specified number of Forbidden Forest runs. Detects player role (leader/member), likes teammates, and handles the entire process without manual input.

- **Auto Wizard Cooking**  
  Automates multiple recipes. Users can customize or add new recipes, with full automation for dragging ingredients, tools, seasonings, and cooking.

- **Auto Sweet Adventure**  
  Automates the limited-time event "Sweet Adventure", including matchmaking, round progression, and result settlement.

<br/>

## Highlights

- Logging System: Overlays logs on the game window and supports local file logging.
- Overlay Mask Window: Customizable real-time overlay showing match results.
- Hotkey Binding: Bind custom hotkeys to quickly activate each feature.
- Notification System: Native Windows notifications for real-time result feedback.

<br/>

## Installation Guide

### Requirements

- Windows 10 or later
- .NET 8.0 or newer

### Steps

1. Go to the [Releases](https://github.com/FelixChristian011226/AutoHPMA/releases) page to download the latest version.
2. Download and run `AutoHPMA-Setup.exe` to start the installer.
3. Follow the instructions to complete the installation. You can choose the installation path and whether to launch on startup.
4. If prompted about missing .NET runtime on first launch, download it from the [Microsoft .NET website](https://dotnet.microsoft.com/download).

### Launch & Usage

1. Run `AutoHPMA.exe`
2. Configure required parameters as instructed
3. Click the "Start" button on the main page
4. Navigate to additional tabs to enable desired features

For more detailed usage, tutorials, and FAQs, visit the [official website](https://autohpma-web.vercel.app/).

<br/>

## Notes

- **Only compatible with the MuMu emulator**, with a recommended resolution of 1280×720. 1600×900 may cause screenshot issues.
- Set game graphics to the default "Standard" quality. Avoid changing graphical parameters that may affect UI layout.
- Do not minimize the game or click “Show Desktop” during script execution; this may cause overlay issues. If issues arise, try switching windows several times or rebooting the system.
- This is a personal project for learning and sharing purposes. Use at your own risk.

<br/>

## Contribution

Feedback and contributions are welcome! If you have suggestions or wish to contribute, feel free to open an issue or submit a pull request on GitHub.

<br/>

## License

This project is licensed under the [GPL-3.0 License](https://github.com/FelixChristian011226/AutoHPMA/blob/master/LICENSE) – see the LICENSE file for details.

