using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;

namespace MOE_System.Application.DTOs.TopUp.Request;

public sealed record GetFilteredAccountHoldersRequest
{
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
    public decimal? MinBalance { get; init; }
    public decimal? MaxBalance { get; init; }
    public List<string>? EducationLevelsIds { get; init; }
    public List<string>? SchoolingStatusesIds { get; init; }
}
