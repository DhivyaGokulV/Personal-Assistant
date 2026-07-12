namespace PersonalAssistant.Domain.Enums;

public enum AssetStatus
{
    InPossession = 1,
    Sold = 2
}

public enum InvestmentStatus
{
    Active = 1,
    Inactive = 2
}

public enum InvestmentType
{
    UnitBased = 1,
    AmountBased = 2
}

public enum InvestmentTxType
{
    Buy = 1,
    Sell = 2,
    Credit = 3,
    Debit = 4
}

public enum InvestmentAuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    StatusChange = 4
}

public enum PreciousMetalTxType
{
    Buy = 1,
    Sell = 2
}

public enum LiabilityStatus
{
    Active = 1,
    Past = 2
}

public enum LiabilityTxType
{
    Acquisition = 1,
    Repayment = 2
}

public enum LiabilityAccountCategory
{
    Loan = 1,
    Debt = 2,
    CreditCard = 3
}

public enum LiabilityAccountStatus
{
    Active = 1,
    Inactive = 2
}

public enum LiabilityAccountTxType
{
    Credit = 1,
    Debit = 2
}
