# Demo 5–7 phút

Mở bằng shortcut STEM Demo hoặc `--demo`. Thiết lập admin riêng, Cài đặt → Nạp dữ liệu minh họa (chỉ một lần trên DB demo rỗng). Bộ dữ liệu chứa 60 loại thiết bị, 150 cá thể, 20 nhóm vật tư, 30 người mượn giả, 209 phiếu và 20 lịch đặt; không có khuôn mặt người thật.

1. Tổng quan: đổi kỳ 6 tháng, Tải lại; chỉ nguồn SQLite, số phiếu quá hạn, top thiết bị và cảnh báo.
2. Thiết bị: tìm Arduino không dấu, mở cá thể và xuất QR.
3. Đặt trước: đặt một cá thể ở ngày mai. Duyệt; đặt cùng cá thể trùng giờ để thấy bị từ chối. Đặt nối tiếp thời gian để thấy được chấp nhận.
4. Mượn–Trả: wizard chọn người demo, thêm 2 cá thể, nhập hạn và mục đích, xác nhận. Tồn khả dụng giảm.
5. Trả một cá thể, tình trạng hỏng. Phiếu vẫn mở; Bảo trì có biên bản. Hoàn tất sửa đưa cá thể về sẵn sàng.
6. Phân tích AI: local insight và dự báo có nguồn/kỳ, LLM tùy chọn xem JSON tổng hợp. Face model inspection chạy local, chỉ demo camera nếu có consent phù hợp và đã nghiệm thu phần cứng.
7. Tắt Wi-Fi, làm tiếp phiếu và xuất báo cáo. Sao lưu, chọn bản backup hợp lệ; phục hồi thử trên DB demo và đăng nhập lại.

Reset: đóng cửa sổ Demo; giữ một backup, chuyển thư mục Demo sang tên lưu trữ rồi mở lại để tạo DB mới. Không xóa thư mục Data của vận hành thật. Các tác động giáo dục và số liệu tiết kiệm thời gian phải được đo thực tế; không gán số giả thành kết quả thử nghiệm.

Video walkthrough tự động 52 giây trong `artifacts/docs/STEMManager-UI-walkthrough.mp4` được tạo từ trạng thái thật của `DemoRecordingTests`, có nhãn dữ liệu giả. Video này không thay thế phần trình diễn thủ công 5–7 phút hay nghiệm thu Windows/camera.
