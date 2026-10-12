# 10. AI và nhận diện khuôn mặt

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


## 10.1. AI thống kê: chính xác trước, LLM sau

- Dữ liệu gốc từ query có định nghĩa rõ: lượt mượn = số phiếu/đơn vị theo metric cấu hình; tần suất mượn tính theo thời gian.
- KPI bắt buộc: tổng lượt mượn, số người mượn riêng biệt, top asset, tỷ lệ quá hạn, tỷ lệ sử dụng theo thời gian, số lần không thể đáp ứng đặt trước.
- Khuyến nghị: khi đủ dữ liệu, moving average/seasonality đơn giản trước; chỉ triển khai ML phức tạp khi có tập huấn luyện và đánh giá lỗi dự báo.
- AI insight phải hiển thị nguồn số liệu, thời gian phân tích, cảnh báo dữ liệu ít; không coi lời khuyên là quyết định mua hàng tự động.

## 10.2. Face recognition — optional, privacy-by-design

- Mặc định OFF. Có đăng ký chủ động, thông báo và cơ sở xử lý/sự đồng ý hợp lệ; trường hợp trẻ em phải có yêu cầu bảo vệ bổ sung theo pháp luật hiện hành.
- Đăng ký: webcam → kiểm chất lượng/liveness phù hợp → detector → embedding → mã hóa trên disk; không mặc định giữ ảnh thô.
- Tra cứu: chỉ dùng tại quầy mượn, trả về 0–N ứng viên trên ngưỡng; không khẳng định danh tính tuyệt đối; bắt buộc nhân viên xác nhận.
- Chống spoofing tối thiểu: kiểm tra liveness phù hợp, xử lý ảnh in/video; không tuyên bố chống giả mạo tuyệt đối.
- Phương án fallback bắt buộc: mã người mượn/QR/thẻ/PIN; sai nhận diện không cản trở tiếp cận dịch vụ.
- Cho phép thu hồi consent và xóa embeddings, lưu sự kiện xóa, giới hạn retention; không đồng bộ embedding ra LLM.
- Kiểm tra quyền thương mại/phân phối của model và thư viện trước khi bundle; lưu model checksum và version.

## 10.3. LLM tùy chọn

- Người dùng đặt câu hỏi từ mẫu hoặc tự do; backend/local service lấy số liệu bằng những truy vấn có kiểm soát, không cho LLM thực thi SQL tùy ý.
- Chỉ gửi tổng hợp đã khử nhận dạng; không gửi ảnh, embedding, mật khẩu, thông tin học sinh hoặc địa chỉ liên hệ.
- UI cho người dùng xem trước phạm vi dữ liệu trước khi gửi; có nút tắt hẳn LLM; internet mất thì hiển thị analytics local như thường.
- Response phải có: tóm tắt, bằng chứng định lượng, khuyến nghị, mức độ tin cậy và giới hạn dữ liệu; tuyệt đối không bịa KPI.
- Thêm giới hạn chi phí/tokens, timeout, retry hữu hạn và thông báo lỗi dễ hiểu.
