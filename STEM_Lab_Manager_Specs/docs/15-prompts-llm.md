# 15. MASTER PROMPT — DÁN CHO LLM LẬP TRÌNH

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).

Dưới đây là prompt có thể sử dụng nguyên văn cùng tài liệu này. Nên đính kèm DOCX hoặc chuyển nội dung đặc tả thành Markdown trong workspace.

Bạn là Principal .NET Engineer + Product Designer + QA Lead.

Xây dựng ứng dụng STEM LAB MANAGER theo tài liệu đặc tả đính kèm.

RÀNG BUỘC BẮT BUỘC:

1. C#/.NET 10, Avalonia UI, MVVM, SQLite + EF Core, modular monolith.

2. Một phòng STEM, 1 máy giáo viên, tất cả core features hoạt động OFFLINE.

3. Giao diện sáng hiện đại, phẳng, TUYỆT ĐỐI KHÔNG GRADIENT,

phân nhóm sidebar rõ ràng, mọi nút có hành động thật.

4. RBAC ở application service; không chỉ ẩn nút UI.

5. Nghiệp vụ đặt trước/mượn/trả atomic, không âm kho, không trùng unit.

6. Face embedding local mặc định ON cho lần thiết lập mới; chỉ enroll sau consent và có fallback QR/mã; AI analytics local.

7. LLM API tùy chọn, không gửi PII/biometric, không cho tự chạy SQL.

8. Cài đặt Windows bằng một Setup.exe, dữ liệu giữ khi update/uninstall.

9. Không viết pseudo-code, TODO placeholder hoặc dữ liệu KPI hardcoded.

10. Mỗi pha hoàn tất với unit/integration tests và hướng dẫn chạy.

CÁCH LÀM:

- Đầu tiên hãy trình bày project tree, entity & migration plan,

navigation map, quyền và acceptance tests của PHA 0.

- Sau đó triển khai PHA 0 đầy đủ bằng các file mã nguồn cụ thể.

- Báo cáo file thay đổi, lệnh build/test, lỗi còn tồn tại và cách xác minh.

- Không nhảy sang pha mới trước khi pha hiện tại build/test đạt.

- Khi thiếu phần cứng/model/API key: dùng adapter có trạng thái rõ ràng;

không mô phỏng như thể production đã hoạt động.


## 15.1. Prompt kiểm tra giao diện

Hãy audit toàn bộ màn hình theo design system trong đặc tả:

- Không gradient / glassmorphism / neon / 3D decor.

- Sidebar phân nhóm rõ; mỗi tab có title, mô tả, trạng thái trống,

loading, error, toolbar và quyền phù hợp.

- Kiểm tra 1366x768, 1920x1080 và DPI 150%.

- Rà soát spacing, contrast, font, keyboard focus, empty states.

- Liệt kê lỗi cụ thể + file sửa + ảnh screenshot trước/sau (nếu có).

- Tuyệt đối không làm mất business logic khi chỉnh UI.


## 15.2. Prompt kiểm tra logic & bảo mật

Viết automated tests cho BR-01..BR-10, RBAC, backup/restore,

trả một phần, nhập dữ liệu trùng, lịch đặt chồng lấn,

thất bại SQLite transaction, LLM offline/timeout,

face opt-in/revocation và permission-denied.

Ưu tiên SQLite integration tests thật, không chỉ in-memory mocks.

Xuất bảng test cases (input, expected, result).

Không được tự tuyên bố PASS nếu chưa chạy test.
