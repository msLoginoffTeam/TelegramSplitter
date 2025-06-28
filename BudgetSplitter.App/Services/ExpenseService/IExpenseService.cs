using BudgetSplitter.Common.Dtos;
using BudgetSplitter.Common.Dtos.Request;
using BudgetSplitter.Common.Dtos.Response;

namespace BudgetSplitter.App.Services.ExpenseService;

public interface IExpenseService
{
    Task<IEnumerable<ExpenseResponseDto>> 
        GetGroupExpensesAsync(Guid groupId, bool includeDrafts = false);

    // детали одной траты (со списком долей)
    Task<ExpenseResponseDto> 
        GetExpenseByIdAsync(Guid groupId, Guid expenseId);

    // создаёт черновик или сразу подтверждённую трату
    Task<ExpenseResponseDto> 
        CreateExpenseAsync(Guid groupId, CreateExpenseRequestDto dto);

    // редактирует поля самой траты (metadata)
    Task UpdateExpenseAsync(Guid expenseId, decimal amount);
    Task UpdateExpenseAsync(Guid expenseId, string title);

    // удаляет (или помечает удалённой) конкретную трату
    Task DeleteExpenseAsync(Guid groupId, Guid expenseId);

    // подтверждает черновик
    Task ConfirmExpenseAsync(Guid groupId, Guid expenseId);

    Task<IEnumerable<ExpenseShareResponseDto>> 
        GetExpenseParticipantsAsync(Guid groupId, Guid expenseId);

    Task AddExpenseParticipantsAsync(
        Guid groupId, 
        Guid expenseId, 
        ExpenseShareCreateDto share);

    Task UpdateExpenseParticipantAsync(
        Guid groupId, 
        Guid expenseId, 
        ExpenseShareCreateDto share);

    Task RemoveExpenseParticipantAsync(
        Guid groupId, 
        Guid expenseId, 
        Guid userId);
}