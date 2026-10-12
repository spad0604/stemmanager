# 09. Mô hình dữ liệu SQLite

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).

Gợi ý bảng/aggregate. LLM phải tạo migration thật, khóa ngoại, unique index, index tra cứu và test constraint; không chỉ tạo model đơn giản.


| Bảng | Trường trọng tâm / quan hệ |
| --- | --- |
| users / roles / user_roles | user_id, username unique, password_hash, is_active, role_id; quyền tập trung. |
| borrowers | borrower_id, code unique, display_name, class_name, type, contact_optional, active. |
| categories / locations | category_id, name; location_id, room, cabinet, shelf. |
| assets | asset_id, code unique, name, kind UNIT/BULK, category_id, minimum_stock, metadata. |
| asset_units | unit_id, asset_id FK, serial unique nullable, qr_value unique, condition, status, location_id. |
| inventory_ledger | entry_id, asset_id, qty_delta, reason, reference_type/id, actor_id, created_utc. |
| reservations / reservation_lines | reservation_id, borrower_id, start_utc, end_utc, status, approved_by; asset/unit/quantity. |
| loans / loan_lines | loan_id, borrower_id, issued_by, due_utc, status; unit_id nullable, asset_id, qty_issued, qty_returned. |
| return_transactions / return_lines | return_id, loan_id, received_by, timestamp, condition, qty_returned. |
| maintenance_cases | unit_id, reason, opened_utc, closed_utc, resolution. |
| face_profiles / consents | borrower_id, encrypted embedding, model_version, consent_id, created_utc, revoked_utc. |
| audit_events | actor_id, action, entity_type/id, before/after sanitized, created_utc, device_id. |
| app_settings / schema_version | key, encrypted sensitive value where needed; EF migrations. |


## 9.1. Index, transaction, migration

- Index borrowers(code), assets(code, category_id), loans(status,due_utc), loan_lines(unit_id), reservations(status,start_utc,end_utc), audit(created_utc).
- Index có điều kiện hoặc logic service bảo đảm một unit chỉ có một loan open; SQLite transaction IMMEDIATE cho các thao tác tranh chấp.
- Số lượng vật tư: ledger là nguồn chân lý, snapshot/cached balance có kiểm tra đối soát khi cần; mọi ghi ledger + phiếu là atomic.
- Thiết lập FK ON; migrations versioned; kiểm thử nâng cấp DB v1→v2 với file dữ liệu thực tế và kịch bản rollback từ backup.
