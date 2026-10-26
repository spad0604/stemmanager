# 12. Build, installer và cập nhật

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


## 12.1. Kết quả cần bàn giao

- STEMManager-Setup-x64.exe: installer có shortcut Desktop/Start Menu, chọn thư mục, gỡ cài đặt; máy sạch không cần SDK hay Python.
- Bản portable ZIP (tùy chọn) để demo; bản cài là sản phẩm chính.
- Bộ dữ liệu demo nạp từ menu “Tạo dữ liệu minh họa”, không trộn với dữ liệu thật.
- User Guide PDF/DOCX ngắn, video demo, file dữ liệu mẫu để import; README kỹ thuật.

## 12.2. Cross-build

Có thể phát triển Avalonia trên Ubuntu và publish win-x64 self-contained. Bước tạo installer Windows với Inno Setup nên chạy qua máy Windows hoặc CI runner Windows để xác nhận bộ cài thật; cần test camera, ONNX native dll, SQLite, fonts và DPI trên Windows sạch. Đừng coi chạy trên Ubuntu là đã nghiệm thu Windows.

dotnet restore STEMManager.sln

dotnet test STEMManager.sln -c Release

dotnet publish src/STEMManager.Desktop/STEMManager.Desktop.csproj \

-c Release -r win-x64 --self-contained true \

-p:PublishSingleFile=false

Dùng PublishSingleFile=false cho đợt đầu để dễ kiểm soát model, DLL native và sửa lỗi. Một file cài Setup.exe không đồng nghĩa chương trình chỉ có một file nội bộ.


## 12.3. First-run & update

1. Kiểm tra quyền thư mục dữ liệu, dung lượng trống, version database, checksum model.
1. Tạo database/migrate và tạo tài khoản quản trị qua wizard; bắt buộc tạo mật khẩu mới, không dùng mật khẩu mặc định cố định.
1. Chọn cấu hình tên phòng, ngôn ngữ, giới hạn thời gian mượn, ngày backup và camera (nếu có).
1. Nâng cấp chương trình: cảnh báo backup, thực hiện migration transaction, kiểm tra mở DB, rollback từ bản backup nếu thất bại.
1. Gỡ cài đặt: mặc định giữ dữ liệu, hỏi rõ nếu muốn xóa và yêu cầu xác nhận bổ sung.
