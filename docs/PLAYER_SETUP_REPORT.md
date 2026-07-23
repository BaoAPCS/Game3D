# PLAYER_SETUP_REPORT - Chapter 1 Player Prototype

## Phạm vi

Tài liệu này mô tả phần Player Prototype Chương 1 cho project Unity `D:\Allproject\My project`, namespace runtime `DormitoryMystery.Chapter1`.

Prototype tập trung vào vòng lặp Play Mode tối thiểu:

- Di chuyển third-person bằng `CharacterController`.
- Camera orbit tự viết, không dùng Cinemachine.
- Sprint có stamina.
- Crouch toggle và kiểm tra trần thấp.
- Khóa input bằng reason để phục vụ dialogue, puzzle, CCTV, hiding hoặc pause sau này.
- Debug overlay bằng `OnGUI`, không phụ thuộc TextMeshPro.

## File tái sử dụng

Các thành phần nền tảng có sẵn được giữ lại và tái sử dụng:

- `Assets/Chapter1/Scripts/Core/Chapter1Manager.cs`
- `Assets/Chapter1/Scripts/Core/Chapter1EventBus.cs`
- `Assets/Chapter1/Scripts/Core/Chapter1Step.cs`
- `Assets/Chapter1/Scripts/Save/Chapter1SaveData.cs`
- `Assets/Chapter1/Scripts/Save/IChapter1SaveService.cs`
- `Assets/Chapter1/Scripts/Save/JsonChapter1SaveService.cs`
- `Assets/Chapter1/Editor/Chapter1ProjectValidator.cs`

Không tạo lại các class lõi đã tồn tại.

## File runtime mới

- `Assets/Chapter1/Scripts/Player/Chapter1InputReader.cs`
- `Assets/Chapter1/Scripts/Player/PlayerInputLock.cs`
- `Assets/Chapter1/Scripts/Player/PlayerStamina.cs`
- `Assets/Chapter1/Scripts/Player/Chapter1PlayerMotor.cs`
- `Assets/Chapter1/Scripts/Player/PlayerVisualController.cs`
- `Assets/Chapter1/Scripts/Camera/CameraTarget.cs`
- `Assets/Chapter1/Scripts/Camera/ThirdPersonCameraRig.cs`
- `Assets/Chapter1/Scripts/Core/Chapter1GameplayBootstrap.cs`
- `Assets/Chapter1/Scripts/UI/PlayerDebugOverlay.cs`

## File editor mới/cập nhật

- `Assets/Chapter1/Editor/Chapter1PlayerPrototypeBuilder.cs`
- `Assets/Chapter1/Editor/Chapter1ProjectValidator.cs`

Builder tạo asset bằng Unity Editor API và `AssetDatabase`, không sửa scene YAML hoặc `.meta` thủ công.

## Input actions

Khi chạy menu `Tools/Chapter 1/Build Player Prototype`, builder tạo:

`Assets/Chapter1/Settings/Chapter1Controls.inputactions`

Action map: `Gameplay`

Actions:

- `Move`: WASD, Arrow keys, Gamepad left stick.
- `Look`: Mouse delta, Gamepad right stick.
- `Sprint`: Left Shift, Gamepad left stick press, Gamepad left shoulder.
- `Crouch`: C, Gamepad button east.
- `Interact`: F, Gamepad button south.
- `ToggleFlashlight`: T, Gamepad dpad up.
- `ThrowCan`: G, Gamepad right shoulder.
- `Pause`: Escape, Gamepad start button.

## Camera

Camera dùng hierarchy:

`CameraRig/CameraPivot/Main Camera`

`Main Camera` giữ tag `MainCamera` và có `AudioListener`. Camera orbit trong `LateUpdate`, đọc `Look` từ Input System, follow `CameraTarget`, khóa cursor mặc định, có tùy chọn khóa look khi input bị khóa, và dùng `SphereCast` với layer `Environment` để tránh xuyên tường.

Prototype không dùng Cinemachine và không yêu cầu cài package mới.

## Asset được builder sinh

Sau khi Unity compile xong, chạy:

`Tools/Chapter 1/Build Player Prototype`

Builder sẽ tạo/cập nhật:

- `Assets/Chapter1/Settings/Chapter1Controls.inputactions`
- `Assets/Chapter1/Prefabs/Characters/Player_Minh_Prototype.prefab`
- `Assets/Chapter1/Prefabs/Gameplay/ThirdPersonCameraRig.prefab`
- `Assets/Chapter1/Scenes/Chapter1_PlayerPrototype.unity`
- `Assets/Chapter1/Materials/M_Player_Prototype.mat`
- `Assets/Chapter1/Materials/M_Ground_Prototype.mat`
- `Assets/Chapter1/Materials/M_Wall_Prototype.mat`

Nếu input asset, prefab hoặc scene đã tồn tại, builder sẽ hỏi xác nhận trước khi tạo lại các asset prototype trong `Assets/Chapter1`.

## Tag và layer

Builder thêm tag `Player`.

Builder thêm các layer sau nếu còn slot trống và không ghi đè layer hiện có:

- `Player`
- `Environment`
- `Interactable`
- `Enemy`
- `HideSpot`

## Kiểm tra sau khi build prototype

Sau khi chạy builder, chạy:

`Tools/Chapter 1/Validate Project Setup`

Kiểm tra Play Mode thủ công:

- WASD, Arrow keys hoặc Gamepad left stick di chuyển; đi chéo không nhanh hơn.
- Mouse hoặc Gamepad right stick orbit camera.
- Camera không xuyên qua các object layer `Environment`.
- Shift hoặc gamepad sprint tiêu hao stamina.
- Khi stamina cạn, player không sprint lại cho tới khi hồi đủ 15.
- C toggle crouch.
- Player không đứng lên được dưới `LowCeilingTest`.
- Input lock chặn movement, sprint và crouch nhưng không dùng `Time.timeScale`.
- Scene chỉ có một `AudioListener`.
- Player root có tag/layer đúng, không có `Rigidbody` hoặc `NavMeshAgent`.
