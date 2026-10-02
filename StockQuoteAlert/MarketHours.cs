namespace StockQuoteAlert;

// Horário de negociação da B3, sempre no horário de Brasília.
// 10h às 18h cobre o pregão regular o ano todo (o fechamento muda com o horário de verão dos EUA).
// Feriados não são considerados: nesses dias a cotação fica parada e nenhum alerta novo é gerado.
public static class MarketHours
{
    private static readonly TimeSpan Open = new(10, 0, 0);
    private static readonly TimeSpan Close = new(18, 0, 0);

    private static readonly TimeZoneInfo Brasilia =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    // Horário atual em Brasília, independente do fuso configurado no computador
    public static DateTime NowInBrasilia() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Brasilia);

    // Pregão só acontece de segunda a sexta
    private static bool IsTradingDay(DateTime day) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    public static bool IsOpen(DateTime now) =>
        IsTradingDay(now) && now.TimeOfDay >= Open && now.TimeOfDay < Close;

    // Data e hora da próxima abertura a partir de "now"
    public static DateTime NextOpening(DateTime now)
    {
        var day = now.Date;
        if (now.TimeOfDay >= Open) day = day.AddDays(1);  // a abertura de hoje já passou

        while (!IsTradingDay(day)) day = day.AddDays(1);

        return day + Open;
    }
}
