# Báo cáo khởi tạo Chương 1 - Ký túc xá

## Thông tin project đã kiểm tra

- Project name: My project
- Project path thực tế: `D:\Allproject\My project`
- Ghi chú đường dẫn: prompt ghi `D:\AIlproject\My project`, nhưng đường dẫn đó không tồn tại trên máy. Thư mục Unity hợp lệ tìm thấy là `D:\Allproject\My project`.
- Unity version: 6000.5.0f1 (88b47c5e7076)
- Unity template: `com.unity.template.urp-blank@17.0.14`
- Scene mặc định trong Build Settings: `Assets/Scenes/SampleScene.unity`
- Scene mặc định từ template: `Assets/Scenes/SampleScene.unity`

## Render pipeline

- Render pipeline hiện tại: Universal Render Pipeline (URP)
- Package URP: `com.unity.render-pipelines.universal` 17.5.0
- `ProjectSettings/GraphicsSettings.asset` đang trỏ tới URP asset qua `m_CustomRenderPipeline`.

## Input system

- Active Input Handling: Input System Package (New), giá trị `activeInputHandler: 1`
- Package Input System: `com.unity.inputsystem` 1.19.0
- Project có asset input mặc định: `Assets/InputSystem_Actions.inputactions`

## Package hiện có liên quan đến Chương 1

- `com.unity.ai.navigation` 2.0.13
- `com.unity.inputsystem` 1.19.0
- `com.unity.render-pipelines.universal` 17.5.0
- `com.unity.test-framework` 1.7.0
- `com.unity.timeline` 1.8.12
- `com.unity.ugui` 2.5.0
- TextMeshPro: không có package `com.unity.textmeshpro` riêng trong manifest, nhưng có assembly `Unity.TextMeshPro` trong package cache của `com.unity.ugui`.

## Package còn thiếu

- Cinemachine: chưa thấy `com.unity.cinemachine` trong `Packages/manifest.json`, `Packages/packages-lock.json` hoặc `Library/PackageCache`.

## Assembly Definition

- Project chưa có `.asmdef` trong `Assets` hoặc `Packages` của project.
- Theo yêu cầu, chưa tạo asmdef trong giai đoạn này để tránh lỗi reference package không cần thiết.

## Quyết định kiến trúc sẽ sử dụng

- Tất cả runtime script mới đặt trong namespace `DormitoryMystery.Chapter1`.
- Toàn bộ hệ thống Chương 1 được đặt trong `Assets/Chapter1`, không dùng `Assets/_Data` và không phụ thuộc project khác.
- Dữ liệu lưu game dùng class serializable `Chapter1SaveData`, chỉ lưu dữ liệu thuần, không lưu Unity Object reference.
- Save chính dùng JSON qua `JsonUtility` tại `Application.persistentDataPath/chapter1_save.json`.
- Event giao tiếp qua C# events trong `Chapter1EventBus`; class khác chỉ phát event qua method Raise rõ ràng.
- `Chapter1Manager` là manager theo scene, không dùng `DontDestroyOnLoad`; nếu trùng instance thì log lỗi và disable component trùng.
- Tiến trình nhiệm vụ chỉ được tiến lên, không tự lùi step.
- Lựa chọn cuối chương chỉ được áp dụng một lần.
- Không tạo Scene gameplay, Player, Enemy, UI hoặc prefab trong giai đoạn khởi tạo này.

## Thao tác thủ công nếu cần package còn thiếu

- Nếu giai đoạn sau cần camera điện ảnh hoặc camera follow phức tạp, người dùng cần mở Unity Package Manager và cài Cinemachine (`com.unity.cinemachine`).
- Không cần cài thêm Input System hoặc AI Navigation vì project hiện đã có.
- Không cần chỉnh Project Settings trong giai đoạn này.

## Kết quả kiểm tra sau khi khởi tạo

- File đã tạo trong `Assets/Chapter1`: 1 file báo cáo, 9 runtime script, 1 editor validator script.
- File đã sửa: không sửa file cấu hình Unity, không sửa Scene, không sửa package, không sửa `.meta` thủ công.
- Unity tự sinh `.meta` khi import asset mới trong Editor đang mở.
- Compile runtime: đạt, kiểm tra bằng Unity Roslyn với response file `Assembly-CSharp.rsp`.
- Compile editor: đạt, kiểm tra bằng Unity Roslyn với response file `Assembly-CSharp-Editor.rsp`.
- Unity batchmode không mở project trọn vẹn vì project đang có Editor instance hiện hữu; kiểm tra compile độc lập vẫn dùng compiler và response file do Unity 6000.5.0f1 sinh ra.
- Menu validator đã tạo: `Tools/Chapter 1/Validate Project Setup`.
- Không tạo Scene gameplay, Player, Enemy, UI hoặc prefab.

## Giai đoạn tiếp theo đề xuất

- Tạo scene khung Chương 1 bằng Unity API/Editor script thay vì sửa YAML thủ công.
- Tạo prefab `Chapter1Manager` hoặc thêm component vào scene theo quy trình trong Editor.
- Xây player controller, camera và interaction sau khi quyết định dùng góc nhìn thứ ba hay góc nhìn từ trên xuống.
- Nếu cần camera follow/cutscene, cài Cinemachine trước khi triển khai camera nâng cao.
