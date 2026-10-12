# 17. Rủi ro và nâng cấp trong tương lai

> Nguồn: đặc tả STEM Lab Manager v1.0. Quy tắc triển khai chung xem [`../AGENTS.md`](../AGENTS.md).


| Rủi ro | Giảm thiểu |
| --- | --- |
| Mất máy / hỏng ổ cứng | Backup tự động và cảnh báo chưa có bản copy ngoài máy. |
| Sinh trắc học sai / thiếu consent | Mặc định tắt, xác nhận thủ công, QR fallback, xóa profile. |
| Tranh chấp lịch mượn | Kiểm lại và ghi SQLite transaction, invariant tests. |
| LLM hallucination | KPI tính bằng SQL, nguồn dữ liệu & kỳ thống kê rõ, không tự sửa DB. |
| Phần mềm dùng trên Linux & Windows | Avalonia; test platform-specific camera/native runtime; Windows release là mục tiêu. |
| Phụ thuộc OSS license | Lập software bill of materials, xác minh model weights và thương mại hóa. |
| Giáo viên không rành kỹ thuật | First-run wizard, quick actions, hướng dẫn tiếng Việt, nút backup/restore. |

Ưu tiên để thi  Một phần mềm P0/P1 hoạt động hoàn chỉnh, không lỗi dữ liệu và giao diện chỉn chu có giá trị thuyết phục hơn một bản demo quá nhiều “AI” nhưng nút giả và không thể cài đặt.

PHỤ LỤC A — CHECKLIST GIAO NHẬN

☐  Có solution build được; Git repository sạch; cấu trúc module đúng

☐  Có vai trò/permission matrix và thử nghiệm quyền

☐  Đủ sidebar/tab; giao diện phẳng không gradient; responsive DPI

☐  Nghiệp vụ mượn/trả/đặt/kiểm kê/bảo trì hoạt động và có audit

☐  SQLite migration, backup/restore chạy thực tế

☐  Không gửi dữ liệu cá nhân hoặc sinh trắc học lên LLM

☐  Face opt-in, no-camera fallback và demo không nhận dạng sai như đúng

☐  Test suite tự động chạy và có kết quả

☐  Installer chạy trên máy Windows sạch; update không mất dữ liệu

☐  Có user guide, demo seed, video và hồ sơ bản quyền
