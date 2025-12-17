using Microsoft.AspNetCore.Mvc;
using CurrencyApp.Helpers;
using CurrencyApp.Models;
using Npgsql;

namespace CurrencyApp.Controllers
{
    public class TransactionController : Controller
    {
        private readonly DbHelper _dbHelper;

        public TransactionController(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        [HttpGet]
        public IActionResult Index(string searchType, DateTime? startDate, DateTime? endDate)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            List<Transaction> transactions = new List<Transaction>();

            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string sql = @"
                        SELECT t.*, 
                               wc.""currencyCode"" as WalletCurrency,
                               o.""targetAmount"",
                               c_base.""currencyCode"" as BaseCode,
                               c_target.""currencyCode"" as TargetCode
                        FROM ""Transaction"" t
                        JOIN ""Wallet"" w ON t.""walletId"" = w.""walletId""
                        JOIN ""Currency"" wc ON w.""currencyId"" = wc.""currencyId""
                        LEFT JOIN ""Order"" o ON t.""orderId"" = o.""orderId""
                        LEFT JOIN ""CurrencyPair"" cp ON o.""currencyPairId"" = cp.""currencyPairId""
                        LEFT JOIN ""Currency"" c_base ON cp.""baseCurrencyId"" = c_base.""currencyId""
                        LEFT JOIN ""Currency"" c_target ON cp.""targetCurrencyId"" = c_target.""currencyId""
                        WHERE w.""userId"" = @uid";

                    if (!string.IsNullOrEmpty(searchType))
                        sql += @" AND t.""operationType""::text ILIKE @sType";

                    if (startDate.HasValue)
                        sql += @" AND t.""date"" >= @sDate";

                    if (endDate.HasValue)
                        sql += @" AND t.""date"" <= @eDate";

                    sql += @" ORDER BY t.""date"" DESC";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);

                        if (!string.IsNullOrEmpty(searchType))
                            cmd.Parameters.AddWithValue("@sType", $"%{searchType}%");

                        if (startDate.HasValue)
                            cmd.Parameters.AddWithValue("@sDate", startDate.Value);

                        if (endDate.HasValue)
                            cmd.Parameters.AddWithValue("@eDate", endDate.Value.AddDays(1));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var trans = new Transaction
                                {
                                    TransactionId = reader.GetInt32(reader.GetOrdinal("transactionId")),
                                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                                    Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                                    OperationType = Enum.Parse<OperationType>(reader.GetString(reader.GetOrdinal("operationType"))),
                                    TransactionStatus = Enum.Parse<ProcessStatus>(reader.GetString(reader.GetOrdinal("transactionStatus")))
                                };
                                string walletCurrency = reader.GetString(reader.GetOrdinal("WalletCurrency"));
                                string? baseCode = reader.IsDBNull(reader.GetOrdinal("BaseCode")) ? null : reader.GetString(reader.GetOrdinal("BaseCode"));
                                string? targetCode = reader.IsDBNull(reader.GetOrdinal("TargetCode")) ? null : reader.GetString(reader.GetOrdinal("TargetCode"));
                                if (trans.OperationType == OperationType.Buy && targetCode != null)
                                {
                                    trans.CurrencyCode = targetCode;
                                    trans.TargetCurrencyCode = baseCode;
                                }
                                else if (trans.OperationType == OperationType.Sell && baseCode != null)
                                {
                                    trans.CurrencyCode = baseCode;
                                    trans.TargetCurrencyCode = targetCode;
                                }
                                else
                                {
                                    trans.CurrencyCode = walletCurrency;
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("targetAmount")))
                                {
                                    trans.TargetAmount = reader.GetDecimal(reader.GetOrdinal("targetAmount"));
                                }

                                transactions.Add(trans);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "ERROR: " + ex.Message;
            }

            return View(transactions);
        }
    }
}