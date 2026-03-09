# CASE STUDY: TINY TOYS FACTORY - TEN-PAGER v1

## Page 1 — Title, Tagline, High Concept, Audience

**Title:** Tiny Toys Factory
**Tagline:** Xây dựng nhanh. Quản lý khéo. Giao hàng đúng hạn.
**High Concept (2–3 sentences)**
Bạn là một quản đốc tại một nhà máy sản xuất đồ chơi nhộn nhịp, chịu trách nhiệm quản lý tài nguyên, nhân công và dây chuyền sản xuất đa công đoạn dưới áp lực thời gian khắt khe. Mỗi ca làm việc đều có những sự kiện ngẫu nhiên buộc bạn phải đưa ra quyết định chớp nhoáng: sửa chữa máy móc hỏng hóc, phân bổ lại điện năng, hay xử lý tình trạng thiếu hụt vật liệu để giữ cho dây chuyền tiếp tục chạy. Trò chơi mang đến trải nghiệm cân não trong việc tối ưu hóa quản lý và xử lý khủng hoảng liên tục.

**Target Player**
* Người chơi yêu thích thể loại mô phỏng quản lý (Management Simulation) và tối ưu hóa tài nguyên.
* Người chơi thích cảm giác áp lực thời gian và giải quyết nhiều công việc cùng lúc (multitasking).
* Người chơi tận hưởng khoảnh khắc "cứu bàn ngoạn mục" khi xử lý thành công một chuỗi sự kiện tồi tệ để giao hàng vừa kịp lúc.

**Platform & Controls**
* PC (Bàn phím & Chuột / Point-and-Click).
* Cốt lõi khả năng tiếp cận (Accessibility): Giao diện người dùng (UI) rõ ràng, có nút tạm dừng (Pause) để kiểm tra trạng thái, các cảnh báo sự kiện dễ đọc/nhận diện.

---

## Page 2 — Emotional Intent & Experience Pillars (with Design Rules)

**Emotional Intent (North Star)**
* **Căng thẳng (Tense):** Áp lực thời gian (deadline) + cạn kiệt tài nguyên khiến người chơi luôn phải giữ sự tập trung cao độ.
* **Thỏa mãn (Satisfaction/Flow):** Nhìn thấy dây chuyền A (Lắp ráp) chuyển tiếp mượt mà sang dây chuyền B (Sơn/Đóng gói) tạo ra cảm giác thành tựu lớn ("Mọi thứ đang vận hành hoàn hảo").

**We know we succeeded when…**
* Người chơi thở phào nhẹ nhõm hoặc reo lên khi giao đơn hàng thành công ở giây cuối cùng.
* Khoảng 70% các màn chơi kết thúc với thời gian còn lại dưới 15% (duy trì "vùng căng thẳng").

**3 Experience Pillars**
1. Cân bằng Đa Tài nguyên (Multi-Resource Tension)
2. Tối ưu hóa Dây chuyền Lắp ráp - Đóng gói
3. Quyết định Khủng hoảng (Crisis Decision Making)

**Pillars → Design Rules (must-haves)**
* **P1 (Tài nguyên):** Vật liệu, Điện năng và Sức lực nhân công (Fatigue) không bao giờ là đủ để chạy tối đa công suất mọi máy móc cùng lúc.
* **P2 (Dây chuyền):** Luôn có độ trễ/thời gian chờ giữa các công đoạn (Lắp ráp -> Sơn). Người chơi phải dự đoán và nạp lệnh sản xuất trước để tránh tắc nghẽn.
* **P3 (Khủng hoảng):** Giải quyết các sự kiện ngẫu nhiên luôn đi kèm một cái giá phải trả (Mất thời gian / Mất tài nguyên / Mất uy tín). Không có lựa chọn nào là "hoàn hảo".

---

## Page 3 — Core Loop, Win/Lose, Mission Structure

**Core Loop (repeatable)**
Nhận Đơn Hàng (Order) → Phân bổ Nhân công/Điện năng → Chạy Máy A (Lắp ráp) → Chuyển sang Máy B (Sơn/Đóng gói) → Xử lý Sự kiện Ngẫu nhiên (Decision) → Giao hàng → Nhận Tiền/Uy tín → Nâng cấp → Lặp lại.

