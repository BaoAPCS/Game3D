namespace DormitoryMystery.Chapter1
{
    public readonly struct InteractionResult
    {
        public InteractionResult(bool success, string message, bool consumeInteractionInput)
        {
            Success = success;
            Message = message ?? string.Empty;
            ConsumeInteractionInput = consumeInteractionInput;
        }

        public bool Success { get; }
        public string Message { get; }
        public bool ConsumeInteractionInput { get; }

        public static InteractionResult Succeeded(string message = "")
        {
            return new InteractionResult(true, message, true);
        }

        public static InteractionResult Failed(string message)
        {
            return new InteractionResult(false, message, true);
        }

        public static InteractionResult Ignored()
        {
            return new InteractionResult(false, string.Empty, false);
        }
    }
}
