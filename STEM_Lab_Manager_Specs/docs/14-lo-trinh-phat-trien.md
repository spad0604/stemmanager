# 14. Lộ trình lập trình bằng LLM

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


| Pha | Đầu ra bắt buộc | Điều kiện qua pha |
| --- | --- | --- |
| 0 — Khởi tạo | Solution, README, theme tokens, AppShell, DI, migrations, seed | Ứng dụng mở trên Ubuntu và build win-x64. |
| 1 — Dữ liệu lõi | Assets, Units, Borrowers, UserAccounts, RBAC | CRUD/test DB; form validation và import hoạt động. |
| 2 — Nghiệp vụ | Reservations, Loans, Returns, Inventory Ledger | Tất cả BR-01…BR-09 được test. |
| 3 — Trải nghiệm | Dashboard, QR, báo cáo, notification, search | Các tab hoàn chỉnh, dark mode tùy chọn, 1366×768. |
| 4 — AI | Statistics, forecast baseline, Face opt-in, LLM opt-in | AI không phá hoạt động offline; không rò dữ liệu định danh. |
| 5 — Phát hành | Installer, backup, restore, demo data, user guide | Cài trên Windows sạch, diễn tập kịch bản dự thi. |


## 14.1. Quy tắc phối hợp với LLM

1. Làm từng pha nhỏ. Trước khi code, LLM trình bày danh sách file sẽ tạo/sửa và test sẽ chạy.
1. Không cho LLM tự ý đổi stack, tạo server cloud, thêm tài khoản mặc định yếu, hoặc đưa bí mật vào repository.
1. Mỗi pha yêu cầu xuất mã nguồn thật, migrations thật, lệnh chạy, log lỗi và test kết quả.
1. Nếu phụ thuộc camera/model không có sẵn, tạo interface + fake adapter chỉ cho development; tính năng production phải tắt có thông báo, không giả vờ nhận diện.
1. Dùng checklist nghiệm thu làm gate trước khi chuyển sang pha tiếp theo.
