namespace SV.Script.Tests;

public class AccountVIPDeal
{
    public int AccountId { get; set; }
    public int VIPId { get; set; }
    public int Qty { get; set; }
    public int LastEditUser { get; set; }
    public string? PromoCode { get; set; }
    public PriceType PriceType { get; set; }
    public long? Now { get; set; }
}

public enum PriceType
{
    Monthly = 0,
    Annual = 1,
}

public class DealTest
{
    private CompiledScript script;

    public DealTest()
    {
        var engine = new ScriptEngine()
    .RegisterType(typeof(Math))
    .RegisterType<AccountVIPDeal>()
    .RegisterType<PriceType>();
        script = engine.Compile("""
            let m = deal.PriceType == PriceType.Monthly ? deal.Qty : deal.Qty * 12;
            if (m >= 1 && m <= 5)
            {
                return amt * 0.9;
            }
            else if (m >= 6 && m <= 11)
            { 
                return amt * 0.8;
            }
            else if (m >= 12)
            {
                return amt * 0.7;
            }
            else
            {
                return amt;
            }
            """);
    }

    [Theory]
    [InlineData(2, PriceType.Monthly, 3, 1.8)]
    [InlineData(2, PriceType.Monthly, 5, 1.8)]
    [InlineData(2, PriceType.Monthly, 6, 1.6)]
    [InlineData(2, PriceType.Monthly, 12, 1.4)]
    [InlineData(2, PriceType.Annual, 1, 1.4)]
    public void TestAccountVIPDeal(decimal amt, PriceType priceType, int qty, decimal expected)
    {
        // Arrange
        var deal = new AccountVIPDeal
        {
            AccountId = 0,
            VIPId = 1,
            Qty = qty,
            LastEditUser = 1,
            PromoCode = "PROMO123",
            PriceType = priceType,
            Now = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var r = script.Run(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            { "deal", deal },
            { "amt", amt}
        });
        //var m = deal.PriceType == PriceType.Monthly ? deal.Qty : deal.Qty * 12;
        //if (m >= 1 && m <= 5)
        //{
        //    return amt * 0.9;
        //}
        //else if (m >= 6 && m <= 11)
        //{
        //    return amt * 0.8;
        //}
        //else if (m >= 12)
        //{
        //    return amt * 0.7;
        //}
        //else
        //{
        //    return amt;
        //}
        Assert.Equal(expected, r.AsDec);
    }

}
