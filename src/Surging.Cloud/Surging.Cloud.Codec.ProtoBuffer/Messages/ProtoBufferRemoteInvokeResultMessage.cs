﻿using ProtoBuf;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Surging.Cloud.Codec.ProtoBuffer.Messages
{
    [ProtoContract]
   public class ProtoBufferRemoteInvokeResultMessage
    {
        #region Constructor

        public ProtoBufferRemoteInvokeResultMessage(RemoteInvokeResultMessage message)
        {
            ExceptionMessage = message.ExceptionMessage;
            Result = message.Result == null ? null : new DynamicItem(message.Result);
            StatusCode = message.StatusCode;
        }

        public ProtoBufferRemoteInvokeResultMessage()
        {
        }

        #endregion Constructor
        
        [ProtoMember(1)]
        public string ExceptionMessage { get; set; }

        
        [ProtoMember(2)]
        public DynamicItem Result { get; set; }

        [ProtoMember(3)]
        public StatusCode StatusCode { get; set; }

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

