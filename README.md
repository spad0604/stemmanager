# STEM Lab Manager

Desktop quản lý một phòng STEM, offline, C#/.NET 10 + Avalonia 12, SQLite/EF Core. UI và tài liệu tiếng Việt, code tiếng Anh.

```bash
dotnet restore STEMManager.sln
dotnet build STEMManager.sln -c Release
dotnet test STEMManager.sln -c Release --logger trx
dotnet run --project App/STEMLabManager.Desktop -c Release
dotnet run --project App/STEMLabManager.Desktop -c Release -- --demo
```

Lần đầu tạo admin qua wizard; không có mật khẩu mặc định. Demo dùng `LocalApplicationData/STEMManager/Demo`, production dùng `.../Data`. Không có dữ liệu vận hành trong thư mục publish.

| Project | Trách nhiệm |
|---|---|
| Domain | Entities, trạng thái, validation và ma trận quyền |
| Application | DTO và hợp đồng use-case; không phụ thuộc Avalonia/EF |
| Infrastructure | SQLite, migrations, transaction IMMEDIATE, RBAC, audit, ledger, backup, import/export, QR, secrets |
| AI | ONNX YuNet/SFace, camera Windows, cosine candidates, insights local, LLM aggregate |
| Desktop | Composition root DI/Host, XAML, MVVM, forms, wizard, navigation |
| Tests / UITests | SQLite thật, concurrency, quyền, migrations, model inference, Avalonia headless và UI→service→DB |
| Migrations / Tools | Công cụ design-time tách khỏi Release; xuất PDF hướng dẫn và mẫu Excel |

Các file model được copy vào `Models` trong output. YuNet do người dùng cung cấp, SFace bổ sung từ OpenCV Zoo; giấy phép ở `licenses/`. Camera native Windows đóng gói; Ubuntu không giả lập camera nếu chưa có adapter native.

Windows: chạy `deploy/windows/build.ps1` trên máy build có .NET 10 và Inno Setup 6; hoặc workflow `.github/workflows/windows-release.yml`. Bộ cài và portable ở `artifacts/windows`; người dùng không cần SDK/runtime. Publish win-x64 trên Ubuntu không thay thế nghiệm thu Windows sạch.

ONNX Windows cần Microsoft Visual C++ DLL. Script Windows lấy bản x64 được phép phân phối từ Visual Studio C++ build tools (hoặc `STEM_VC_CRT_DIR`) và copy app-local; core vẫn offline, không yêu cầu người dùng cài SDK. Bản cross-publish Ubuntu chưa chứa các CRT này. Xem [Microsoft về app-local deployment](https://learn.microsoft.com/en-us/cpp/windows/choosing-a-deployment-method?view=msvc-170).

Migrations: `dotnet tool install dotnet-ef --version 10.0.0 --tool-path .tools`; tạo schema bằng `.tools/dotnet-ef migrations add Name --project App/STEMLabManager.Infrastructure --startup-project App/STEMLabManager.Migrations`. Factory design-time dùng memory; DB vận hành được app migrate tự động sau snapshot. Hướng dẫn PDF/mẫu Excel: `dotnet run --project App/STEMLabManager.Tools -c Release -- prepare-docs`.

Xem [hướng dẫn sử dụng](docs/USER_GUIDE.md), [quyền riêng tư](docs/PRIVACY.md), [kiểm chứng](docs/VALIDATION.md), [quyết định](STEM_Lab_Manager_Specs/docs/DECISIONS.md).
# stemmanager
