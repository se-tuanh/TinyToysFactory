# 🧸 Tiny Toys Factory

**Tiny Toys Factory** là một tựa game mô phỏng quản lý nhà máy sản xuất đồ chơi (Manufacture Simulation) được phát triển trên Unity 2D URP. Trong game, người chơi sẽ vào vai một quản đốc nhà máy, chịu trách nhiệm quản lý tài nguyên, tối ưu hóa dây chuyền sản xuất đa công đoạn (Lắp ráp & Sơn/Đóng gói), và xử lý các sự kiện ngẫu nhiên áp lực cao để hoàn thành các đơn hàng đúng hạn.

---

## 🎮 Tính năng cốt lõi (Core Features)

- **Quản lý đa tài nguyên:** Cân đối giữa Vật liệu (Materials), Điện năng (Power) và Nhân công (Workers). Đảm bảo nhân công được nghỉ ngơi để tránh kiệt sức (Fatigue mechanic).
- **Dây chuyền sản xuất thực tế:** Các sản phẩm trải qua quy trình sản xuất A (Lắp ráp - Assembly) và quy trình B (Sơn & Đóng gói - Paint & Pack).
- **Hệ thống sự kiện ngẫu nhiên (Random Events):** Đối mặt với các sự kiện đòi hỏi xử lý nhanh như: Hỏng hóc máy móc (Machine Breakdown), Thiếu hụt vật liệu (Material Shortage), Biến động điện năng (Power Surge)... bằng hệ thống Decision Triad (Ưu tiên/Sửa chữa/Giao dịch).
- **Đa dạng Đơn hàng & Sản phẩm:** 
  - *Sản phẩm:* Xe đồ chơi (Toy Car), Robot, Búp bê cao cấp (Luxury Doll).
  - *Đối tác/Đơn hàng:* Toy Kingdom, Prestige Play, Flash Deals với các yêu cầu và thời hạn (deadline) khắt khe.

---



## 🚀 Hướng dẫn Cài đặt & Chạy (Thiết lập Unity 2022 LTS+)

### Bước 1 — Tạo Unity Project
1. Mở **Unity Hub** và chọn **New Project**.
2. Chọn Template **2D (URP)**.
3. Đặt tên Project là: `TinyToysFactory`.
4. Copy toàn bộ thư mục `Assets` của repository này đè lên thư mục `Assets` của project vừa tạo.

### Bước 2 — Cài đặt TextMeshPro
- Mở **Window → Package Manager → chọn TextMeshPro** → Install.
- Khi có thông báo Prompt, hãy bấm import **TMP Essential Resources**.

### Bước 3 — Tạo Dữ liệu ScriptableObject

**Products (Assets/ScriptableObjects/Products/)**
Click chuột phải → Create → TinyToysFactory → ProductData
- `Product_ToyCar` (Xe đồ chơi): Chi phí (Cost) 10/8, Lắp ráp 8s, Đóng gói 6s, 30 Điểm.
- `Product_Robot` (Robot): Chi phí 15/12, Lắp ráp 12s, Đóng gói 10s, 55 Điểm.
- `Product_Doll` (Búp bê): Chi phí 20/18, Lắp ráp 18s, Đóng gói 14s, 90 Điểm.
*(Tất cả cần 1 nhân công, 30 Điện năng lắp ráp, 20 Điện năng đóng gói)*

**Orders (Assets/ScriptableObjects/Orders/)**
Click chuột phải → Create → TinyToysFactory → OrderData
- Máy chủ và đối tác: `Order_ToyKingdom1`, `Order_Prestige1`, `Order_FlashDeal1` với các yêu cầu riêng biệt.

**Events (Assets/ScriptableObjects/Events/)**
Click chuột phải → Create → TinyToysFactory → RandomEventData
- Tạo các sự kiện tương ứng như hỏng máy, quá tải đơn hàng,...

### Bước 4 — Thiết lập Scene (`GameScene.unity`)

Đảm bảo cấu trúc Scene Hierarchy có đầy đủ các Manager (Sử dụng Singleton):
```text
├── [Manager]              (Empty GameObject)
│   ├── GameManager       
│   ├── ResourceManager   
│   ├── ProductionManager 
│   ├── OrderManager       (Gán danh sách các Order .asset vào availableOrders)
│   └── PressureDirector   (Gán các Event .asset vào eventPool)
├── [Machines]
│   ├── AssemblyMachineA   (Cần có Machine.cs, type=AssemblyA)
│   └── PaintMachineB      (Cần có Machine.cs, type=PaintPackB)
├── [Workers]              (Các Worker GameObject kèm Worker.cs)
└── Canvas (UI)
    ├── HUD                (UIManager.cs - Kéo thả các tham chiếu text/image)
    └── EventPopup         (EventPopupUI.cs - Bắt đầu với SetActive=false)
```
> **Lưu ý quan trọng:** Hãy đảm bảo tất cả các file ScriptableObject (Products, Orders, Events) đã được gán đầy đủ vào các thành phần Manager tương ứng trong thẻ `Inspector` trước khi bấm Play.

---

## 📋 Luồng trò chơi (Core Game Flow)

1. `GameManager.StartGame()` khởi chạy.
2. `OrderManager` tự động nhận đơn hàng đầu tiên.
3. Người chơi click vào các **Machine** để bắt đầu dải sản xuất (`StartBatchA()`).
4. `ProductionManager` xử lý dây chuyền từ công đoạn A sang công đoạn B.
5. Sau mỗi lô hoàn thành, `OrderManager` cập nhật tiến độ.
6. `PressureDirector` liên tục đánh giá và có thể kích hoạt Sự kiện ngẫu nhiên (`Random Events`).
7. Bảng `EventPopupUI` hiện lên, yêu cầu người chơi đưa ra quyết định (Ưu tiên sản xuất / Sửa chữa / Chấp nhận tổn thất).
8. Trò chơi kết thúc (Win) khi toàn bộ sản phẩm của đơn hàng được giao thành công trong thời gian cho phép; hoặc Thua (Lose) nếu hết thời gian/cạn kiệt tài nguyên.




