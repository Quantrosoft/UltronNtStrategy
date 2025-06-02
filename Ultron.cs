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
#define CKECK_MZx
#define USE_R2Sx

#if USE_R2S
using R2sIndis;
using R2sIndis.NT8;
using R2sIndis.NT8.Algo;
using R2sIndis.NT8.Algo.Indicators;
#endif
#if CKECK_MZ
using MZpack;
using MZpack.NT8;
using MZpack.NT8.Algo;
using MZpack.NT8.Algo.Indicators;
#endif
using cAlgo.API;
#if !CTRADER
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System.ComponentModel.DataAnnotations;
using cAlgo.Robots;
#endif
using RobotLib;
using RobotLib.Cs;
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

using TdsCommons;
using static TdsCommons.CoFu;
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

#if CTRADER
namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
#else
namespace NinjaTrader.NinjaScript.Strategies
{
    [CategoryOrder("System", 1)]
#endif
    public class UltronParent : Robot
    {
        #region History
        // There must be an space between name and version!
        [XmlIgnore]
        public string Version =
#if CTRADER
            "Ct-Ultron "
#else
            "Nt-Ultron " // Must have a minus sign before the bot name and a space after the name
#endif
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
#if CTRADER
        [Parameter("IsLicencse", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(Name = "IsLicencse",
            GroupName = "System",
            Order = 1,
            Description = "")]
#endif
        public bool IsLicense { get; set; }

#if CTRADER
        [Parameter("IsLaunchDebugger", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(Name = "IsLaunchDebugger",
            GroupName = "System",
            Order = 2,
            Description = "")]
#endif
        public bool IsLaunchDebugger { get; set; }

#if CTRADER
        [Parameter("LogModes", Group = "System", DefaultValue = LogModes.Off)]
#else
        [NinjaScriptProperty]
        [Display(Name = "LogModes",
            GroupName = "System",
            Order = 3,
            Description = "")]
#endif
        public LogModes LogModes { get; set; }

#if CTRADER
        [Parameter("SymbolCsv_All_Visual", Group = "System", DefaultValue = "vis")]
#else
        [NinjaScriptProperty]
        [Display(Name = "SymbolCsv_All_Visual",
            GroupName = "System",
            Order = 4,
            Description = "")]
#endif
        public string SymbolCsvAllVisual { get; set; } = "vis";

#if CTRADER
        [Parameter("Direction", Group = "System", DefaultValue = TradeDirections.Neither)]
#else
        [NinjaScriptProperty]
        [Display(Name = "Direction",
           GroupName = "System",
           Order = 5,
           Description = "")]
#endif
        public TradeDirections TradeDirection { get; set; }

#if CTRADER
        [Parameter("ConfigPath", Group = "System", DefaultValue = "")]
#else
        [NinjaScriptProperty]
        [Display(Name = "ConfigPath",
            GroupName = "System",
            Order = 6,
            Description = "")]
#endif
        public string ConfigPath { get; set; } = "";

#if CTRADER
        [Parameter("ProfitMode", Group = "System", DefaultValue = ProfitMode.Lots)]
#else
        [NinjaScriptProperty]
        [Display(Name = "ProfitMode",
            GroupName = "System",
            Order = 7,
            Description = "")]
#endif
        public ProfitMode ProfitMode { get; set; }

#if CTRADER
        [Parameter("ProfitModeValue", Group = "System", DefaultValue = 20)]
#else
        [NinjaScriptProperty]
        [Display(Name = "ProfitModeValue",
            GroupName = "System",
            Order = 8,
            Description = "")]
#endif
        public double ProfitModeValue { get; set; }
        #endregion

        #region Algo
#if CTRADER
        [Parameter("NormNyHourStart", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 1)]
#endif
        public int NormNyHourStart { get; set; }

#if CTRADER
        [Parameter("NormNyHourEnd", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 2)]
#endif
        public int NormNyHourEnd { get; set; }

#if CTRADER
        [Parameter("Period1", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 3)]
#endif
        public int Period1 { get; set; }

#if CTRADER
        [Parameter("Period2", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 4)]
#endif
        public int Period2 { get; set; }

#if CTRADER
        [Parameter("Period3", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 5)]
#endif
        public int Period3 { get; set; }

#if CTRADER
        [Parameter("Period4", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 6)]
#endif
        public int Period4 { get; set; }

#if CTRADER
        [Parameter("Ma3Ma4DiffMaxPercent", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 7)]
#endif
        public double Ma3Ma4DiffMaxPercent { get; set; }

#if CTRADER
        [Parameter("Ma1Ma2MinPercent", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 8)]
#endif
        public double Ma1Ma2MinPercent { get; set; }

#if CTRADER
        [Parameter("Ma1Ma2MaxPercent", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 9)]
#endif
        public double Ma1Ma2MaxPercent { get; set; }

#if CTRADER
        [Parameter("TakeProfitPips", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 10)]
#endif
        public double TakeProfitPips { get; set; }

#if CTRADER
        [Parameter("StopLossPips", Group = "System", DefaultValue = false)]
#else
        [NinjaScriptProperty]
        [Display(GroupName = "Algo", Order = 11)]
#endif
        public double StopLossPips { get; set; }
        #endregion

        #region Optimization Targets
#if CTRADER
        [Parameter("TargetNetProfit", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetNetProfit",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetNetProfit { get; set; } = "";

#if CTRADER
        [Parameter("TargetMaxBalanceDrawdown", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetMaxBalanceDrawdown",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetMaxBalanceDrawdown { get; set; } = "";

#if CTRADER
        [Parameter("TargetMaxEquityDrawdown", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetMaxEquityDrawdown",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetMaxEquityDrawdown { get; set; } = "";

#if CTRADER
        [Parameter("TargetWinningTrades", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetWinningTrades",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetWinningTrades { get; set; } = "";

#if CTRADER
        [Parameter("TargetLosingTrades", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetLosingTrades",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetLosingTrades { get; set; } = "";

#if CTRADER
        [Parameter("TargetTotalTrades", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetTotalTrades",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetTotalTrades { get; set; } = "";

#if CTRADER
        [Parameter("TargetAverageTrades", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetAverageTrades",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetAverageTrades { get; set; } = "";

#if CTRADER
        [Parameter("TargetProfitFactor", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetProfitFactor",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetProfitFactor { get; set; } = "";

#if CTRADER
        [Parameter("TargetCalmarRatio", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetCalmarRatio",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetCalmarRatio { get; set; } = "";

#if CTRADER
        [Parameter("TargetTradesPerMonth", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetTradesPerMonth",
                    GroupName = "11 Optimization Targets",
                    Order = 1,
                    Description = "")]
#endif
        public string TargetTradesPerMonth { get; set; } = "";

#if CTRADER
        [Parameter("TargetChanceRiskRatio", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetChanceRiskRatio",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetChanceRiskRatio { get; set; } = "";

#if CTRADER
        [Parameter("TargetAverageDuration", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetAverageDuration",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetAverageDuration { get; set; } = "";

#if CTRADER
        [Parameter("TargetProfitConstanceness", Group = "Optimization Targets")]
#else
        [NinjaScriptProperty]
        [Display(Name = "TargetProfitConstanceness",
            GroupName = "11 Optimization Targets",
            Order = 1,
            Description = "")]
#endif
        public string TargetProfitConstanceness { get; set; } = "";
        #endregion

        #endregion

        #region Members
        [XmlIgnore] public IRobotFactory mRobotFactory;
        [XmlIgnore] public IRobot mRobot;
        [XmlIgnore] public ILogger mLogger;
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
#if CKECK_MZ || USE_R2S
       private IVolumeProfile mCurrent;
        private StrategyVolumeDeltaIndicator mVolumeDeltaIndicator;
        private StrategyVolumeProfileIndicator mVolumeProfileIndicator;
#endif
        #endregion

        #region ctor
#if CKECK_MZ || USE_R2S
        public Road2Success()
        {
            // Set OnCreateIndicators delegate
            OnCreateIndicators = new OnCreateIndicatorsDelegate(CreateIndicators);

            // Set OnBarCloseHandler for our on bar close strategy
            //OnBarCloseHandler = new OnTickDelegate(StrategyOnBarCloseHandler);
        }
#endif
        #endregion

        #region OnStart
#if !CTRADER

#if CKECK_MZ || USE_R2S
        public List<TickIndicator> CreateIndicators()
        {
            // Initialize new mzIndicators list
            var r2sIndicators = new List<TickIndicator>();

            mVolumeDeltaIndicator = new StrategyVolumeDeltaIndicator(this, @"Volume Delta")
            {
                Calculate = Calculate.OnEachTick, // Force Calculate
                VolumeDeltaMode = VolumeDeltaMode.Delta, // Delta mode
                DeltaMode = DeltaMode.Cumulative,  // Cumulative Delta
                TradeFilterMin = 0,  // Filters
                TradeFilterMax = -1,
                LevelsEnabled = true,
            };

            // Add indicator to the list
            r2sIndicators.Add(mVolumeDeltaIndicator);
#if truex
            mVolumeProfileIndicator = new StrategyVolumeProfileIndicator(this, @"Volume Profile")
            {
                Calculate = Calculate.OnBarClose, // Force Calculate OnBarClose for Minute accuracy
                ShowProfileType = ProfileType.VP_TPO,  // Volume profile and Time Price Opportunity
                ProfileAccuracy = ProfileAccuracy.Minute,
                ProfileCreation = ProfileCreation.Session, // Bar, Daily, Weekly, ...
                POCMode = LevelMode.Developing,
                VAHVALMode = LevelMode.On,
                VWAPMode = VWAPMode.DynamicStdDev1,
                ProfileWidthPercentage = 20,
                Values1KDivider = false,

                // No stacked profiles
                StackedProfileCreation1 = ProfileCreation.None,
                StackedProfileCreation2 = ProfileCreation.None,
                StackedProfileCreation3 = ProfileCreation.None
            };
            r2sIndicators.Add(mVolumeProfileIndicator);
#endif
            return r2sIndicators;
        }
#endif
        protected override void OnSetDefaults()
        {
            Name = "Ultron";
#if CKECK_MZ || USE_R2S
            // This is required for backtesting in Strategy Analyzer,
            // set false if you don't need it to reduce loading time
            EnableBacktesting = true;
#endif
        }

        protected override void OnConfigure()
        {
            DoConfigure();
        }
#endif
        protected override void OnStart()
        {
#if CTRADER
            if (IsLaunchDebugger)
                Debugger.Launch();

            DoConfigure();
#endif
            Print("\nStarting " + Version + "\n"
                + (IsLicense
                ? "MIT License accepted. WORKING\nSee LICENSE.txt in the delivery"
                : "NOT WORKING ! You must accept the MIT License"));
            if (!IsLicense)
            {
                Stop();
                throw new Exception("NOT WORKING ! You must accept the MIT License");
            }

            for (int i = 0; i < FilteredOptiBots.Count; i++)
            {
                //Print(FilteredOptiBots[i].SymbolCsvAllVisual
                //    + " | " + (IsTickServer ? "Tickserver" : FilteredOptiBots[i].TradeDirection.ToString()));
                FilteredOptiBots[i].OnInstanceDataLoaded();
            }

            #region Init Logging
            if (LogModes.Off != LogModes)
            {
                mLogger = mRobotFactory.CreateLogger(this);

                var mode = LogFlags.SelfMade;
                switch (LogModes)
                {
                    case LogModes.Print:
                    mode |= LogFlags.LogPrint;
                    break;
                    case LogModes.File:
                    mode |= LogFlags.LogFile;
                    break;
                    case LogModes.FileAndPrint:
                    mode |= LogFlags.LogPrint | LogFlags.LogFile;
                    break;
                }

                // self made header and 1 lineSplit
                var header = "sep=,\n"
                   + "Number"
                   + ",NetProfit"
                   + ",Saldo"
                   + ",Symbol"
                   + ",Mode"
                   + ",Lots"
                   //+ ",Volume"
                   + ",Swap"
                   + ",OpenDate"
                   + ",OpenUTC"
                   + ",CloseDate"
                   + ",CloseUTC"
                   + ",Dur. d.h.m.s"
                   + ",OpenPrice"
                   + ",ClosePrice"
                   + ",TradeMargin"

                   + ",DV5,DV4,DV3,DV2,DV1 newest"
                   + ",DC4,DC3,DC2,DC1 newest"
                   + ",Color4,Color3,Color2,Color1 newest"
                ;

                mRobot.OpenLogfile(
                    mLogger,
                    Version.Split(' ')[0],
                    mode,
                    header);
            }
            #endregion

            mRobot.DataLoadedInit();
            //Positions.Opened += OnPositionOpened;
            Positions.Closed += OnPositionClosed;
            //PendingOrders.Cancelled += OnPendingOrderCancelled;
        }
#if !CTRADER
        protected override bool GetDebugLaunch()
        {
            return IsLaunchDebugger;
        }
#endif
        #endregion

        #region OnTick
        // Called at datarate frequency
        protected override void OnTick()
        {
            #region OnTick entry
            if (LogModes.Print == LogModes || LogModes.FileAndPrint == LogModes)
                if (mRobot.PrevTime.Date != Time.Date)
                    Print(Time.ToString("dd.MM.yyyy"));

            mRobot.PreTick();
            #endregion

            #region OnTick
            var sInitialLots = "";
            var sTp = "";
            var sSl = "";
            var sBots = "";

            if (IsLicense && (TradeDirection != TradeDirections.Neither || IsTickServer))
                for (int j = 0; j < FilteredOptiBots.Count; j++)
                {
                    var systemBot = FilteredOptiBots[j];
                    if (null == systemBot)
                        continue;

                    systemBot.OnInstanceTick();

                    sInitialLots = string.Format(UsCulture, "{0:N2}\t",
                       systemBot.BotSymbol.VolumeInUnitsToQuantity(systemBot.InitialVolume));

                    sTp += ("" == sTp ? " " : ", ")
                        + systemBot.TrademanagementTakeProfit.ToString("F2", UsCulture)
                        + " " + Account.Asset.Name + " = "
                        + systemBot.TpPoints / systemBot.InitialVolume + " Ticks";

                    sSl += ("" == sSl ? " " : ", ")
                        + systemBot.TrademanagementStopLoss.ToString("F2", UsCulture)
                        + " " + Account.Asset.Name + " = "
                        + systemBot.SlPoints / systemBot.InitialVolume + " Ticks";

                    sBots += ("" == sBots ? " " : ", ")
                        + systemBot.BotSymbol.Name + " "
                        + systemBot.TradeDirection.ToString();
                }

            Max(ref MaxMargin, Account.Margin);
            if (Max(ref SameTimeOpen, Positions.Count))
            {
                SameTimeOpenDateTime = Time;
                SameTimeOpenCount = History.Count;
            }

            Max(ref MaxBalance, Account.Balance);
            if (Max(ref MaxBalanceDrawdownValue, MaxBalance - Account.Balance))
            {
                MaxBalanceDrawdownTime = Time;
                MaxBalanceDrawdownCount = History.Count;
            }

            Max(ref MaxEquity, Account.Equity);
            if (Max(ref MaxEquityDrawdownValue, MaxEquity - Account.Equity))
            {
                MaxEquityDrawdownTime = Time;
                MaxEquityDrawdownCount = History.Count;
            }

            var comment = ""
                   + mRobot.CommentTab
                   + (IsLicense
                   ? "MIT License is accepted. WORKING" + mRobot.CommentTab + "See LICENSE.txt in the delivery"
                   : "NOT WORKING ! You must accept the MIT License");

            if (IsTickServer)
            {
                comment += ""
#if CTRADER
                   + mRobot.CommentTab + "Receiving Ticks";
#else
                   + mRobot.CommentTab + "Serving Ticks";
#endif
            }
            else
                comment += ""
                   + mRobot.CommentTab + $"{FilteredOptiBots.Count.ToString()} Bots: " + sBots
                   + mRobot.CommentTab + "TP:" + sTp
                   + mRobot.CommentTab + "SL:" + sSl;


            if (mRobot.IsVisible)
            {
                mRobot.PrintComment(Version, comment);
            }
            #endregion

            #region OnTick exit
            mRobot.PostTick();
            #endregion
        }

        // Called at bars frequency
        protected override void OnBar()
        {
            LastBar = Bars.Count;
        }
        #endregion

        #region OnStop
        // OnStop() gets called before GetFitness()
        protected override void OnStop()
        {
            #region OnDeinit entry
            if (!IsLicense)
                return;
            mRobot.PreTick();
            #endregion

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
            int winningTrades = History.Where(x => x.NetProfit >= 0).Count();
            int loosingTrades = History.Where(x => x.NetProfit < 0).Count();
            var netProfit = History.Sum(x => x.NetProfit);
            var annualProfit = netProfit / ((Time - mRobot.InitialTime).TotalDays / 365);
            int totalTrades = winningTrades + loosingTrades;
            //var averageProfitPer10k = 0 == totalTrades ? 0 : netProfit / totalTrades * 10000 / Robot.InitialAccountBalance;
            var annualProfitPercent = 0 == totalTrades ? 0 : 100.0 * annualProfit / mRobot.InitialAccountBalance;
            var lossProfit = History.Where(x => x.NetProfit < 0).Sum(x => x.NetProfit);
            ProfitFactor = 0 == loosingTrades ? 0 : Math.Abs(History.Where(x => x.NetProfit >= 0).Sum(x => x.NetProfit) / lossProfit);
            var maxCurrentEquityDdPercent = 100 * MaxEquityDrawdownValue / MaxEquity;
            var maxStartEquityDdPercent = 100 * MaxEquityDrawdownValue / mRobot.InitialAccountBalance;
            Calmar = 0 == MaxEquityDrawdownValue ? 0 : annualProfit / MaxEquityDrawdownValue;
            var winningRatioPercent = 0 == totalTrades ? 0 : 100 * (double)winningTrades / totalTrades;
            TradesPerMonth = ((double)totalTrades / ((Time - mRobot.InitialTime).TotalDays / 365)) / 12;
            ProfitConstanceness = CalculateGoodnessOfFit(History) * 100; // R² = 1 means perfect fit, R² = 0 means no fit
            //var SharpeRatio = SharpeSortino(false, History.Select(trade => trade.NetProfit));
            //var SortinoRatio = SharpeSortino(true, History.Select(trade => trade.NetProfit));
#if !CTRADER
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
#endif
            infoText += DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " | " + Version;
            infoText += "\n# of config files: " + ConfigsCount.ToString();
            infoText += "\nMaxMargin: " + Account.Asset + " "
                + ConvertUtils.DoubleToString(MaxMargin, 2);
            infoText += "\nMaxSameTimeOpen: " + SameTimeOpen.ToString()
                + "; @ " + SameTimeOpenDateTime.ToString("dd.MM.yyyy HH:mm:ss")
                + "; Count# " + SameTimeOpenCount.ToString();
            infoText += "\nMax Balance Drawdown Value: " + Account.Asset
                + " " + ConvertUtils.DoubleToString(MaxBalanceDrawdownValue, 2)
                + "; @ " + MaxBalanceDrawdownTime.ToString("dd.MM.yyyy HH:mm:ss")
                + "; Count# " + MaxBalanceDrawdownCount.ToString();
            infoText += "\nMax Balance Drawdown%: " + (0 == MaxBalance
                ? "NaN"
                : ConvertUtils.DoubleToString(100 * MaxBalanceDrawdownValue / MaxBalance, 2));
            infoText += "\nMax Equity Drawdown Value: " + Account.Asset
                + " " + ConvertUtils.DoubleToString(MaxEquityDrawdownValue, 2)
               + "; @ " + MaxEquityDrawdownTime.ToString("dd.MM.yyyy HH:mm:ss")
               + "; Count# " + MaxEquityDrawdownCount.ToString();
            infoText += "\nMax Current Equity Drawdown %: " + ConvertUtils.DoubleToString(maxCurrentEquityDdPercent, 2);
            infoText += "\nMax Start Equity Drawdown %: " + ConvertUtils.DoubleToString(maxStartEquityDdPercent, 2);
            infoText += "\nNet Profit: " + Account.Asset + " " + ConvertUtils.DoubleToString(netProfit, 2);
            infoText += "\nProfit Factor: " + (0 == loosingTrades
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

            mRobot.LoggerAddText("\n\n" + infoText + "\n\n\n");
            mRobot.LoggerAddText(GetAllParameterValues());
            mRobot.LoggerAddText("\n\n");
            mRobot.LoggerClose();
        }

        protected override double GetFitness(GetFitnessArgs args)
        {
            if (Account.Equity < StartBalance)
                return -1e3;

            if (MaxEquityDrawdownValue > StartBalance)
                return -2e3;

            var targetNetProfit = GetFitnessValue(TargetNetProfit, args.NetProfit);
            var targetMaxBalance = GetFitnessValue(TargetMaxBalanceDrawdown, args.MaxBalanceDrawdown);
            var targetMaxEquityDrawdown = GetFitnessValue(TargetMaxEquityDrawdown, args.MaxEquityDrawdown);
            var targetWinningTrades = GetFitnessValue(TargetWinningTrades, args.WinningTrades);
            var targetLosingTrades = GetFitnessValue(TargetLosingTrades, args.LosingTrades);
            var targetTotalTrades = GetFitnessValue(TargetTotalTrades, args.TotalTrades);
            var targetAverageTrades = GetFitnessValue(TargetAverageTrades, args.AverageTrade);
            var targetProfitFactor = GetFitnessValue(TargetProfitFactor, args.ProfitFactor);

            var targetCalmarRatio = GetFitnessValue(TargetCalmarRatio, Calmar);
            var targetTradesPerMonth = GetFitnessValue(TargetTradesPerMonth, TradesPerMonth);
            var targetChanceRiskRatio = GetFitnessValue(TargetChanceRiskRatio, ChanceRiskRatio);
            var targetAverageDuration = GetFitnessValue(TargetAverageDuration, AvgDuration.TotalSeconds);
            var targetProfitConstanceness = GetFitnessValue(TargetProfitConstanceness, ProfitConstanceness);

            var retVal = 100    // Max. possible is 100%
                - targetNetProfit
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
        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            foreach (var bot in FilteredOptiBots)
                bot.OnInstancePositionClosed(args);
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
            mRobotFactory = new CSRobotFactory();
            mRobot = mRobotFactory.CreateRobot();
            var error = mRobot.ConfigInit(this);
            if ("" != error)
            {
                Print(error);
                Stop();
                Debugger.Break();
            }
            #endregion

            Chart.RemoveAllObjects();

            if (TradeDirections.FromConfigFiles == TradeDirection
                && "" == ConfigPath)
            {
                Print("ConfigPath" + " must be set");
                Stop();
                Debugger.Break();
            }

            StartBalance = Account.Balance;
            var isOptimization = RunningMode == RunningMode.Optimization;
            var isUseParameters = TradeDirections.FromConfigFiles != TradeDirection;

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
                    SymbolCsvAllVisual += Symbol.Name;
                    symbolCsv_All_VisualSplit[i] = Symbol.Name;
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
#if CTRADER
                var isXml = false;
                var objectName = "Parameters";
#else
                var isXml = true;
                var objectName = Version.Split(' ')[0].Split('-')[1];
#endif
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
#if CTRADER
                           "Chart",
                           "Symbol",
#else
                            objectName,
                           "InstrumentOrInstrumentList",
#endif
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
#if CTRADER
                        var dir = (TradeDirections)Int32.Parse(value);
#else
                        var dir = (TradeDirections)Enum.Parse(typeof(TradeDirections), value);
#endif
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
                        if (Account.BrokerName.ToLower().Contains("pepper"))
                            currentBot.SymbolCsvAllVisual = mIcm2Pepper.TryGetValue(workSymbol, out string convertedSymbol)
                               ? currentBot.SymbolCsvAllVisual = convertedSymbol
                               : workSymbol;

                        // TradeDirection conversion
                        if ((TradeDirection == dir)
                           || (TradeDirections.FromConfigFiles == TradeDirection
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
#if CTRADER
                mAllConfigFiles = Directory.GetFiles(configPath, "*.cbotset", SearchOption.TopDirectoryOnly);
#else
                mAllConfigFiles = Directory.GetFiles(configPath, "*.xml", SearchOption.TopDirectoryOnly);
#endif
                return mAllConfigFiles.Length;
            }
            return 0;
        }

        private bool SetProperties(
            UltronInstance currentBot,
            string propertyName,
            string jsonText)
        {
#if CTRADER
            var isXml = false;
            var objectName = "Parameters";
#else
            var isXml = true;
            var objectName = Version.Split(' ')[0].Split('-')[1];
#endif
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
                    prop.SetValue(currentBot, double.Parse(valueAsTrimmedString, UsCulture), null);
                else if (prop.PropertyType == typeof(int))
                    prop.SetValue(currentBot, int.Parse(valueAsTrimmedString), null);
                else if (prop.PropertyType.IsEnum)
                {
                    var propertyValue = Enum.Parse(prop.PropertyType, valueAsTrimmedString, true);
                    prop.SetValue(currentBot, Enum.ToObject(prop.PropertyType, Convert.ToUInt64(propertyValue)), null);
                }
                //else if (property.PropertyType == typeof(TimeFrame))
                //{
                //    prop.SetValue(currentBot, TimeFrame.Parse(valueAsTrimmedString), null);
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

            var dTarget = double.Parse(Regex.Replace(target, @"[^0-9.]", ""), UsCulture);

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

        // returns R² of HistoricalTrade NetProfits over the time
        // R² = 1 means perfect fit, R² = 0 means no fit
        public double CalculateGoodnessOfFit(History trades)
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
            // Moved from Math.Numerics to CoFu since NinjaTrader cannot work with Math.Numerics :-(
            var (intercept, slope) = Fit(x, y);

            // Calculate predicted y-values
            var yPredicted = x.Select(xi => slope * xi + intercept);

            // Return R²
            // 1.0      = Perfect fit — all data points lie exactly on the regression line
            // 0.0      = No linear correlation — model explains none of the variation in the data
            // < 0.0    =  Worse than a horizontal line — model fits worse than using the mean
            return CoFu.RSquared(y, yPredicted);
        }

        public string GetAllParameterValues(UltronInstance bot = null)
        {
            var result = new StringBuilder();
            var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
#if CTRADER
                var isParameter = prop.GetCustomAttributes(typeof(ParameterAttribute), false).Any();
#else
                var isParameter = prop.GetCustomAttributes(typeof(NinjaScriptPropertyAttribute), false).Any();
#endif
                // Skip if no [Parameter] attribute
                if (!isParameter)
                    continue;

                // Skip if name starts with "Target"
                if (prop.Name.StartsWith("Target"))
                    continue;

                // Get the DisplayAttribute if it exists
#if CTRADER
                var displayAttr = prop.GetCustomAttribute<ParameterAttribute>();
#else
                var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
#endif
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
        #endregion
    }
}
