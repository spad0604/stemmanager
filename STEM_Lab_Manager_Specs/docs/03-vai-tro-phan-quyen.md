# 03. Nhóm người dùng & ma trận phân quyền

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).

Dùng RBAC theo nguyên tắc tối thiểu quyền: SUPER_ADMIN (quản trị cấu hình), LAB_MANAGER (giáo viên phụ trách), TEACHER (giáo viên sử dụng), VIEWER (chỉ đọc/báo cáo). STUDENT là đối tượng mượn trong dữ liệu, không bắt buộc có tài khoản đăng nhập desktop.


| Hành động | Admin | Quản lý | Giáo viên | Chỉ đọc |
| --- | --- | --- | --- | --- |
| Xem danh mục, dashboard | ✓ | ✓ | ✓ | ✓ |
| Thêm/sửa thiết bị | ✓ | ✓ | — | — |
| Nhập kho, kiểm kê, bảo trì | ✓ | ✓ | — | — |
| Tạo yêu cầu đặt trước | ✓ | ✓ | ✓ | — |
| Duyệt/hủy đặt trước | ✓ | ✓ | — | — |
| Giao/nhận thiết bị | ✓ | ✓ | ✓* | — |
| Xem PII người mượn | ✓ | ✓ | Giới hạn | Ẩn |
| Quản lý mặt/consent | ✓** | ✓** | — | — |
| Xem báo cáo tổng hợp | ✓ | ✓ | ✓ | ✓ |
| Quản lý tài khoản, sao lưu, restore | ✓ | — | — | — |
| Xóa cứng dữ liệu giao dịch | Không | Không | Không | Không |

* Giáo viên chỉ được thao tác trong giới hạn cấu hình và phải xác nhận bàn giao. ** Chỉ khi đã bật tính năng, có căn cứ xử lý dữ liệu, phân quyền chi tiết và nhật ký truy cập.


## 3.1. Quy tắc đặc biệt

- Không cho xóa tài khoản từng có giao dịch: khóa vô hiệu hóa (soft-delete).
- Admin không được xóa audit log bằng UI. Phải có nhật ký cả hành động admin.
- Mọi quyền phải được kiểm tra ở application/domain service, không chỉ ẩn nút giao diện.
- Không dùng nhận diện khuôn mặt làm xác thực đăng nhập admin; dùng mật khẩu mạnh và khóa phiên.
