using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LeverageRECalculator
{
    public class Program
    {
        static Asset CreateAsset()
        {
            Console.Write("Name: ");
            string name = Console.ReadLine();
            Console.Write("Cost: ");
            double cost = ReadDouble();
            Console.Write("Down payment: ");
            double downPayment = ReadDouble();
            Console.Write("Years of lease: ");
            int leasePeriod = ReadValue();
            Console.Write("Appreciation per year (for 2%, write 0.02): ");
            double app = ReadDouble();
            Console.Write("Interest for mortgage (5% => 0.05): ");
            double interest = ReadDouble();
            Console.Write("Return per year (rent; standard is 0.07): ");
            double rpy = ReadDouble();

            Console.Write("Confirmation: ");
            if (Console.ReadLine() == "no")
                return null;

            return new Asset(name, cost, downPayment, leasePeriod, app, interest, rpy);
        }

        public static double Cash;
        public static DateTime Start = DateTime.Now, Now = Start;
        public static List<Asset> Assets = new List<Asset>(1000);
        public static Dictionary<string, Asset> PresetAssets = new Dictionary<string, Asset>();
        public static string KeyNow = "X0";

        public static double LastYearAssets = 1;
        public static double LastYearLiabilities = 1;
        public static double LastYearNetWorth = 1;

        public static bool ShowTransactionsOutput = true;
        public static bool ShowTimerOutput = false;

        public static double AssetsValue
        {
            get
            {
                return Assets.Sum(a => a.Value);
            }
        }
        public static double LiabilitiesValue
        {
            get
            {
                return Assets.Sum(a => a.OutstandingDebt);
            }
        }

        public static void Main()
        {
            //if (!License()) {
            //    Console.WriteLine("Broken environment - check license - check version.");
            //    return;
            //}

            ReadPresetAssets();

            Console.Write("\nStarting with: ", FormatCash(Cash));
            Cash = ReadDouble();
            System.Console.WriteLine("\n\n");

            KeyNow = "X1";
            while (Cash >= -1000000)
            {
                ApplicationAction();
            }
        }

        private static void ApplicationAction()
        {
            Stopwatch timer = null;
            if (Program.ShowTimerOutput)
            {
                Console.WriteLine("Routine starting");
                timer = Stopwatch.StartNew();
            }
            PassTime(TimeSpan.FromDays(365));

            double deltaA = AssetsValue - LastYearAssets, deltaL = LiabilitiesValue - LastYearLiabilities, deltaNW = (Cash + AssetsValue - LiabilitiesValue) - LastYearNetWorth;
            Console.WriteLine("\nTime is {1}={0:F0} years", (Now - Start).TotalDays / 365, 'Δ');
            Console.WriteLine("Cash in bank: {0}", FormatCash(Cash));
            Console.WriteLine("  Assets: {0}  ({1}{2}    {1}{3:F2}%)", FormatCash(AssetsValue).PadLeft(24, ' '), deltaA >= 0 ? "+" : "", FormatCash(deltaA), AssetsValue / LastYearAssets * 100 - 100);
            Console.WriteLine("  Liabilities: {0}  ({1}{2}    {1}{3:F2}%)", string.Format("-{0}", FormatCash(LiabilitiesValue)).PadLeft(19, ' '), deltaL >= 0 ? "+" : "", FormatCash(deltaL), LiabilitiesValue / LastYearLiabilities * 100 - 100);
            Console.WriteLine("\tNet worth: {0}  ({1}{2}    {1}{3:F2}%)", FormatCash(Cash + AssetsValue - LiabilitiesValue).PadLeft(19), deltaNW >= 0 ? "+" : "", FormatCash(deltaNW), (Cash + AssetsValue - LiabilitiesValue) / LastYearNetWorth * 100 - 100);
            Console.WriteLine();

            LastYearAssets = AssetsValue != 0 ? AssetsValue : 1;
            LastYearLiabilities = LiabilitiesValue != 0 ? LiabilitiesValue : 1;
            LastYearNetWorth = (Cash + AssetsValue - LiabilitiesValue) != 0 ? (Cash + AssetsValue - LiabilitiesValue) : 1;

            if (Program.ShowTimerOutput)
            {
                timer.Stop();
                Console.WriteLine("\t--> Routine executed in {0:F2} ms", timer.Elapsed.TotalMilliseconds);
            }

            while (Cash >= 25000)
            {
                #region Handle command
                Console.WriteLine("\n\nBuy building of type {0}?\n\t \"'\" = buy 1, \";\" = buy with all spare cash, \"c\" = create custom building, \"t\"= set type, ENTER = no", KeyNow);
                string line = Console.ReadLine();
                if (line == "'")
                {
                    Asset type = PresetAssets[KeyNow];
                    Asset myAsset = type.Clone() as Asset;
                    myAsset.Name = string.Format(myAsset.Name, Asset.ODO + 1);
                    BuyAsset(myAsset);
                }
                else if (line.StartsWith(";"))
                {
                    Asset type = PresetAssets[KeyNow];
                    double x = 1000;
                    while (Cash >= type.DownPayment)
                    {
                        if ((int)type.DownPayment == 0)
                        {
                            if (x-- > 0)
                                continue;
                            else break;
                        }
                        Asset myAsset = type.Clone() as Asset;
                        myAsset.Name = string.Format(myAsset.Name, Asset.ODO + 1);
                        BuyAsset(myAsset);
                    }
                }
                else if (line == "c")
                {
                    Asset newB = CreateAsset();
                    if (newB == null)
                        continue;

                    BuyAsset(newB);
                }
                else if (line == "t")
                {
                    Console.Write("Write asset key: ");
                    line = Console.ReadLine().ToUpper();
                    Asset type;
                    if (PresetAssets.TryGetValue(line, out type))
                    {
                        Asset myAsset = type.Clone() as Asset;
                        myAsset.Name = string.Format(myAsset.Name, Asset.ODO + 1);
                        BuyAsset(myAsset);
                        KeyNow = line;
                    }
                    else
                    {
                        Console.WriteLine("Type does not exist in iv.txt");
                    }
                }
                else if (line == "sell")
                {
                    Console.Write("Which asset do you want to sell? ");
                    line = Console.ReadLine();
                    Asset soldAsset = Assets.FirstOrDefault(a => a.Name == line);
                    if (soldAsset == null)
                    {
                        Console.WriteLine("Cannot find an asset named {0}", line);
                    }
                    else
                    {
                        double debt = soldAsset.OutstandingDebt;
                        double value_ = soldAsset.Value;
                        double balance = -debt + value_;
                        Receive(value_, "Sold asset " + soldAsset.Name + " for " + FormatCash(value_));
                        Pay(debt, "Paid off debt of " + FormatCash(debt));
                        Console.WriteLine("Balance: {0}", FormatCash(balance));

                        Assets.Remove(soldAsset);
                    }
                }
                else if (line == "sell all")
                {
                    Console.WriteLine("Are you sure?");
                    if (Console.ReadLine() == "yes")
                    {
                        double totalBalance = 0;
                        foreach (var asset in Assets)
                        {
                            double debt = asset.OutstandingDebt;
                            double value_ = asset.Value;
                            double balance = -debt + value_;
                            Receive(value_, "\nSold asset " + asset.Name + " for " + FormatCash(value_));
                            Pay(debt, "Paid off debt of " + FormatCash(debt));
                            Console.WriteLine("\tBalance: {0}", FormatCash(balance));
                            // TODO BALANCE VS PROFIT ?
                            totalBalance += balance;
                        }
                        Assets.Clear();
                        Console.WriteLine("\n\n\t Total Balance: {0}", FormatCash(totalBalance));
                    }
                }
                else if (line == "sell range")
                {
                    if (!SellRange())
                        continue;
                }
                else if (line == "t?")
                {
                    Console.WriteLine(IVTXT);
                }
                else if (line.StartsWith("is"))
                {
                    Console.WriteLine("By how much?");
                    double x = ReadDouble();
                    if (x == 0)
                        continue;
                    ExpCat.OtherExp += x;
                }
                else if (line.StartsWith("assets"))
                {
                    if (Assets.Count == 0)
                    {
                        Console.WriteLine("No assets!");
                        continue;
                    }
                    int breaker = 0;
                    foreach (var asset in Assets)
                    {
                        if (line != "assets verbose")
                            Console.WriteLine("{0}  = Valued at: {1}, original value: {2}, bought with {3}, total debt of {4}", asset.Name, FormatCash(asset.Value), FormatCash(asset.Cost), FormatCash(asset.DownPayment), FormatCash(asset.OutstandingDebt));
                        else
                        {
                            DescribeAsset(asset);

                            Console.WriteLine("\n");

                        }
                        if (++breaker % 40000 == 0)
                        {
                            Console.WriteLine("Operation seems time consuming.");
                            if (Console.ReadLine() != "yes")
                            {
                                break;
                            }
                        }
                    }
                }
                else if (line.StartsWith("asset1")) {
                    try
                    {
                        line = line.Substring(7);
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        Asset a = Assets.First(x => x.Name.EndsWith(line));
                        DescribeAsset(a);
                        Console.WriteLine();
                    }
                        catch (Exception e) {
                            Console.WriteLine(e.Message);
                    }
                }
                else if (line == "assc")
                {
                    Console.WriteLine("You own {0} assets.", Assets.Count);
                }
                else if (line == "cout")
                {
                    bool on = Program.ShowTransactionsOutput = !Program.ShowTransactionsOutput;
                    Console.WriteLine("cout " + (on ? "ON" : "OFF"));
                }
                else if (line == "timer")
                {

                    bool on = Program.ShowTimerOutput = !Program.ShowTimerOutput;
                    Console.WriteLine("timer " + (on ? "ON" : "OFF"));
                }
                else
                    break;
                #endregion
            }


            Console.WriteLine("\n\n\nPress Enter to pass another year");
            Console.ReadLine();
        }

        private static void DescribeAsset(Asset asset)
        {
            Console.WriteLine("  {0}", asset.Name);
            Console.WriteLine("\tYears of age: {0}", asset.Tracker.YearsOfAge);
            Console.WriteLine("\tValued at: {0}", FormatCash(asset.Value));
            Console.WriteLine("\tOriginal value: {0}", FormatCash(asset.Cost));
            Console.WriteLine("\tDown payment: {0}", FormatCash(asset.DownPayment));
            Console.WriteLine("\tEquity: {0}", FormatCash(asset.Equity));
            Console.WriteLine("\tOwnership: {0:F2}%", asset.Tracker.EquityPercentage * 100);
            Console.WriteLine("\tDebt: {0}\n", FormatCash(asset.OutstandingDebt));
            Console.WriteLine("\tDTV: {0:F2}%", asset.Tracker.DebtToValue * 100);

            Console.WriteLine("\tLast year profit margin: {0:F2}%", asset.Tracker.LastYearProfitMargin * 100);
            Console.WriteLine("\tCash on cash average: {0:F2}%", asset.Tracker.CashOnCashAverage * 100);
        }

        static bool SellRange() {
            Console.WriteLine("Warning! Do not sell more than 1000 assets on one occasion!");
            try
            {
                Console.Write("Start index: ");
                int start = ReadValue();
                Console.Write("End index: ");
                int end = ReadValue();
                if (start < 0 || start > end || end > Assets.Count)
                    return false;
                var removing = Assets.Where(asset =>
                {
                    // cla x-1
                    // 123456
                    // 012345
                    string str = new string(asset.Name.Skip(asset.Name.LastIndexOf('-') + 1).ToArray());
                    int id = int.Parse(str);
                    return start <= id && end >= id;
                });
                double totalBalance = 0;
                int sold = 0, rc = 0;
                foreach (var asset in removing)
                {
                    double debt = asset.OutstandingDebt;
                    double value_ = asset.Value;
                    double balance = -debt + value_;
                    Receive(value_, "\nSold asset " + asset.Name );
                    Pay(debt, "Paid off debt of " + asset.Name);
                    Console.WriteLine("\tBalance: {0}", FormatCash(balance));
                    totalBalance += balance;
                    sold++;
                }
                Stopwatch sw = Stopwatch.StartNew();
                Assets.RemoveAll(a =>
                {
                    if (rc == sold)
                        // this makes sure we don't perform unnecessary checks, if all that should have been sold are sold
                        // we used to spend minutes here when the list was long
                        return false;
                    bool willRemove = removing.Contains(a);
                    rc = willRemove ? rc + 1 : rc;
                    return willRemove;
                });
                sw.Stop();
                Console.WriteLine("\n\n\tSold {0} assets for {1}\nTime needed: {2:F2} seconds", sold, FormatCash(totalBalance), sw.Elapsed.TotalSeconds);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        static string IVTXT = "";
        private static void ReadPresetAssets()
        {
            PresetAssets.Clear();
            Func<string, double> parser = s => double.Parse(s);
            StringBuilder str = new StringBuilder();
            if (File.Exists("iv.txt"))
            {
                Console.WriteLine("Reading from iv.txt:");
                using (StreamReader reader = new StreamReader("iv.txt"))
                {
                    string line;
                    while (!string.IsNullOrEmpty((line = reader.ReadLine())))
                    {
                        string[] data = line.Split('|');
                        try
                        {
                            double cost = parser(data[2]);
                            double down = parser(data[3]);
                            int leasePd = (int)parser(data[4]);
                            double app = parser(data[5]);
                            double interest = parser(data[6]);
                            double rpy = parser(data[7]);
                            PresetAssets.Add(data[0], new Asset(data[1], cost, down, leasePd, app, interest, rpy));
                            Console.WriteLine("  " + line);
                            str.AppendLine(line);
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine(e.Message);
                            continue;
                        }
                        catch (IndexOutOfRangeException e)
                        {
                            Console.WriteLine(e.Message);
                            continue;
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine(e.Message);
                            continue;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("iv.txt does not exist");
                Console.WriteLine("Adding classic building");
                PresetAssets.Add("X1", new Asset("Bloc X1-{0}", 150000, 40000, 30, 0.03, 0.045, 0.07));
                str.AppendLine("X1|Bloc X1-{0}|150000|40000|30|0.03|0.045|0.07");
            }
            IVTXT = str.ToString();
        }

        static double JobIncome
        {
            get
            {
                var yearsSinceStart = (Now - Start).TotalDays / 365;
                return Math.Min(2250000, Math.Pow(1.06, yearsSinceStart) * baseSalary + Math.Pow(1.08, yearsSinceStart) * baseBonus);
            }
        }

        static double Expenses
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ExpCat.RentExp + ExpCat.FoodExp + ExpCat.CarExp + ExpCat.GasExp + ExpCat.ClothesExp + ExpCat.Wine + ExpCat.FunExp + ExpCat.TravelExp + ExpCat.OtherExp;
            }
        }



        static double baseSalary = 70000;
        static double baseBonus = 10000;
        static void PassTime(TimeSpan time)
        {
            int yearsPassed = (int)time.TotalDays / 365;
            double balance = 0;
            while (yearsPassed-- > 0)
            {
                var yearsSinceStart = (Now - Start).TotalDays / 365;
                Now += TimeSpan.FromDays(365);
                Console.WriteLine();
                balance += Receive(JobIncome, "Salary + Bonus");
                balance -= Pay(Expenses, "Expenses");
                foreach (var item in Assets)
                {
                    if (Program.ShowTransactionsOutput)
                        Console.WriteLine();
                    double rent = item.ReturnPerYear * item.Value;
                    balance += Receive(rent, "Rent for " + item.Name);
                    item.Tracker.OnRentReceived(rent);
                    if (item.LeasePassed < item.LeasePeriod)
                    {
                        double payment = item.PeriodicPayment(item.LeasePeriod);// * (int)(365/time.TotalDays));
                        double towardsInterest = item.Interest / 1 * item.PrincipalDebt;
                        double towardsPrincipal = payment - towardsInterest;

                        item.InterestDebt -= towardsInterest;
                        item.PrincipalDebt -= towardsPrincipal;
                        item.Equity += towardsPrincipal;
                        item.LeasePassed++;
                        balance -= Pay(towardsInterest, string.Format("Interest for {0}", item.Name));
                        balance -= Pay(towardsPrincipal, string.Format("Principal [{0}/{1}] {2}", item.LeasePassed, item.LeasePeriod, item.Name));
                        item.Tracker.OnPaymentMade(towardsInterest, towardsPrincipal);
                    }
                    else {
                        item.Tracker.OnPaymentMade(0, 0);
                    }
                }

                Console.WriteLine("Net cashflow: {0}, out of which {1} is passive income", FormatCash(balance), FormatCash(balance - JobIncome + Expenses));
            }
        }

        static double Pay(double sum, string whatFor)
        {
            Cash -= sum;
            if (Program.ShowTransactionsOutput)
                Console.WriteLine(" -{0} was payed for {1}   ==>  {2}", FormatCash(sum), whatFor, FormatCash(Cash));
            return sum;
        }

        static double Receive(double sum, string whatFor)
        {
            Cash += sum;
            if (Program.ShowTransactionsOutput) 
                Console.WriteLine(" +{0} was received for {1}   ==>  {2}", FormatCash(sum), whatFor, FormatCash(Cash));
            return sum;
        }

        static string FormatCash(double n)
        {
            return string.Format(System.Globalization.CultureInfo.GetCultureInfo("en-US"), "{0:C}", n);
        }

        static void WriteDouble(double n)
        {
            Console.WriteLine(FormatCash(n));
        }

        static void BuyAsset(Asset asset)
        {
            if (asset.DownPayment <= Cash)
            {
                Assets.Add(asset);
                Pay(asset.DownPayment, "Down payment for " + asset.Name);

                asset.Equity = asset.DownPayment;
                asset.PrincipalDebt = (asset.Cost - asset.DownPayment);
                asset.InterestDebt = asset.InitialTotalDebt - asset.PrincipalDebt;
                asset.Acquired = Now;
                /*
				asset.OutstandingDebt = asset.Cost - asset.DownPayment;
				asset.Equity = asset.DownPayment;
				asset.InterestDebt = (asset.Cost - asset.DownPayment) * asset.Interest;
				asset.Acquired = Now;
				*/

                Asset.ODO++;
            }
            else Console.WriteLine("Not enough money");
        }

        static int ReadValue()
        {
            try
            {
                return int.Parse(Console.ReadLine());
            }
            catch
            {
                return 0;
            }
        }

        static double ReadDouble()
        {
            try
            {
                return double.Parse(Console.ReadLine());
            }
            catch
            {
                return 0;
            }
        }




        static class ExpCat
        {
            public static double RentExp = 1200 * 12;
            public static double FoodExp = 2775 * 12;
            public static double CarExp = 7500;
            public static double GasExp = 1650;
            public static double ClothesExp = 3000;
            public static double Wine = 15 * 200;
            public static double FunExp = 87 * 52;
            public static double TravelExp = 0;
            public static double OtherExp = 0;
        }
    }
}
