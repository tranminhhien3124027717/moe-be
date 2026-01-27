using Microsoft.EntityFrameworkCore;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.DTOs.Provider.Request;
using MOE_System.Application.DTOs.Provider.Response;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using static MOE_System.Domain.Common.BaseException;

namespace MOE_System.Application.Services;

public class ProviderService : IProviderService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProviderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProviderListResponse>> GetAllProvidersAsync(string? search, CancellationToken cancellationToken)
    {
        var providerRepository = _unitOfWork.GetRepository<Provider>();

        var query = providerRepository.Entities
            .Include(p => p.SchoolingLevels)
            .AsQueryable();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search));
        }

        var providers = await query.ToListAsync(cancellationToken);

        return providers.Select(p => new ProviderListResponse(
            p.Id,
            p.Name,
            p.SchoolingLevels.Select(sl => sl.Name).ToList(),
            p.SchoolingLevels.Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            )).ToList(),
            p.Status
        )).OrderBy(p => p.ProviderName).ToList();
    }

    public async Task<IReadOnlyList<ProviderListResponse>> GetActiveProvidersAsync(string? search, CancellationToken cancellationToken)
    {
        var providerRepository = _unitOfWork.GetRepository<Provider>();

        var query = providerRepository.Entities
            .Include(p => p.SchoolingLevels)
            .Where(p => p.Status == "Active")
            .AsQueryable();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search));
        }

        var providers = await query.ToListAsync(cancellationToken);

        return providers.Select(p => new ProviderListResponse(
            p.Id,
            p.Name,
            p.SchoolingLevels.Select(sl => sl.Name).ToList(),
            p.SchoolingLevels.Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            )).ToList(),
            p.Status
        )).OrderBy(p => p.ProviderName).ToList();
    }

    public async Task<ProviderDetailResponse> GetProviderByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new BadRequestException("Provider ID cannot be empty");
        }

        var providerRepository = _unitOfWork.GetRepository<Provider>();
        
        var provider = await providerRepository.Entities
            .Include(p => p.SchoolingLevels)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (provider == null)
        {
            throw new NotFoundException($"Provider with ID {id} not found");
        }

        return new ProviderDetailResponse(
            provider.Id,
            provider.Name,
            provider.Status,
            provider.SchoolingLevels.Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            )).ToList(),
            provider.CreatedAt,
            provider.UpdatedAt
        );
    }

    public async Task<IReadOnlyList<SchoolingLevelDto>> GetAllSchoolingLevelsAsync(CancellationToken cancellationToken)
    {
        var schoolingLevelRepository = _unitOfWork.GetRepository<SchoolingLevel>();
        
        var schoolingLevels = await schoolingLevelRepository.Entities
            .ToListAsync(cancellationToken);

        return schoolingLevels
            .Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            ))
            .ToList();
    }

    public async Task<IReadOnlyList<SchoolingLevelDto>> GetSchoolingLevelsByProviderIdAsync(string providerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new BadRequestException("Provider ID cannot be empty");
        }

        var providerRepository = _unitOfWork.GetRepository<Provider>();
        
        var provider = await providerRepository.Entities
            .Include(p => p.SchoolingLevels)
            .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

        if (provider == null)
        {
            throw new NotFoundException($"Provider with ID {providerId} not found");
        }

        return provider.SchoolingLevels
            .Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            ))
            .ToList();
    }

    public async Task<ProviderDetailResponse> CreateProviderAsync(CreateProviderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException("Provider name cannot be empty");
        }

        var providerRepository = _unitOfWork.GetRepository<Provider>();
        var schoolingLevelRepository = _unitOfWork.GetRepository<SchoolingLevel>();

        // Fetch schooling levels if provided
        var schoolingLevels = new List<SchoolingLevel>();
        if (request.SchoolingLevelIds.Any())
        {
            schoolingLevels = await schoolingLevelRepository.Entities
                .Where(sl => request.SchoolingLevelIds.Contains(sl.Id))
                .ToListAsync(cancellationToken);

            if (schoolingLevels.Count != request.SchoolingLevelIds.Count)
            {
                throw new BadRequestException("One or more schooling level IDs are invalid");
            }
        }

        var provider = new Provider
        {
            Name = request.Name,
            Status = request.Status,
            SchoolingLevels = schoolingLevels
        };

        await providerRepository.InsertAsync(provider);
        await _unitOfWork.SaveAsync();

        return new ProviderDetailResponse(
            provider.Id,
            provider.Name,
            provider.Status,
            provider.SchoolingLevels.Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            )).ToList(),
            provider.CreatedAt,
            provider.UpdatedAt
        );
    }

    public async Task<ProviderDetailResponse> UpdateProviderAsync(UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new BadRequestException("Provider ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException("Provider name cannot be empty");
        }

        var providerRepository = _unitOfWork.GetRepository<Provider>();
        var schoolingLevelRepository = _unitOfWork.GetRepository<SchoolingLevel>();

        var provider = await providerRepository.Entities
            .Include(p => p.SchoolingLevels)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (provider == null)
        {
            throw new NotFoundException($"Provider with ID {request.Id} not found");
        }

        // Update basic properties
        provider.Name = request.Name;
        provider.Status = request.Status;
        provider.UpdatedAt = DateTime.UtcNow;

        // Update schooling levels
        if (request.SchoolingLevelIds.Any())
        {
            var schoolingLevels = await schoolingLevelRepository.Entities
                .Where(sl => request.SchoolingLevelIds.Contains(sl.Id))
                .ToListAsync(cancellationToken);

            if (schoolingLevels.Count != request.SchoolingLevelIds.Count)
            {
                throw new BadRequestException("One or more schooling level IDs are invalid");
            }

            provider.SchoolingLevels = schoolingLevels;
        }
        else
        {
            provider.SchoolingLevels.Clear();
        }

        await providerRepository.UpdateAsync(provider);
        await _unitOfWork.SaveAsync();

        return new ProviderDetailResponse(
            provider.Id,
            provider.Name,
            provider.Status,
            provider.SchoolingLevels.Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            )).ToList(),
            provider.CreatedAt,
            provider.UpdatedAt
        );
    }

    public async Task DeleteProviderAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new BadRequestException("Provider ID cannot be empty");
        }

        var providerRepository = _unitOfWork.GetRepository<Provider>();
        
        var provider = await providerRepository.GetByIdAsync(id);

        if (provider == null)
        {
            throw new NotFoundException($"Provider with ID {id} not found");
        }

        await providerRepository.DeleteAsync(provider);
        await _unitOfWork.SaveAsync();
    }

    public async Task<ProviderDetailResponse> ActivateProviderAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new BadRequestException("Provider ID cannot be empty");
        }

        var providerRepository = _unitOfWork.GetRepository<Provider>();
        
        var provider = await providerRepository.Entities
            .Include(p => p.SchoolingLevels)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (provider == null)
        {
            throw new NotFoundException($"Provider with ID {id} not found");
        }

        provider.Status = "Active";
        provider.UpdatedAt = DateTime.UtcNow;

        await providerRepository.UpdateAsync(provider);
        await _unitOfWork.SaveAsync();

        return new ProviderDetailResponse(
            provider.Id,
            provider.Name,
            provider.Status,
            provider.SchoolingLevels.Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            )).ToList(),
            provider.CreatedAt,
            provider.UpdatedAt
        );
    }

    public async Task<ProviderDetailResponse> DeactivateProviderAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new BadRequestException("Provider ID cannot be empty");
        }

        var providerRepository = _unitOfWork.GetRepository<Provider>();
        
        var provider = await providerRepository.Entities
            .Include(p => p.SchoolingLevels)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (provider == null)
        {
            throw new NotFoundException($"Provider with ID {id} not found");
        }

        provider.Status = "Inactive";
        provider.UpdatedAt = DateTime.UtcNow;

        await providerRepository.UpdateAsync(provider);
        await _unitOfWork.SaveAsync();

        return new ProviderDetailResponse(
            provider.Id,
            provider.Name,
            provider.Status,
            provider.SchoolingLevels.Select(sl => new SchoolingLevelDto(
                sl.Id,
                sl.Name,
                sl.Description
            )).ToList(),
            provider.CreatedAt,
            provider.UpdatedAt
        );
    }
}