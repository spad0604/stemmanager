# Giao diện theo hướng Material 3

UI dùng các vai trò màu surface/container/primary thay cho nền và viền mặc định; nền phẳng, không gradient/blur. Tham khảo [Material 3 color roles](https://m3.material.io/styles/color/roles) và [typography](https://m3.material.io/styles/typography/applying-type). Đây là theme Avalonia riêng lấy cảm hứng Material 3, không thêm runtime UI khác.

- Theme ở `App/STEMLabManager.Desktop/Themes/MaterialTheme.axaml`; token light/dark ở `App.axaml`. Typography Inter; card 20 px, panel đăng nhập/dialog 24–28 px, nút pill và input filled 12 px.
- Icon outline vector ở `Controls/AppIcon.cs`, dùng đồng bộ trong sidebar, hành động, KPI, bảng, form và wizard. Không phụ thuộc icon font hoặc bitmap tải từ mạng.
- Sidebar hiển thị nhóm một lần, mục đang chọn là pill; topbar có tìm nhanh, thông báo và avatar tài khoản.
- Dashboard tách thành `Views/DashboardView`. `Controls/Charts.cs` vẽ biểu đồ đường theo ngày, thanh xếp hạng và donut tỷ lệ quá hạn; tooltip đường hiển thị ngày và số phiếu khi rê chuột. Tất cả nguồn số liệu lấy từ Analytics hiện có, không có dữ liệu chart giả.
- `AdaptivePanels` đổi KPI từ 4 cột sang 2 cột và biểu đồ từ hai cột sang xếp dọc khi cửa sổ hẹp, hỗ trợ DPI lớn.
- Bảng dùng chip trạng thái, icon từng loại bản ghi; form và wizard dùng cùng theme, stepper, icon và trạng thái lỗi.

Kiểm chứng: build Release và 8 UI tests gồm toàn bộ trang, nhập phiếu qua ViewModel/service/SQLite, các kích thước 1366/1600/1920 và DPI 100/125/150%. Có ảnh light/dark dashboard riêng theo kích thước/DPI. Đã xem các render thật để kiểm bố cục; chưa thay thế nghiệm thu bằng mắt trên Windows thật.

Preview trong `artifacts/ui`: `material-dashboard.png`, `material-assets.png`, `material-login.png`, `material-dashboard-dark.png`, `material-dashboard-150.png`. Các ảnh dùng dữ liệu demo sinh từ SQLite.
