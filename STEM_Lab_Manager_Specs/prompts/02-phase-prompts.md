# Prompt triển khai từng pha

> Dùng tuần tự; sau mỗi pha phải chạy test và ghi rõ phạm vi hoàn thành.

## Pha 0 — Khung dự án
Đọc `AGENTS.md`, docs kiến trúc/UI và `qa/ACCEPTANCE.md`. Tạo solution các project `STEM.Desktop`, `STEM.Domain`, `STEM.Application`, `STEM.Infrastructure`, `STEM.Tests`; Dependency Injection, logging, app data path, SQLite migration, shell UI, navigation, theme light/dark màu phẳng; build/test thật.

## Pha 1 — Danh mục, tồn kho, người dùng
Đọc docs 03/07/09/11. Tạo entity, repository/service, CRUD, import CSV/Excel với validation + dry-run, role checks, audit, soft delete và tests. UI danh sách/filter/form với trạng thái loading/empty/error.

## Pha 2 — Mượn, trả và đặt trước
Đọc docs 07/08/09. Tạo transactional workflows, chống trùng lịch, trả một phần, quá hạn, gia hạn, tình trạng thiết bị, tests concurrency và boundary. Viết manual scenario end-to-end.

## Pha 3 — Kiểm kê, bảo trì, dashboard
Đọc docs 07/09/13. Bổ sung luồng kiểm kê, bảo trì, truy vấn analytics SQL chính xác, export CSV và chart. Không xuất biểu đồ từ số liệu hardcode.

## Pha 4 — Face AI tùy chọn
Đọc docs 10/11. Cài model đã kiểm tra giấy phép, local inference, consent, enrollment, threshold configurable, liveness nếu khả thi, fallback QR/ID, delete biometric data, test false-match/error camera. Không tự động chốt phiếu theo nhận diện.

## Pha 5 — LLM tùy chọn
Đọc docs 10/11. Giao diện cấu hình API key lưu an toàn, tool calling backend lấy số liệu tổng hợp, prompt guardrails, kiểm soát chi phí, redaction PII, timeout/offline fallback. Không truy cập DB tùy tiện theo câu do model sinh.

## Pha 6 — Release và thi
Đọc docs 12/13/16 và `qa/ACCEPTANCE.md`. Tạo installer Windows self-contained, E2E demo, kiểm tra restore backup, release checklist, user guide 1 trang, tài khoản & dữ liệu demo không chứa PII thật.
