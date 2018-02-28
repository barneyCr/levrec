using System;
namespace LeverageRECalculator
{
    public class Asset : ICloneable
    {
        public static int ODO = 0;

        public string Name;
        public double Cost;
        public double DownPayment;
        public double ReturnPerYear;
        public double AppreciationPerYear;
        public double Interest;
        public double PrincipalDebt, InterestDebt;
        public double Equity;
        public int LeasePeriod, LeasePassed;
        internal AssetTracker Tracker;
        DateTime _acq;
        public DateTime Acquired
        {
            get
            {
                return _acq;
            }
            set
            {
                _acq = value;
            }
        }


        public Asset(string name, double cost, double down, int years, double appreciation, double interest, double rpy)
        {
            this.Name = name;
            this.Cost = cost;
            this.DownPayment = down;
            this.LeasePeriod = years;
            this.AppreciationPerYear = appreciation;
            this.Interest = interest;
            this.ReturnPerYear = rpy;
            this.Tracker = new AssetTracker(this);
        }

        Asset()
        {

        }

        private TimeSpan timePassedSinceAcquisition
        {
            get
            {
                return Program.Now - this.Acquired;
            }
        }

        public double Value
        {
            get
            {
                return (Cost) * (Math.Pow(1 + AppreciationPerYear, timePassedSinceAcquisition.TotalDays / 365));
            }
        }

        public double PeriodicPayment(int payments)
        {
            if (payments == 0 || LeasePeriod == 0)
                return 0;
            double p = Cost - DownPayment;

            double j = Interest / (payments / LeasePeriod);
            double toThePower = Math.Pow((1 + j), -payments);

            double numitor = 1 - toThePower;
            double quotient = j / numitor;

            return p * quotient;
        }

        public double InitialTotalDebt
        {
            get
            {
                return LeasePeriod * PeriodicPayment(LeasePeriod);
            }
        }

        public double OutstandingDebt
        {
            get
            {
                return PrincipalDebt + InterestDebt;
            }
        }

        public double NetValue
        {
            get
            {
                return Value - OutstandingDebt;
            }
        }

        public object Clone()
        {
            Asset asset = new Asset();
            asset.Name = this.Name;
            asset.Cost = this.Cost;
            asset.DownPayment = this.DownPayment;
            asset.ReturnPerYear = this.ReturnPerYear;
            asset.LeasePeriod = this.LeasePeriod;
            asset.LeasePassed = 0;
            asset.AppreciationPerYear = this.AppreciationPerYear;
            asset.Interest = this.Interest;
            asset.InterestDebt = 0;
            asset.Equity = 0;
            asset.Tracker = new AssetTracker(asset);

            return asset;
        }
    }

}
