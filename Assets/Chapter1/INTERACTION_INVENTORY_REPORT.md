# Chapter 1 Interaction And Inventory Prototype Report

## Architecture
- Interaction uses `Chapter1InteractionController` on the player and `IChapter1Interactable` implementations on world objects.
- Detection is camera-forward `SphereCastNonAlloc` against the `Interactable` layer with an `Environment` obstruction check.
- `Chapter1Interactable` owns prompt text, focus highlight, one-shot behavior, optional step gating, and common failure handling.

## Inventory And Save Sync
- `PlayerInventory` is the single runtime inventory source.
- Existing `Chapter1SaveData` fields store Lan recording, flashlight, fuse, hard drive, and throwable can count.
- `CollectedUniqueItemIds` stores world pickup IDs and item IDs without dedicated save fields.
- Successful inventory changes raise `Chapter1EventBus.RaiseInventoryChanged()` and save through `Chapter1Manager.SaveChapter()` when a manager is available.

## Created Scripts
- Interaction: `InteractionResult`, `InteractionContext`, `IChapter1Interactable`, `Chapter1Interactable`, `Chapter1InteractionController`, `TestInspectableInteractable`.
- Inventory: `PlayerInventory`.
- Items: `ItemPickup`, `WorldPickupPersistence`, `FlashlightController`.
- UI: `Chapter1HUD`, `InteractionPromptUI`, `StaminaHUD`, `InventoryHUD`, `NotificationUI`, `ObjectiveHUD`.
- Editor: `Chapter1InteractionInventoryBuilder`.

## Builder-Created Prefabs And UI
- `Pickup_Flashlight.prefab`
- `Pickup_Fuse.prefab`
- `Pickup_ThrowableCan.prefab`
- `TestInspectableTable.prefab`
- Scene HUD under `UI/Chapter1HUD`
- Single EventSystem using `InputSystemUIInputModule`

## Not Included In This Slice
- Nam dialogue.
- Audio puzzle.
- Full dormitory map.
- Raincoat enemy AI.
- NavMesh.
- Physics can throwing.
- CCTV, blackout sequence, NamTrust choices, or final chapter flow.
