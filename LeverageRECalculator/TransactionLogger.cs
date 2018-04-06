using System;
using System.IO;
namespace LeverageRECalculator
{
    public class TransactionLogger
    {
        private StreamWriter writer;
        const string LINE_SEPARATOR = "----------------------";
        public TransactionLogger(string path)
        {
            writer = new StreamWriter(path);
        }

        public void WriteYear(int year)
        {
            writer.WriteLine("End of Year {0}\n{1}\n{1}Start of year {2}", year, LINE_SEPARATOR, year + 1);
        }

        public void WriteCashAvailable(double cash)
        {
            writer.WriteLine("Cash available: ", Program.FormatCash(cash));
        }
    }
}