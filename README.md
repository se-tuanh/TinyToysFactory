#  Tiny Toys Factory

**Tiny Toys Factory** là một tựa game mô phỏng quản lý nhà máy sản xuất đồ chơi (Manufacture Simulation) được phát triển trên Unity 2D URP. Trong game, người chơi sẽ vào vai một quản đốc nhà máy, chịu trách nhiệm quản lý tài nguyên, tối ưu hóa dây chuyền sản xuất đa công đoạn (Lắp ráp & Sơn/Đóng gói), và xử lý các sự kiện ngẫu nhiên áp lực cao để hoàn thành các đơn hàng đúng hạn.

---

##  Core Features

- **Quản lý đa tài nguyên:** Cân đối giữa Vật liệu (Materials), Điện năng (Power) và Nhân công (Workers). Đảm bảo nhân công được nghỉ ngơi để tránh kiệt sức (Fatigue mechanic).
- **Dây chuyền sản xuất thực tế:** Các sản phẩm trải qua quy trình sản xuất A (Lắp ráp - Assembly) và quy trình B (Sơn & Đóng gói - Paint & Pack).
- **Hệ thống sự kiện ngẫu nhiên (Random Events):** Đối mặt với các sự kiện đòi hỏi xử lý nhanh như: Hỏng hóc máy móc (Machine Breakdown), Thiếu hụt vật liệu (Material Shortage), Biến động điện năng (Power Surge)... bằng hệ thống Decision Triad (Ưu tiên/Sửa chữa/Giao dịch).
- **Đa dạng Đơn hàng & Sản phẩm:** 
  - *Sản phẩm:* Xe đồ chơi (Toy Car), Robot, Búp bê cao cấp (Luxury Doll).
  - *Đối tác/Đơn hàng:* Toy Kingdom, Prestige Play, Flash Deals với các yêu cầu và thời hạn (deadline) khắt khe.





## Core Game Flow

1. `GameManager.StartGame()` khởi chạy.
2. `OrderManager` tự động nhận đơn hàng đầu tiên.
3. Người chơi click vào các **Machine** để bắt đầu dải sản xuất (`StartBatchA()`).
4. `ProductionManager` xử lý dây chuyền từ công đoạn A sang công đoạn B.
5. Sau mỗi lô hoàn thành, `OrderManager` cập nhật tiến độ.
6. `PressureDirector` liên tục đánh giá và có thể kích hoạt Sự kiện ngẫu nhiên (`Random Events`).
7. Bảng `EventPopupUI` hiện lên, yêu cầu người chơi đưa ra quyết định (Ưu tiên sản xuất / Sửa chữa / Chấp nhận tổn thất).
8. Trò chơi kết thúc (Win) khi toàn bộ sản phẩm của đơn hàng được giao thành công trong thời gian cho phép; hoặc Thua (Lose) nếu hết thời gian/cạn kiệt tài nguyên.




