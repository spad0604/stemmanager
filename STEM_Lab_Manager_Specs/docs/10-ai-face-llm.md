# 10. AI và nhận diện khuôn mặt

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


## 10.1. AI thống kê: chính xác trước, LLM sau

- Dữ liệu gốc từ query có định nghĩa rõ: lượt mượn = số phiếu/đơn vị theo metric cấu hình; tần suất mượn tính theo thời gian.
- KPI bắt buộc: tổng lượt mượn, số người mượn riêng biệt, top asset, tỷ lệ quá hạn, tỷ lệ sử dụng theo thời gian, số lần không thể đáp ứng đặt trước.
- Dự báo local dùng ML.NET TimeSeries SSA trên chuỗi số phiếu/ngày từ SQLite; yêu cầu ít nhất 21 ngày. Model huấn luyện lại khi tải kỳ thống kê, dự báo 7 ngày và không gửi chuỗi ra mạng.
- Khi có ít nhất 28 ngày, giữ lại tối đa 7 ngày cuối để kiểm tra lùi và hiển thị MAE theo phiếu/ngày. Dưới ngưỡng tối thiểu hoặc khi model lỗi, UI phải ghi rõ chưa có dự báo; không thay bằng con số trung bình nhưng vẫn gắn nhãn AI.
- Dự báo là tham khảo, cần hiển thị nguồn/khoảng thời gian và sai số khi có; không tự quyết định mua sắm.
- AI insight phải hiển thị nguồn số liệu, thời gian phân tích, cảnh báo dữ liệu ít; không coi lời khuyên là quyết định mua hàng tự động.

## 10.2. Face recognition — optional, privacy-by-design

- Mặc định OFF. Có đăng ký chủ động, thông báo và cơ sở xử lý/sự đồng ý hợp lệ; trường hợp trẻ em phải có yêu cầu bảo vệ bổ sung theo pháp luật hiện hành.
- Đăng ký: xin consent trước → preview camera trực tiếp → chụp một ảnh trực diện → yêu cầu đúng một khuôn mặt → embedding → mã hóa trên disk; ảnh thô chỉ giữ trong RAM và được xóa sau xử lý.
- Tra cứu: chỉ dùng tại quầy mượn, trả về 0–N ứng viên trên ngưỡng; không khẳng định danh tính tuyệt đối; bắt buộc nhân viên xác nhận.
- Không có kiểm tra liveness tự động. Nhân viên phải quan sát người thật và đối chiếu thẻ/QR; UI phải nói rõ giới hạn này. Không tuyên bố chống ảnh in/video hoặc chống giả mạo.
- Phương án fallback bắt buộc: mã người mượn/QR/thẻ/PIN; sai nhận diện không cản trở tiếp cận dịch vụ.
- Cho phép thu hồi consent và xóa embeddings, lưu sự kiện xóa, giới hạn retention; không đồng bộ embedding ra LLM.
- Kiểm tra quyền thương mại/phân phối của model và thư viện trước khi bundle; lưu model checksum và version.

## 10.3. LLM tùy chọn

- Người dùng đặt câu hỏi từ mẫu hoặc tự do; backend/local service lấy số liệu bằng những truy vấn có kiểm soát, không cho LLM thực thi SQL tùy ý.
- Chỉ gửi tổng hợp đã khử nhận dạng; không gửi ảnh, embedding, mật khẩu, thông tin học sinh hoặc địa chỉ liên hệ.
- UI cho người dùng xem trước phạm vi dữ liệu trước khi gửi; có nút tắt hẳn LLM; internet mất thì hiển thị analytics local như thường.
- Response phải có: tóm tắt, bằng chứng định lượng, khuyến nghị, mức độ tin cậy và giới hạn dữ liệu; tuyệt đối không bịa KPI.
- Thêm giới hạn chi phí/tokens, timeout, retry hữu hạn và thông báo lỗi dễ hiểu.
