# 05. Sơ đồ điều hướng & cấu trúc các tab

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).

Sidebar trái cố định 240 px (khi thu gọn 68 px), topbar có tiêu đề trang, ô tìm nhanh, biểu tượng thông báo, tài khoản hiện tại. Các chức năng phân theo tác vụ — KHÔNG dùng tab lẫn lộn dữ liệu và cấu hình.


| Nhóm | Tab / màn hình | Vai trò nhìn thấy |
| --- | --- | --- |
| Tổng quan | Dashboard | Tất cả; dữ liệu tùy vai trò |
| Tài sản | Thiết bị; Danh mục; Nhập kho & Kiểm kê; Bảo trì | Tất cả (nút sửa theo quyền) |
| Giao dịch | Mượn – Trả; Đặt trước; Quá hạn | Admin / Quản lý / Giáo viên |
| Con người | Người mượn; Giáo viên & tài khoản | Chỉ chức năng được phép |
| Thông minh | Phân tích AI; Nhận diện khuôn mặt (tùy chọn) | Theo quyền và consent |
| Báo cáo | Báo cáo & Xuất dữ liệu | Theo quyền |
| Hệ thống | Cài đặt; Sao lưu; Nhật ký; Trợ giúp | Admin và vai trò được phân quyền |


## 5.1. Breadcrumb, search, keyboard

- Breadcrumb ví dụ: Thiết bị / Arduino / ARD-00015; click quay lại ngữ cảnh.
- Ctrl+K mở command/search palette cho thiết bị và người mượn; Esc đóng modal; Enter xác nhận khi hợp lệ.
- Không thao tác nguy hiểm bằng một click: hủy phiếu, restore, xóa dữ liệu đều có hộp xác nhận nêu hậu quả.
- Sidebar nhóm rõ ràng; mục không có quyền được ẩn, truy cập sâu trái quyền phải bị từ chối ở service.
