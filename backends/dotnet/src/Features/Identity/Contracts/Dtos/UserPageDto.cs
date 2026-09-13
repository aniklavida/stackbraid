namespace StackBraid.Features.Identity.Contracts.Dtos;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>UserPage</c> (<c>Page</c> + typed <c>items</c>) schema.</summary>
public sealed record UserPageDto(int Page, int PageSize, int TotalItems, int TotalPages, IReadOnlyList<UserDto> Items);
