# 06. Hệ thống thiết kế UI/UX

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


## 6.1. Ngôn ngữ hình ảnh — tuyệt đối không gradient


| Token | Giá trị tham chiếu |
| --- | --- |
| Surface nền | #F5F7FA (xám rất nhạt) |
| Card / panel | #FFFFFF |
| Primary | #2563EB (xanh lam rõ, phẳng) |
| Primary hover | #1D4ED8 |
| Text chính | #172434 |
| Text phụ | #66758A |
| Border | #E2E8F0 |
| Success | #15803D |
| Warning | #B45309 |
| Error | #B91C1C |
| Radius / shadow | Card 12 px; input 8 px; bóng đổ mờ rất nhẹ; không dùng glassmorphism |
| Spacing | Lưới 8 px: 8, 16, 24, 32 |
| Typography | Segoe UI / Inter (nếu giấy phép/phân phối cho phép); body 14 px; title 26; section 18 |

- Không gradient ở bất kỳ đâu: header, sidebar, button, chart nền, modal, loading, splash hoặc card.
- Không làm UI “nhựa”: tránh blur mạnh, glow neon, hình 3D không cần thiết, quá nhiều icon màu và animation phô trương.
- Trạng thái không chỉ dùng màu: luôn kèm chữ/icon (Đang mượn, Quá hạn, Bảo trì, Sẵn sàng).
- Tương phản văn bản theo WCAG 2.2 AA ở mức phù hợp; vùng click dễ thao tác; focus keyboard thấy rõ.
- Mặc định light theme; dark theme là phụ trợ; tối ưu 1366×768, 1600×900, 1920×1080 và scale 125–150%.

## 6.2. Layout chuẩn

Sidebar 240 px; content max width 1480 px, margin 24 px; topbar 64 px. Trang danh sách: hàng tiêu đề → mô tả ngắn → toolbar hành động → filter bar → bảng dữ liệu → phân trang. Trang chi tiết: header tên/mã/trạng thái → cột thông tin chính + panel thao tác/hình ảnh → lịch sử ở tab phụ.


## 6.3. Component library bắt buộc

- AppShell, SidebarNav, HeaderBar, PageHeader, PrimaryButton, DangerButton, SearchBox, EmptyState, LoadingSkeleton, ErrorState.
- StatCard, StatusBadge, DataTable (sort/search/filter/paging), DetailDrawer, ConfirmDialog, FormField, DateTimePicker, Toast, TabStrip.
- AssetImage, QRPreview, CameraPreview, PermissionGuard, AuditTimeline, ChartPanel.
- Luôn có empty-state với CTA có ích; hiển thị lỗi bằng tiếng Việt, cho phép thử lại; không để spinner chạy vô hạn.

## 6.4. Mô tả cụ thể các màn hình quan trọng


| Màn hình | Bố cục / hành động |
| --- | --- |
| Dashboard | 4 KPI: tổng thiết bị khả dụng, đang mượn, quá hạn, lịch đặt hôm nay; biểu đồ lượt mượn 30 ngày; top 5 thiết bị; bảng giao dịch gần nhất; CTA “Tạo phiếu mượn”. |
| Thiết bị | Lưới/thẻ hoặc bảng; bộ lọc danh mục, tình trạng, vị trí; nút thêm/import/in QR; số lượng tồn và số khả dụng; click vào chi tiết và lịch sử. |
| Mượn – Trả | Hai tab con “Đang mượn”/“Lịch sử”; CTA “Tạo phiếu”; wizard 3 bước chọn người → chọn thiết bị → kiểm tra và xác nhận; trả có tình trạng và ghi chú. |
| Đặt trước | Calendar tuần + danh sách; trạng thái chờ duyệt/đã duyệt/đã hủy; modal kiểm tra số lượng khả dụng, lý do từ chối. |
| Người mượn | Danh bạ có lớp/nhóm, mã người mượn, số phiếu đang giữ, số lần quá hạn; không mặc định lộ email/số điện thoại. |
| Phân tích AI | Bộ lọc ngày, top thiết bị, biểu đồ nhu cầu, cảnh báo; chú thích cách tính; khối “AI nhận xét” chỉ khi người dùng nhấn phân tích. |
| Cài đặt | Các mục riêng: phòng học, vai trò, bảo mật, AI/LLM, camera, backup, dữ liệu mẫu, thông tin phiên bản. |
