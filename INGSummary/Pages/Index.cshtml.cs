using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace INGSummary.Pages
{
    public class IndexModel : PageModel
    {
        public List<Transaction> transactions = new List<Transaction>();
        public List<Transaction> categorizedTransactions = new List<Transaction>();
        public List<List<Transaction>> transactionsByWeek = new List<List<Transaction>>();
        public List<InterpretedTransaction> interpretedTransactions = new List<InterpretedTransaction>();
        public List<List<Transaction>> transactionsByMonth = new List<List<Transaction>>();

        public void OnGet()
        {

        }

        private IHostingEnvironment _environment;
        [BindProperty]
        public IFormFile Upload { get; set; }

        public JsonResult OnPost()
        {
            try
            {
                if (!Directory.Exists("uploads"))
                {
                    Directory.CreateDirectory("uploads");
                }

                var file = Path.Combine("uploads", Upload.FileName);
                using (var fileStream = new FileStream(file, FileMode.Create))
                {
                    Upload.CopyTo(fileStream);
                }

                transactions = getTransactionsFromFile(file);
                transactionsByMonth = findMonthlyTransactions(transactions);
                //categorizedTransactions = categorizeTransactions(transactions);
                //transactionsByWeek = findWeeklyTransactions(categorizedTransactions);
                //interpretedTransactions = interpretTransactionsByWeek(transactionsByWeek);
            }
            catch (Exception)
            {
                throw;
            }

            return new JsonResult(JsonConvert.SerializeObject(transactionsByMonth));

        }

        private List<List<Transaction>> findMonthlyTransactions(List<Transaction> categorizedTransactions)
        {
            List<List<Transaction>> monthly = new List<List<Transaction>>();

            IEnumerable<int> months = categorizedTransactions.Select(o => o.Date.Month).Distinct();
            IEnumerable<int> years = categorizedTransactions.Select(o => o.Date.Year).Distinct();

            foreach (var year in years)
            {
                foreach (var month in months)
                {
                    monthly.Add(new List<Transaction>());
                    IEnumerable<int> days = categorizedTransactions.Where(o => o.Date.Year == year && o.Date.Month == month).Select(o => o.Date.Day).Distinct();

                    foreach (var day in days)
                    {
                        monthly.Last().Add(new Transaction
                        {
                            Date = categorizedTransactions.Where(o => o.Date.Year == year && o.Date.Month == month && o.Date.Day == day).First().Date,
                            Debit = categorizedTransactions.Where(o => o.Date.Year == year && o.Date.Month == month && o.Date.Day == day).Sum(o => o.Debit)
                        });

                        monthly.Last().Reverse();
                    }
                }
            }

            monthly.Reverse();
            return monthly;
        }

        private List<InterpretedTransaction> interpretTransactionsByWeek(List<List<Transaction>> transactionsByWeek)
        {
            List<InterpretedTransaction> response = new List<InterpretedTransaction>();

            transactionsByWeek.Reverse();

            for (int i = 0; i < transactionsByWeek.Count - 2; i++)
            {
                response.Add(new InterpretedTransaction());

                response.Last().WeekNo = transactionsByWeek[i].First().CalendarWeek;
                response.Last().TotalSpent = transactionsByWeek[i].Sum(o => o.Debit).ToString();

                double dThisWeek = (double)transactionsByWeek[i].Sum(o => o.Debit);
                double dLastWeek = (double)transactionsByWeek[i + 1].Sum(o => o.Debit);
                double dDiff = dThisWeek - dLastWeek;
                double dPercent = (double)(dDiff / dLastWeek * 100);

                response.Last().TotalSpentPreviousPercent = dPercent.ToString().Substring(0, dPercent.ToString().IndexOf(".") + 2);

                response.Last().Payments = new List<List<string>>();
                foreach (var item in transactionsByWeek[i])
                {
                    response.Last().Payments.Add(new List<string>());
                    response.Last().Payments.Last().Add(item.To);
                    response.Last().Payments.Last().Add(item.SpendingType.ToString());
                    response.Last().Payments.Last().Add(item.Debit.ToString());
                }
            }
            return response;
        }

        private static List<Transaction> getTransactionsFromFile(string path)
        {
            List<Transaction> transactionList = new List<Transaction>();

            HSSFWorkbook hssfwb;
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                hssfwb = new HSSFWorkbook(file);
            }

            ISheet sheet = hssfwb.GetSheet("transactions");

            int iDateColumnIndex = -1;
            int iTransactionDetailsColumnIndex = -1;
            int iDebitColumnIndex = -1;
            int iCreditColumnIndex = -1;

            for (int col = 0; col < sheet.GetRow(1).Cells.Count - 1; col++)
            {
                NPOI.SS.UserModel.ICell cell = sheet.GetRow(1).GetCell(col);

                if (cell.StringCellValue == "Data")
                {
                    iDateColumnIndex = col;
                }

                if (cell.StringCellValue == "Detalii tranzactie")
                {
                    iTransactionDetailsColumnIndex = col;
                }

                if (cell.StringCellValue == "Debit")
                {
                    iDebitColumnIndex = col;
                }

                if (cell.StringCellValue == "Credit")
                {
                    iCreditColumnIndex = col;
                }
            }

            if (iDateColumnIndex == -1 || iTransactionDetailsColumnIndex == -1 || iDebitColumnIndex == -1 || iCreditColumnIndex == -1)
            {
                throw new Exception("Could not find one of the columns.");
            }

            for (int row = 0; row <= sheet.LastRowNum; row++)
            {
                for (int col = 0; col < sheet.GetRow(row).Cells.Count - 1; col++)
                {
                    NPOI.SS.UserModel.ICell cell = sheet.GetRow(row).GetCell(col);

                    if (cell.CellType != CellType.Blank)
                    {
                        if (col == iDebitColumnIndex)
                        {
                            if (cell.CellType == CellType.Numeric)
                            {
                                cell.SetCellType(CellType.String);
                                transactionList.Add(new Transaction());
                                transactionList[transactionList.Count - 1].Date = sheet.GetRow(row).GetCell(iDateColumnIndex).DateCellValue;
                                transactionList[transactionList.Count - 1].Debit = double.Parse(cell.StringCellValue, CultureInfo.InvariantCulture);
                                transactionList[transactionList.Count - 1].To = sheet.GetRow(row + 2).GetCell(iTransactionDetailsColumnIndex).StringCellValue;
                                transactionList[transactionList.Count - 1].Type = sheet.GetRow(row).GetCell(iTransactionDetailsColumnIndex).StringCellValue;
                            }
                        }
                        if (col == iCreditColumnIndex)
                        {
                            if (cell.CellType == CellType.Numeric)
                            {
                                cell.SetCellType(CellType.String);
                                transactionList.Add(new Transaction());
                                transactionList[transactionList.Count - 1].Date = sheet.GetRow(row).GetCell(iDateColumnIndex).DateCellValue;
                                transactionList[transactionList.Count - 1].Credit = double.Parse(cell.StringCellValue, CultureInfo.InvariantCulture);
                                transactionList[transactionList.Count - 1].From = sheet.GetRow(row + 1).GetCell(iTransactionDetailsColumnIndex).StringCellValue;
                                transactionList[transactionList.Count - 1].Type = sheet.GetRow(row).GetCell(iTransactionDetailsColumnIndex).StringCellValue;
                            }
                        }
                    }
                }
            }
            return transactionList;
        }

        private static List<Transaction> categorizeTransactions(List<Transaction> transactions)
        {
            double WeekNo;
            double TotalSpent;
            double TotalWithdraws;
            double TotalPOS;
            double TotalTransferHome;
            double TotalFood;
            double TotalCar;
            double TotalWork;
            double TotalOther;

            foreach (var item in transactions)
            {
                switch (item.Type)
                {
                    case "Cumparare POS":
                        item.TypeOfTransaction = TransactionType.CumpararePOS;
                        break;
                    case "Retragere numerar":
                        item.TypeOfTransaction = TransactionType.RetragereNumerar;
                        break;
                    case "Transfer Home'Bank":
                        item.TypeOfTransaction = TransactionType.TransferHomeBank;
                        break;
                    default:
                        break;
                }
            }

            foreach (var item in transactions)
            {
                if (item.To != null)
                {
                    if (item.To.Contains("KAUFLAND")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("CORA")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("CARREFOUR")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("LIDL")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("PROFI")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("CHOPSTIX")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("TIMAS")) { item.SpendingType = SpentOn.Car; }
                    else if (item.To.Contains("MEGA")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("AUCHAN")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("PREMIER")) { item.SpendingType = SpentOn.Food; }
                    else if (item.To.Contains("EAT ETC")) { item.SpendingType = SpentOn.Work; }
                    else if (item.To.Contains("ERIC STEFAN")) { item.SpendingType = SpentOn.Work; }
                    else if (item.To.Contains("INMEDIO")) { item.SpendingType = SpentOn.Work; }
                    else if (item.To.Contains("LUCA")) { item.SpendingType = SpentOn.Work; }
                    else if (item.To.Contains("OMV")) { item.SpendingType = SpentOn.Car; }
                    else if (item.To.Contains("AUTOKARMA")) { item.SpendingType = SpentOn.Car; }
                    else if (item.To.Contains("LAGARDERE")) { item.SpendingType = SpentOn.Work; }
                    else
                    {
                        item.SpendingType = SpentOn.Other;
                    }
                }
            }

            return transactions;
        }

        private static List<List<Transaction>> findWeeklyTransactions(List<Transaction> _transactions)
        {
            List<List<Transaction>> weeklyTransactions = new List<List<Transaction>>();
            CultureInfo cul = CultureInfo.CurrentCulture;
            List<int> cws = new List<int>();

            foreach (var item in _transactions)
            {
                item.CalendarWeek = cul.Calendar.GetWeekOfYear(item.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                cws.Add(item.CalendarWeek);
            }

            foreach (var cw in cws.Distinct().OrderBy(o => o))
            {
                weeklyTransactions.Add(new List<Transaction>());
                foreach (var transaction in _transactions)
                {
                    if (transaction.CalendarWeek == cw)
                    {
                        weeklyTransactions.Last().Add(transaction);
                    }
                }
            }

            return weeklyTransactions;
        }
    }

    public enum TransactionType
    {
        CumpararePOS,
        TransferHomeBank,
        RetragereNumerar
    }

    public enum SpentOn
    {
        Food,
        Work,
        Car,
        Other
    }

    public class Transaction
    {
        public double? Debit { get; set; }
        public double? Credit { get; set; }
        public string Type { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Date { get; set; }
        public int CalendarWeek { get; set; }
        public SpentOn SpendingType { get; set; }
        public TransactionType TypeOfTransaction { get; set; }
    }
}
