using System.Collections.Generic;

namespace DormitoryMystery.Chapter1
{
    public static class Mission01DungConversation
    {
        public const string DungContactId = "dung";
        public const string DungDisplayName = "Dũng";

        public const string BorrowQuestion = "Cho tớ mượn máy tách âm được không?";
        public const string BorrowReplyYes = "Ừ, cậu tự lên phòng tớ lấy nhé.";
        public const string BorrowReplyRoom = "Phòng tớ nằm ở giữa lầu 2.";
        public const string PasswordQuestion = "Mật khẩu phòng cậu là gì?";
        public const string PasswordHint = "Mật khẩu là ngày sinh nhật của tớ.";
        public const string BirthdayQuestion = "Sinh nhật cậu ngày nào?";
        public const string BirthdayHint = "Tròn một tháng trước cậu còn đi ăn sinh nhật của tớ mà.";
        public const string BirthdayReminder = "Đừng nói là cậu không nhớ nha.";

        public static readonly string[] ForbiddenDirectAnswers =
        {
            "2502",
            "25/02",
            "ngày 25 tháng 2",
            "mật khẩu là 2502"
        };

        public static List<Mission01DungMessage> BuildMessages(Chapter1SaveData data)
        {
            List<Mission01DungMessage> messages = new List<Mission01DungMessage>();
            if (data == null)
            {
                return messages;
            }

            data.EnsureValidDefaults();

            if (data.Mission01DungBorrowRequestSent)
            {
                messages.Add(new Mission01DungMessage("Nam", BorrowQuestion, true));
            }

            if (data.Mission01DungBorrowReplyReceived)
            {
                messages.Add(new Mission01DungMessage(DungDisplayName, BorrowReplyYes, false));
                messages.Add(new Mission01DungMessage(DungDisplayName, BorrowReplyRoom, false));
            }

            if (data.Mission01DungPasswordQuestionSent)
            {
                messages.Add(new Mission01DungMessage("Nam", PasswordQuestion, true));
            }

            if (data.Mission01DungPasswordHintReceived)
            {
                messages.Add(new Mission01DungMessage(DungDisplayName, PasswordHint, false));
            }

            if (data.Mission01DungBirthdayQuestionSent)
            {
                messages.Add(new Mission01DungMessage("Nam", BirthdayQuestion, true));
            }

            if (data.Mission01DungBirthdayHintReceived)
            {
                messages.Add(new Mission01DungMessage(DungDisplayName, BirthdayHint, false));
                messages.Add(new Mission01DungMessage(DungDisplayName, BirthdayReminder, false));
            }

            return messages;
        }

        public static List<Mission01DungChoice> BuildChoices(FirstMissionState state, Chapter1SaveData data)
        {
            List<Mission01DungChoice> choices = new List<Mission01DungChoice>();
            if (data == null)
            {
                return choices;
            }

            data.EnsureValidDefaults();

            if (state == FirstMissionState.MessageDung && !data.Mission01DungBorrowRequestSent)
            {
                choices.Add(Mission01DungChoice.BorrowAudioSeparator);
            }

            if (state == FirstMissionState.DiscoverLockedDoor && !data.Mission01DungPasswordQuestionSent)
            {
                choices.Add(Mission01DungChoice.AskRoomPassword);
            }

            if (state == FirstMissionState.AskDungPassword && !data.Mission01DungBirthdayQuestionSent)
            {
                choices.Add(Mission01DungChoice.AskBirthday);
            }

            return choices;
        }

        public static string GetChoiceText(Mission01DungChoice choice)
        {
            switch (choice)
            {
                case Mission01DungChoice.BorrowAudioSeparator:
                    return BorrowQuestion;
                case Mission01DungChoice.AskRoomPassword:
                    return PasswordQuestion;
                case Mission01DungChoice.AskBirthday:
                    return BirthdayQuestion;
                default:
                    return string.Empty;
            }
        }

        public static bool ContainsForbiddenDirectAnswer(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string lower = text.ToLowerInvariant();
            for (int i = 0; i < ForbiddenDirectAnswers.Length; i++)
            {
                if (lower.Contains(ForbiddenDirectAnswers[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
