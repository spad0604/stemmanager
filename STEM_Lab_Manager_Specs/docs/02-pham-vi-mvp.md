# 02. Phạm vi, giả định và tiêu chí thành công

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


## 2.1. Giới hạn triển khai

- Một phòng STEM; một cơ sở dữ liệu SQLite lưu trên một máy chính. Không dùng chung tệp SQLite qua thư mục mạng.
- Giáo viên thao tác trực tiếp trên máy chính; học sinh có thể đăng ký nhu cầu thông qua giáo viên. Đặt qua điện thoại/LAN là giai đoạn nâng cấp, không nằm trong MVP.
- Internet không bắt buộc cho mọi quy trình cốt lõi. LLM bên ngoài chỉ dùng khi bật và có mạng.
- Không phát triển nhận diện khuôn mặt để giám sát hay chấm điểm học sinh; chỉ đề xuất danh tính tại thời điểm giao dịch khi có căn cứ pháp lý phù hợp.

## 2.2. Mức ưu tiên


| Mức | Ý nghĩa | Phạm vi |
| --- | --- | --- |
| P0 | Bắt buộc, chạy được khi offline | Đăng nhập, vai trò, thiết bị, cá thể/stock, người mượn, đặt trước, mượn/trả, dashboard, backup. |
| P1 | Hoàn thiện trải nghiệm dự thi | QR, ảnh thiết bị, import/export Excel, nhật ký, thông báo quá hạn, báo cáo PDF. |
| P2 | AI tăng giá trị | Face recognition tùy chọn, phân tích nhu cầu, trợ lý LLM với bộ dữ liệu tổng hợp. |
| P3 | Sau dự thi | Mobile/web self-booking, NFC/RFID, kết nối SIS, nhiều phòng. |


## 2.3. Chỉ số nghiệm thu sản phẩm

- Cài đặt trên Windows sạch mà không yêu cầu Python, SDK, Docker hoặc thao tác command line.
- Trong 3 phút đầu dùng được dữ liệu mẫu và demo giao dịch mượn/trả/đặt trước.
- 100% thao tác quan trọng có thông báo thành công/thất bại và nhật ký; không có thao tác ghi thiếu transaction.
- Không cho mượn hoặc đặt vượt số lượng; không trùng lịch với thiết bị cá thể.
- Tự động khôi phục dữ liệu từ bản sao lưu hợp lệ; ứng dụng không mất dữ liệu khi cập nhật.
- Tài khoản quyền thấp không xem được thông tin sinh trắc học, không tự nâng quyền hay xóa nhật ký.
