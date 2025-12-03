/* MIT License
Copyright (c) 2025 Quantrosoft Pty. Ltd.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 
*/

#region Usings
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
#endregion

/* Knowledge base
 * In NinjaTrader 8, all public properties in a strategy or indicator automatically 
 * appear in the GUI (Strategy Parameters or Indicator Properties). 
 * If you want to hide a public property or variable from the NinjaTrader UI
 * If you want to hide a property and prevent it from being saved in workspaces, 
 * use [XmlIgnore] along with [Browsable(false)].
 * 
 * NinjaTrader downloads from IBKR can only do minutes and days and does NOT have volumes
 * 
 * NQ trades from 00:00 to 23:00 German time (18:00 - 17:00 New York, 23:00 – 22:00 UTC )
 * 
 */

namespace NinjaTrader.NinjaScript.Strategies
{
    [CategoryOrder("System", 1)]
    public class UltronParent : Strategy
    {
        #region History
        // There must be an space between name and version!
        [XmlIgnore]
        public string Version =
"Nt-Ultron " // Must have a minus sign before the bot name and a space after the name
+ "V0.11";
        // V0.11    13.05.25    HMz Limits in Log
        // V0.10    12.05.25    HMz Packages 1 released
        // V0.05    12.04.25    HMz Minus values in DV and DC, TP, Sl in cash
        // V0.04    10.04.25    HMz Fixed SubLong ==> DiffLong bug in Close, TP, Sl in %
        //                      parameters from Conf Files, Time mit UTC in Log, 
        //                      Parameter in Log, TP & Sl in Ticks in Comment,
        //                      import of M1 files instead of Data Path
        // V0.03    08.04.25    HMz Log filter & Logging working
        // V0.02    07.04.25    HMz TargetProfitConstanceness
        // V0.01    04.04.25    HMz + and - in Optimization Targets
        // V0.0     04.02.25    HMz created
        #endregion

        #region Parameters

