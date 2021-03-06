﻿using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Surging.Cloud.KestrelHttpServer.Filters.Implementation
{
   public class CustomerExceptionFilterAttribute : IExceptionFilter
    {
        public Task OnException(ExceptionContext context)
        {
            context.Result = new HttpResultMessage<object>
            {
                Data = null,
                StatusCode = context.Exception.GetExceptionStatusCode(),
                IsSucceed = false,
                Message = context.Exception.Message
            };
            return Task.CompletedTask;
        }
    }
}
