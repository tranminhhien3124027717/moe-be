using MOE_System.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Domain.Entities
{
    public class TopUpConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RuleName { get; set; } = string.Empty;
        public decimal? TopupAmount { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? MinBalance { get; set; }
        public decimal? MaxBalance { get; set; }
        public string EducationLevels { get; set; } = string.Empty;
        public string SchoolingStatuses { get; set; } = string.Empty;
        public string? InternalRemarks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}
