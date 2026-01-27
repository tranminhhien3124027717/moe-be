using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.Common.Events
{
    public class CheckUserEducationLevelEvent : INotification
    {
        public string AccountHolderId { get; set; } = string.Empty;
    }
}
