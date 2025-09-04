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

using System;
#if CTRADER
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
#else
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
#endif
using RobotLib;
using TdsCommons;
using System.Xml.Serialization;

#if CTRADER
namespace cAlgo.API
#else
namespace NinjaTrader.NinjaScript.Strategies
#endif
{
    public class UltronInstance
    {
        #region Instace Parameters
        public TradeDirections BotDirection { get; set; }
        public int BotNumber { get; set; }
        public ProfitModes ProfitMode { get; set; }
        public double ProfitModeValue { get; set; }
        public string SymbolCsvAllVisual { get; set; }
        public int BarsMinutes { get; set; }
        public int NormNyHourStart { get; set; }
        public int NormNyHourEnd { get; set; }
        public int Period1 { get; set; }
        public int Period2 { get; set; }
        public int Period3 { get; set; }
        public int Period4 { get; set; }
        public double Ma3Ma4DiffMaxPercent { get; set; }
        public double Ma1Ma2MinPercent { get; set; }
        public double Ma1Ma2MaxPercent { get; set; }
        public int TakeProfitPips { get; set; }
        public int StopLossPips { get; set; }
        public string QrSymbolName { get; set; }
        public double Open2 { get; private set; }
        public double Close1 { get; private set; }
        public double Close2 { get; private set; }
        public double Ma1ma2 { get; private set; }
        public double Ma2ma1 { get; private set; }
        #endregion

        #region Members
        [XmlIgnore] public TradeDirections TradeDirection { get; set; }
        [XmlIgnore] public int OpenDurationCount;
        [XmlIgnore] public TimeSpan MinOpenDuration = new TimeSpan(long.MaxValue);
        [XmlIgnore] public TimeSpan AvgOpenDurationSum = new TimeSpan(0);
        [XmlIgnore] public TimeSpan MaxOpenDuration = new TimeSpan(0);
        [XmlIgnore] public int BotCurrentNumber;
        [XmlIgnore] public int BotMaxNumber;
        [XmlIgnore] public Symbol BotSymbol;
        [XmlIgnore] public double InitialVolume;
        [XmlIgnore] public bool IsOpened;
        [XmlIgnore] public double LastProfit;
        [XmlIgnore] public bool IsLong;
        [XmlIgnore] public bool IsNewBotBar;
        [XmlIgnore] public int TpPoints;    // needed for comment
        [XmlIgnore] public int SlPoints;
        [XmlIgnore] public double TrademanagementTakeProfit;
        [XmlIgnore] public double TrademanagementStopLoss;
        [XmlIgnore] public DateTime NormalizedNyTime;
        [XmlIgnore] public bool IsTradingTime;
        [XmlIgnore] public double Ma1Value;
        [XmlIgnore] public double Ma2Value;
        [XmlIgnore] public double Ma3Value;
        [XmlIgnore] public double Ma4Value;
        [XmlIgnore] public double Ma3ma4Diff;

        private int mState;
        private double mMa3ma4DiffMaxVal, mMa1Ma2MaxVal, mMa1Ma2MinVal;
        private const int STATUS_IDLE = 0, STATUS_TRADING = 1;
        private UltronParent mBot;
        private bool mIsLong;
#if CTRADER
        private MovingAverage ma1, ma2, ma3, ma4;
#else
        private WMA ma1, ma2;
        private SMA ma3, ma4;
#endif
        #endregion

        #region OnStart
        public bool OnInstanceConfigure(UltronParent bot)
        {
            mBot = bot;

            // needed for Optimization
            if (NormNyHourStart >= NormNyHourEnd)
                return false;

            mState = STATUS_IDLE;
            mMa3ma4DiffMaxVal = Ma3Ma4DiffMaxPercent / 100;
            mMa1Ma2MinVal = Ma1Ma2MinPercent / 100;
            mMa1Ma2MaxVal = Ma1Ma2MaxPercent / 100;
            mBot.Positions.Closed += OnInstancePositionClosed;
            mIsLong = BotDirection == TradeDirections.Long;

            return true;
        }

        public bool OnInstanceDataLoaded()
        {
#if CTRADER
            ma1 = mBot.Indicators.MovingAverage(mBot.Bars.OpenPrices, Period1, MovingAverageType.Weighted);
            ma2 = mBot.Indicators.MovingAverage(mBot.Bars.ClosePrices, Period2, MovingAverageType.Weighted);
            ma3 = mBot.Indicators.MovingAverage(mBot.Bars.ClosePrices, Period3, MovingAverageType.Simple);
            ma4 = mBot.Indicators.MovingAverage(mBot.Bars.ClosePrices, Period4, MovingAverageType.Simple);
#else
            // Initialize moving averages
            ma1 = mBot.WMA(mBot.Opens[0], Period1); // Weighted MA on Open Prices
            ma2 = mBot.WMA(mBot.Closes[0], Period2); // Weighted MA on Close Prices
            ma3 = mBot.SMA(mBot.Closes[0], Period3); // Simple MA on Close Prices
            ma4 = mBot.SMA(mBot.Closes[0], Period4); // Simple MA on Close Prices
#endif
            return true;
        }
        #endregion

        #region OnTick
        public void OnInstanceTick()
        {
            #region Entry stuff
            NormalizedNyTime = CoFu.TimeUtc2Nyt(mBot.Time, true);
            IsTradingTime = NormNyHourStart <= NormalizedNyTime.Hour && NormalizedNyTime.Hour <= NormNyHourEnd;

            mBot.mRobot.CalcProfitMode2Lots(mBot.Symbol, ProfitMode, ProfitModeValue, 0, 0,
               out double desiredMoney, out double lotSize);
            var volume = mBot.Symbol.NormalizeVolumeInUnits(mBot.Symbol.QuantityToVolumeInUnits(lotSize));
            var targetProfit = mBot.mRobot.CalcTicksAndVolume2Money(mBot.Symbol, TakeProfitPips * 10, volume);
            var targetStopLoss = mBot.mRobot.CalcTicksAndVolume2Money(mBot.Symbol, StopLossPips * 10, volume);
            var openComment = mBot.mRobot.MakeLogComment(mBot.Symbol, mBot.Version);

            Ma1Value = ma1.GetLast(0);
            Ma2Value = ma2.GetLast(0);
            Ma3Value = ma3.GetLast(0);
            Ma4Value = ma4.GetLast(0);

            Ma1ma2 = Ma1Value - Ma2Value;
            Ma2ma1 = Ma2Value - Ma1Value;
            Ma3ma4Diff = Math.Abs(Ma3Value - Ma4Value);

            Close1 = mBot.mRobot.QcBars.BidClosePrices.Last(1);
            Close2 = mBot.mRobot.QcBars.BidClosePrices.Last(2);
            Open2 = mBot.mRobot.QcBars.BidOpenPrices.Last(2);
            #endregion

            #region Close
            if (IsTradingTime)
                if (mBot.Positions.Count >= 1)
                {
                    if (mIsLong && mBot.Positions[0].TradeType == TradeType.Buy
                    || !mIsLong && mBot.Positions[0].TradeType == TradeType.Sell)
                        if (mBot.Positions[0].NetProfit > targetProfit
                                || mBot.Positions[0].NetProfit < -targetStopLoss)
                            mBot.Positions[0].Close();
                }
            #endregion

            #region Open
            if (mBot.mRobot.QcBars.Count > mBot.LastBar)
                if (!mIsLong
                    && STATUS_IDLE == mState
                    && IsTradingTime
                    && Ma3ma4Diff < mMa3ma4DiffMaxVal
                    && Ma3Value > Ma1Value
                    && Ma3Value > Ma2Value
                    && Close1 < Close2
                    && Close2 < Open2
                    && Ma1ma2 < mMa1Ma2MaxVal
                    && Ma1ma2 > mMa1Ma2MinVal)
                {
                    // Close opposite trade before opening
                    if (mBot.Positions.Count >= 1)
                        mBot.Positions[0].Close();

                    //mBeforeOpenMargin = Account.Margin;
                    var result = mBot.ExecuteMarketOrder(TradeType.Sell,
                       mBot.Symbol.Name,
                       volume,
                       "",
                       null, //StopLossPips,
                       null, //TakeProfitPips,
                       openComment,
                       CalculationMode.Price,
                       ProfitCloseModes.TakeProfit);

                    if (!result.IsSuccessful)
                        mBot.Print("Error when placing a sell order");
                    else
                        mState = STATUS_TRADING;
                }

            if (mBot.mRobot.QcBars.Count > mBot.LastBar)
                if (mIsLong
                    && STATUS_IDLE == mState
                    && IsTradingTime
                    && Ma3ma4Diff < mMa3ma4DiffMaxVal
                    && Ma3Value < Ma1Value
                    && Ma3Value < Ma2Value
                    && Close1 > Close2
                    && Close2 > Open2
                    && Ma2ma1 < mMa1Ma2MaxVal
                    && Ma2ma1 > mMa1Ma2MinVal)
                {
                    // Close opposite trade before opening
                    if (mBot.Positions.Count >= 1)
                        mBot.Positions[0].Close();

                    //mBeforeOpenMargin = Account.Margin;
                    var result = mBot.ExecuteMarketOrder(TradeType.Buy,
                       mBot.Symbol.Name,
                       volume,
                       "",
                       null, //StopLossPips,
                       null, //TakeProfitPips,
                       openComment,
                       CalculationMode.Price,
                       ProfitCloseModes.TakeProfit);

                    if (!result.IsSuccessful)
                        mBot.Print("Error when placing a buy order");
                    else
                        mState = STATUS_TRADING;
                }
            #endregion
        }
        #endregion

        #region OnStop
        public void QrOnStop()
        {
        }
        #endregion

        #region Methods
        public void OnInstancePositionClosed(PositionClosedEventArgs args)
        {
            if (args.Position.SymbolName == mBot.Symbol.Name)
                mState = STATUS_IDLE;
        }
        #endregion
    }
}
// end of file