**Win / Lose Conditions (MVP)**
* **Win:** Hoàn thành và giao đủ số lượng Sản phẩm (Car, Robot, Doll) cho đối tác (Toy Kingdom, Prestige Play...) trước khi Thời gian (Deadline) = 0.
* **Lose:** Hết giờ trước khi hoàn thành đơn, HOẶC làm cạn kiệt Ngân sách/Tài nguyên dẫn đến dây chuyền dừng hoạt động hoàn toàn.

**Mission Structure (Ca làm việc 5–8 phút)**
* Chia làm 3-4 đợt sản xuất (Batches).
* **Nhịp độ (Pacing):** Bắt đầu trơn tru (1-2 phút) → Chạy luân phiên (2 phút) → Khủng hoảng/Sự kiện xuất hiện (10-15s) → Nước rút cuối cùng (Final Sprint).

---

## Page 4 — Signature Mechanics (USP) & Game Identity

**Game Identity (one line)**
Một tựa game quản lý thời gian và tài nguyên nhà máy, tập trung vào việc duy trì nhịp độ sản xuất thông qua việc xử lý các "đỉnh điểm khủng hoảng" (decision spikes).

**Signature Mechanics (4 bullets)**
* **Dây chuyền 2 Công đoạn (A & B):** Sản phẩm phải đi qua Lắp ráp (Assembly) tốn 30 Điện năng, sau đó chuyển sang Đóng gói (Paint & Pack) tốn 20 Điện năng. Cần căn chỉnh thời gian chuẩn xác. 
* **Bộ ba Quyết định Sự kiện (Decision Triad):** Mỗi khi có Sự kiện ngẫu nhiên (VD: Hỏng máy), người chơi chọn 1 trong 3 cách: Rút tiền sửa nhanh / Dừng máy để nhân công sửa / Bỏ qua và chịu sản xuất chậm.
* **Hệ thống Độ mỏi (Worker Fatigue):** Nhân công không phải là máy tính. Họ cần nghỉ ngơi. Ép nhân công làm việc liên tục sẽ giảm tốc độ sản xuất hoặc gây ra lỗi.
* **Đạo diễn Áp lực (Pressure Director):** AI ngầm liên tục đo lường tiến độ của người chơi. Chơi càng tốt, tiến độ càng nhanh -> các sự kiện ngẫu nhiên sinh ra (Hỏng hóc, Thiếu vật tư) càng dồn dập ở cuối màn để thử thách.

**No-Go Rules**
* Không có yếu tố xây dựng căn cứ tự do (Base-building). Vị trí máy móc là cố định để tập trung vào quản lý.
* Không có chu kỳ sản xuất tự động hoàn toàn; người chơi phải luôn click để kích hoạt các lô hàng (Start Batch).

---

## Page 5 — Player Kit (Management & Tools)

**Management Kit (MVP)**
* **Phân công (Assign):** Kéo thả hoặc click để chỉ định Worker vào Máy A, Máy B, hoặc Phòng nghỉ (Break Room).
* **Khởi động (Start Batch):** Bấm nút chạy dây chuyền khi rổ nguyên liệu đủ và có nhân công.
* **Điều phối Điện (Power Routing):** Bật/Tắt công tắc điện của các khu vực để dồn 100% điện năng cho khu vực đang cần gấp.

**Khủng hoảng & Công cụ Hỗ trợ (Gadgets/Actions)**
* **Coffee Break (Hồi phục):** Gửi gấp nhân công đi uống cà phê để giảm Fatigue ngay lập tức.
* **Overdrive (Ép xung):** Tăng 150% tốc độ máy móc nhưng tiêu hao gấp đôi Điện năng và tăng tỷ lệ hỏng máy trong 10 giây.
* **Emergency Supplies (Gọi vật tư Khẩn):** Gọi ngay Vật liệu bay đến bằng drone khi kho cạn kiệt (tốn nhiều tiền hơn mua bình thường).

---

## Page 6 — Core Systems (States) & Pressure Director

