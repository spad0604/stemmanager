# 13. Test plan và tiêu chí nghiệm thu

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


| Nhóm | Test tối thiểu |
| --- | --- |
| Cốt lõi | CRUD, lọc, sort, import trùng mã, hình ảnh lỗi, search tiếng Việt có dấu/không dấu. |
| Giao dịch | Mượn 1 unit, nhiều unit; trả một phần; trả hỏng; không cho trả quá; lịch đặt giao nhau; gia hạn xung đột. |
| Quyền | Đăng nhập từng role; deep-link vào trang cấm; service API/use-case kiểm quyền; export nhạy cảm. |
| Offline | Rút mạng; tạo phiếu/đặt trước/báo cáo/face local; LLM tắt và hiện trạng thái đúng. |
| Dữ liệu | Đóng app bất thường giữa ghi, toàn vẹn FK, migration, backup và restore trên DB khác. |
| UI | 1366×768, scale 125/150%, bàn phím, focus, empty/error/loading state, light/dark, không có gradient. |
| AI | Không có camera, không thấy mặt, nhiều khuôn mặt, nhận diện sai, thu hồi consent, model lỗi. |
| Installer | Windows 10/11 máy sạch, thiếu mạng, không có admin quyền khi không hỗ trợ, gỡ và cài lại vẫn giữ dữ liệu. |


## 13.1. Definition of Done cho mỗi tính năng

- Có use-case, entity/DTO, validation, permission checks, migration (nếu cần), View + ViewModel + state xử lý lỗi.
- Có unit test cho nghiệp vụ, integration test cho SQLite và test thủ công giao diện.
- Không để TODO placeholder, fake data trong luồng sản xuất, nút bấm không có hành động hoặc số KPI hardcoded.
- Build Release sạch; mọi màn hình không bị tràn, văn bản tiếng Việt thống nhất.
