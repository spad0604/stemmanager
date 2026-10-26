# 04. Kiến trúc và cấu trúc solution

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).

Kiến trúc modular monolith trong cùng một tiến trình; giao diện Avalonia → Application services → Domain model → EF Core/SQLite; AI adapters và LLM adapters chạy bất đồng bộ, tách biệt khỏi nghiệp vụ lõi. Không bắt buộc server HTTP.

STEMManager.sln

src/

STEMManager.Desktop/       # Avalonia: Views, ViewModels, Themes, Navigation

STEMManager.Application/   # Use cases, DTO, validation, permission policies

STEMManager.Domain/        # Entities, value objects, state machine, rules

STEMManager.Infrastructure/# EF Core, SQLite, files, backup, QR, exports

STEMManager.AI/            # ONNX inferencing, analytics, LLM adapter

STEMManager.Contracts/     # Shared enums/results/interfaces

tests/

STEMManager.UnitTests/

STEMManager.IntegrationTests/

STEMManager.UITests/

deploy/windows/             # Build, signing, installer scripts

docs/                       # README, user guide, privacy, ADR


## 4.1. Quy ước lập trình

- MVVM thuần; dùng CommunityToolkit.Mvvm; ViewModel không trực tiếp truy vấn DbContext.
- Dependency Injection và Microsoft.Extensions.Hosting, Serilog/logging, FluentValidation hoặc validators tự viết.
- Không chứa API key/secret trong source; LLM key lưu qua Windows DPAPI hoặc OS secret store, UI cho phép xóa và kiểm tra kết nối.
- Async/await cho I/O; cancellation, timeout, không khóa UI khi import Excel, làm báo cáo hay chạy inference.
- Tất cả tiền tố timestamp lưu UTC, hiển thị theo giờ địa phương; định dạng ngày dd/MM/yyyy, giờ 24h; đơn vị đo và ngôn ngữ tiếng Việt.
- Mọi migration được thử trên bản sao dữ liệu; SQLite bật foreign_keys, WAL (trên ổ local), busy_timeout; backup qua SQLite Online Backup API.

## 4.2. Phụ thuộc đề xuất


| Nhu cầu | Giải pháp |
| --- | --- |
| UI và MVVM | Avalonia UI + CommunityToolkit.Mvvm |
| Dữ liệu | Microsoft.EntityFrameworkCore.Sqlite |
| Excel | ClosedXML hoặc thư viện được kiểm tra giấy phép |
| QR | QRCoder hoặc ZXing.Net |
| Biểu đồ | LiveCharts2 / ScottPlot tích hợp Avalonia |
| Ảnh & camera | Thư viện camera hỗ trợ Windows và license phù hợp; adapter có thể thay thế |
| AI local | Microsoft.ML.OnnxRuntime, models do dự án cung cấp và kiểm tra license |
| Log / file export | Serilog, QuestPDF hoặc giải pháp tương đương có kiểm tra giấy phép |
