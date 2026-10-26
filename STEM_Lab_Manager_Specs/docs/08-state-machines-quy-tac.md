# 08. Quy tắc và trạng thái nghiệp vụ

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


| ID | Bất biến / quy tắc |
| --- | --- |
| BR-01 | Một unit không đồng thời nằm trong hai phiếu mượn đang mở. |
| BR-02 | Không đặt/cho mượn vượt capacity trong cùng khoảng thời gian. |
| BR-03 | Không thể trả quá lượng đã mượn còn chưa trả. |
| BR-04 | Thiết bị LOST/RETIRED/MAINTENANCE không được tính vào số khả dụng. |
| BR-05 | Gia hạn có xung đột lịch APPROVED phải bị từ chối có lý do. |
| BR-06 | Chỉ sửa phiếu theo nghiệp vụ điều chỉnh có audit, không xóa dấu vết. |
| BR-07 | Mã phiếu/QR/asset là duy nhất; tránh race bằng transaction. |
| BR-08 | Tổng tồn = nhập − xuất tiêu hao ± điều chỉnh; không được âm. |
| BR-09 | Hủy reservation không làm thay đổi loan đã giao trừ qua quy trình riêng. |
| BR-10 | Mỗi ảnh sinh trắc học/embedding có consent version, thời điểm đăng ký và lịch sử xóa. |
