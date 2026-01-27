using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.EService.Domain.Enums
{
    public enum InvoiceStatus
    {
        Scheduled = 0,
        Outstanding = 1,
        Paid = 2,
        Overdue = 3,
    }
}
