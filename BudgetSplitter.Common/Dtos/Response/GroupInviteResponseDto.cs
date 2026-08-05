namespace BudgetSplitter.Common.Dtos.Response;

public class GroupInviteResponseDto
{
    public string InviteUrl { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
