# Hướng dẫn STEM Lab Manager

## Bắt đầu

1. Cài `STEMManager-Setup-x64.exe` và mở ứng dụng. Lần đầu, tạo tên phòng, tài khoản quản lý và mật khẩu. Không có mật khẩu mặc định.
2. Màn đăng nhập hiển thị nhận diện Trường THCS Trọng Điểm.
3. Thêm loại thiết bị và vị trí trong **Danh mục & vị trí**, sau đó tạo thiết bị. Có thể tải ảnh ngay trong biểu mẫu thiết bị; ảnh cũng xuất hiện ở danh sách và khi chọn thiết bị để mượn.
4. Thêm **Người mượn** hoặc nhập danh sách từ CSV/Excel. Tài khoản đăng nhập và hồ sơ người mượn là hai khái niệm riêng; hệ thống chỉ tự chọn người mượn khi mã tài khoản hoặc tên hiển thị khớp duy nhất.

## Mượn – trả

- Từ **Thiết bị**, chọn thiết bị rồi bấm **Thêm vào giỏ mượn**. Giỏ giữ các thiết bị đã chọn và mở phiếu với các món đó.
- Trong **Mượn – Trả**, tạo phiếu, xác nhận người mượn, hạn trả và mục đích trước khi bàn giao. Có thể tra cứu khuôn mặt để chọn người khi đã đăng ký embedding; nhân viên vẫn phải xác nhận đúng người và đúng thiết bị.
- Khi trả, chọn phiếu và ghi số lượng/tình trạng từng dòng. Thiết bị hỏng được chuyển sang bảo trì; thiết bị mất không cộng lại vào số sẵn sàng.
- Chọn **Đặt trước** để tạo và quản lý lịch sử dụng. Lịch phải được duyệt trước khi bàn giao.

## Tổng quan và báo cáo

- Bấm các ô **Thiết bị sẵn sàng**, **Đang mượn**, **Quá hạn** hoặc **Lịch hôm nay** trên Tổng quan để mở danh sách chi tiết tương ứng. Ô thiết bị sẵn sàng lọc chỉ các thiết bị có thể giao ngay.
- **Phân tích AI** mở biểu đồ trực quan. Dự báo SSA chạy cục bộ; nhận xét LLM chỉ gửi dữ liệu tổng hợp khi quản lý bật và cấu hình dịch vụ.
- Trong **Báo cáo & xuất dữ liệu**, chọn kỳ, loại báo cáo rồi xuất CSV, Excel hoặc PDF. Có báo cáo lượt mượn theo thiết bị/người mượn, báo cáo quá hạn theo thiết bị/người mượn và báo cáo nhập/điều chỉnh kho (STT, tên thiết bị, số lượng nhập/giảm, lý do). Báo cáo nhập/điều chỉnh chỉ dành cho quản lý kho; báo cáo theo người mượn yêu cầu quyền xem dữ liệu cá nhân.

## Dữ liệu và quyền riêng tư

- Face embedding được bật mặc định cho cài đặt mới. Chỉ đăng ký khi có căn cứ và sự đồng ý phù hợp. Đăng ký bằng một ảnh trực diện; nhân viên đối chiếu người thật trước khi xác nhận. Khuôn mặt chỉ giúp tìm hồ sơ, không thay thế bước xác minh và QR/mã vẫn dùng được.
- Dữ liệu ứng dụng lưu cục bộ. Sao lưu thường xuyên và cất riêng file sao lưu vì file có dữ liệu nhạy cảm.
- Shortcut **STEM Demo** hoặc `--demo` dùng cơ sở dữ liệu minh họa riêng.

Ngày giờ hiển thị theo giờ máy. Ctrl+K mở tìm nhanh. Phiên tự khóa sau thời gian không hoạt động được cấu hình.
