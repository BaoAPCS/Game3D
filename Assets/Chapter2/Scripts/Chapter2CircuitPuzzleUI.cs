using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2CircuitPuzzleUI : MonoBehaviour
    {
        private const float TileSize = 94f;
        private const float TileGap = 8f;
        private const float GridPadding = 14f;
        private const float GridSize =
            Chapter2CircuitPuzzle.Width * TileSize +
            (Chapter2CircuitPuzzle.Width - 1) * TileGap +
            GridPadding * 2f;

        private static readonly Color Cyan =
            new Color(0.12f, 0.78f, 1f, 1f);
        private static readonly Color CyanGlow =
            new Color(0.16f, 0.92f, 1f, 1f);
        private static readonly Color Green =
            new Color(0.25f, 0.91f, 0.52f, 1f);
        private static readonly Color Red =
            new Color(1f, 0.29f, 0.31f, 1f);
        private static readonly Color PipeOff =
            new Color(0.31f, 0.40f, 0.45f, 1f);
        private static readonly Color CellOff =
            new Color(0.075f, 0.105f, 0.13f, 0.98f);
        private static readonly Color CellPowered =
            new Color(0.045f, 0.21f, 0.28f, 0.99f);

        private sealed class TileView
        {
            public Button Button;
            public Image Frame;
            public Image Face;
            public Image Center;
            public Image North;
            public Image East;
            public Image South;
            public Image West;
        }

        private readonly TileView[] tiles =
            new TileView[Chapter2CircuitPuzzle.TileCount];
        private readonly Image[] outputIndicators =
            new Image[Chapter2CircuitPuzzle.OutputCount];
        private readonly Image[] outputLines =
            new Image[Chapter2CircuitPuzzle.OutputCount];
        private readonly TextMeshProUGUI[] outputStateTexts =
            new TextMeshProUGUI[Chapter2CircuitPuzzle.OutputCount];

        private TextMeshProUGUI statusText;
        private TextMeshProUGUI instructionText;
        private TextMeshProUGUI selectedTileText;
        private Image statusPanel;
        private Image sourceIndicator;
        private Image sourceLine;
        private Button rotateButton;
        private Button resetButton;
        private Action<int> tileSelected;
        private Action rotateSelected;
        private Action resetRequested;
        private Action closeRequested;

        public bool IsVisible => gameObject.activeSelf;

        public static Chapter2CircuitPuzzleUI Create(
            Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "Chapter2_Mission02_CircuitUI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.SetActive(false);
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 720;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect =
                canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect, Vector2.zero, Vector2.zero);

            Chapter2CircuitPuzzleUI ui =
                canvasObject.AddComponent<Chapter2CircuitPuzzleUI>();
            ui.Build();
            return ui;
        }

        public void Configure(
            Action<int> onTileSelected,
            Action onRotateSelected,
            Action onResetRequested,
            Action onCloseRequested)
        {
            tileSelected = onTileSelected;
            rotateSelected = onRotateSelected;
            resetRequested = onResetRequested;
            closeRequested = onCloseRequested;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Refresh(
            Chapter2CircuitPuzzle puzzle,
            int selectedIndex)
        {
            if (puzzle == null)
            {
                return;
            }

            bool solved = puzzle.IsSolved;
            for (int y = 0;
                 y < Chapter2CircuitPuzzle.Height;
                 y++)
            {
                for (int x = 0;
                     x < Chapter2CircuitPuzzle.Width;
                     x++)
                {
                    int index = y * Chapter2CircuitPuzzle.Width + x;
                    TileView view = tiles[index];
                    Chapter2CircuitDirection connections =
                        puzzle.GetConnections(x, y);
                    bool powered = puzzle.IsTilePowered(x, y);
                    bool selected = index == selectedIndex;
                    Color pipeColor = powered ? CyanGlow : PipeOff;

                    view.Frame.color = selected
                        ? CyanGlow
                        : new Color(0.16f, 0.25f, 0.30f, 1f);
                    view.Face.color = powered
                        ? CellPowered
                        : CellOff;
                    SetPipe(view.North, connections,
                        Chapter2CircuitDirection.North, pipeColor);
                    SetPipe(view.East, connections,
                        Chapter2CircuitDirection.East, pipeColor);
                    SetPipe(view.South, connections,
                        Chapter2CircuitDirection.South, pipeColor);
                    SetPipe(view.West, connections,
                        Chapter2CircuitDirection.West, pipeColor);
                    view.Center.color = pipeColor;
                    view.Button.interactable = !solved;
                }
            }

            bool sourcePowered =
                puzzle.IsTilePowered(0, Chapter2CircuitPuzzle.SourceRow);
            sourceIndicator.color = sourcePowered ? CyanGlow : PipeOff;
            sourceLine.color = sourcePowered ? CyanGlow : PipeOff;

            for (int i = 0;
                 i < Chapter2CircuitPuzzle.OutputCount;
                 i++)
            {
                Chapter2CircuitOutput output =
                    (Chapter2CircuitOutput)i;
                bool powered = puzzle.IsOutputPowered(output);
                Color color = powered ? Green : PipeOff;
                outputIndicators[i].color = color;
                outputLines[i].color = color;
                outputStateTexts[i].text = powered
                    ? "ĐÃ KẾT NỐI"
                    : "CHƯA KẾT NỐI";
                outputStateTexts[i].color = color;
            }

            statusPanel.color = solved
                ? new Color(0.02f, 0.24f, 0.14f, 0.98f)
                : new Color(0.27f, 0.045f, 0.065f, 0.98f);
            statusText.text = solved
                ? "✓  TRẠNG THÁI: MẠCH ĐÃ HOÀN TẤT"
                : "!  TRẠNG THÁI: MẠCH CHƯA HOÀN CHỈNH";
            statusText.color = solved ? Green : Red;
            instructionText.text = solved
                ? "ĐÃ XÁC THỰC BA ĐẦU RA — ĐANG VÔ HIỆU HÓA KHÓA..."
                : "DẪN NGUỒN ĐẾN RELAY, CONTROL VÀ DOOR LOCK";

            if (selectedIndex >= 0 &&
                selectedIndex < Chapter2CircuitPuzzle.TileCount)
            {
                int selectedX =
                    selectedIndex % Chapter2CircuitPuzzle.Width;
                int selectedY =
                    selectedIndex / Chapter2CircuitPuzzle.Width;
                selectedTileText.text =
                    $"Ô ĐÃ CHỌN: HÀNG {selectedY + 1} · CỘT {selectedX + 1}";
            }
            else
            {
                selectedTileText.text =
                    "CHỌN MỘT Ô TRÊN BẢNG MẠCH";
            }

            rotateButton.interactable =
                !solved && selectedIndex >= 0;
            resetButton.interactable = !solved;
        }

        private void Build()
        {
            RectTransform backdrop = CreateImage(
                transform,
                "Backdrop",
                new Color(0f, 0.025f, 0.045f, 0.88f));
            Stretch(backdrop, Vector2.zero, Vector2.zero);
            backdrop.GetComponent<Image>().raycastTarget = true;

            TextMeshProUGUI missionHeader = CreateText(
                transform,
                "MissionHeader",
                "CHƯƠNG 2\nNHIỆM VỤ 2 — THOÁT KHỎI BUỒNG GIAM",
                25f,
                TextAlignmentOptions.TopLeft);
            SetRect(
                missionHeader.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(36f, -24f),
                new Vector2(700f, 72f),
                new Vector2(0f, 1f));
            missionHeader.color = Cyan;
            missionHeader.fontStyle = FontStyles.Bold;

            TextMeshProUGUI objective = CreateText(
                transform,
                "Objective",
                "◆  Xoay các đoạn mạch để cấp điện cho cả ba hệ thống.",
                18f,
                TextAlignmentOptions.Left);
            SetRect(
                objective.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(38f, -104f),
                new Vector2(720f, 34f),
                new Vector2(0f, 1f));
            objective.color = new Color(0.83f, 0.91f, 0.96f, 1f);

            RectTransform boardFrame = CreateImage(
                transform,
                "MaintenanceBoardFrame",
                new Color(0.06f, 0.58f, 0.78f, 0.95f));
            SetRect(
                boardFrame,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(1240f, 840f),
                new Vector2(0.5f, 0.5f));

            RectTransform board = CreateImage(
                boardFrame,
                "MaintenanceBoard",
                new Color(0.025f, 0.065f, 0.09f, 0.995f));
            Stretch(
                board,
                new Vector2(3f, 3f),
                new Vector2(-3f, -3f));

            TextMeshProUGUI title = CreateText(
                board,
                "BoardTitle",
                "CELL B3 — MAINTENANCE CONTROL",
                34f,
                TextAlignmentOptions.Center);
            SetRect(
                title.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -38f),
                new Vector2(-80f, 48f),
                new Vector2(0.5f, 0.5f));
            title.color = new Color(0.63f, 0.88f, 1f, 1f);
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI accepted = CreateText(
                board,
                "ServiceCardAccepted",
                "SERVICE CARD: ACCEPTED",
                23f,
                TextAlignmentOptions.Center);
            SetRect(
                accepted.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -84f),
                new Vector2(-80f, 34f),
                new Vector2(0.5f, 0.5f));
            accepted.color = Green;
            accepted.fontStyle = FontStyles.Bold;

            RectTransform gridFrame = CreateImage(
                board,
                "CircuitGridFrame",
                new Color(0.08f, 0.35f, 0.46f, 1f));
            SetRect(
                gridFrame,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-85f, 10f),
                new Vector2(GridSize + 6f, GridSize + 6f),
                new Vector2(0.5f, 0.5f));

            RectTransform grid = CreateImage(
                gridFrame,
                "CircuitGrid",
                new Color(0.018f, 0.045f, 0.06f, 1f));
            Stretch(
                grid,
                new Vector2(3f, 3f),
                new Vector2(-3f, -3f));
            BuildTiles(grid);
            BuildSource(board);
            BuildOutputs(board);

            selectedTileText = CreateText(
                board,
                "SelectedTile",
                "CHỌN MỘT Ô TRÊN BẢNG MẠCH",
                17f,
                TextAlignmentOptions.Center);
            SetRect(
                selectedTileText.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-85f, 139f),
                new Vector2(530f, 30f),
                new Vector2(0.5f, 0.5f));
            selectedTileText.color = new Color(0.63f, 0.82f, 0.92f, 1f);

            rotateButton = CreateButton(
                board,
                "RotateSelectedButton",
                "XOAY Ô ĐÃ CHỌN",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-202f, 91f),
                new Vector2(290f, 56f),
                new Color(0.025f, 0.38f, 0.56f, 1f));
            rotateButton.onClick.AddListener(
                () => rotateSelected?.Invoke());

            resetButton = CreateButton(
                board,
                "ResetPuzzleButton",
                "ĐẶT LẠI",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(80f, 91f),
                new Vector2(170f, 56f),
                new Color(0.12f, 0.18f, 0.22f, 1f));
            resetButton.onClick.AddListener(
                () => resetRequested?.Invoke());

            statusPanel = CreateImage(
                board,
                "CircuitStatus",
                new Color(0.27f, 0.045f, 0.065f, 0.98f)).
                GetComponent<Image>();
            SetRect(
                statusPanel.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(28f, 10f),
                new Vector2(430f, 48f),
                new Vector2(0f, 0f));
            statusText = CreateText(
                statusPanel.transform,
                "StatusText",
                string.Empty,
                18f,
                TextAlignmentOptions.Center);
            Stretch(statusText.rectTransform,
                new Vector2(12f, 6f), new Vector2(-12f, -6f));
            statusText.fontStyle = FontStyles.Bold;

            RectTransform instructionPanel = CreateImage(
                board,
                "MaintenanceNote",
                new Color(0.025f, 0.11f, 0.16f, 0.98f));
            SetRect(
                instructionPanel,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-28f, 10f),
                new Vector2(690f, 48f),
                new Vector2(1f, 0f));
            instructionText = CreateText(
                instructionPanel,
                "InstructionText",
                string.Empty,
                17f,
                TextAlignmentOptions.Center);
            Stretch(instructionText.rectTransform,
                new Vector2(12f, 6f), new Vector2(-12f, -6f));
            instructionText.color = new Color(0.48f, 0.82f, 1f, 1f);

            RectTransform footer = CreateImage(
                transform,
                "ControlFooter",
                new Color(0.018f, 0.07f, 0.10f, 0.98f));
            SetRect(
                footer,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(1050f, 66f),
                new Vector2(0.5f, 0f));

            TextMeshProUGUI controls = CreateText(
                footer,
                "Controls",
                "[LMB] Chọn ô   [NÚT XOAY / SPACE] Xoay   [R] Đặt lại   [ESC] Thoát",
                19f,
                TextAlignmentOptions.Center);
            Stretch(controls.rectTransform,
                new Vector2(20f, 8f), new Vector2(-150f, -8f));
            controls.color = new Color(0.82f, 0.90f, 0.96f, 1f);

            Button closeButton = CreateButton(
                footer,
                "CloseButton",
                "THOÁT",
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f),
                new Vector2(128f, 46f),
                new Color(0.14f, 0.21f, 0.25f, 1f),
                new Vector2(1f, 0.5f));
            closeButton.onClick.AddListener(
                () => closeRequested?.Invoke());
        }

        private void BuildTiles(RectTransform grid)
        {
            for (int y = 0;
                 y < Chapter2CircuitPuzzle.Height;
                 y++)
            {
                for (int x = 0;
                     x < Chapter2CircuitPuzzle.Width;
                     x++)
                {
                    int index = y * Chapter2CircuitPuzzle.Width + x;
                    GameObject tileObject = new GameObject(
                        $"CircuitTile_{x}_{y}",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image),
                        typeof(Button));
                    tileObject.transform.SetParent(grid, false);

                    RectTransform tileRect =
                        tileObject.GetComponent<RectTransform>();
                    SetRect(
                        tileRect,
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(
                            GridPadding + x * (TileSize + TileGap),
                            -GridPadding - y * (TileSize + TileGap)),
                        new Vector2(TileSize, TileSize),
                        new Vector2(0f, 1f));

                    Image frame = tileObject.GetComponent<Image>();
                    frame.color = new Color(0.16f, 0.25f, 0.30f, 1f);
                    Button button = tileObject.GetComponent<Button>();
                    button.targetGraphic = frame;
                    button.navigation = new Navigation
                    {
                        mode = Navigation.Mode.None
                    };
                    int capturedIndex = index;
                    button.onClick.AddListener(
                        () => tileSelected?.Invoke(capturedIndex));

                    RectTransform faceRect = CreateImage(
                        tileObject.transform,
                        "Face",
                        CellOff);
                    Stretch(faceRect,
                        new Vector2(3f, 3f), new Vector2(-3f, -3f));
                    Image face = faceRect.GetComponent<Image>();

                    Image north = CreatePipe(
                        faceRect,
                        "North",
                        new Vector2(0f, 23f),
                        new Vector2(12f, 46f));
                    Image east = CreatePipe(
                        faceRect,
                        "East",
                        new Vector2(23f, 0f),
                        new Vector2(46f, 12f));
                    Image south = CreatePipe(
                        faceRect,
                        "South",
                        new Vector2(0f, -23f),
                        new Vector2(12f, 46f));
                    Image west = CreatePipe(
                        faceRect,
                        "West",
                        new Vector2(-23f, 0f),
                        new Vector2(46f, 12f));
                    Image center = CreatePipe(
                        faceRect,
                        "Center",
                        Vector2.zero,
                        new Vector2(20f, 20f));

                    tiles[index] = new TileView
                    {
                        Button = button,
                        Frame = frame,
                        Face = face,
                        Center = center,
                        North = north,
                        East = east,
                        South = south,
                        West = west
                    };
                }
            }
        }

        private void BuildSource(RectTransform board)
        {
            float rowY = GetBoardRowY(Chapter2CircuitPuzzle.SourceRow);
            TextMeshProUGUI label = CreateText(
                board,
                "PowerLabel",
                "POWER",
                21f,
                TextAlignmentOptions.Center);
            SetRect(
                label.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-500f, rowY - 61f),
                new Vector2(130f, 32f),
                new Vector2(0.5f, 0.5f));
            label.color = new Color(0.56f, 0.83f, 1f, 1f);
            label.fontStyle = FontStyles.Bold;

            RectTransform source = CreateImage(
                board,
                "PowerSourceFrame",
                new Color(0.08f, 0.42f, 0.57f, 1f));
            SetRect(
                source,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-500f, rowY),
                new Vector2(82f, 82f),
                new Vector2(0.5f, 0.5f));
            RectTransform sourceCore = CreateImage(
                source,
                "PowerSource",
                PipeOff);
            Stretch(sourceCore,
                new Vector2(7f, 7f), new Vector2(-7f, -7f));
            sourceIndicator = sourceCore.GetComponent<Image>();

            TextMeshProUGUI powerText = CreateText(
                sourceCore,
                "PowerSymbol",
                "PWR",
                19f,
                TextAlignmentOptions.Center);
            Stretch(powerText.rectTransform,
                Vector2.zero, Vector2.zero);
            powerText.color = new Color(0.015f, 0.075f, 0.10f, 1f);
            powerText.fontStyle = FontStyles.Bold;

            RectTransform line = CreateImage(
                board,
                "PowerInputLine",
                PipeOff);
            SetRect(
                line,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-405f, rowY),
                new Vector2(108f, 10f),
                new Vector2(0.5f, 0.5f));
            sourceLine = line.GetComponent<Image>();
        }

        private void BuildOutputs(RectTransform board)
        {
            string[] labels =
            {
                "SECURITY RELAY",
                "CONTROL",
                "DOOR LOCK"
            };

            for (int i = 0;
                 i < Chapter2CircuitPuzzle.OutputCount;
                 i++)
            {
                Chapter2CircuitOutput output =
                    (Chapter2CircuitOutput)i;
                float rowY = GetBoardRowY(
                    output == Chapter2CircuitOutput.SecurityRelay
                        ? 0
                        : output == Chapter2CircuitOutput.Control
                            ? 2
                            : 4);

                RectTransform line = CreateImage(
                    board,
                    labels[i].Replace(" ", string.Empty) + "Line",
                    PipeOff);
                SetRect(
                    line,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(255f, rowY),
                    new Vector2(144f, 10f),
                    new Vector2(0.5f, 0.5f));
                outputLines[i] = line.GetComponent<Image>();

                RectTransform indicatorFrame = CreateImage(
                    board,
                    labels[i].Replace(" ", string.Empty) + "Frame",
                    new Color(0.08f, 0.42f, 0.57f, 1f));
                SetRect(
                    indicatorFrame,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(365f, rowY),
                    new Vector2(76f, 76f),
                    new Vector2(0.5f, 0.5f));
                RectTransform indicator = CreateImage(
                    indicatorFrame,
                    "Indicator",
                    PipeOff);
                Stretch(indicator,
                    new Vector2(7f, 7f), new Vector2(-7f, -7f));
                outputIndicators[i] = indicator.GetComponent<Image>();

                TextMeshProUGUI shortLabel = CreateText(
                    indicator,
                    "Symbol",
                    i == 0 ? "RLY" : i == 1 ? "CTL" : "LCK",
                    17f,
                    TextAlignmentOptions.Center);
                Stretch(shortLabel.rectTransform,
                    Vector2.zero, Vector2.zero);
                shortLabel.color = new Color(0.015f, 0.075f, 0.10f, 1f);
                shortLabel.fontStyle = FontStyles.Bold;

                TextMeshProUGUI outputLabel = CreateText(
                    board,
                    labels[i].Replace(" ", string.Empty) + "Label",
                    labels[i],
                    18f,
                    TextAlignmentOptions.Left);
                SetRect(
                    outputLabel.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(418f, rowY + 11f),
                    new Vector2(190f, 28f),
                    new Vector2(0f, 0.5f));
                outputLabel.color = new Color(0.60f, 0.84f, 1f, 1f);
                outputLabel.fontStyle = FontStyles.Bold;

                TextMeshProUGUI outputState = CreateText(
                    board,
                    labels[i].Replace(" ", string.Empty) + "State",
                    "CHƯA KẾT NỐI",
                    13f,
                    TextAlignmentOptions.Left);
                SetRect(
                    outputState.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(418f, rowY - 17f),
                    new Vector2(190f, 24f),
                    new Vector2(0f, 0.5f));
                outputState.color = PipeOff;
                outputStateTexts[i] = outputState;
            }
        }

        private static float GetBoardRowY(int row)
        {
            const float gridCenterY = 10f;
            float firstRowY =
                gridCenterY + GridSize * 0.5f -
                GridPadding - TileSize * 0.5f;
            return firstRowY - row * (TileSize + TileGap);
        }

        private static void SetPipe(
            Image pipe,
            Chapter2CircuitDirection connections,
            Chapter2CircuitDirection direction,
            Color color)
        {
            pipe.gameObject.SetActive((connections & direction) != 0);
            pipe.color = color;
        }

        private static Image CreatePipe(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect = CreateImage(parent, name, PipeOff);
            SetRect(
                rect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                size,
                new Vector2(0.5f, 0.5f));
            return rect.GetComponent<Image>();
        }

        private static RectTransform CreateImage(
            Transform parent,
            string name,
            Color color)
        {
            GameObject target = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.transform.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return target.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Color color,
            Vector2? pivot = null)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            SetRect(
                rect,
                anchorMin,
                anchorMax,
                position,
                size,
                pivot ?? new Vector2(0.5f, 0.5f));

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.75f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.50f, 0.82f, 0.94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.42f, 0.46f, 0.72f);
            button.colors = colors;

            TextMeshProUGUI buttonText = CreateText(
                buttonObject.transform,
                "Label",
                label,
                18f,
                TextAlignmentOptions.Center);
            Stretch(buttonText.rectTransform,
                new Vector2(10f, 4f), new Vector2(-10f, -4f));
            buttonText.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }
}
