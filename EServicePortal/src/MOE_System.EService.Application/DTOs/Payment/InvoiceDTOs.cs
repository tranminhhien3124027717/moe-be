using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.EService.Application.DTOs.Payment
{
    public class InvoiceDetailsResponse
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
    }
}
