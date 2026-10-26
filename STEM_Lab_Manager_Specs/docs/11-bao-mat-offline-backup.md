# 11. Bảo mật, riêng tư và khả năng vận hành offline

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).

- Mật khẩu: Argon2id hoặc PBKDF2 cấu hình tốt với salt; không lưu plain text; hỗ trợ đổi mật khẩu và khóa tài khoản sau nhiều lần sai.
- Khóa màn hình sau bất hoạt; xóa clipboard dữ liệu nhạy cảm khi phù hợp; ngăn export dữ liệu cá nhân đối với viewer.
- SQLite đặt trong %ProgramData%\STEMManager\Data (quyền thư mục chặt) hoặc vị trí phù hợp chính sách tài khoản; không đặt Program Files.
- Face embedding và secrets cần mã hóa ở trạng thái lưu; Windows DPAPI có rủi ro gắn với máy/tài khoản khi restore, cần quy trình key backup an toàn.
- Export báo cáo định danh phải theo vai trò và ghi nhật ký.
- Không thu thập telemetry từ máy nếu chưa cấu hình/thông báo; log che mật khẩu, token, ảnh/embedding và dữ liệu cá nhân không cần thiết.
- Backup tự động mỗi ngày khi app hoạt động, luân phiên bản, có lịch sử và nút restore qua wizard; backup dùng API tương thích SQLite (không copy file .db thô lúc ghi).
- Kiểm tra restore bằng smoke test; cảnh báo nếu quá 7 ngày chưa có bản backup ngoài máy; khuyến nghị USB/NAS.
- Ở Việt Nam, rà soát quy định bảo vệ dữ liệu cá nhân hiện hành với nhà trường trước triển khai nhận diện khuôn mặt.
