# DataSenderPlus (SmartDataSync)

> **Version:** 9.3.1.0 | **Framework:** .NET Framework 4.7.2 | **Type:** Windows Desktop Application (WPF)

<div dir="rtl">

## 📌 معرفی

**DataSenderPlus** (با نام تجاری SmartDataSync) یک اپلیکیشن دسکتاپ ویندوز است که فرآیند استخراج داده از دیتابیس SQL Server و آپلود آن روی سرورهای FTP را به‌صورت زمان‌بندی‌شده و خودکار انجام می‌دهد. این برنامه تا ۵ کانال مستقل ارسال داده را پشتیبانی می‌کند که هر کدام تنظیمات، بازه‌ی زمانی، فرمت خروجی و مقصد FTP مخصوص به خود را دارند.

</div>

---

## 🏗️ Project Structure

```
DataSenderPlus/
├── DataSenderPlusProject/          # Main WPF Application
│   ├── Models/                     # Data models & business logic
│   │   ├── DataSenderPlus.cs       # Persian (Jalali) date helper
│   │   ├── C_JasonFile.cs          # DB connection, FTP upload, JSON creation
│   │   ├── FtpConfigurationModel.cs
│   │   └── Logger.cs               # File-based logging
│   ├── Helpers/
│   │   ├── ExportHelper.cs         # Export: JSON, CSV, XML, XLSX
│   │   └── SmsHelper.cs            # SMS alerts via Kavenegar API
│   ├── Security/
│   │   ├── CryptoHelper.cs         # AES-256 encrypt/decrypt
│   │   ├── HardwareInfo.cs         # WMI hardware fingerprint
│   │   ├── LicenseManager.cs       # License file management
│   │   └── LicenseModel.cs         # License data model
│   ├── App.xaml / App.xaml.cs      # Application entry point
│   ├── MainWindow.xaml/.cs         # Main UI (900+ lines)
│   ├── ActivationWindow.xaml/.cs   # Hardware activation UI
│   └── Fonts/                      # Persian B Nazanin fonts
│
├── LicenseGenerator/               # License Generator Tool (WPF)
│   ├── MainWindow.xaml/.cs
│   └── Models/
│       ├── CryptoHelper.cs
│       └── LicenseModel.cs
│
├── RainProcessWatcher/             # Windows Service - Process Watchdog
│   ├── Program.cs                  # Service entry point
│   ├── ProcessWatcherService.cs    # Timer-based process monitoring
│   └── ProjectInstaller.cs         # Service installer
│
└── DataSenderPlusProject.sln       # Visual Studio Solution
```

---

## ⚙️ Core Features

### 🔄 Automated Data Export (5 Independent Channels)

Each channel (ConfigID 1-5) performs the following workflow:

1. **Read Configuration** — Reads FTP and export settings from `FtpConfiguration` DB table
2. **Execute Stored Procedure** — Looks up the SP name from `SenderType` table and executes it
3. **Log to Database** — Writes an entry to `LogRegisteration` table
4. **Export Data** — Saves the result to `ExportFile/` folder in one of these formats:

| Format | Type ID | Library |
|--------|---------|---------|
| JSON | 1 | Newtonsoft.Json |
| CSV | 2 | Built-in |
| XML | 3 | Built-in |
| Excel (.xlsx) | 4 | ClosedXML |

5. **FTP Upload** — If `SendFlag` is enabled, uploads the file to the configured FTP server
6. **Error Handling** — Logs errors locally and sends SMS alerts via Kavenegar API

### 🖥️ User Interface (Persian / Farsi)
- 5 tabs, each controlling one export channel
- Right-to-left (RTL) layout with B Nazanin Persian font
- Countdown timer showing next execution cycle
- **Start** button: begins automatic periodic sending
- **Test Connection** button: runs export/upload once immediately
- **Service Mode**: Auto-starts on launch if enabled in DB config

### 🔐 License & Activation System
- Hardware-based licensing (SHA256 fingerprint from CPU + Motherboard + Disk serials)
- AES-256 encrypted license file (`config.cache`)
- License validation at startup (checks active flag and expiry date)
- Companion **LicenseGenerator** tool for creating license files

### 🛡️ Process Watchdog (RainProcessWatcher)
- Windows Service that monitors the main application process
- Automatically restarts the app if it stops or crashes
- Configurable via `watcher.config` (process name, EXE path, check interval)
- Daily log files: `WatcherLog_YYYYMMDD.txt`

