using BudgetSplitter.Common.Domain;

namespace BudgetSplitter.App.UnitTests.Domain;

public sealed class ExpenseShareSettlementTests
{
    [Fact]
    public void IsPaid_PayerShareIsAlwaysSettled()
    {
        var payerId = Guid.NewGuid();

        var isPaid = ExpenseShareSettlement.IsPaid(payerId, payerId, shareAmount: 100, paidAmount: 0);

        Assert.True(isPaid);
    }

    [Theory]
    [InlineData(30, 0, false)]
    [InlineData(30, 29.99, false)]
    [InlineData(30, 30, true)]
    [InlineData(30, 31, true)]
    public void IsPaid_NonPayerDependsOnRecordedPayments(decimal shareAmount, decimal paidAmount, bool expected)
    {
        var isPaid = ExpenseShareSettlement.IsPaid(Guid.NewGuid(), Guid.NewGuid(), shareAmount, paidAmount);

        Assert.Equal(expected, isPaid);
    }
}
