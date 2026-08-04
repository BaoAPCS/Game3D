namespace DormitoryMystery.Chapter1
{
    public readonly struct Mission01DungMessage
    {
        public Mission01DungMessage(string sender, string text, bool fromPlayer)
        {
            Sender = sender ?? string.Empty;
            Text = text ?? string.Empty;
            FromPlayer = fromPlayer;
        }

        public string Sender { get; }
        public string Text { get; }
        public bool FromPlayer { get; }
    }
}
