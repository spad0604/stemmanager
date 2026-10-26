# Acceptance Test — Các ca bắt buộc

## Thiết lập
- [ ] Installer hoạt động trên Windows sạch không cài .NET SDK/Python; chạy offline.
- [ ] Mở app lần đầu tạo cấu hình, tài khoản admin an toàn, DB, local folders; không password mặc định dùng mãi.
- [ ] Data files ngoài thư mục cài đặt; uninstall/update không âm thầm xóa dữ liệu.

## Phân quyền
- [ ] Admin quản lý tài khoản/quyền và cài đặt.
- [ ] Giáo viên phụ trách xử lý thiết bị, mượn trả, lịch, kiểm kê đúng phạm vi.
- [ ] Giáo viên sử dụng chỉ làm các việc được cấp; vai trò viewer không ghi/sửa.
- [ ] Gọi trực tiếp application service vẫn bị từ chối nếu không có quyền.

## Kho và mượn trả
- [ ] Tạo thiết bị có mã duy nhất và đơn vị riêng; không cho trùng mã.
- [ ] Vật tư tiêu hao không được trả như thiết bị mượn từng chiếc.
- [ ] Mượn thiết bị available → borrowed và audit được tạo trong một transaction.
- [ ] Không thể mượn thiết bị đang bảo trì/đang được mượn hoặc quá số lượng.
- [ ] Trả một phần không đóng toàn bộ phiếu; trả đủ thì đóng đúng trạng thái.
- [ ] Quá hạn tính theo timezone local và ngày giờ thực, không hardcode.
- [ ] Hai yêu cầu đặt cùng thiết bị, trùng thời gian: không thể cùng xác nhận.
- [ ] Hủy/no-show/đặt hết hạn được xử lý theo rule docs.
- [ ] Import file sai format/cột bắt buộc trả lỗi rõ và không nhập nửa chừng ngoài chính sách đã định.

## UI/UX
- [ ] Không có gradient ở bất kỳ màn hình, dialog, chart background nào.
- [ ] Sidebar phân nhóm rõ; trạng thái active rõ; keyboard navigation và focus dễ thấy.
- [ ] Tất cả trang có empty/loading/error; xác nhận destructive action; form validate inline.
- [ ] Dashboard số liệu đồng nhất với truy vấn DB; lọc thời gian thực sự thay đổi kết quả.

## AI và bảo mật
- [ ] Face AI tắt thì toàn bộ luồng QR/ID chạy đủ.
- [ ] Hủy đồng ý/xóa khuôn mặt xóa embedding theo chính sách, không ghi vào log.
- [ ] Face matching có khả năng trả `unknown`, không ép gán danh tính.
- [ ] Mất Internet: CRUD/loan/booking/analytics, local face (nếu bật) hoạt động.
- [ ] External LLM timeout: thông báo dễ hiểu, app không bị treo.
- [ ] Dữ liệu gửi LLM không có ảnh, embedding, tên, số điện thoại hay mã học sinh.

## Backup/release
- [ ] Sao lưu trong khi DB hoạt động và restore vào bản DB test được kiểm chứng.
- [ ] Schema migrations có test DB mới và DB phiên bản cũ.
- [ ] Release không chứa API key, file DB thật, ảnh học sinh thật, mặt thật.
- [ ] Checklist demo cuộc thi chạy end-to-end trên seed data giả và có khả năng reset.
