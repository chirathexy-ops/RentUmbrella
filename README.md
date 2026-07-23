# Umbrella Rental System

以 ASP.NET Core MVC 製作的雨傘租借管理系統。系統提供管理者維護雨傘、站點、贊助商與帳號的功能，也提供一般使用者註冊、借傘、還傘及遺失通報。

## 功能

### 管理者

- 管理者登入與登出
- 雨傘新增、查詢、修改、刪除與依站點篩選
- 站點管理
- 贊助商管理
- 帳號管理

### 一般使用者

- 註冊與登入（可使用使用者名稱或 Email）
- 查看可借雨傘
- 借用及歸還雨傘
- 選擇歸還站點
- 遺失雨傘通報

## 技術

- .NET 10 / ASP.NET Core MVC
- Entity Framework Core 10
- SQL Server LocalDB
- Razor Views、Bootstrap、Session

## 快速開始

### 需求

- .NET 10 SDK
- SQL Server LocalDB

### 執行方式

```powershell
dotnet restore .\UmbrellaRentalSystem.slnx
dotnet run --project .\UmbrellaRentalSystem\UmbrellaRentalSystem.csproj
```

第一次執行時，系統會依 `appsettings.json` 的連線字串建立或初始化本機資料庫。

### 預設帳號

| 身分 | 帳號 | 密碼 |
| --- | --- | --- |
| 管理者 | `admin` | `1234` |
| 一般使用者 | `user` | `1234` |

> 這些帳號僅供開發與展示使用；正式部署前應更換密碼，並採用雜湊方式儲存密碼。

## 資料模型

| 模型 | 用途 |
| --- | --- |
| `Account` | 使用者與管理者帳號資料 |
| `Umbrella` | 雨傘編號、狀態、所在站點與贊助商 |
| `Location` | 雨傘站點 |
| `Sponsor` | 贊助商資料 |
| `Transaction` | 借傘與還傘紀錄 |
| `LostReport` | 遺失雨傘通報 |

`UmbrellaId` 是資料庫用的自動遞增主鍵，因此刪除資料後可能跳號；對使用者而言應以 `UmbrellaCode` 作為雨傘識別編號。

## 專案結構與檔案清單

```text
UmbrellaRentalSystem/
├── UmbrellaRentalSystem.slnx                 # Visual Studio solution
├── README.md                                 # 專案說明文件
└── UmbrellaRentalSystem/
    ├── UmbrellaRentalSystem.csproj           # 專案設定與 NuGet 套件
    ├── Program.cs                            # 應用程式進入點、服務與路由設定
    ├── appsettings.json                      # 資料庫連線與應用程式設定
    ├── appsettings.Development.json          # 開發環境設定
    ├── Controllers/
    │   ├── AccountsController.cs             # 帳號、登入、註冊與管理
    │   ├── HomeController.cs                 # 首頁與錯誤頁面
    │   ├── SponsorsController.cs             # 贊助商管理
    │   └── UmbrellasController.cs            # 雨傘、借還與遺失通報
    ├── Data/
    │   ├── ApplicationDbContext.cs           # EF Core DbContext 與資料表集合
    │   └── DbInitializer.cs                  # 資料庫初始化、結構相容處理與種子資料
    ├── Models/
    │   ├── Account.cs                        # 帳號模型
    │   ├── ErrorViewModel.cs                 # 錯誤頁面模型
    │   ├── Location.cs                       # 站點模型
    │   ├── LostReport.cs                     # 遺失通報模型
    │   ├── Sponsor.cs                        # 贊助商模型
    │   ├── Transaction.cs                    # 借還紀錄模型
    │   └── Umbrella.cs                       # 雨傘模型
    ├── Migrations/
    │   ├── 20260615164114_InitialCreatePerfect.cs
    │   ├── 20260615164114_InitialCreatePerfect.Designer.cs
    │   └── ApplicationDbContextModelSnapshot.cs
    ├── Properties/
    │   ├── launchSettings.json               # 本機啟動設定
    │   ├── serviceDependencies.json          # 服務相依設定
    │   └── serviceDependencies.local.json    # 本機服務相依設定
    ├── Areas/Identity/Pages/_ViewStart.cshtml # Identity Razor Pages 共用設定
    ├── Views/
    │   ├── Accounts/                         # 帳號相關頁面
    │   ├── Home/                             # 首頁與隱私權頁面
    │   ├── Locations/                        # 站點 CRUD 頁面與 LocationsController.cs
    │   ├── Shared/                           # 共用版面、驗證與錯誤頁面
    │   ├── Sponsors/                         # 贊助商 CRUD 頁面
    │   ├── Umbrellas/                        # 雨傘、還傘、狀態與遺失通報頁面
    │   ├── _ViewImports.cshtml
    │   └── _ViewStart.cshtml
    └── wwwroot/
        ├── css/site.css                      # 網站樣式
        ├── js/site.js                        # 網站 JavaScript
        ├── favicon.ico
        └── lib/                              # Bootstrap、jQuery 與表單驗證套件
```

## 注意事項

- 目前使用 Session 判斷登入狀態與角色。
- 連線字串位於 `UmbrellaRentalSystem/appsettings.json`，若你的 LocalDB 名稱或環境不同，請先調整該設定。
- `bin/`、`obj/`、`.vs/` 為編譯或 IDE 產生檔，不需要放入 README 的檔案清單，也通常不應提交至版本控制。
