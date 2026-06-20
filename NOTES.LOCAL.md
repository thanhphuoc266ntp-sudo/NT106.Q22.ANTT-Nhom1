Date: 2026-06-16
Task / Ticket: Thêm JWT auth + revoke local, cleanup và logging
Files changed:
- Services/AuthService.cs (added, JWT gen/validate + revoke store)
- Models/UserSession.cs (add AccessToken)
- Views/LoginWindow.xaml.cs (generate and store AccessToken on login)
- Services/TcpClientService.cs (send token in CONTROL_REQUEST)
- Services/TcpServerService.cs (validate token; update stream.WriteAsync overload)
- Views/MainWindow.xaml.cs (ConnectToRemote -> async Task, logging, OnClosing cleanup, revoke on logout)
- README.md (local notes section)

Summary:
- Triển khai flow token-based auth cơ bản (JWT) cho CONTROL_REQUEST; token được sinh tại client sau login và gửi kèm khi kết nối.
- Thêm revocation in-memory để revoke token local khi logout.
- Cải thiện cleanup khi đóng window và thêm logging cho xử lý ảnh.

Quick test instructions:
1. Build project.
2. Login (LoginWindow) — kiểm tra `UserSession.AccessToken` được set.
3. Trên máy khác, chọn client, bật Control Mode — server nên nhận token và yêu cầu ConfirmWindow.
4. Logout trên host có token — gọi `AuthService.RevokeToken`; nếu server và client chạy trên cùng host thì token sẽ bị reject ngay.
5. Kiểm tra Output window (Debug) để thấy lỗi xử lý ảnh nếu có.

Notes / TODO:
- Triển khai token issuance trên server trung tâm nếu muốn secure/central auth.
- Thêm TLS (`SslStream`) cho kết nối TCP trước khi mở Internet access.