using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.DTOs.Invoice.Request
{
    public class PrintInvoiceRequest
    {
        public string EducationAccountId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
    }
}
