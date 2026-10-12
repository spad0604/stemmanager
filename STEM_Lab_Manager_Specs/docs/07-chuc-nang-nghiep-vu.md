# 07. Đặc tả chức năng từng phân hệ

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


## 7.1. Thiết bị và tồn kho

- Thiết bị định danh đơn chiếc (serial riêng) và vật tư theo số lượng; đơn vị tính khác nhau; không trộn logic.
- Trường cơ bản: mã duy nhất, tên, loại, mô tả, ảnh, nhà sản xuất, model, serial (nếu có), đơn vị, danh mục, vị trí ngăn/kệ, tình trạng, ngày mua, giá tham khảo (tùy chọn), ghi chú.
- Trạng thái unit: AVAILABLE, RESERVED, CHECKED_OUT, MAINTENANCE, LOST, RETIRED. Số lượng consumable: on_hand, allocated, consumed (ledger), không âm.
- Các thao tác: thêm/sửa; nhập tồn; chuyển vị trí; gắn QR; kiểm kê đối chiếu và tạo phiếu điều chỉnh với lý do; lịch sử đầy đủ.
- Không xóa cứng asset đã phát sinh giao dịch; có nút ngừng sử dụng/archived.

## 7.2. Người mượn và tài khoản

- Người mượn (Borrower) tách biệt người đăng nhập (UserAccount). Một học sinh có thể không được cấp quyền truy cập ứng dụng.
- Các trường cần thiết: mã, họ tên, lớp/bộ môn, loại học sinh/giáo viên/khác, trạng thái, tùy chọn liên hệ và người phụ trách.
- Import từ Excel có xem trước, phát hiện trùng mã và xác nhận mapping cột; không tự suy đoán giới tính từ tên.
- Một phiếu mượn ghi lại người thực hiện (staff) và người nhận (borrower) độc lập.

## 7.3. Phiếu mượn

1. Chọn người mượn: tìm tên/mã hoặc dùng QR; nhận diện khuôn mặt chỉ là tùy chọn gợi ý.
1. Thêm nhiều dòng thiết bị; phân biệt asset unit và quantity item; xem số khả dụng thực tế.
1. Nhập hạn trả, mục đích mượn, ghi chú; cảnh báo nợ quá hạn hoặc yêu cầu phê duyệt.
1. Nhấn Xác nhận: validate quyền, kiểm tra lại số lượng trong transaction, tạo loan + loan_items + stock ledger + audit.
1. Hiển thị biên nhận có mã phiếu, người giao, người nhận, giờ giao, trạng thái và QR phiếu.

## 7.4. Trả và gia hạn

- Cho phép trả một phần; mỗi dòng có số lượng trả, tình trạng sau trả (tốt/hỏng/mất), ghi chú; tạo ReturnTransaction riêng.
- Trả hỏng tự chuyển unit sang MAINTENANCE hoặc ghi nhận damaged quantity, không tự vào sẵn sàng.
- Gia hạn chỉ khi quyền cho phép và không xung đột đặt trước; lưu lý do và lịch sử thay đổi.
- Không xóa lịch sử khi sửa phiếu; các điều chỉnh cần chứng từ và audit.

## 7.5. Đặt trước

- Tạo đặt trước theo khoảng [start_at, end_at); start < end, không đặt quá khứ theo chính sách cấu hình.
- Có thể đặt asset cụ thể hoặc số lượng theo loại; tổng đặt đã duyệt + đang mượn trong khoảng không vượt sức chứa phù hợp.
- Trạng thái DRAFT → PENDING → APPROVED / REJECTED / CANCELLED → FULFILLED / EXPIRED.
- Phiếu APPROVED chưa đồng nghĩa thiết bị đã giao; lúc mượn phải đối chiếu đặt trước và chuyển trạng thái nhất quán.
- Nếu thiết bị hỏng, lịch bị ảnh hưởng cần cảnh báo, đề xuất thay thế và yêu cầu xử lý thủ công.

## 7.6. Bảo trì và kiểm kê

- Biên bản báo hỏng: thiết bị, ngày, người báo, mô tả, mức độ, ảnh, trạng thái sửa chữa.
- Lịch bảo trì và nhật ký chi phí nếu có; sau khi sửa, người có quyền xác nhận đưa vào sử dụng.
- Kiểm kê: tạo phiên kiểm kê theo vị trí; scan QR/nhập số lượng; tính chênh lệch; chỉ manager/admin xác nhận điều chỉnh.

## 7.7. Thông báo và báo cáo

- Notification Center: sắp đến hạn (24 giờ), quá hạn, yêu cầu đặt chờ duyệt, tồn kho thấp, thiết bị cần bảo trì.
- Dashboard thống kê theo mốc thời gian và bộ lọc danh mục; giải thích metric.
- Xuất Excel/PDF: danh sách thiết bị, giao dịch, lịch đặt, kết quả kiểm kê, hiệu suất sử dụng theo kỳ.
- Không tự động gửi tin qua Internet; ưu tiên cảnh báo ngay trong app.