**Core States (Năm chỉ số cốt lõi)**
1. **Time (Deadline):** Bộ đếm ngược quyết định thắng thua.
2. **Materials (Vật liệu):** Nhiên liệu để chế tạo đồ chơi.
3. **Power (Điện năng):** Giới hạn tổng công suất (Ví dụ Max 50).
4. **Fatigue (Sức lực):** Trạng thái của từng Worker.
5. **Reputation (Uy tín):** Điểm số dùng để mở khóa màn/đơn hàng mới.

**Hệ thống Pressure Director (Đạo diễn Áp lực)**
* **Pressure tăng:** Khi có nhiều máy móc cùng chạy, hoặc khi sắp đến sát giờ deadline của đơn hàng Hot (ví dụ Flash Deals).
* **Pressure Tier:**
  * T1: Thiếu hụt vật tư nhỏ, chớp nháy điện nhẹ.
  * T3: Nhân sự kiệt sức nhanh hơn, đơn hàng yêu cầu biến đổi đột xuất.
  * T5: Mất điện cục bộ, một máy hỏng hoàn toàn cần sửa chữa khẩn cấp.
* **Resource Tension trong thực tế:** 
  "Bạn có một lượng điện giới hạn. Nếu bật cả máy Lắp Robot (chi phí cao) và máy Lắp Búp bê (chi phí cao), hệ thống sẽ quá tải. Bạn bắt buộc phải cho máy Đóng gói nghỉ tạm thời."

---

## Page 7 — Encounter Grammar: Decision Triad Events

**Encounter = "Khủng hoảng cục bộ" (Decision Spike)**
Mục tiêu: Đưa ra lựa chọn chớp nhoáng (dưới 5 giây) khi thanh pop-up báo động xuất hiện.

| Choice (Lựa chọn) | Lợi ích (Benefit) | Đánh đổi (Cost) | Tình huống lý tưởng (When it shines) |
| :--- | :--- | :--- | :--- |
| **Bỏ tiền giải quyết (Pay/Fix Fast)** | Máy móc lập tức hoạt động lại, tiết kiệm thời gian | Tốn nhiều Tiền/Điểm số | Sắp đến Hạn chót (Deadline priority) |
| **Nhân công tự sửa (Manual Repair)** | Không tốn tiền, giữ nguyên biên độ lợi nhuận | Máy dừng hoạt động 10s, Worker hao mòn sức lực | Ở đầu màn chơi, thời gian còn dư dả |
| **Phớt lờ/Tạm chấp nhận (Ignore/Trade-off)** | Dây chuyền không bị lùi thời gian | Sản lượng chậm đi 30% hoặc mất đi Vật liệu | Khi không rảnh tay, đang tập trung dồn sản phẩm khác |

**Telegraph Rules (Nhận biết trong 1 giây)**
* **Hỏng máy:** Còi báo động đỏ + Tia lửa điện tóe ra từ GameObject của Máy.
* **Worker kiệt sức:** Biểu tượng mồ hôi giọt nước to trên đầu Worker + tốc độ di chuyển chậm hẳn đi.
* **Sự cố đơn hàng:** Flash UI màu vàng cạy trên góc Order Manager + m thanh "Ping" khẩn cấp.

---

## Page 8 — Progression & Upgrade Philosophy

**Progression Currencies**
* **Credits (Tiền):** Mua nhân công mới, mua nâng cấp trong màn chơi hoặc giữa các màn.
* **Reputation (Uy tín):** Mở khóa các Đối tác yêu cầu cao hơn (Prestige Play).

**Upgrade Philosophy**
Mỗi mục nâng cấp phải thay đổi trực tiếp chiến thuật của người chơi:
* **Tốc độ vs Tiêu hao:** Nâng cấp máy Lắp ráp A chạy nhanh hơn 20%, nhưng sẽ yêu cầu cộng thêm 10 Điện năng.
* **Quản trị Nhân sự:** Nâng cấp ghế phòng nghỉ giúp Worker hồi phục Fatigue nhanh gấp đôi.

**3 Branches (Hướng phát triển)**
1. **WORKER-FOCUS (Nhân sự):** Ít Fatigue, chạy nhanh hơn, tự động sửa chữa các hỏng hóc nhỏ.
2. **MACHINE-FOCUS (Máy móc):** Rút ngắn quy trình từ 12s -> 8s (đối với Robot), chứa được nhiều thành phẩm chờ trong hàng đợi hơn.
3. **LOGISTICS-FOCUS (Hậu cần):** Tăng giới hạn Điện năng tổng, giảm chi phí mua Vật tư.

