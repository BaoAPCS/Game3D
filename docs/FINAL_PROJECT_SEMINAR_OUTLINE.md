# Dàn ý Final Project Seminar — Game 3D ba chương

> **Mục đích:** Khung nội dung để viết báo cáo và chuẩn bị phần trình bày Final Project Seminar. Các mục có nhãn **[Cần chốt]** là định hướng cần được nhóm thống nhất, không phải tính năng đã hoàn thành.

## 1. Thông tin dự án

| Hạng mục | Nội dung |
| --- | --- |
| Tên game | **[Cần chốt]** |
| Thể loại / góc nhìn | Game khám phá – giải đố / góc nhìn thứ ba (third-person) |
| Engine | Unity `6000.5.0f1` |
| Render pipeline | Universal Render Pipeline (URP) `17.5.0` |
| Phạm vi | Một hành trình gồm ba chương: ký túc xá, đồn cảnh sát và bệnh viện |
| Nền tảng mục tiêu | **[Cần chốt]** — hiện codebase có cấu hình input cho bàn phím, chuột và gamepad |

### 1.1. Ý tưởng tổng quan

Người chơi khám phá các địa điểm liên kết với cùng một vụ việc. Mỗi chương đưa người chơi đến một không gian có bầu không khí và thử thách riêng, đồng thời cung cấp manh mối để mở đường sang chương tiếp theo.

> **[Cần chốt]** Viết 2–3 câu về nhân vật chính, bí ẩn trung tâm và điều kiện kết thúc trò chơi. Phần này nên dùng cùng một cốt truyện xuyên suốt cả ba chương.

### 1.2. Mục tiêu thiết kế

- Tạo trải nghiệm khám phá 3D có tiến trình rõ ràng qua ba bản đồ.
- Kết hợp điều khiển nhân vật, tương tác môi trường, nhặt vật phẩm, mục tiêu và giải đố.
- Dùng ánh sáng, âm thanh, bố cục không gian và NPC để tạo không khí căng thẳng.
- Tái sử dụng hệ thống gameplay cốt lõi giữa các chương để tập trung công sức cho nội dung bản đồ và nhiệm vụ.

## 2. Tổng quan tiến trình game

```text
Chương 1: Ký túc xá
        │  Manh mối / vật phẩm then chốt
        ▼
Chương 2: Đồn cảnh sát
        │  Làm rõ vụ việc, mở ra địa điểm cuối
        ▼
Chương 3: Bệnh viện
        │  Đối đầu / giải đáp bí ẩn
        ▼
Kết thúc game
```

Mỗi chương nên có cùng cấu trúc: **mở đầu → khám phá → thu thập hoặc giải đố → trở ngại → mục tiêu kết chương**. Nội dung truyện cụ thể cần được nhóm chốt trước khi làm cutscene và hội thoại.

## 3. Nội dung theo chương

### Chương 1 — Ký túc xá

**Vai trò trong game:** Giới thiệu nhân vật, cách điều khiển, tương tác và bí ẩn khởi đầu.

**Bản đồ và tiến trình hiện có trong codebase:** scene `Assets/Chapter1/Scenes/Chapter1_Dormitory.unity` có các khu vực Room Nam, Room Minh, phòng máy tính, kho thiết bị, nhà vệ sinh, hành lang chính, hành lang tối, cầu thang, sân thượng và khu nhà hàng đối diện. Các marker mục tiêu đã định nghĩa luồng từ rời phòng Nam đến khu nhà hàng.

**Gameplay đã có hoặc đã được chuẩn bị:**

- Di chuyển third-person, camera orbit, chạy nước rút có stamina và cúi người.
- Tương tác bằng ray/sphere cast, prompt UI, highlight vật thể và kiểm tra vật cản.
- Nhặt đồ, túi đồ, đèn pin, vật phẩm ném được và đồng bộ lưu game JSON.
- Cửa, keypad cho cửa phòng Dũng, vật thể tương tác và hệ thống mục tiêu/HUD.
- NPC tuần tra, đuổi bắt Henry, giao thông đường phố, animation idle/chạy và màn hình game over.

**Dàn ý nội dung trình bày:**

1. Người chơi thức dậy hoặc bắt đầu tại ký túc xá. **[Cần chốt bối cảnh mở đầu]**
2. Thực hiện các nhiệm vụ hướng dẫn: di chuyển, sprint, crouch, quan sát và tương tác.
3. Khám phá các phòng, thu thập vật phẩm/manh mối và mở khu vực bị khóa bằng keypad hoặc điều kiện nhiệm vụ.
4. Vượt qua hành lang tối/khu vực có NPC nguy hiểm. **[Cần chốt điều kiện thất bại và cách vượt qua]**
5. Đến điểm kết chương, nhận manh mối dẫn đến đồn cảnh sát.

