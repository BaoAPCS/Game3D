# FIRST MISSION PHONE SETUP REPORT

## Lan Recording Audio
- Expected real AudioClip path: `Assets/Chapter1/Audio/Phone/Lan_LastRecording_Mixed.mp3`
- MP3 file exists on disk: `True`
- `AssetDatabase.LoadAssetAtPath<AudioClip>()` result: `Lan_LastRecording_Mixed`
- Voice message data path: `Assets/Chapter1/Data/Phone/Messages/Lan_LastRecordingMessage.asset`
- Voice message data exists: `True`
- Voice message data uses expected clip: `True`
- Missing audio behavior: write a clear Console warning and leave the AudioClip reference empty; do not create or assign a fake AudioClip.
- Audio file content policy: setup only imports/assigns the MP3 reference and never edits the audio file contents.

## First Backpack Hint
- Expected first hint text: `[B] Mở balo`
- MissionHintController exists in open scene: `True`
- Hint behavior: show after Play starts, hide when the first Inventory input is pressed.

## Setup Tool
- Menu: `Tools > Chapter 1 > Setup First Mission Phone Sequence`
- The setup tool also runs this audio assignment from `Tools > Chapter 1 > Setup Backpack And Phone`.

## Console Report Lines
- `[PASS] Inventory action exists.`
- `[PASS] Inventory action binding is <Keyboard>/b.`
- `[PASS] Chapter1InputReader has InventoryPressed event.`
- `[PASS] PhoneItem.asset exists.`
- `[PASS] PhoneItem itemId is phone.`
- `[PASS] PhoneItem has icon.`
- `[PASS] PhoneItem is not droppable.`
- `[PASS] PhoneItem is usable.`
- `[PASS] InventorySlot prefab exists.`
- `[PASS] InventoryPanel prefab exists.`
- `[PASS] PhonePanel prefab exists.`
- `[PASS] InventoryPanel has at least 12 slots.`
- `[PASS] PhonePanel has PhoneUIController.`
- `[PASS] PhonePanel has Messenger, Ghi am, Camera, and Google app buttons.`
- `[PASS] Scene Player exists.`
- `[PASS] Scene has Chapter1BackpackPhoneCanvas.`
- `[PASS] Chapter1BackpackPhoneCanvas is Screen Space Overlay.`
- `[PASS] Scene has one EventSystem. Count=1.`
- `[PASS] EventSystem uses InputSystemUIInputModule.`
- `[PASS] Player has InventoryController.`
- `[PASS] Player has BackpackPhoneInputController.`
- `[PASS] Inventory has exactly one default phone by itemId.`
- `[PASS] Scene InventoryPanel exists.`
- `[PASS] Scene PhonePanel exists.`
- `[PASS] InventoryPanel is under Backpack canvas.`
- `[PASS] PhonePanel is under Backpack canvas.`
- `[PASS] Scene PhonePanel has Messenger, Ghi am, Camera, and Google app buttons.`
- `[PASS] Scene InventoryPanel has 12 slots.`
- `[PASS] Scene has LanRecordingMissionController.`
- `[PASS] LanRecordingMissionController uses the real Lan_LastRecording AudioClip.`
- `[PASS] Scene has no Missing Script components.`
- `[PASS] Lan recording MP3 exists on disk: Assets/Chapter1/Audio/Phone/Lan_LastRecording_Mixed.mp3.`
- `[PASS] Lan recording AudioClip loads from exact path: Assets/Chapter1/Audio/Phone/Lan_LastRecording_Mixed.mp3.`
- `[PASS] Lan voice message data exists: Assets/Chapter1/Data/Phone/Messages/Lan_LastRecordingMessage.asset.`
- `[PASS] Lan voice message data uses the real Lan_LastRecording AudioClip.`

## Summary
- PASS: 35 | WARNING: 0 | FAIL: 0
