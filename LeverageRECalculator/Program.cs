using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace LeverageRECalculator
{
    public partial class Program
    {
        #region Properties and fields
        public static double Cash;
        public static DateTime Start = DateTime.Now, Now = Start;
        public static List<Asset> Assets = new List<Asset>(1000);
        public static Dictionary<string, Asset> PresetAssets = new Dictionary<string, Asset>();
        public static List<double> HardMoneyTransactions = new List<double>(50);
        public static string KeyNow = "X0";

        public static double LastYearAssets = 1;
        public static double LastYearLiabilities = 1;
        public static double LastYearNetWorth = 1;

        public static bool ShowTransactionsOutput = true;
        public static bool ShowTimerOutput = false;

		public static bool SalaryBumps = false;

        public static bool AdminMode = false;
		#endregion

		public static void Main()
		{
			DeviateTextStreams();
			ReadPresetAssets();


			Out.Write("\nStarting with: ", FormatCash(Cash));
			Cash = ReadDouble();

			Out.Write("Salary bumps [ON/OFF]: ");
			SalaryBumps = In.ReadLine().ToLower() == "on";

			Out.WriteLine("\n\n");

			KeyNow = "X1";
			while (Cash >= -1000000)
			{
				ApplicationAction();
			}
		}

        #region Action
        private static void ApplicationAction()
        {
            Out.Flush();
            Stopwatch timer = null;
            if (Program.ShowTimerOutput)
            {
                Out.WriteLine("Routine starting");
                timer = Stopwatch.StartNew();
            }
            PassTime(TimeSpan.FromDays(365));


            double deltaA = AssetsValue - LastYearAssets, deltaL = LiabilitiesValue - LastYearLiabilities, deltaNW = (Cash + AssetsValue - LiabilitiesValue) - LastYearNetWorth;
            Out.WriteLine("\nTime is {1}={0:F0} years", (Now - Start).TotalDays / 365, 'Δ');
            Out.WriteLine("Cash in bank: {0}", FormatCash(Cash));
            Out.WriteLine("  Assets: {0}  ({1}{2}    {1}{3:F2}%)", FormatCash(AssetsValue).PadLeft(24, ' '), deltaA >= 0 ? "+" : "", FormatCash(deltaA), AssetsValue / LastYearAssets * 100 - 100);
            Out.WriteLine("  Liabilities: {0}  ({1}{2}    {1}{3:F2}%)", string.Format("-{0}", FormatCash(LiabilitiesValue)).PadLeft(19, ' '), deltaL >= 0 ? "+" : "", FormatCash(deltaL), LiabilitiesValue / LastYearLiabilities * 100 - 100);
            Out.WriteLine("\tNet worth: {0}  ({1}{2}    {1}{3:F2}%)", FormatCash(Cash + AssetsValue - LiabilitiesValue).PadLeft(19), deltaNW >= 0 ? "+" : "", FormatCash(deltaNW), (Cash + AssetsValue - LiabilitiesValue) / LastYearNetWorth * 100 - 100);
            Out.WriteLine();

            LastYearAssets = AssetsValue != 0 ? AssetsValue : 1;
            LastYearLiabilities = LiabilitiesValue != 0 ? LiabilitiesValue : 1;
            LastYearNetWorth = (Cash + AssetsValue - LiabilitiesValue) != 0 ? (Cash + AssetsValue - LiabilitiesValue) : 1;

            if (Program.ShowTimerOutput)
            {
                timer.Stop();
                Out.WriteLine("\t--> Routine executed in {0:F2} ms", timer.Elapsed.TotalMilliseconds);
            }

            Out.Flush();


            while (Cash >= 1)
            {
				if (HandleConsoleCommand())
					continue;
				break;
            }


            Out.WriteLine("\n\n\nEND OF YEAR {0}\n-------------\n\n", (Now-Start).TotalDays/365);
            Out.WriteLine("[Press enter to pass another year]");
            In.ReadLine();
        }

        static bool HandleConsoleCommand()
		{
            Out.WriteLine("\n\nBuy building of type {0}?", KeyNow);
            Console.WriteLine("\t \"'\" = buy 1, \";\" = buy with all spare cash, \"c\" = create custom building, \"t\"= set type, ENTER = no");
            string line = In.ReadLine();
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
                    return true;

                BuyAsset(newB);
            }
            else if (line == "t")
            {
                Out.Write("Write asset key: ");
                line = In.ReadLine().ToUpper();
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
                    Out.WriteLine("Type does not exist in iv.txt");
                }
            }
            else if (line == "sell")
            {
                Out.Write("Which asset do you want to sell? ");
                line = In.ReadLine();
                Asset soldAsset = Assets.FirstOrDefault(a => a.Name == line);
                if (soldAsset == null)
                {
                    Out.WriteLine("Cannot find an asset named {0}", line);
                }
                else
                {
                    double debt = soldAsset.PrincipalDebt;
                    double value_ = soldAsset.Value;
                    double balance = -debt + value_;
                    Receive(value_, "Sold asset " + soldAsset.Name + " for " + FormatCash(value_));
                    //Pay(debt, "Paid off debt of " + FormatCash(debt));
                    PayTowardsPrincipal(debt, soldAsset);
                    Out.WriteLine("Balance: {0}", FormatCash(balance));

                    Assets.Remove(soldAsset);
                }
            }
            else if (line == "sell all")
            {
                Out.WriteLine("Are you sure?");
                if (In.ReadLine() == "yes")
                {
                    double totalBalance = 0;
                    foreach (var asset in Assets)
                    {
                        double debt = asset.PrincipalDebt;
                        double value_ = asset.Value;
                        double balance = -debt + value_;
                        Receive(value_, "\nSold asset " + asset.Name + " for " + FormatCash(value_));
                        //Pay(debt, "Paid off debt of " + FormatCash(debt));
                        PayTowardsPrincipal(debt, asset);
                        if (Program.ShowTransactionsOutput)
                            Out.WriteLine("\tBalance: {0}", FormatCash(balance));
                        // TODO BALANCE VS PROFIT ?
                        totalBalance += balance;
                    }
                    Assets.Clear();
                    Out.WriteLine("\n\n\t Total Balance: {0}", FormatCash(totalBalance));
                }
            }
            else if (line == "sell range")
            {
                if (!SellRange())
                    return true;
            }
            else if (line == "t?")
            {
                Out.WriteLine(IVTXT);
            }
            else if (line.StartsWith("pay"))
            {
                Out.WriteLine("Which asset?");
                string name = In.ReadLine();
                Asset a = Assets.FirstOrDefault(x => x.Name.EndsWith(name));
                if (a != null)
                {
                    double sum;
                    if (line == "pay full")
                    {
                        sum = a.PrincipalDebt;
                    }
                    else
                    {
                        Out.Write("Payment: ");
                        sum = ReadDouble();
                    }
                    PayTowardsPrincipal(sum, a);
                }
                else
                {
                    Out.WriteLine("Cannot find asset {0}", name);
                }
            }
            else if (line.StartsWith("is"))
            {
                Out.WriteLine("By how much?");

                double x = ReadDouble();
                if (x == 0)
                    return true;
                ExpCat.OtherExp += x;
            }
            else if (line.StartsWith("assets"))
            {
                if (Assets.Count == 0)
                {
                    Out.WriteLine("No assets!");
                    return true;
                }
                int breaker = 0;
                foreach (var asset in Assets)
                {
                    if (line != "assets verbose")
                        Out.WriteLine("{0}  = Valued at: {1}, original value: {2}, bought with {3}, total debt of {4}", asset.Name, FormatCash(asset.Value), FormatCash(asset.Cost), FormatCash(asset.DownPayment), FormatCash(asset.OutstandingDebt));
                    else
                    {
                        DescribeAsset(asset);
                        Out.WriteLine("\n");
                    }
                    if (++breaker % 40000 == 0)
                    {
                        Out.WriteLine("Operation seems time consuming.");
                        if (In.ReadLine() != "yes")
                        {
                            break;
                        }
                    }
                }
            }
            else if (line.StartsWith("asset1"))
            {
                try
                {
                    line = line.Substring(7);
                    if (string.IsNullOrWhiteSpace(line))
                        return true;
                    Asset a = Assets.First(x => x.Name.EndsWith(line));
                    DescribeAsset(a);
                    Out.WriteLine();
                }
                catch (Exception e)
                {
                    Out.WriteLine(e.Message);
                }
            }
            else if (line == "assc")
            {
                Out.WriteLine("You own {0} assets.", Assets.Count);
            }
            else if (line == "cash")
            {
                Out.WriteLine("Cash available: {0}", FormatCash(Cash));
            }
            else if (line == "cout")
            {
                bool on = Program.ShowTransactionsOutput = !Program.ShowTransactionsOutput;
                Out.WriteLine("cout " + (on ? "ON" : "OFF"));
            }
            else if (line == "timer")
            {
                bool on = Program.ShowTimerOutput = !Program.ShowTimerOutput;
                Out.WriteLine("timer " + (on ? "ON" : "OFF"));
            }
            else if (line == "odo-reset")
            {
                Out.WriteLine("Warning! Resetting ODO can result in unexpected problems. Are you sure you want to continue?");
                if (In.ReadLine() == "yes")
                {
                    Asset.ODO = 0;
                    Out.WriteLine("ODO = 0.");
                }
            }
            else if (line == "hardmoney")
            {
                if (!HardMoneyTransactions.Any())
                {
                    Out.WriteLine("\tHardmoney is a function mainly used for hard money\n" +
                                      "\tlending simulation or for debugging purposes\n" +
                                      "\tTo see how it was used type \'showhardmoney\'." +
                                      "\nYou can now type the amount of money you want to receive.\n" +
                                      "If you want to pay it back simply input a negative sum.");
                }
                Out.Write("Amount? ");
                double sum = ReadDouble();
                Cash += sum;
                HardMoneyTransactions.Add(sum);
            }
            else if (line == "showhardmoney")
            {
                Out.WriteLine("Hardmoney log:");
                HardMoneyTransactions.ForEach(t => Out.WriteLine("\t{0}{1}", t < 0 ? "-" : " ", FormatCash(Math.Abs(t))));
                Out.WriteLine("\t--------\n\t== {0}", FormatCash(HardMoneyTransactions.Sum()));
                Out.WriteLine();
            }
            else if (line == "hardmoney handle")
            {
                double harddebt = HardMoneyTransactions.Sum();
                Cash -= harddebt;
                HardMoneyTransactions.Add(-harddebt);
            }
            else if (line == "help")
            {
                Out.WriteLine(Texts.help);
            }
            else if (line == "clear")
            {
                Console.Clear();
            }
            else if (line == "gc.collect()")
            {
                GC.Collect();
                Console.WriteLine("OK");
            }
            else if (line == "i-am-admin")
            {
                Program.AdminMode = true;
                Out.WriteLine("Admin mode on.");
            }
            else if (line == "admin-out")
            {
                Program.AdminMode = false;
                Out.WriteLine("Admin out of role.");
            }
            else if (line == "flush")
            {
                Out.Flush();
            }
			else if (line == "salarybumps")
			{
				SalaryBumps = !SalaryBumps;
				Console.WriteLine(SalaryBumps ? "ON" : "OFF");
			}
            else if (line == "exit-01")
            {
                Environment.Exit(1);
            }
            
            else if (line != "")
            {
                return true;
            }
            else
                return false;
			return true;
		} 

        static void PassTime(TimeSpan time)
        {
            int yearsPassed = (int)time.TotalDays / 365;
            double balance = 0;
            while (yearsPassed-- > 0)
            {
                var yearsSinceStart = (Now - Start).TotalDays / 365;
                Now += TimeSpan.FromDays(365);
                Out.WriteLine();
                balance += Receive(JobIncome, "Salary + Bonus");
                balance -= Pay(Expenses, "Expenses");
                foreach (var item in Assets)
                {
                    if (Program.ShowTransactionsOutput)
                        Out.WriteLine();
                    double rent = item.ReturnPerYear * item.Value;
                    balance += Receive(rent, "Income for " + item.Name);
                    item.Tracker.OnRentReceived(rent);
                    if (item.LeasePassed < item.LeasePeriod)
                    {
                        bool haveToPay = true;
                        double payment = item.PeriodicPayment(item.LeasePeriod);// * (int)(365/time.TotalDays));
                        double towardsInterest = 0, towardsPrincipal = 0;
                        if (item.PrincipalDebt >= payment)
                        {
                            towardsInterest = item.Interest * item.PrincipalDebt;
                            towardsPrincipal = payment - towardsInterest;
                        }
                        else if (item.PrincipalDebt < payment && item.PrincipalDebt > 0)
                        {
                            towardsInterest = item.Interest * item.PrincipalDebt;
                            towardsPrincipal = item.PrincipalDebt;
                        }
                        else if (item.PrincipalDebt < 0)
                        {
                            haveToPay = false;
                            item.InterestDebt = 0;
                        }

                        if (haveToPay)
                        {
                            item.InterestDebt -= towardsInterest;
                            item.PrincipalDebt -= towardsPrincipal;
                            item.Equity += towardsPrincipal;
                            item.LeasePassed++;
                            balance -= Pay(towardsInterest, string.Format("Interest for {0}", item.Name));
                            balance -= Pay(towardsPrincipal, string.Format("Principal [{0}/{1}] {2}", item.LeasePassed, item.LeasePeriod, item.Name));
                            item.Tracker.OnPaymentMade(towardsInterest, towardsPrincipal);
                        }
                    }
                    else
                    {
                        item.Tracker.OnPaymentMade(0, 0);
                    }
                }

                Out.WriteLine("Net cashflow: {0}, out of which {1} is passive income", FormatCash(balance), FormatCash(balance - JobIncome + Expenses));
            }
        }
        #endregion

        #region Asset managing
        private static void DescribeAsset(Asset asset)
        {
            Out.WriteLine("  {0}", asset.Name);
            Out.WriteLine("\tYears of age: {0}", asset.Tracker.YearsOfAge);
            Out.WriteLine("\tValued at: {0}", FormatCash(asset.Value));
            Out.WriteLine("\tOriginal value: {0}", FormatCash(asset.Cost));
            Out.WriteLine("\tDown payment: {0}", FormatCash(asset.DownPayment));
            Out.WriteLine("\tEquity: {0}", FormatCash(asset.Equity));
            Out.WriteLine("\tOwnership: {0:F2}%", asset.Tracker.EquityPercentage * 100);
            Out.WriteLine("\tDebt: {0} \n\t\t[{1}]\n", FormatCash(asset.OutstandingDebt), FormatCash(asset.PrincipalDebt));


            Out.WriteLine("\tDTV: {0:F2}%", asset.Tracker.DebtToValue * 100);
            Out.WriteLine("\tLast year profit margin: {0:F2}%", asset.Tracker.LastYearProfitMargin * 100);
            Out.WriteLine("\tCash on cash average: {0:F2}%", asset.Tracker.CashOnCashAverage * 100);
        }
        static Asset CreateAsset()
        {
            Out.Write("Name: ");
            string name = In.ReadLine();
            Out.Write("Cost: ");
            double cost = ReadDouble();
            Out.Write("Down payment: ");
            double downPayment = ReadDouble();
            Out.Write("Years of lease: ");
            int leasePeriod = ReadValue();
            Out.Write("Appreciation per year (for 2%, write 0.02): ");
            double app = ReadDouble();
            Out.Write("Interest for mortgage (5% => 0.05): ");
            double interest = ReadDouble();
            Out.Write("Return per year (rent; standard is 0.07): ");
            double rpy = ReadDouble();

            Out.Write("Confirmation: ");
            if (In.ReadLine() == "no")
                return null;

            return new Asset(name, cost, downPayment, leasePeriod, app, interest, rpy);
        }
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

        static bool SellRange()
        {
            Out.WriteLine("Warning! Do not sell more than 1000 assets on one occasion!");
            try
            {
                Out.Write("Start index: ");
                int start = ReadValue();
                Out.Write("End index: ");
                int end = ReadValue();
                if (start < 0 || start > end)// || end > Assets.Count) this last bit comes with a lot of problems
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
                Stopwatch sw = Stopwatch.StartNew();
                foreach (var asset in removing)
                {
                    double debt = asset.PrincipalDebt;
                    double value_ = asset.Value;
                    double balance = -debt + value_;
                    Receive(value_, "\nSold asset " + asset.Name);
                    //Pay(debt, "Paid off debt of " + asset.Name);
                    PayTowardsPrincipal(debt, asset);
                    if (Program.ShowTransactionsOutput)
                    Out.WriteLine("\tBalance: {0}", FormatCash(balance));
                    totalBalance += balance;
                    sold++;
                }
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
                Out.WriteLine("\n\n\tSold {0} assets for {1}\nTime needed: {2:F2} seconds", sold, FormatCash(totalBalance), sw.Elapsed.TotalSeconds);
                return true;
            }
            catch (Exception e)
            {
                Out.WriteLine(e.Message);
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
                        Thread.Sleep(10);
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
                            Out.WriteLine("  " + line);
                            str.AppendLine(line);
                        }
                        catch (FormatException e)
                        {
                            Out.WriteLine(e.Message);
                            continue;
                        }
                        catch (IndexOutOfRangeException e)
                        {
                            Out.WriteLine(e.Message);
                            continue;
                        }
                        catch (ArgumentException e)
                        {
                            Out.WriteLine(e.Message);
                            continue;
                        }
                    }
                }
            }
            else
            {
                Out.WriteLine("iv.txt does not exist");
                Out.WriteLine("Adding classic building");
                PresetAssets.Add("X1", new Asset("Bloc X1-{0}", 150000, 40000, 30, 0.03, 0.045, 0.07));
                str.AppendLine("X1|Bloc X1-{0}|150000|40000|30|0.03|0.045|0.07");
            }
            IVTXT = str.ToString();
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
            else Out.WriteLine("Not enough money");
        }
        #endregion

        #region Income and expenses
        
        static double JobIncome
		{
			get
			{
				var yearsSinceStart = (Now - Start).TotalDays / 365;
				if (SalaryBumps == false)
					return 100000;
				else
					return Math.Min(2245000, Math.Pow(1.06, yearsSinceStart) * baseSalary + Math.Pow(1.08, yearsSinceStart) * baseBonus);
			}
		}

        static double Expenses
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
			{
				if (SalaryBumps == false)
					return 100000;
				return ExpCat.RentExp + ExpCat.FoodExp + ExpCat.CarExp + ExpCat.GasExp + ExpCat.ClothesExp + ExpCat.Wine + ExpCat.FunExp + ExpCat.TravelExp + ExpCat.OtherExp;
			}
        }
        static double baseSalary = 70000;
        static double baseBonus = 10000;

        static double Pay(double sum, string whatFor)
        {
            Cash -= sum;
            if (Program.ShowTransactionsOutput)
                Out.WriteLine(" -{0} was payed for {1}   ==>  {2}", FormatCash(sum), whatFor, FormatCash(Cash));
            return sum;
        }

        static void PayTowardsPrincipal(double sum, Asset a)
        {
            if (sum > Cash || a.PrincipalDebt < 0)
            {
                return;
            }
            sum = (sum >= a.PrincipalDebt) ? a.PrincipalDebt : sum;
            Pay(sum, "Paying down principal debt of asset " + a.Name);
            a.PrincipalDebt -= sum;
            a.Equity += sum;
            a.Tracker.OnPaymentMade(0, sum);

            // we also have to recalculate the interest debt
            /* DOES NOT WORK
            double payment = a.PeriodicPayment(a.LeasePeriod);
            double debt = (a.LeasePeriod - a.LeasePassed) * payment;
            double newInterestDebt = debt - a.Equity;
            double delta = a.InterestDebt - a.PrincipalDebt;
            */

            // we will simulate payments
            double interestDebt = 0, interestNow = 0, towardsPrincipal = 0, totalPrincipal = 0;
            double pd = a.PrincipalDebt;
            int leaseRemaining = a.LeasePeriod - a.LeasePassed;
            double payment = a.PeriodicPayment(a.LeasePeriod);
            while (a.PrincipalDebt - totalPrincipal > 1)
            {
                interestNow = a.Interest * pd;
                interestDebt += interestNow;
                if (payment <= pd)
                {
                    towardsPrincipal = payment - interestNow;
                }
                else if (payment > pd)
                {
                    towardsPrincipal = pd;
                }
                pd -= towardsPrincipal;
                totalPrincipal += towardsPrincipal;
            }
            if (ShowTransactionsOutput)
                Out.WriteLine("Old interest debt: {0}, new interest debt {1}, savings of {2}", FormatCash(a.InterestDebt), FormatCash(interestDebt), FormatCash(a.InterestDebt - interestDebt));
            a.InterestDebt = interestDebt;
        }

        static double Receive(double sum, string whatFor)
        {
            Cash += sum;
            if (Program.ShowTransactionsOutput)
                Out.WriteLine(" +{0} was received for {1}   ==>  {2}", FormatCash(sum), whatFor, FormatCash(Cash));
            return sum;
        }

        #endregion

        #region Text formatting
        public static string FormatCash(double n)
        {
            return string.Format(System.Globalization.CultureInfo.GetCultureInfo("en-US"), "{0:C}", n);
        }

        static void WriteDouble(double n)
        {
            Out.WriteLine(FormatCash(n));
        }

        static int ReadValue()
        {
            try
            {
                return int.Parse(In.ReadLine());
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
                return double.Parse(In.ReadLine());
            }
            catch
            {
                return 0;
            }
        }
        #endregion


        static class Texts{ 
            public static string help = 
@"ASSET CHARACTERISTICS explained
example: X1|Proprietate X1-{0}|90000|17500|30|0.028|0.0425|0.0725
    Type: X1, Name = Proprietate X1-1, 
    Property cost = 90000, Down payment = 17500
    Loan term(years) = 30, Appreciation/year = 0.028*100 (2.8%)
    Loan interest rate = 0.0425 (4.25%), Rent = 0.0725 (7.25%)
------------------------

MANAGING COMMANDS explained

' => buy 1 asset (of set type, or default)
; => buy assets with all available money($50000 => 3 x $15000)
c => buy(customize) new type of asset
t => set type of asset(for future buying)
t? => view available asset types(for example X1, X2, ...)
sell => sell asset by name(input in new line)
        you will pay the principal debt
        interest will be set to 0
sell range => sell assets in given range(input in new line)
        for example: for Property X0-50, ..., Property X0-80
        input should be: 50 [Enter] 80
pay => pay a sum to reduce principal debt(input in new line)
       (there will be a reduction in interest
       debt as a result of this)
pay full => pay down all the principal debt on the property
is => increase yearly spending amount by x (input in new line)
        (use negative amount to decrease spending)
assets => list of assets owned
assets verbose => list of assets and some details
asset1[NAME] => details about an asset owned by you
assc => number of assets owned
cash => shows available cash
cout => turn transaction log ON/OFF(default is ON)
        just write cout
timer => set timer ON/OFF
odo-reset => reset asset counter to 1. 
    use only after selling everything
hardmoney => cheat code.add/remove money
showhardmoney => hardmoney transactions log
hardmoney handle => pays back all money that
           was taken using the cheat code
           (if you have already paid a portion,
           it will only pay the remaining amount)
clear => clear console screen
gc.collect() => run memory garbage collection
    (internal functions)";
                    }


        static class ExpCat
        {
            public static double RentExp = 1200 * 12;
            public static double FoodExp = 2775 * 12;
            public static double CarExp = 7500;
            public static double GasExp = 1650;
            public static double ClothesExp = 3000;
            public static double Wine = 15 * 200;
            public static double FunExp = 88 * 52;
            public static double TravelExp = 0;
            public static double OtherExp = 0;
        }
    }
}
