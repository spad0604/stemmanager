# Architecture Decision Record (ADR)

## ADR-2026-10-13 — Chụp một ảnh trực diện khi đăng ký Face AI

- Theo yêu cầu vận hành, bỏ thử thách quay đầu hai ảnh. Sau consent, preview camera chạy trực tiếp; người dùng chụp một ảnh, detector yêu cầu đúng một khuôn mặt rồi tạo embedding từ ảnh đó.
- Ảnh không lưu; chỉ embedding được mã hóa. Nhân viên phải xác minh người thật và thẻ/QR. Sản phẩm không tuyên bố liveness hay chống ảnh giả; QR/mã tiếp tục là lựa chọn thay thế.

## ADR-001 — Chọn Avalonia thay WPF
**Trạng thái:** Accepted. **Lý do:** phát triển/test trên Ubuntu, chạy Windows khi triển khai, giữ UI C#/XAML.

## ADR-002 — SQLite trên máy chính
**Trạng thái:** Accepted. **Lý do:** một phòng STEM/một máy, không cần vận hành database server. Không chia sẻ file DB qua SMB.

## ADR-003 — Local-first và optional LLM
**Trạng thái:** Accepted. **Lý do:** mọi nghiệp vụ chính phải hoạt động khi mất Internet; LLM chỉ giải thích số liệu tổng hợp.

## ADR-004 — Face recognition là tính năng tự nguyện
**Trạng thái:** Accepted. **Lý do:** rủi ro sai nhận diện, dữ liệu sinh trắc học nhạy cảm; phải có QR/ID fallback và xác nhận thủ công.

## Mẫu ghi ADR mới
- ID / ngày / người quyết định
- Bối cảnh / các lựa chọn / quyết định / hệ quả / cách rollback

## ADR-005 — 2026-10-12 — Phân lớp và dữ liệu theo tài khoản OS
Accepted. Giữ Desktop → Application contracts/policies → Domain; Infrastructure triển khai ILabService, SQL, transaction và adapters. Application không tham chiếu EF/Avalonia. LocalAppData của tài khoản OS là vị trí dữ liệu phù hợp trường hợp một máy; Windows kế thừa quyền tài khoản, Linux 0700/0600. Demo là DB riêng qua --demo. Một process cho mỗi DB bằng named mutex; không chia sẻ SQLite qua mạng.

## ADR-006 — 2026-10-12 — Stock và lịch đặt
Accepted. Unit, Bulk mượn/trả và Consumable xuất tiêu hao là ba loại tách biệt. Bulk ledger giữ số sở hữu, allocation từ dòng loan đang giữ; return hỏng/mất trừ ledger. Consumable phải giữ đủ tổng lượng đã phân bổ cho các lịch tương lai. Lịch Unit/Bulk dùng sweep theo peak trên khoảng [start,end), không cộng toàn bộ lịch không trùng nhau. Loan quá hạn chiếm capacity đến khi trả, không giả định đã trả. Index có điều kiện bảo vệ cá thể đang mượn tại DB. Mọi mutation dùng SQLite IMMEDIATE và audit cùng transaction.

## ADR-007 — 2026-10-12 — Model và giới hạn AI
Accepted. YuNet người dùng cung cấp là detector; bổ sung SFace unmodified từ OpenCV Zoo, kèm license/checksum. ONNX Runtime chạy detector/embedding local. Camera OpenCvSharp/native Windows; Ubuntu thông báo không có adapter camera. Enrollment dùng một ảnh trực diện theo ADR-2026-10-13, nhân viên xác minh người thật và thẻ/QR; không có PAD/liveness, không tuyên bố chống ảnh giả. Theo yêu cầu người dùng, Face embedding bật mặc định trong lần thiết lập mới; enroll vẫn yêu cầu consent, không tự xác nhận giao dịch. LLM không gửi câu hỏi tự do hoặc tên asset tự do; chỉ aggregate JSON/alias, timeout 25 giây, không SQL/tools. GPT-5: max_completion_tokens=2048 (gồm reasoning), reasoning_effort=minimal, verbosity=low, store=false, không gửi temperature/max_tokens; model khác giữ payload 600 token. Khóa mã hóa nằm trong LocalAppData, không trong mã nguồn/publish.

## ADR-008 — 2026-10-12 — Backup, secrets, restore
Accepted. AES-256-GCM cho secrets và embedding với khóa local riêng được OS bảo vệ; backup ZIP chứa DB + khóa + ảnh để phục hồi được trên máy khác, cần hạn chế quyền giữ file. Online Backup API cho DB đang hoạt động. Candidate restore kiểm integrity/FK/admin/schema/ảnh/khóa, migrate trước thay active DB, giữ DB rollback. Restore tự tắt Face để rà soát consent trước bật lại. Snapshot trước migration; 14 backup tự động mới nhất, bản cũ chuyển Retired để có thể khôi phục. Chưa mã hóa toàn bộ SQLite/ZIP, tài liệu giải thích giới hạn.

## ADR-009 — 2026-10-12 — Pha và phát hành
Accepted. Hai tài liệu chia pha khác nhau: triển khai theo dependency/use-case và gate nghiệm thu thay vì số pha. .sln giữ project prefix STEMLabManager từ khung có sẵn. PDF qua SkiaSharp để tránh phụ thuộc license commercial; các báo cáo PDF định dạng văn bản đa trang. Publish win-x64 thực hiện Ubuntu; Inno Setup và Windows sạch cần runner Windows, không coi cross-publish là nghiệm thu installer. Chưa có Git repository nên workflow được bàn giao dạng file, chưa chạy từ xa.

## ADR-010 — 2026-10-12 — UI theo hướng Material 3
Accepted theo yêu cầu người dùng: giao diện hiện đại có icon, màu sắc và biểu đồ. Giữ Avalonia/MVVM và bất biến không gradient; cập nhật token màu theo surface/container, card bo 20 px, button pill, filled input, Inter và navigation pill. Icon và chart vẽ bằng vector native để không thêm phụ thuộc theme/chart bên ngoài. Dashboard dùng dữ liệu Analytics hiện có; thêm presentation model và adaptive panels, không đổi schema/service nghiệp vụ. Các form, wizard và bảng chia sẻ theme mới. Reference radius/color trong docs 06 được cập nhật cách áp dụng theo yêu cầu thiết kế mới của người dùng.
# 2026-10-13 — Dự báo nhu cầu local bằng SSA

- Chọn `Microsoft.ML.TimeSeries` SSA, huấn luyện theo chuỗi số phiếu/ngày lấy từ kỳ SQLite người dùng chọn; inference và backtest chạy trong process desktop, không gọi endpoint ngoài.
- Dự báo 7 ngày; tối thiểu 21 ngày để fit window 7. Khi có ít nhất 28 ngày, đánh giá bằng holdout tối đa 7 ngày và hiển thị MAE. Không đủ mẫu hoặc lỗi fit thì UI báo rõ trạng thái.
- Model không quyết định đặt mua và không thay thế KPI SQL. LLM tùy chọn hiện có vẫn chỉ nhận aggregate sau bước xem trước.
