# Kiểm chứng triển khai — 12/10/2026

Môi trường: Ubuntu 26.04 x64, .NET SDK 10.0.112. Kết quả dưới đây chỉ áp dụng các lệnh đã chạy, không thay thế nghiệm thu Windows/phần cứng thật.

| Kiểm tra | Kết quả |
|---|---|
| Build Release solution | Đạt; 0 lỗi, 0 warning trong lượt build trước publish cuối |
| Unit/integration/model tests | 48/48 đạt trên SQLite file thật, gồm migration DB cũ và payload GPT-5/custom endpoint |
| Avalonia UI tests | 8/8 đạt: setup/login, RBAC navigation, wizard → service → SQLite, tất cả trang với demo, light/dark, kích thước 1366/1600/1920 và DPI 125%/150% |
| BR nghiệp vụ | Unit không mượn kép, giới hạn quantity, rollback nhiều dòng, trả một phần/quá lượng, hỏng/bảo trì/ngừng, lịch trùng/nối tiếp/peak, gia hạn xung đột, tiêu hao có allocation tương lai |
| SQLite constraints/concurrency | Index open-unit được test gọi DB trực tiếp; hai service tranh mượn chỉ một thành công; FK/unique/check constraints trong migrations |
| Backup/restore | Online backup khi DB hoạt động, restore sang thư mục khác giữ khóa/ảnh, backup hỏng không thay active DB, giữ DB rollback, scheduled backup cho manager, restore khóa phiên và tắt Face |
| Import/export | CSV quoted, trùng mã, all-or-nothing, asset/borrower previews, XLSX/PDF/PNG tạo file thật; CSV chống công thức |
| Security | Salted PBKDF2, lockout, đổi mật khẩu, role checks tại service, phiên tài khoản bị vô hiệu hóa, PII viewer ẩn, API key/embedding AES-GCM, consent thu hồi/xóa, log không có embedding |
| ONNX | YuNet supplied nạp và chạy ảnh đen không mặt; SFace chạy tensor thực trả vector 128 chiều; checksum ghi manifest; cosine có unknown |
| Camera movement | Unit test static/wrong direction/movement trên landmark; chưa nghiệm thu với camera/người thật |
| LLM | Test aggregate không tên/PII, lỗi network có kiểm soát, gateway nhận KPI query local, key không lộ viewer, giới hạn 20 request/ngày. GPT-5 payload không temperature/max_tokens, có max_completion_tokens và store=false. Smoke test tới endpoint OpenAI thật bằng số liệu tổng hợp giả lập đã nhận phản hồi 1.279 ký tự; không gửi dữ liệu vận hành. Khóa người dùng lưu mã hóa trong LocalAppData hiện tại, không trong publish |
| NuGet audit | `dotnet list STEMManager.sln package --vulnerable --include-transitive`: không thấy package dễ tổn thương ở lượt đã chạy; kiểm tra lại trước phát hành |
| Windows cross-publish | `dotnet publish ... -r win-x64 --self-contained true -p:PublishSingleFile=false`: đạt, có exe/runtime/models/native libraries |
| Windows installer | Đã có Inno Setup script + PowerShell + CI; chưa có Setup.exe đã biên dịch vì môi trường hiện tại không có Windows/Inno Setup/Wine |
| Visual C++ runtime | ONNX import cần MSVCP140/VCRUNTIME140; script Windows copy CRT app-local từ nguồn redist được phép. Cross-publish Ubuntu chưa có CRT; không coi đây là gói Face AI đã nghiệm thu Windows sạch |

Lệnh tái kiểm:

```bash
dotnet build STEMManager.sln -c Release -m:1
dotnet test STEMManager.sln -c Release --logger trx -m:1
dotnet list STEMManager.sln package --vulnerable --include-transitive
dotnet publish App/STEMLabManager.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/windows/app -m:1
dotnet run --project App/STEMLabManager.Tools -c Release -- prepare-docs
```

Test TRX ở `tests/*/TestResults`; ảnh chụp test ở `tests/STEMLabManager.UITests/bin/Release/net10.0/Screenshots`. Các screenshot có dữ liệu sinh tự động trong DB tạm, không có thông tin học sinh thật. Video `artifacts/docs/STEMManager-UI-walkthrough.mp4` dài 52 giây từ 13 trạng thái UI thật của bài test, có nhãn tự động/dữ liệu giả; là walkthrough bằng ảnh trạng thái, không phải video thao tác thủ công hay bằng chứng nghiệm thu camera.

Tài liệu PDF và mẫu XLSX ở `artifacts/docs`. Windows staging ở `artifacts/windows/app`; thư mục `cross-publish-before-design-isolation` là bản staging cũ được giữ để phục hồi. Không chứa DB vận hành/API key/mặt trong payload.

Các gate còn cần môi trường thực: chạy script Windows tạo Setup.exe và kiểm cài/gỡ/update offline máy Windows 10/11 sạch; camera/liveness/false-match với consent hợp lệ; thử timeout LLM thực và luồng UI → service → endpoint thật (smoke test adapter đã đạt); kiểm fonts/native/DPI bằng mắt trên Windows; diễn tập demo 5–7 phút, rà soát license/native redistributables và chính sách bảo vệ dữ liệu của trường. Chưa đánh dấu các gate này hoàn thành.