---

## Page 9 — Level Design, Client Identity, Mission Types

**Level Design Rules (MVP)**
* Dây chuyền trải dọc màn hình từ trái sang phải: Kho Vật liệu -> Máy A -> Băng chuyền trung gian -> Máy B -> Khu xuất hàng.
* Giao diện UIManager (HUD) phải hiển thị luôn được 3 thông số: Điện, Vật tư, Thời gian ở vị trí dễ nhìn nhất.
* Onboarding: Màn đầu tiên chỉ yêu cầu sản xuất Toy Car (sản phẩm đơn giản nhất) và không có Sự kiện ngẫu nhiên.

**Các Đối tác (Clients/Orders)**
1. **Toy Kingdom:** Đơn hàng cơ bản, yêu cầu số lượng lớn xe đồ chơi (Toy Car), thời gian thoải mái.
2. **Prestige Play:** Yêu cầu Búp bê và Robot chất lượng cao, tiền phạt (penalty) cực lớn nếu trễ giờ.
3. **Flash Deals:** Đơn hàng chớp nhoáng, deadline cực ngắn nhưng tiền công gấp 3.

**Mission Types (Thể loại Màn chơi)**
* **Standard Shift:** Sản xuất cân bằng các loại hàng theo yêu cầu.
* **Power Outage Shift:** Điện năng trần bị giảm 30%. Ép người chơi phải liên tục bật/tắt (micromanage) các máy luân phiên.
* **Rush Hour:** Không có thời gian chờ, tất cả các máy đều phải chạy liên tục nếu muốn chiến thắng.

---

## Page 10 — MVP Scope, Vertical Slice, Risks, Metrics

**MVP Scope Box (v1)**
* **Trong phạm vi (In-scope):**
  * 1 Level/Scene chuẩn (`GameScene.unity`).
  * 3 Loại sản phẩm (Toy Car, Robot, Luxury Doll) kết nối qua ScriptableObjects.
  * 2 Công đoạn máy móc (A & B) và 1 loại Worker.
  * Hệ thống Pressure Director xử lý tối thiểu 3 Sự kiện ngẫu nhiên (Hỏng máy, Nghỉ mệt, Thiếu vật tư).
  * Vòng lập nhận đơn - xử lý - trả hàng.
* **Ngoài phạm vi (Out-of-scope for now):**
  * Hệ thống xây dựng lắp đặt máy tự do, cốt truyện chi tiết, nhiều phân xưởng (multi-factory).

**Vertical Slice (Màn Demo 5 phút)**
* Phải bao gồm:
  * Một đơn hàng gồm 5 Toy Car và 2 Robot.
  * Thể hiện rõ việc phải phân bổ Điện năng (Power) nếu bật cả máy A và máy B cùng lúc.
  * Xuất hiện ít nhất 1 đợt Hỏng máy ngẫu nhiên (chạy EventPopupUI) để kiểm chứng Decision Triad.

**Top Risks & Risk-First Prototype Plan**
* **Rủi ro:** UI hiển thị quá rối rắm đối với các thông số (Thời gian, Điện, Fatigue, Vật tư).
  * *Giải pháp:* Thiết kế HUD tối giản, dùng biểu tượng màu sắc (Đỏ = Thiếu, Xanh = Đủ) thay vì chỉ dùng số.
* **Rủi ro:** Pacing (nhịp độ) bị nhàm chán khi chờ hàng chuyển từ A sang B.
  * *Giải pháp:* Ép người chơi quản lý Event ngẫu nhiên hoặc lo tính toán cho đơn hàng tiếp theo trong thời gian máy đang chạy.

**Success Metrics (Chỉ số Thành công Playtest)**
* Thời gian ra quyết định (Decision Latency) ở bảng pop-up Sự kiện < 5 giây.
* Ít nhất 60% người chơi trải nghiệm cảm giác "vừa kịp lúc" (hoàn thành từ 0-10s cuối cùng).
* Số lượng người chơi thua tập trung vào "Hết giờ" (deadline) thay vì "Chán mạng" (bỏ game giữa chừng do không hiểu luật).