---

## 📦 NuGet Dependencies

### Main Application (DataSenderPlusProject)

| Package | Version | Purpose |
|---------|---------|---------|
| ClosedXML | 0.105.0 | Excel (.xlsx) export |
| Newtonsoft.Json | 13.0.3 | JSON serialization |
| DocumentFormat.OpenXml | 3.1.1 | Excel file format |
| Kavenegar | 1.2.4 | SMS API integration |

### LicenseGenerator
| Package | Version |
|---------|---------|
| Newtonsoft.Json | 13.0.3 |

### RainProcessWatcher
> No external dependencies — uses only .NET Framework built-in libraries

---

## 🗄️ Database Schema

The application interacts with a SQL Server database containing these key tables:

| Table | Description |
|-------|-------------|
| `FtpConfiguration` | FTP/export settings per channel (address, credentials, interval, format) |
| `SenderType` | Maps ConfigID to stored procedure names |
| `LogRegisteration` | Export execution logs |
| `SystemConfiguration` | Global settings (service mode, admin mobile for SMS) |

---

## ⚙️ Configuration Files

| File | Location | Purpose |
|------|----------|---------|
| `Config.txt` | EXE directory | SQL Server connection: Server, Database, Username, Password |
| `config.cache` | EXE directory | AES-256 encrypted license file |
| `watcher.config` | RainProcessWatcher dir | Watchdog settings: ProcessName, ExePath, CheckInterval |

---

## 🚀 Getting Started

### Prerequisites
- **Windows** OS
- **.NET Framework 4.7.2** runtime
- **SQL Server** (database with required tables and stored procedures)
- **Visual Studio 2022** (for development/build)

### Build & Run
1. Clone the repository:
   ```bash
   git clone https://github.com/Ashkanhi/DataSenderPlus.git
   ```
2. Open `DataSenderPlusProject.sln` in Visual Studio
3. Restore NuGet packages
4. Place a valid `Config.txt` alongside the output EXE with your SQL Server credentials:
   ```
   YOUR_SERVER
   YOUR_DATABASE
   YOUR_USERNAME
   YOUR_PASSWORD
   ```
5. Build and run

### Install the Watchdog Service
```bash
# Install
installutil.exe RainProcessWatcher.exe

# Start
net start RainProcessWatcher
```

---

## 🏛️ Architecture

```
┌─────────────────────────┐
│   SQL Server Database   │
│   - FtpConfiguration    │
│   - SenderType          │
│   - LogRegisteration    │
│   - SystemConfiguration │
│   - Stored Procedures   │
└───────────┬─────────────┘
            │ ADO.NET (Sync)
            │
┌───────────┴──────────────────────────┐
│                                      │
│  ┌─────────────────┐   ┌───────────┐ │
│  │  DataSenderPlus  │   │  License  │ │
│  │  (WPF WinExe)    │◄──│ Generator │ │
│  │                  │   │  (WPF)    │ │
│  │  • 5 channels    │   └───────────┘ │
│  │  • JSON/CSV/XML/ │                │
│  │    XLSX export   │   ┌───────────┐ │
│  │  • FTP upload    │   │  Process  │ │
│  │  • SMS alerts    │   │  Watcher  │ │
│  │  • File logging  │   │ (Service) │ │
│  └────────┬─────────┘   └───────────┘ │
│           │                           │
└───────────┼───────────────────────────┘
            │ FTP (WebClient)
            ▼
   ┌────────────────┐
   │   FTP Server   │
   └────────────────┘
```

---

## 📝 Technical Notes

- **Database access:** Raw ADO.NET (`SqlConnection`, `SqlCommand`, `SqlDataAdapter`) — no ORM
- **FTP:** Uses legacy `WebClient.UploadFile` (synchronous)
- **No async/await** — all I/O operations are blocking
- **No DI container** — objects are created directly with `new`
- **UI:** Persian/Farsi, right-to-left layout with custom B Nazanin fonts
- **Logging:** Two-channel — file logs (`Logs/YYYY-MM-DD.log`) and DB logs (`LogRegisteration`)
- **Error alerts:** SMS via Kavenegar API to admin mobile number

---

## 📄 License

Proprietary — All rights reserved.