**Kết quả cần trình diễn:** Một vòng chơi hoàn chỉnh từ điểm spawn đến objective cuối; người chơi sử dụng tối thiểu một vật phẩm, một tương tác khóa/mở và một tình huống áp lực hoặc truy đuổi.

### Chương 2 — Đồn cảnh sát

**Vai trò trong game:** Mở rộng điều tra, làm rõ mối liên hệ giữa manh mối ở ký túc xá và bệnh viện.

**Tài nguyên hiện có:** scene `Assets/Chapter2/Scenes/Police_Station.unity` và model `Assets/Chapter2/Models/police_station_3d_model.glb`. Chưa thấy script gameplay chuyên biệt trong `Assets/Chapter2` tại thời điểm lập dàn ý.

**Dàn ý nội dung đề xuất:**

1. Người chơi đến đồn cảnh sát với manh mối từ Chương 1.
2. Khám phá sảnh, quầy trực, văn phòng điều tra, phòng hồ sơ và khu tạm giữ. **[Cần chốt layout thực tế của map]**
3. Tìm hồ sơ/camera/ghi âm để ghép các thông tin thành bằng chứng.
4. Dùng puzzle truy cập hồ sơ hoặc tìm chìa khóa/thẻ để mở khu vực quan trọng.
5. Một phát hiện mới chỉ ra bệnh viện là địa điểm cuối hoặc nơi nhân vật cần đến.

**Hệ thống cần triển khai hoặc tái sử dụng:** player/camera/input, tương tác, inventory, objective HUD, cửa khóa, save/load và chuyển scene. Có thể bổ sung NPC đối thoại, puzzle tài liệu và kiểm tra điều kiện hoàn thành chương.

**Kết quả cần trình diễn:** Một puzzle điều tra có đầu vào–đầu ra rõ ràng, một chuỗi mục tiêu, và chuyển tiếp hợp lý sang Chương 3.

### Chương 3 — Bệnh viện

**Vai trò trong game:** Cao trào và kết thúc bí ẩn.

**Trạng thái hiện tại:** Chưa có thư mục `Assets/Chapter3` trong codebase. Map, scene, tài nguyên và gameplay của chương này cần được tạo mới.

**Dàn ý nội dung đề xuất:**

1. Người chơi vào bệnh viện để xác minh manh mối cuối cùng.
2. Khám phá sảnh, hành lang, khu tiếp nhận, phòng bệnh và khu hạn chế. **[Cần chốt layout]**
3. Kết hợp thông tin/vật phẩm thu được để mở đường tới khu vực trọng tâm.
4. Tạo thử thách cao trào: né tránh mối nguy, giải puzzle nhiều bước hoặc lựa chọn đối thoại. **[Cần chốt cơ chế chính]**
5. Trình bày sự thật của vụ việc và ending. Có thể dùng một ending hoặc nhiều ending tùy lựa chọn trước đó. **[Cần chốt]**

**Hệ thống cần triển khai:** scene bệnh viện, ánh sáng/âm thanh không khí, objective chain, checkpoint/save, nội dung cuối game và màn hình kết thúc. Nếu thêm AI hoặc stealth, cần xác định sớm để kiểm thử NavMesh và hiệu năng.

**Kết quả cần trình diễn:** Vòng chơi hoàn tất trò chơi, có cao trào rõ ràng và ending có thể hiểu được mà không cần giải thích ngoài game.

## 4. Kiến trúc và kỹ thuật

### 4.1. Cấu trúc hiện tại

| Khu vực | Trạng thái / vai trò |
| --- | --- |
| `Assets/Chapter1` | Chương hoàn thiện nhất: scene, scripts, UI, input, save, cinematic và test edit mode |
| `Assets/Chapter2` | Scene và model đồn cảnh sát |
| `Assets/Chapter3` | Chưa tạo — cần là nơi chứa scene, model, scripts và tài nguyên của bệnh viện |
| `Assets/Keypad` | Asset/script keypad và cửa trượt có thể tái sử dụng |
| `Packages/manifest.json` | Có Input System, AI Navigation, URP, Timeline, UGUI và Unity Test Framework |

### 4.2. Hệ thống gameplay cốt lõi

- **Player:** `CharacterController`, third-person camera, sprint/stamina, crouch và khóa input theo ngữ cảnh.
- **Interaction:** interface `IChapter1Interactable`, controller tương tác và layer `Interactable`.
- **Inventory & save:** `PlayerInventory`, `Chapter1SaveData` và `JsonChapter1SaveService`.
- **UI:** HUD cho mục tiêu, stamina, inventory, notification và prompt tương tác.
- **Nhiệm vụ:** `Chapter1Manager`, step/event bus và marker mục tiêu trong scene.
- **Kiểm thử:** Chương 1 có edit-mode test xác nhận cấu trúc graybox và keypad cửa phòng Dũng.