        #region System
        [NinjaScriptProperty]
        [Display(Name = "IsLicencse",
                    GroupName = "System",
                    Order = 1,
                    Description = "")]
        public bool IsLicense { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "IsLaunchDebugger",
                    GroupName = "System",
                    Order = 2,
                    Description = "")]
        public bool IsLaunchDebugger { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "LogModes",
                    GroupName = "System",
                    Order = 3,
                    Description = "")]
        public LogModes LogModes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "SymbolCsv_All_Visual",
                    GroupName = "System",
                    Order = 4,
                    Description = "")]
        public string SymbolCsvAllVisual { get; set; } = "vis";

        [NinjaScriptProperty]
        [Display(Name = "Direction",
                   GroupName = "System",
                   Order = 5,
                   Description = "")]
        public TradeDirections TradeDirection { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ConfigPath",
                    GroupName = "System",
                    Order = 6,
                    Description = "")]
        public string ConfigPath { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "ProfitMode",
                    GroupName = "System",
                    Order = 7,
                    Description = "")]
        public ProfitModes ProfitMode { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ProfitModeValue",
                    GroupName = "System",
                    Order = 8,
                    Description = "")]
        public double ProfitModeValue { get; set; }
        #endregion

        #region Algo
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 1)]
        public int NormNyHourStart { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 2)]
        public int NormNyHourEnd { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 3)]
        public int Period1 { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 4)]
        public int Period2 { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 5)]
        public int Period3 { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 6)]
        public int Period4 { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 7)]
        public double Ma3Ma4DiffMaxPercent { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 8)]
        public double Ma1Ma2MinPercent { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 9)]
        public double Ma1Ma2MaxPercent { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 10)]
        public double TakeProfitPips { get; set; }

        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 11)]
        public double StopLossPips { get; set; }
        #endregion

        #region Optimization Targets
        [NinjaScriptProperty]
        [Display(Name = "TargetAccountNetProfit",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetAccountNetProfit { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetMaxBalanceDrawdown",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetMaxBalanceDrawdown { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetMaxEquityDrawdown",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetMaxEquityDrawdown { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetWinningTrades",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetWinningTrades { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetLosingTrades",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetLosingTrades { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetTotalTrades",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetTotalTrades { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetAverageTrades",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetAverageTrades { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetProfitFactor",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetProfitFactor { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetCalmarRatio",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetCalmarRatio { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetTradesPerMonth",
                            GroupName = "13 Optimization Targets",
                            Order = 1,
                            Description = "")]
        public string TargetTradesPerMonth { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetChanceRiskRatio",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetChanceRiskRatio { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetAverageDuration",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetAverageDuration { get; set; } = "";

        [NinjaScriptProperty]
        [Display(Name = "TargetProfitConstanceness",
                    GroupName = "13 Optimization Targets",
                    Order = 1,
                    Description = "")]
        public string TargetProfitConstanceness { get; set; } = "";
        #endregion

        #endregion

        #region Members
        // Native NinjaTrader: Wrapper removed - use RobotStrategy base class functionality
        // [XmlIgnore] public IRobotFactory mRobotFactory;
        // [XmlIgnore] public AbstractRobot mRobot;
        // [XmlIgnore] public ILogger mLogger;
        [XmlIgnore] public int ConfigsCount, SameTimeOpen, SameTimeOpenCount, MaxEquityDrawdownCount;
        [XmlIgnore] public int MaxBalanceDrawdownCount;
        [XmlIgnore] public double StartBalance, MaxEquityDrawdownValue, Calmar, TradesPerMonth, ChanceRiskRatio;
        [XmlIgnore] public double ProfitConstanceness, ProfitFactor, MaxEquity, MaxMargin, MaxBalanceDrawdownValue;
        [XmlIgnore] public double MaxBalance;
        [XmlIgnore] public DateTime SameTimeOpenDateTime, MaxBalanceDrawdownTime, MaxEquityDrawdownTime;
        [XmlIgnore] public TimeSpan AvgDuration, MaxDuration;
        [XmlIgnore] public List<UltronInstance> FilteredOptiBots = new List<UltronInstance>();
        [XmlIgnore] public TimeZoneInfo PlatformTimeZoneInfo = TimeZoneInfo.Utc;
        [XmlIgnore] public bool IsTickServer;
        [XmlIgnore] public int LastBar;


        private List<UltronInstance> mAllSysSymDirBots = new List<UltronInstance>();
        private string[] mAllConfigFiles;
        private DateTime mPrevTime = DateTime.MinValue;
        private Dictionary<string, string> mIcm2Pepper = new()  // ICM ==> Pepperstone symbol convert
        {
            {"STOXX50", "EUSTX50"},
            {"F40", "FRA40"},
            {"DE40", "GER40"},
            {"JP225", "JPN225"},
            {"ES35", "SPA35"},
            {"USTEC", "NAS100"},
            {"TecDE30", "GERTEC30"},
            {"XBRUSD", "SpotBrent"},
            {"XTIUSD", "SpotCrude"},
            {"XNGUSD", "NatGas"}
        };
        #endregion

        #region OnStart
        protected override void OnSetDefaults()
        {
            Name = "Ultron";
        }

        protected override void OnConfigure()
        {
            DoConfigure();
        }

        protected override void OnStart()
        {
            #region Print Version Info
            string comment = "";
            if (IsTickServer)
                comment += "\t" + "Serving Ticks";
            else
            {
                // Build comment string with bot info
                var sBots = ""; // Will be populated from bot instances
                var sTp = ""; // Will be populated from bot instances
                var sSl = ""; // Will be populated from bot instances
                comment += ""
                   + "\t" + $"{FilteredOptiBots.Count.ToString()} Bots: " + sBots
                   + "\t" + "TP:" + sTp
                   + "\t" + "SL:" + sSl;
            }

            // Native NinjaTrader: Chart is always visible
            Print(Version + ": " + comment);
            #endregion

            #region OnTick exit
            // Native NinjaTrader: PostTick removed - handled by base class
            #endregion
        }

        // Called at bars frequency
        protected override void OnBar()
        {
            // LastBar is now a property in Strategy base class
        }
        #endregion

        #region OnStop
        // OnStop() gets called before GetFitness()
        protected override void OnStop()
        {
            if (!IsLicense)
                return;

            //if (RunningMode == RunningMode.RealTime)
            //   TelegramClient.Dispose();   // needed to close config file

            // Call OnStop() of all bot instances and calc open duration
            var infoText = "";
            var minDuration = new TimeSpan(long.MaxValue);
            var avgDurationSum = new TimeSpan(0);
            MaxDuration = new TimeSpan(0);
            int durationCount = 0;
            foreach (var systemBot in FilteredOptiBots)
            {
                if (null == systemBot)
                    continue;

                systemBot.QrOnStop();

                minDuration = systemBot.MinOpenDuration < minDuration ? systemBot.MinOpenDuration : minDuration;
                avgDurationSum += systemBot.AvgOpenDurationSum;
                durationCount += systemBot.OpenDurationCount;
                MaxDuration = systemBot.MaxOpenDuration > MaxDuration
                    ? systemBot.MaxOpenDuration
                    : MaxDuration;
            }

            // calc and print performance numbers
            int mWinningTrades = History.Where(x => x.NetProfit >= 0).Count();
            int mLoosingTrades = History.Where(x => x.NetProfit < 0).Count();
            var NetProfit = History.Sum(x => x.NetProfit);
            // Native NinjaTrader: Use Time for initial time calculation
            var initialTime = StartBalance > 0 ? Time : Time; // TODO: Track initial time properly
            var daysElapsed = (Time - initialTime).TotalDays;
            if (daysElapsed <= 0) daysElapsed = 1; // Prevent division by zero
            var annualProfit = NetProfit / (daysElapsed / 365);
            int TotalTrades = mWinningTrades + mLoosingTrades;
            //var averageProfitPer10k = 0 == TotalTrades ? 0 : NetProfit / TotalTrades * 10000 / StartBalance;
            var annualProfitPercent = 0 == TotalTrades ? 0 : 100.0 * annualProfit / StartBalance;
            var mLossProfit = History.Where(x => x.NetProfit < 0).Sum(x => x.NetProfit);
            ProfitFactor = 0 == mLoosingTrades
                ? 0
                : Math.Abs(History.Where(x => x.NetProfit >= 0).Sum(x => x.NetProfit) / mLossProfit);
            var maxCurrentEquityDdPercent = 100 * MaxEquityDrawdownValue / MaxEquity;
            var maxStartEquityDdPercent = 100 * MaxEquityDrawdownValue / StartBalance;
            Calmar = 0 == MaxEquityDrawdownValue ? 0 : annualProfit / MaxEquityDrawdownValue;
            var winningRatioPercent = 0 == TotalTrades ? 0 : 100 * (double)mWinningTrades / TotalTrades;
            TradesPerMonth = ((double)TotalTrades / (daysElapsed / 365)) / 12;
            ProfitConstanceness = CalculateGoodnessOfFit(History) * 100; // R² = 1 means perfect fit, R² = 0 means no fit
            //var SharpeRatio = SharpeSortino(false, History.Select(trade => trade.NetProfit));
            //var SortinoRatio = SharpeSortino(true, History.Select(trade => trade.NetProfit));
            var AverageEtd = SystemPerformance.AllTrades.TradesPerformance.Currency.AverageEtd;
            var AverageMae = SystemPerformance.AllTrades.TradesPerformance.Currency.AverageMae;
            var AverageMfe = SystemPerformance.AllTrades.TradesPerformance.Currency.AverageMfe;
            var AverageProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.AverageProfit;
            var CumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
            var Drawdown = SystemPerformance.AllTrades.TradesPerformance.Currency.Drawdown;
            var LargestLoser = SystemPerformance.AllTrades.TradesPerformance.Currency.LargestLoser;
            var LargestWinner = SystemPerformance.AllTrades.TradesPerformance.Currency.LargestWinner;
            var ProfitPerMonth = SystemPerformance.AllTrades.TradesPerformance.Currency.ProfitPerMonth;
            var StdDev = SystemPerformance.AllTrades.TradesPerformance.Currency.StdDev;
            var Turnaround = SystemPerformance.AllTrades.TradesPerformance.Currency.Turnaround;
            var Ulcer = SystemPerformance.AllTrades.TradesPerformance.Currency.Ulcer;
            // Native NinjaTrader: Use Currency from Account or Strategy
            var currency = (Account != null ? Account.Denomination : Currency).ToString();
            infoText += DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " | " + Version;
            infoText += "\n# of config files: " + ConfigsCount.ToString();
            infoText += "\nMaxMargin: " + currency + " "
                + ConvertUtils.DoubleToString(MaxMargin, 2);
            infoText += "\nMaxSameTimeOpen: " + SameTimeOpen.ToString()
                + "; @ " + SameTimeOpenDateTime.ToString("dd.MM.yyyy HH:mm:ss")
                + "; Count# " + SameTimeOpenCount.ToString();
            infoText += "\nMax Balance Drawdown Value: " + currency
                + " " + ConvertUtils.DoubleToString(MaxBalanceDrawdownValue, 2)
                + "; @ " + MaxBalanceDrawdownTime.ToString("dd.MM.yyyy HH:mm:ss")
                + "; Count# " + MaxBalanceDrawdownCount.ToString();
            infoText += "\nMax Balance Drawdown%: " + (0 == MaxBalance
                ? "NaN"
                : ConvertUtils.DoubleToString(100 * MaxBalanceDrawdownValue / MaxBalance, 2));
            infoText += "\nMax Equity Drawdown Value: " + currency
                + " " + ConvertUtils.DoubleToString(MaxEquityDrawdownValue, 2)
               + "; @ " + MaxEquityDrawdownTime.ToString("dd.MM.yyyy HH:mm:ss")
               + "; Count# " + MaxEquityDrawdownCount.ToString();
            infoText += "\nMax Current Equity Drawdown %: " + ConvertUtils.DoubleToString(maxCurrentEquityDdPercent, 2);
            infoText += "\nMax Start Equity Drawdown %: " + ConvertUtils.DoubleToString(maxStartEquityDdPercent, 2);
            infoText += "\nNet Profit: " + currency + " " + ConvertUtils.DoubleToString(NetProfit, 2);
            infoText += "\nProfit Factor: " + (0 == mLoosingTrades
                ? "-"
                : ConvertUtils.DoubleToString(ProfitFactor, 2));
            //infoText += "\nSharpe Ratio: " + ConvertUtils.DoubleToString(SharpeRatio, 2);
            //infoText += "\nSortino Ratio: " + ConvertUtils.DoubleToString(SortinoRatio, 2);
            infoText += "\nCalmar Ratio: " + ConvertUtils.DoubleToString(Calmar, 2);
            infoText += "\nProfit Constanceness %: " + ConvertUtils.DoubleToString(ProfitConstanceness, 2);
            infoText += "\nWinning Ratio: " + ConvertUtils.DoubleToString(winningRatioPercent, 2);
            infoText += "\nTrades Per Month: " + ConvertUtils.DoubleToString(TradesPerMonth, 2);
            if (FilteredOptiBots.Count > 0)
                infoText += "\nChance/Risk Ratio: "
                    + ConvertUtils.DoubleToString(ChanceRiskRatio = FilteredOptiBots[0].TrademanagementTakeProfit
                    / FilteredOptiBots[0].TrademanagementStopLoss, 2);
            infoText += "\nAverage Annual Profit Percent: " + ConvertUtils.DoubleToString(annualProfitPercent, 2);

            if (0 != durationCount)
            {
                infoText += "\nMin / Avg / Max Tradeopen Duration (Day.Hour.Min.Sec): "
                   + minDuration.ToString(@"dd\.hh\.mm\.ss") + " / "
                   + (AvgDuration = TimeSpan.FromTicks(avgDurationSum.Ticks / durationCount))
                   .ToString(@"dd\.hh\.mm\.ss")
                   + " / " + MaxDuration.ToString(@"dd\.hh\.mm\.ss");
            }

            // Native NinjaTrader: Logger removed - use Print for now
            Print("\n\n" + infoText + "\n\n\n");
            Print(GetAllParameterValues());
            Print("\n\n");
        }

        protected override double GetFitness(GetFitnessArgs args)
        {
            // Native NinjaTrader: Calculate account equity from SystemPerformance
            var cashValue = Account != null ? Account.Get(AccountItem.CashValue, Currency) : 0;
            var unrealizedPL = Account != null ? Account.Get(AccountItem.UnrealizedProfitLoss, Currency) : 0;
            var accountEquity = cashValue + unrealizedPL;

            if (accountEquity < StartBalance)
                return -1e3;

            if (MaxEquityDrawdownValue > StartBalance)
                return -2e3;

            // Native NinjaTrader: Calculate values from SystemPerformance
            var netProfit = SystemPerformance.AllTrades.TradesPerformance.GrossProfit + SystemPerformance.AllTrades.TradesPerformance.GrossLoss;
            var winningTrades = SystemPerformance.AllTrades.WinningTrades.Count;
            var losingTrades = SystemPerformance.AllTrades.LosingTrades.Count;
            var totalTrades = SystemPerformance.AllTrades.Count;
            var averageTrade = totalTrades > 0 ? netProfit / totalTrades : 0;
            var profitFactor = SystemPerformance.AllTrades.TradesPerformance.ProfitFactor;
            var maxBalanceDrawdown = SystemPerformance.AllTrades.TradesPerformance.Currency.Drawdown;
            var maxEquityDrawdown = MaxEquityDrawdownValue;

            var targetAccountNetProfit = GetFitnessValue(TargetAccountNetProfit, netProfit);
            var targetMaxBalance = GetFitnessValue(TargetMaxBalanceDrawdown, maxBalanceDrawdown);
            var targetMaxEquityDrawdown = GetFitnessValue(TargetMaxEquityDrawdown, maxEquityDrawdown);
            var targetWinningTrades = GetFitnessValue(TargetWinningTrades, winningTrades);
            var targetLosingTrades = GetFitnessValue(TargetLosingTrades, losingTrades);
            var targetTotalTrades = GetFitnessValue(TargetTotalTrades, totalTrades);
            var targetAverageTrades = GetFitnessValue(TargetAverageTrades, averageTrade);
            var targetProfitFactor = GetFitnessValue(TargetProfitFactor, profitFactor);

            var targetCalmarRatio = GetFitnessValue(TargetCalmarRatio, Calmar);
            var targetTradesPerMonth = GetFitnessValue(TargetTradesPerMonth, TradesPerMonth);
            var targetChanceRiskRatio = GetFitnessValue(TargetChanceRiskRatio, ChanceRiskRatio);
            var targetAverageDuration = GetFitnessValue(TargetAverageDuration, AvgDuration.TotalSeconds);
            var targetProfitConstanceness = GetFitnessValue(TargetProfitConstanceness, ProfitConstanceness);

            var retVal = 100    // Max. possible is 100%
                - targetAccountNetProfit
                - targetMaxBalance
                - targetMaxEquityDrawdown
                - targetWinningTrades
                - targetLosingTrades
                - targetTotalTrades
                - targetAverageTrades
                - targetProfitFactor
                - targetCalmarRatio
                - targetTradesPerMonth
                - targetChanceRiskRatio
                - targetAverageDuration
                - targetProfitConstanceness;

            return retVal;
        }
        #endregion

        #region Methods
        // Called from OnExecution
        //private void OnPositionOpened(PositionOpenedEventArgs args)
        //{
        //    foreach (var bot in FilteredOptiBots)
        //        bot.OnInstancePositionOpened(args);
        //}

        // Called from OnExecution
        private void OnPositionClosed(object sender, PositionClosedEventArgs args)
        {
            foreach (var bot in FilteredOptiBots)
                bot.OnInstancePositionClosed(sender, args);
        }

        //private void OnPendingOrderCancelled(PendingOrderCancelledEventArgs args)
        //{
        //    foreach (var bot in FilteredOptiBots)
        //        bot.OnInstancePendingOrderCancelled(args);
        //}

        private void DoConfigure()
        {
            #region OnStart Entry
            IsTickServer = ConfigPath.ToLower() == "tickserver";
            // Native NinjaTrader: mRobotFactory and mRobot removed - functionality integrated directly
            // No initialization needed here
            #endregion

            // Native NinjaTrader: Chart access
            if (ChartControl != null)
                RemoveDrawObjects();

            // Native NinjaTrader: Use TradeDirections enum directly
            if (TradeDirection == TradeDirections.None && "" == ConfigPath)
            {
                Print("ConfigPath" + " must be set");
                Stop();
                Debugger.Break();
            }

            // Native NinjaTrader: Get account balance
            StartBalance = Account != null ? Account.Get(AccountItem.CashValue, Currency) : 0;
            var isOptimization = State == State.Historical; // TODO: Check if this is correct for optimization
            var isUseParameters = TradeDirection != TradeDirections.None;

            var symbolCsv_All_VisualSplit = SymbolCsvAllVisual.Split(',');
            var isAllSymbols = false;

            for (int i = 0; i < symbolCsv_All_VisualSplit.Length; i++)
            {
                symbolCsv_All_VisualSplit[i] = symbolCsv_All_VisualSplit[i].Trim();

                if ("all" == symbolCsv_All_VisualSplit[i].ToLower())
                    isAllSymbols = true;
            }

            // rebuild symbol list with replaced %vis
            SymbolCsvAllVisual = "";
            for (int i = 0; i < symbolCsv_All_VisualSplit.Length; i++)
            {
                var sym = symbolCsv_All_VisualSplit[i];

                if (0 != i)
                    SymbolCsvAllVisual += ',';

                if ("vis" == sym.ToLower())
                {
                    SymbolCsvAllVisual += Instrument.FullName;
                    symbolCsv_All_VisualSplit[i] = Instrument.FullName;
                }
                else
                {
                    if (isUseParameters && isAllSymbols)
                    {
                        Stop();
                        var text = "\"all\" or symbol name only can be used with multiple config files in "
                            + "SymbolCsv_All_Visual. Use vis";
                        Print(text);
                        throw new Exception(text);
                    }

                    SymbolCsvAllVisual += sym;
                }
            }

            if (isUseParameters)
            {
                // do exact 1 sys/sym/IsLong combination from cTraders target
                for (int i = 0; i < symbolCsv_All_VisualSplit.Length; i++)
                {
                    UltronInstance currentBot = new UltronInstance();

                    // copy all bot parameters to bot
                    var properties = currentBot.GetType().GetProperties();
                    foreach (PropertyInfo property in properties)
                        SetProperties(currentBot, property.Name, null);

                    currentBot.SymbolCsvAllVisual = symbolCsv_All_VisualSplit[i];
                    currentBot.TradeDirection = TradeDirection;

                    mAllSysSymDirBots.Add(currentBot);
                    FilteredOptiBots.Add(currentBot);
                }
            }

            if (!isUseParameters)
            {
                if (IsTickServer)
                {
                    throw new Exception("TickServer cannot not run on multi configs. Use Parameters");
                }
                ConfigsCount = InitConfigs();
                var isXml = true;
                var objectName = Version.Split(' ')[0].Split('-')[1];
                for (int i = 0; i < ConfigsCount; i++)
                {
                    string workSymbol;
                    var configText = File.ReadAllText(mAllConfigFiles[i]);
                    var success = GetParameterFromConfigFile(configText,
                        objectName,
                        "SymbolCsvAllVisual",
                        out string value,
                        isXml);

                    if (success && "vis" != value.ToLower())
                        workSymbol = value;
                    else
                    {
                        success = GetParameterFromConfigFile(configText,
                            objectName,
                            "InstrumentOrInstrumentList",
                            out value,
                            isXml);

                        if (success)
                            workSymbol = value;
                        else
                            continue;
                    }

                    if (isAllSymbols || symbolCsv_All_VisualSplit.Contains(workSymbol))
                    {
                        success = GetParameterFromConfigFile(configText,
                            objectName,
                            "TradeDirection",
                            out value,
                            isXml);
                        var dir = (TradeDirections)Enum.Parse(typeof(TradeDirections), value);
                        UltronInstance currentBot = new UltronInstance();

                        // copy all bot parameters to bot
                        var properties = currentBot.GetType().GetProperties();
                        foreach (PropertyInfo property in properties)
                            SetProperties(currentBot, property.Name, configText);

                        // override some parameters of new bot
                        // if the is a -1 in the GUI target, use value from config file
                        if (ProfitModeValue >= 0)
                        {
                            currentBot.ProfitModeValue = ProfitModeValue;
                            currentBot.ProfitMode = ProfitMode;
                        }

                        // SymbolName name conversion
                        currentBot.SymbolCsvAllVisual = workSymbol;
                        // Native NinjaTrader: Broker name check - TODO: Implement if needed
                        // if (Account.BrokerName.ToLower().Contains("pepper"))
                        if (true) // Placeholder - implement broker check if needed
                            currentBot.SymbolCsvAllVisual = mIcm2Pepper.TryGetValue(workSymbol, out string convertedSymbol)
                               ? currentBot.SymbolCsvAllVisual = convertedSymbol
                               : workSymbol;

                        // TradeDirection conversion
                        if ((TradeDirection == dir)
                           || (TradeDirections.None == TradeDirection
                              && (TradeDirections.Long == dir
                                 || TradeDirections.Short == dir
                              )))
                        {
                            currentBot.TradeDirection = dir;
                            mAllSysSymDirBots.Add(currentBot);
                        }
                    }
                }

                foreach (var bot in mAllSysSymDirBots)
                {
                    if (!isOptimization
                    //|| (isOptimization && (!isUseParameters || isInternalSymbols)
                    //   && mSymbolList[(int)OptiSymbol] == bot.SymbolCsvAllVisual
                    // )
                    )
                        FilteredOptiBots.Add(bot);
                }
            }

            for (int i = 0; i < FilteredOptiBots.Count; i++)
            {
                var systemBot = FilteredOptiBots[i];
                systemBot.BotCurrentNumber = i;
                systemBot.BotMaxNumber = FilteredOptiBots.Count;
                var isValid = systemBot.OnInstanceConfigure(this);
                if (!isValid)
                    FilteredOptiBots[i] = null;
            }
        }

        private int InitConfigs()
        {
            var configPath = Environment.ExpandEnvironmentVariables(ConfigPath);
            if (Directory.Exists(configPath))
            {
                mAllConfigFiles = Directory.GetFiles(configPath, "*.xml", SearchOption.TopDirectoryOnly);
                return mAllConfigFiles.Length;
            }
            return 0;
        }

        private bool SetProperties(
            UltronInstance currentBot,
            string propertyName,
            string jsonText)
        {
            var isXml = true;
            var objectName = Version.Split(' ')[0].Split('-')[1];
            var prop = currentBot.GetType().GetProperty(propertyName);

            string value = null;
            if (null != prop && prop.CanWrite)
            {
                if (null != jsonText)
                {
                    // skip properties which are not in config file like SymbolCsvAllVisual
                    var success = GetParameterFromConfigFile(jsonText,
                        objectName,
                        propertyName,
                        out value,
                        isXml);

                    if (!success)
                        return true;
                }
                else
                {
                    PropertyInfo thisProp = null;
                    try
                    {
                        thisProp = GetType().GetProperty(propertyName);
                    }
                    catch
                    {
                        Debugger.Break();
                    }
                    if (null == thisProp)
                        return true;

                    var propValue = thisProp.GetValue(this);
                    var configParts = new string[] { propertyName, propValue.ToString().Replace(',', '.') };
                    value = configParts[1];
                }

                var valueAsTrimmedString = value.Trim();

                if (prop.PropertyType == typeof(string))
                    prop.SetValue(currentBot, valueAsTrimmedString, null);
                else if (prop.PropertyType == typeof(bool))
                    prop.SetValue(currentBot, bool.Parse(valueAsTrimmedString), null);
                else if (prop.PropertyType == typeof(double))
                    prop.SetValue(currentBot, double.Parse(valueAsTrimmedString, System.Globalization.CultureInfo.GetCultureInfo("en-US")), null);
                else if (prop.PropertyType == typeof(int))
                    prop.SetValue(currentBot, int.Parse(valueAsTrimmedString), null);
                else if (prop.PropertyType.IsEnum)
                {
                    var propertyValue = Enum.Parse(prop.PropertyType, valueAsTrimmedString, true);
                    prop.SetValue(currentBot, Enum.ToObject(prop.PropertyType, Convert.ToUInt64(propertyValue)), null);
                }
                //else if (property.PropertyType == typeof(mTimeFrame))
                //{
                //    prop.SetValue(currentBot, mTimeFrame.Parse(valueAsTrimmedString), null);
                //}
                else
                {
                    throw new Exception("Unknown parameter type " + prop.PropertyType.ToString());
                    //Stop();
                }
            }
            return false;
        }

        // returns fitness in %; 0 is best, everything > 0 is worse
        public double GetFitnessValue(string target, double value)
        {
            // if target is empty, return max value
            // less is better beacause it will be subtracted from 100
            if ("" == target)
                return 0;

            if (double.IsNaN(value))
                return 3e3;

            var dTarget = double.Parse(Regex.Replace(target, @"[^0-9.]", ""), System.Globalization.CultureInfo.GetCultureInfo("en-US"));

            // more than target gives max result
            if (target.ToLower().Contains('+'))
                if (value > dTarget)
                    value = dTarget;

            // less than target gives max result
            if (target.ToLower().Contains('-'))
                if (value < dTarget)
                    value = dTarget;

            // vaule is in %
            return Math.Abs(dTarget - value) / dTarget * 100;
        }

        // returns R² of HistoricalTrade AccountNetProfits over the time
        // R² = 1 means perfect fit, R² = 0 means no fit
        public double CalculateGoodnessOfFit(HistoryCollection trades)
        {
            var sortedTrades = trades.OrderBy(t => t.ClosingTime).ToList();
            if (sortedTrades.Count < 2)
                return double.NaN;

            // X = time (double), Y = cumulative NetProfit
            var x = sortedTrades.Select(t => t.ClosingTime.ToOADate()).ToArray();
            var y = sortedTrades
                .Select(t => t.NetProfit)
                .Aggregate(new List<double>(), (acc, profit) =>
                {
                    acc.Add((acc.Count > 0 ? acc[acc.Count - 1] : 0) + profit);
                    return acc;
                })
                .ToArray();

            // Get regression parameters: intercept and slope
            // Native NinjaTrader: Simple linear regression implementation
            var (intercept, slope) = SimpleLinearRegression(x, y);

            // Calculate predicted y-values
            var yPredicted = x.Select(xi => slope * xi + intercept);

            // Return R²
            // 1.0      = Perfect fit — all data points lie exactly on the regression line
            // 0.0      = No linear correlation — model explains none of the variation in the data
            // < 0.0    =  Worse than a horizontal line — model fits worse than using the mean
            return CalculateRSquared(y, yPredicted.ToArray());
        }

        public string GetAllParameterValues(UltronInstance bot = null)
        {
            var result = new StringBuilder();
            var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var isParameter = prop.GetCustomAttributes(typeof(NinjaScriptPropertyAttribute), false).Any();
                // Skip if no [Parameter] attribute
                if (!isParameter)
                    continue;

                // Skip if name starts with "Target"
                if (prop.Name.StartsWith("Target"))
                    continue;

                // Get the DisplayAttribute if it exists
                var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                string displayName = displayAttr?.Name ?? prop.Name; // fallback to raw name
                displayName = displayName.Replace(",", ";");
                string formattedValue = "";

                if (null == bot)
                    foreach (var loopBot in FilteredOptiBots)
                    {
                        if (GetOneParameterValues(loopBot, prop, ref formattedValue))
                            break;
                    }
                else
                    GetOneParameterValues(bot, prop, ref formattedValue);

                result.AppendLine($"{displayName}{formattedValue}");
            }

            return result.ToString();
        }

        private bool GetOneParameterValues(UltronInstance bot, PropertyInfo prop, ref string formattedValue)
        {
            var type = bot.GetType();
            var instanceProp = type.GetProperty(prop.Name,
                BindingFlags.Public | BindingFlags.Instance);

            object value = null;
            if (null == instanceProp)
                value = prop.GetValue(this);
            else
                value = instanceProp?.GetValue(bot);

            if (value is double doubleVal)
                formattedValue += "," + doubleVal.ToString("F2", CultureInfo.InvariantCulture);
            else
                formattedValue += "," + value?.ToString() ?? "";

            return null == instanceProp;
        }

        // Native NinjaTrader: Simple linear regression implementation
        private (double intercept, double slope) SimpleLinearRegression(double[] x, double[] y)
        {
            if (x.Length != y.Length || x.Length == 0)
                return (0, 0);

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            int n = x.Length;

            for (int i = 0; i < n; i++)
            {
                sumX += x[i];
                sumY += y[i];
                sumXY += x[i] * y[i];
                sumX2 += x[i] * x[i];
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            return (intercept, slope);
        }

        // Native NinjaTrader: R-squared calculation
        private double CalculateRSquared(double[] y, double[] yPredicted)
        {
            if (y.Length != yPredicted.Length || y.Length == 0)
                return 0;

            double meanY = y.Average();
            double ssRes = 0, ssTot = 0;

            for (int i = 0; i < y.Length; i++)
            {
                ssRes += Math.Pow(y[i] - yPredicted[i], 2);
                ssTot += Math.Pow(y[i] - meanY, 2);
            }

            if (ssTot == 0)
                return 0;

            return 1 - (ssRes / ssTot);
        }

        // Native NinjaTrader: Get parameter from XML config file
        private bool GetParameterFromConfigFile(string configText, string objectName, string parameterName, out string value, bool isXml)
        {
            value = "";
            try
            {
                if (isXml)
                {
                    // Simple XML parsing - look for <Parameter Name="parameterName">value</Parameter>
                    var pattern = $@"<Parameter\s+Name=""{parameterName}""[^>]*>([^<]+)</Parameter>";
                    var match = Regex.Match(configText, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        value = match.Groups[1].Value.Trim();
                        return true;
                    }
                }
            }
            catch
            {
                // Return false on error
            }
            return false;
        }
        #endregion
    }
}
