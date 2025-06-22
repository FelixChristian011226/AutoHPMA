using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AutoHPMA.Messages
{
    /// <summary>
    /// 用于通知停止所有任务的消息
    /// </summary>
    public class StopAllTasksMessage : ValueChangedMessage<bool>
    {
        public StopAllTasksMessage(bool value) : base(value) { }
    }
} 