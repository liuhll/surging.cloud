using MessagePack;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Messages;
using System.Runtime.CompilerServices;

namespace Surging.Cloud.Codec.MessagePack.Messages
{
    [MessagePackObject]
    public class MessagePackRemoteInvokeResultMessage
    {
        #region Constructor

        public MessagePackRemoteInvokeResultMessage(RemoteInvokeResultMessage message)
        {
            ExceptionMessage = message.ExceptionMessage;
            Result = message.Result == null ? null : new DynamicItem(message.Result);
            StatusCode = message.StatusCode;
        }

        public MessagePackRemoteInvokeResultMessage()
        {
        }

        #endregion Constructor

        [Key(0)]
        public string ExceptionMessage { get; set; }

        [Key(1)]
        public DynamicItem Result { get; set; }

        [Key(2)]
        public StatusCode StatusCode { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RemoteInvokeResultMessage GetRemoteInvokeResultMessage()
        {
            return new RemoteInvokeResultMessage
            {
                ExceptionMessage = ExceptionMessage,
                Result = Result?.Get(),
                StatusCode = StatusCode
            };
        }
    }
}

