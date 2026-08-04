using UnityEngine;

namespace DormitoryMystery.Chapter1
{
    [CreateAssetMenu(menuName = "Dormitory Mystery/Chapter 1/Phone/Message Data")]
    public sealed class PhoneMessageData : ScriptableObject
    {
        [SerializeField] private string messageId = string.Empty;
        [SerializeField] private string senderId = string.Empty;
        [SerializeField, TextArea(1, 4)] private string content = string.Empty;
        [SerializeField] private PhoneMessageType messageType = PhoneMessageType.Text;
        [SerializeField] private bool isFromPlayer = false;
        [SerializeField] private bool isRead = false;
        [SerializeField] private AudioClip audioClip = null;
        [SerializeField] private bool isDownloaded = false;

        public string MessageId => messageId;
        public string SenderId => senderId;
        public string Content => content;
        public PhoneMessageType MessageType => messageType;
        public bool IsFromPlayer => isFromPlayer;
        public bool IsRead => isRead;
        public AudioClip AudioClip => audioClip;
        public bool IsDownloaded => isDownloaded;

        private void OnValidate()
        {
            messageId = (messageId ?? string.Empty).Trim();
            senderId = (senderId ?? string.Empty).Trim();
            content ??= string.Empty;
        }
    }
}
