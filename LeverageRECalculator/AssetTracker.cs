using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeverageRECalculator
{
    class AssetTracker
    {
        private static DateTime Now { get { return Program.Now; } }


        private Asset parent;
        public Dictionary<int, double> YearlyRent;
        public Dictionary<int, double> YearlyInterest;
        public Dictionary<int, double> YearlyPrincipalDeposit;



        public AssetTracker(Asset parent)
        {
            this.YearlyRent = new Dictionary<int, double>();
            this.YearlyInterest = new Dictionary<int, double>();
            this.YearlyPrincipalDeposit = new Dictionary<int, double>();
        }

        public void OnRentReceived(double sum)
        {
            int key = YearsOfAge;
            this.YearlyRent.Add(key, sum);
        }

        public void OnPaymentMade(double interest, double principal)
        {
            int key = YearsOfAge;
            this.YearlyInterest.Add(key, interest);
            this.YearlyPrincipalDeposit.Add(key, principal);
        }

        int YearsOfAge
        {
            get
            {
                return (int)(Now - this.parent.Acquired).TotalDays / 365;
            }
        }

        #region Sums
        public double TotalRentCollected
        {

            get
            {
                return this.YearlyRent.Sum(p => p.Value);
            }
        }
        public double TotalInterestPaid
        {

            get
            {
                return this.YearlyInterest.Sum(p => p.Value);
            }
        }
        public double TotalPrincipalPaid
        {

            get
            {
                return this.YearlyPrincipalDeposit.Sum(p => p.Value);
            }
        }
        public double TotalMortgagePaid
        {
            get
            {
                return this.TotalInterestPaid + this.TotalPrincipalPaid;
            }
        }
        #endregion

        public double CashOnCashTotal
        {
            get
            {
                return (this.TotalRentCollected - this.TotalMortgagePaid) / this.parent.DownPayment;
            }
        }
        public double CashOnCashAverage
        {
            get
            {
                return CashOnCashTotal / (YearsOfAge);
            }
        }

        public double? CashOnCashThisYear
        {
            get
            {
                try
                {
                    return ((this.YearlyRent.LastOrDefault().Value - this.YearlyInterest.LastOrDefault().Value - this.YearlyPrincipalDeposit.LastOrDefault().Value) / this.parent.DownPayment);
                }
                catch (NullReferenceException)
                {
                    return null;
                }
            }
        }

        public double DebtToValue
        {
            get { return this.parent.OutstandingDebt / this.parent.Value; }
        }

        public double LastYearProfitMargin
        {
            get
            {
                int age = YearsOfAge;
                if (age >= 1)
                {
                    double rent = this.YearlyRent[age];
                    double payments = this.YearlyPrincipalDeposit[age] + this.YearlyInterest[age];
                    return 1 - payments / rent;
                }
                else return 0;
            }
        }
    }
}
