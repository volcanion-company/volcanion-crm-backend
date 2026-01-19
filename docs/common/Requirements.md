1. Mục tiêu của CRM

CRM được xây dựng để:

Quản lý toàn bộ vòng đời khách hàng

Tăng hiệu quả bán hàng – marketing – chăm sóc khách hàng

Chuẩn hóa dữ liệu khách hàng, tránh phân tán

Hỗ trợ ra quyết định bằng dữ liệu (report, insight)

2. Nhóm người dùng chính
Nhóm	Vai trò
Admin	Quản trị hệ thống, phân quyền
Sales	Quản lý lead, cơ hội, đơn hàng
Marketing	Chiến dịch, phân khúc KH
CSKH	Ticket, hỗ trợ, chăm sóc
Manager	Báo cáo, KPI
Integration/System	Đồng bộ dữ liệu
3. Phân tích chức năng theo module
3.1. Quản lý khách hàng (Customer Management)
Chức năng

Tạo / sửa / xoá / merge khách hàng

Lưu thông tin:

Cá nhân / Doanh nghiệp

Thông tin liên hệ

Nguồn khách hàng

Tags / phân loại

Quản lý lịch sử tương tác

Nhiệm vụ hệ thống

Tránh trùng dữ liệu (duplicate detection)

Ghi log lịch sử thay đổi

Phân quyền xem/sửa dữ liệu

📌 Output: Hồ sơ khách hàng 360°

3.2. Quản lý Lead (Lead Management)
Chức năng

Thu lead từ:

Website

Facebook / Google

Import Excel

API

Phân công lead cho sales

Chuyển lead → customer/opportunity

Nhiệm vụ

Validate dữ liệu lead

Chống trùng lead

Theo dõi trạng thái lead

📌 Pipeline ví dụ:

New → Contacted → Qualified → Converted → Lost

3.3. Quản lý cơ hội bán hàng (Opportunity / Deal)
Chức năng

Tạo deal

Gắn với khách hàng

Theo dõi pipeline bán hàng

Dự báo doanh thu

Nhiệm vụ

Tính toán xác suất thắng

Tổng hợp giá trị deal

Tracking thời gian bán hàng

📌 Output: Sales pipeline, forecast

3.4. Quản lý đơn hàng & hợp đồng
Chức năng

Tạo báo giá

Chuyển báo giá → đơn hàng

Quản lý hợp đồng

Gia hạn hợp đồng

Nhiệm vụ

Mapping với sản phẩm/dịch vụ

Kiểm soát trạng thái:

Draft → Approved → Signed → Completed

3.5. Marketing Automation
Chức năng

Quản lý chiến dịch

Email / SMS / Push notification

Phân khúc khách hàng

Tracking hiệu quả chiến dịch

Nhiệm vụ

Gửi theo kịch bản

Ghi nhận open / click / conversion

A/B testing

📌 Output: ROI chiến dịch marketing

3.6. Chăm sóc khách hàng (Customer Support / Ticket)
Chức năng

Tạo ticket từ:

Email

Hotline

Chat

SLA / ưu tiên xử lý

Lịch sử xử lý

Nhiệm vụ

Đảm bảo SLA

Theo dõi trạng thái ticket

Đánh giá mức độ hài lòng

📌 Lifecycle:

Open → In Progress → Waiting → Resolved → Closed

3.7. Lịch & công việc (Task / Activity)
Chức năng

Lịch hẹn

Nhắc việc

Gắn task với customer/deal

Nhiệm vụ

Đồng bộ calendar

Nhắc việc realtime

3.8. Báo cáo & phân tích (Report & Analytics)
Chức năng

Dashboard realtime

KPI sales

Phân tích khách hàng

Nhiệm vụ

Tổng hợp dữ liệu đa chiều

Xuất Excel / PDF

Phân quyền xem báo cáo

3.9. Phân quyền & bảo mật (Security & RBAC)
Chức năng

Role-based access control

Phân quyền theo:

Module

Field

Dữ liệu (data scope)

Nhiệm vụ

Audit log

Token / OAuth / SSO

3.10. Tích hợp hệ thống (Integration)
Chức năng

Kết nối:

ERP

Email

SMS

Payment

Social network

Nhiệm vụ

Webhook

Retry / Queue

Đồng bộ dữ liệu

4. Chức năng phi chức năng (Non-functional)
Nhóm	Yêu cầu
Performance	Xử lý lớn, realtime
Scalability	Microservices
Availability	99.9% uptime
Security	Encryption, RBAC
Audit	Trace hành vi
Backup	Dữ liệu khách hàng
5. Mapping sang tài liệu BA / QC
Tối giản tài liệu cho team nhỏ

👉 1 tài liệu có thể cover cả nghiệp vụ + test:

Use Case

Business Rule

Acceptance Criteria

Test Scenario

📌 Ví dụ:

Use case: Tạo Lead
Given: Lead chưa tồn tại
When: User nhập email hợp lệ
Then: Lead được tạo và gán cho sales