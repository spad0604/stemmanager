# 16. Dữ liệu demo và kịch bản trình diễn dự thi

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


## 16.1. Bộ dữ liệu mẫu an toàn

- 12 danh mục, 60 loại thiết bị, 150 cá thể, 20 nhóm vật tư, 30 người mượn GIẢ LẬP (không dùng tên/sinh trắc học thật).
- 150–250 giao dịch mượn lịch sử trong 6 tháng; có đủ trạng thái: đúng hạn, trễ hạn, trả một phần, hỏng, bảo trì.
- 20 lịch đặt: chờ duyệt, đã duyệt, bị từ chối, xung đột.
- Biểu đồ và AI insights tính từ bộ dữ liệu mẫu thật, không vẽ số liệu giả trực tiếp trong view.

## 16.2. Kịch bản demo 5–7 phút

1. Mở phần mềm, đăng nhập quản lý. Dashboard có KPI, lịch mượn, cảnh báo quá hạn.
1. Tìm “Arduino Uno”, xem tồn khả dụng, QR và lịch sử.
1. Thêm lịch đặt robot tại ngày/giờ; thử một lịch chồng lấn để thấy hệ thống ngăn trùng.
1. Tạo phiếu mượn cho học sinh demo; quét QR (hoặc nhập mã); xác nhận và thấy tồn kho cập nhật.
1. Trả một phần; ghi nhận một unit hỏng để chuyển sang bảo trì.
1. Mở Analytics, hỏi “Thiết bị nào dùng nhiều nhất và nên bổ sung gì?”; cho thấy kết quả có số liệu chứng minh; LLM optional.
1. Tắt Wi-Fi, thực hiện giao dịch tiếp; mở Sao lưu và chứng minh ứng dụng chạy offline.

## 16.3. Bộ hồ sơ nộp cuộc thi

- Bài toán thực tế và khảo sát nhu cầu giáo viên; sơ đồ trước/sau triển khai.
- Ảnh chụp giao diện thật, video demo không cắt ghép các trạng thái lỗi.
- Mã nguồn, hướng dẫn cài, bộ cài, tài liệu vận hành, bộ test, danh sách thư viện và license.
- Tác động đo được: thời gian tra cứu, số lượng phiếu quản lý, tỷ lệ mượn quá hạn; nếu chưa thử nghiệm thực tế thì ghi “minh họa/giả định”.
- Tuyên bố minh bạch những phần AI dùng mô hình có sẵn, những gì AI do LLM hỗ trợ phát triển và phần con người thiết kế/kiểm thử.
