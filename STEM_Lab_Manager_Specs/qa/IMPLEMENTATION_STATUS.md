# Trạng thái triển khai

Nguồn kiểm chứng: `../../docs/VALIDATION.md`, test TRX và source code trong `App/`, `tests/`. Ngày 12/10/2026.

- [x] P0: local setup/login, RBAC tại service, thiết bị/unit/bulk/consumable, người mượn, lịch đặt, mượn/trả/gia hạn, dashboard, backup/restore.
- [x] P1: QR PNG, ảnh thiết bị, import CSV/Excel với mapping/preview, export CSV/XLSX/PDF, audit, cảnh báo, kiểm kê số lượng/QR, bảo trì, calendar tuần, search/filter/sort/paging, Ctrl+K.
- [x] P2 phần đã kiểm bằng code/test: analytics local, forecast trung bình có giới hạn, ONNX detector/embedding thật, cosine candidates/unknown, consent/encrypted embedding/revocation, adapter camera Windows, movement challenge cơ bản, LLM optional aggregate và timeout/network handling.
- [x] Phân lớp Domain / Application / Infrastructure / AI / Desktop; design-time Migrations và Tools tách khỏi payload.
- [x] Ba EF migrations, DB v1 nâng lên current giữ dữ liệu, FK/unique/open-unit constraint, transaction IMMEDIATE và audit.
- [x] Demo DB riêng: 60 loại, 150 cá thể, 20 nhóm vật tư, 30 người giả, 209 phiếu, 20 lịch; reset bằng chuyển thư mục demo sau backup/đóng app.
- [x] 45 unit/integration/model tests + 8 UI tests đạt; build/publish thực, không dựa vào mock screens trong production.
- [x] User guide Markdown/PDF, mẫu CSV/XLSX, model licenses/checksums, kịch bản demo và walkthrough UI tự động có nhãn.
- [ ] Installer Setup.exe biên dịch bằng script trên Windows và cài trên Windows sạch.
- [ ] Nghiệm thu camera/ảnh đăng ký một lần/false-match/native libraries trên phần cứng Windows, không đồng nhất với inference tensor test.
- [ ] LLM endpoint/key thật; test outage/timeout bằng network thật.
- [ ] Chứng nhận rà soát UI thủ công, quyền xử lý sinh trắc học/retention/license; video demo thủ công 5–7 phút và khảo sát tác động giáo dục.

Không triển khai P3 (web/mobile/LAN multi-user/cloud/NFC/SIS/nhiều phòng), đúng phạm vi đặc tả. Checklist Acceptance gốc là gate sản phẩm; các mục kết hợp nhiều điều kiện chỉ được tick khi đủ toàn bộ điều kiện, không tick vì mới có code.
