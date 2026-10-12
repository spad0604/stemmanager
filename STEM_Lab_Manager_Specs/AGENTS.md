# STEM Lab Manager — Chỉ dẫn bắt buộc cho AI coding agent

## Mục tiêu
Xây ứng dụng desktop **thực sự hoạt động** cho **một phòng STEM / một máy quản lý**, Windows là mục tiêu phân phối, phát triển được trên Ubuntu. Không xây web server, cloud hay Docker trong MVP.

## Hợp đồng công nghệ
- C#/.NET 10 + Avalonia UI, XAML, MVVM (CommunityToolkit.Mvvm).
- SQLite qua EF Core; migrations rõ ràng; bật foreign keys và WAL nếu phù hợp.
- ONNX Runtime cho nhận diện khuôn mặt **tùy chọn**, model phải có giấy phép phù hợp; không tự huấn luyện trong MVP.
- Báo cáo tính bằng SQL/C# trên dữ liệu thật. External LLM là tùy chọn, chỉ gửi dữ liệu tổng hợp đã ẩn danh.
- Windows installer đơn giản, người dùng không cài SDK/runtime thủ công; self-contained publish.

## Thứ tự đọc
1. `README.md`.
2. `docs/02-pham-vi-mvp.md`, `docs/03-vai-tro-phan-quyen.md`.
3. `docs/06-ui-ux-design-system.md`, `docs/07-chuc-nang-nghiep-vu.md`.
4. `docs/08-state-machines-quy-tac.md`, `docs/09-database-sqlite.md`.
5. `docs/10-ai-face-llm.md`, `docs/11-bao-mat-offline-backup.md`.
6. `docs/12-build-installer.md`, `docs/13-kiem-thu-nghiem-thu.md`, `qa/ACCEPTANCE.md`.

## Bất biến
1. Không gradient, không glassmorphism, không animation gây rối; UI thuần nền phẳng, contrast tốt.
2. Không hardcode dữ liệu analytics/danh sách; dữ liệu demo chỉ bật trong chế độ demo tách biệt.
3. Không fake backend, fake nút bấm, mock authentication hoặc báo cáo giả trong bản Release.
4. Thực thi quyền tại **application/service layer**, không chỉ ẩn nút trên UI.
5. Mượn, trả, đặt trước, chuyển trạng thái, chỉnh tồn kho và xóa mềm phải atomic, ghi audit logs.
6. Không lưu ảnh khuôn mặt/embedding trong log; người dùng phải có đường thay thế không dùng mặt.
7. Không gửi PII, ảnh, khuôn mặt, embedding hay dữ liệu học sinh tới LLM.
8. Không lưu dữ liệu vận hành trong `Program Files` hoặc thư mục publish.
9. Không đòi Internet để login local, mượn trả, đặt trước, export, thống kê hoặc nhận diện local.
10. Chỉ tuyên bố chức năng “hoàn thành” khi có test và chạy xuyên suốt UI → service → SQLite.

## Chu trình làm việc của agent
- Mỗi feature: đọc spec → xác định entity/rule → migration → service → tests → ViewModel → XAML → manual test → cập nhật checklist.
- Trước sửa lớn, liệt kê file tác động và tiêu chí hoàn thành; tránh thay đổi kiến trúc ngoài phạm vi.
- Khi spec mâu thuẫn: ưu tiên `AGENTS.md` → `qa/ACCEPTANCE.md` → docs theo thứ tự; ghi rõ quyết định vào `docs/DECISIONS.md`.
- Không tự khẳng định đã build/test nếu chưa chạy lệnh; ghi lỗi/giới hạn nếu môi trường thiếu SDK.

## Definition of Done
Build không lỗi; migration chạy từ DB rỗng; quyền được test; nghiệp vụ có kiểm thử biên; UI có empty/loading/error states; backup phục hồi được; không có request Internet bắt buộc; demo end-to-end chạy trên dữ liệu thật.
