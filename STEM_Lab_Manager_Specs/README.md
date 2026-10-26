# STEM Lab Manager — Bộ đặc tả Markdown cho LLM

Ứng dụng desktop quản lý thiết bị của **một phòng STEM** trong trường học. Mục đích: giáo viên có thể cài và sử dụng bằng giao diện dễ hiểu; đồng thời có demo ứng dụng AI thực tế cho cuộc thi giáo dục. Tài liệu tiếng Việt, chia nhỏ theo module để AI coding agent tải theo ngữ cảnh.

**Phạm vi MVP:** 1 máy quản lý chính, local SQLite, hoạt động offline; camera/Face Recognition tùy chọn; chỉ LLM API có thể dùng Internet. **UI không gradient.**

## Cách sử dụng
1. Đặt toàn bộ thư mục này vào root repository.
2. Mở coding agent, yêu cầu đọc `AGENTS.md` và `prompts/01-master-prompt.md`.
3. Triển khai từng pha theo `prompts/02-phase-prompts.md`, không làm tất cả cùng một lượt.
4. Kiểm tra từng luồng bằng `qa/ACCEPTANCE.md`; ghi quyết định phát sinh vào `docs/DECISIONS.md`.

## Danh mục đặc tả
- [`docs/01-boi-canh-muc-tieu.md`](docs/01-boi-canh-muc-tieu.md) — 01. Bối cảnh và mục tiêu
- [`docs/02-pham-vi-mvp.md`](docs/02-pham-vi-mvp.md) — 02. Phạm vi, giả định và tiêu chí thành công
- [`docs/03-vai-tro-phan-quyen.md`](docs/03-vai-tro-phan-quyen.md) — 03. Nhóm người dùng & ma trận phân quyền
- [`docs/04-kien-truc-ky-thuat.md`](docs/04-kien-truc-ky-thuat.md) — 04. Kiến trúc và cấu trúc solution
- [`docs/05-dieu-huong-tabs.md`](docs/05-dieu-huong-tabs.md) — 05. Sơ đồ điều hướng & cấu trúc các tab
- [`docs/06-ui-ux-design-system.md`](docs/06-ui-ux-design-system.md) — 06. Hệ thống thiết kế UI/UX
- [`docs/07-chuc-nang-nghiep-vu.md`](docs/07-chuc-nang-nghiep-vu.md) — 07. Đặc tả chức năng từng phân hệ
- [`docs/08-state-machines-quy-tac.md`](docs/08-state-machines-quy-tac.md) — 08. Quy tắc và trạng thái nghiệp vụ
- [`docs/09-database-sqlite.md`](docs/09-database-sqlite.md) — 09. Mô hình dữ liệu SQLite
- [`docs/10-ai-face-llm.md`](docs/10-ai-face-llm.md) — 10. AI và nhận diện khuôn mặt
- [`docs/11-bao-mat-offline-backup.md`](docs/11-bao-mat-offline-backup.md) — 11. Bảo mật, riêng tư và khả năng vận hành offline
- [`docs/12-build-installer.md`](docs/12-build-installer.md) — 12. Build, installer và cập nhật
- [`docs/13-kiem-thu-nghiem-thu.md`](docs/13-kiem-thu-nghiem-thu.md) — 13. Test plan và tiêu chí nghiệm thu
- [`docs/14-lo-trinh-phat-trien.md`](docs/14-lo-trinh-phat-trien.md) — 14. Lộ trình lập trình bằng LLM
- [`docs/15-prompts-llm.md`](docs/15-prompts-llm.md) — 15. MASTER PROMPT — DÁN CHO LLM LẬP TRÌNH
- [`docs/16-demo-cuoc-thi.md`](docs/16-demo-cuoc-thi.md) — 16. Dữ liệu demo và kịch bản trình diễn dự thi
- [`docs/17-rui-ro-backlog.md`](docs/17-rui-ro-backlog.md) — 17. Rủi ro và nâng cấp trong tương lai

## Tài liệu điều phối
- [`AGENTS.md`](AGENTS.md): ràng buộc bắt buộc.
- [`prompts/01-master-prompt.md`](prompts/01-master-prompt.md): prompt bootstrap dự án.
- [`prompts/02-phase-prompts.md`](prompts/02-phase-prompts.md): prompt từng pha.
- [`qa/ACCEPTANCE.md`](qa/ACCEPTANCE.md): kiểm thử end-to-end và nghiệm thu.
- [`docs/DECISIONS.md`](docs/DECISIONS.md): nhật ký quyết định kiến trúc.

## Không nằm trong MVP
Website học sinh tự đặt trước qua Wi-Fi, đồng bộ đa máy, cloud, mobile, training từ đầu, theo dõi camera liên tục, phân tích khuôn mặt bắt buộc.
