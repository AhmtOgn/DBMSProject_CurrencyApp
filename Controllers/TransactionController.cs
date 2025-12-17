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

        public IActionResult Index()
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
                               wc.""currencyCode"" as SourceCode,
                               o.""targetAmount"",
                               c_base.""currencyCode"" as BaseCode,
                               c_target.""currencyCode"" as TargetCode,
                               cp.""baseCurrencyId"",
                               cp.""targetCurrencyId""
                        FROM ""Transaction"" t
                        JOIN ""Wallet"" w ON t.""walletId"" = w.""walletId""
                        JOIN ""Currency"" wc ON w.""currencyId"" = wc.""currencyId"" -- Cüzdanın para birimi (Source)
                        LEFT JOIN ""Order"" o ON t.""orderId"" = o.""orderId""       -- Order detayları (TargetAmount için)
                        LEFT JOIN ""CurrencyPair"" cp ON o.""currencyPairId"" = cp.""currencyPairId"" -- Parite bilgisi
                        LEFT JOIN ""Currency"" c_base ON cp.""baseCurrencyId"" = c_base.""currencyId""
                        LEFT JOIN ""Currency"" c_target ON cp.""targetCurrencyId"" = c_target.""currencyId""
                        WHERE w.""userId"" = @uid
                        ORDER BY t.""date"" DESC";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var trans = new Transaction
                                {
                                    TransactionId = reader.GetInt32(reader.GetOrdinal("transactionId")),
                                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                                    Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                                    CurrencyCode = reader.GetString(reader.GetOrdinal("SourceCode")),
                                };

                                string opString = reader.GetString(reader.GetOrdinal("operationType"));
                                trans.OperationType = Enum.Parse<OperationType>(opString);
                                
                                string statusString = reader.GetString(reader.GetOrdinal("transactionStatus"));
                                trans.TransactionStatus = Enum.Parse<ProcessStatus>(statusString);

                                if (!reader.IsDBNull(reader.GetOrdinal("targetAmount")))
                                {
                                    trans.TargetAmount = reader.GetDecimal(reader.GetOrdinal("targetAmount"));
                                    
                                    if (trans.OperationType == OperationType.Buy)
                                    {
                                        // BUY: Give TARGET, Get BASE
                                        trans.TargetCurrencyCode = reader.GetString(reader.GetOrdinal("BaseCode"));
                                    }
                                    else if (trans.OperationType == OperationType.Sell)
                                    {
                                        // SELL: Give BASE, Get TARGET
                                        trans.TargetCurrencyCode = reader.GetString(reader.GetOrdinal("TargetCode"));
                                    }
                                }

                                transactions.Add(trans);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "ERROR:" + ex.Message;
            }

            return View(transactions);
        }
    }
}