### 4.3. Định hướng mở rộng liên chương

- Tách các thành phần dùng chung (player, interaction, inventory, save, UI) thành module dùng lại trước khi phát triển sâu Chương 2 và 3.
- Định nghĩa dữ liệu chung cho `currentChapter`, checkpoint, vật phẩm quan trọng và lựa chọn cốt truyện.
- Mỗi chương quản lý asset/scene riêng, còn hệ thống dùng chung không phụ thuộc tên map cụ thể.
- Kiểm thử độc lập từng scene, sau đó kiểm thử luồng chuyển Chapter 1 → 2 → 3.

## 5. Kế hoạch hoàn thiện

| Ưu tiên | Hạng mục | Tiêu chí hoàn thành |
| --- | --- | --- |
| P0 | Chốt cốt truyện và objective của cả 3 chương | Có một luồng nhiệm vụ xuyên suốt, không mâu thuẫn giữa các map |
| P0 | Hoàn thiện Chương 1 | Chơi được từ đầu đến cuối, không có lỗi chặn tiến trình |
| P0 | Xây Chương 2 | Có map, objective chain, puzzle và kết chương |
| P0 | Xây Chương 3 | Có map bệnh viện, cao trào, ending và chuyển scene hợp lệ |
| P1 | Tích hợp hệ thống dùng chung | Save/checkpoint, UI và inventory hoạt động liên chương |
| P1 | Polish | Ánh sáng, âm thanh, animation, hướng dẫn UI và cân bằng độ khó |
| P1 | Kiểm thử & đóng gói | Build chạy được, test luồng chính và loại bỏ lỗi nghiêm trọng |

## 6. Dàn ý trình bày seminar

1. **Giới thiệu:** tên game, thể loại, vấn đề/câu chuyện trung tâm và mục tiêu dự án.
2. **Ý tưởng gameplay:** vòng lặp khám phá → tương tác → thu thập/giải đố → mở mục tiêu mới.
3. **Thiết kế ba chương:** trình bày lần lượt ký túc xá, đồn cảnh sát, bệnh viện; nêu vai trò của từng map trong cốt truyện.
4. **Demo Chương 1:** điều khiển, HUD, nhặt đồ, keypad/cửa và một objective hoặc chase sequence.
5. **Demo Chương 2 & 3:** map, puzzle tiêu biểu, chuyển chương và ending. Nếu chưa hoàn thiện, trình bày đúng trạng thái prototype cùng kế hoạch hoàn tất.
6. **Kỹ thuật:** Unity URP, Input System, kiến trúc interaction/inventory/save/UI, scene organization và test.
7. **Khó khăn và cách xử lý:** quản lý asset 3D, liên kết scene, tương tác, AI/hiệu năng và phân công nhóm. **[Cần bổ sung theo thực tế]**
8. **Kết quả và hướng phát triển:** phần đã hoàn thành, giới hạn hiện tại, các hạng mục nâng cấp sau dự án.

## 7. Phân công và bằng chứng đóng góp

| Thành viên | Phụ trách | Bằng chứng nên chuẩn bị |
| --- | --- | --- |
| [Tên thành viên 1] | [Ví dụ: gameplay / player / interaction] | Commit, script, video demo |
| [Tên thành viên 2] | [Ví dụ: map Chương 1 / 2] | Scene, prefab, ảnh before/after |
| [Tên thành viên 3] | [Ví dụ: map Chương 3 / UI / âm thanh] | Scene, UI, asset list |
| [Tên thành viên 4] | [Ví dụ: test / build / báo cáo] | Test result, build checklist, tài liệu |

> Thay các ô mẫu bằng tên và đóng góp thật của nhóm trước khi nộp.

## 8. Checklist nộp bài

Thông tin trong ảnh giao bài:

- Nén **toàn bộ các file liên quan** vào một file `.rar`.
- Đặt tên theo mẫu `GroupXY-S2.rar`, trong đó `XY` là số nhóm. Ví dụ: `Group01-S2.rar`.
- Hạn nộp: **cuối ngày 01/08/2026**.

Trước khi nén, kiểm tra:

- [ ] Project Unity mở được và các scene cần demo đã được thêm vào Build Settings.
- [ ] Có hướng dẫn chạy game, phiên bản Unity và control trong tài liệu nộp kèm.
- [ ] Không nén các thư mục tạm không cần thiết nếu giảng viên không yêu cầu; giữ `Assets/`, `Packages/` và `ProjectSettings/` để tái mở project.
- [ ] Kiểm thử đường chơi chính của ba chương và xác nhận không có lỗi chặn tiến trình.
- [ ] Đã thay toàn bộ nội dung **[Cần chốt]** bằng thông tin thực tế của nhóm.
- [ ] Đổi `XY` thành đúng số nhóm trước khi upload.
