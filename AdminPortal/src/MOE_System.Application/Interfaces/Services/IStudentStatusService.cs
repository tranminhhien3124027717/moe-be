using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.Interfaces.Services
{
    public interface IStudentStatusService
    {
        Task TriggerSchoolingStatusCheckAsync(string accountHolderId);
        Task TriggerEducationLevelCheckAsync(string accountHolderId);
    }
}
