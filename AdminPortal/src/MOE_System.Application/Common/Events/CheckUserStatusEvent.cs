using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace MOE_System.Application.Common.Events
{
    public class CheckUserStatusEvent : INotification
    {
        public string AccountHolderId { get; set; } = string.Empty;
    }
}
