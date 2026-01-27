using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Domain.Entities
{
    public class OutBoxMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Type { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime OccurredOn { get; set; }

        public DateTime? ProcessedOn { get; set; }

        public string? Error { get; set; }
    }
}
