using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.Interfaces.Services
{
    public interface ICourseStatusService
    {
        Task TriggerCourseStatusCheckAsync(CancellationToken cancellationToken);
    }
}
