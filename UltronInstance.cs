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
using System.Xml.Serialization;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class UltronInstance
    {
        #region Instace Parameters
        // Native NinjaTrader: Use TradeDirections from RobotStrategy
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
        [XmlIgnore] public NinjaTrader.Cbi.Instrument BotSymbol;
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
        private Strategy mBot;
        private bool mIsLong;
private WMA ma1, ma2;
        private SMA ma3, ma4;
#endregion

        #region OnStart
        public bool OnInstanceConfigure(Strategy bot)
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
// Initialize moving averages
            ma1 = mBot.WMA(mBot.Opens[0], Period1); // Weighted MA on Open Prices
            ma2 = mBot.WMA(mBot.Closes[0], Period2); // Weighted MA on Close Prices
            ma3 = mBot.SMA(mBot.Closes[0], Period3); // Simple MA on Close Prices
            ma4 = mBot.SMA(mBot.Closes[0], Period4); // Simple MA on Close Prices
return true;
        }
        #endregion

        #region OnTick
        public void OnInstanceTick()
        {
            #region Entry stuff
            // Native NinjaTrader: Convert to New York time
            var nyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            NormalizedNyTime = TimeZoneInfo.ConvertTimeFromUtc(mBot.Time.ToUniversalTime(), nyTimeZone);
            IsTradingTime = NormNyHourStart <= NormalizedNyTime.Hour && NormalizedNyTime.Hour <= NormNyHourEnd;

            // Native NinjaTrader: Use RobotStrategy helper methods
            mBot.CalcProfitMode2Lots(mBot.Instrument, ProfitMode, ProfitModeValue, 0, 0,
               out double desiredMoney, out double lotSize);
            // Native NinjaTrader: Use Instrument.MasterInstrument for volume calculations
            var volume = mBot.Instrument.MasterInstrument.RoundToTickSize(lotSize * mBot.Instrument.MasterInstrument.PointValue);
            var targetProfit = mBot.CalcTicksAndVolume2Money(mBot.Instrument, TakeProfitPips * 10, volume);
            var targetStopLoss = mBot.CalcTicksAndVolume2Money(mBot.Instrument, StopLossPips * 10, volume);
            var openComment = mBot.Version; // Native NinjaTrader: Simplified comment

            // Native NinjaTrader: Use indicator values directly
            Ma1Value = ma1[0];
            Ma2Value = ma2[0];
            Ma3Value = ma3[0];
            Ma4Value = ma4[0];

            Ma1ma2 = Ma1Value - Ma2Value;
            Ma2ma1 = Ma2Value - Ma1Value;
            Ma3ma4Diff = Math.Abs(Ma3Value - Ma4Value);

            // Native NinjaTrader: Use BarsArray[0] directly
            Close1 = mBot.BarsArray[0].GetClose(1);
            Close2 = mBot.BarsArray[0].GetClose(2);
            Open2 = mBot.BarsArray[0].GetOpen(2);
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
            // Native NinjaTrader: Use BarsArray[0].Count instead of mRobot.QcBars
            if (mBot.BarsArray[0].Count > mBot.LastBar)
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
                       mBot.Instrument.FullName,
                       volume,
                       "",
                       null, //StopLossPips,
                       null, //TakeProfitPips,
                       openComment,
                       CalculationMode.Price,
                       ProfitCloseModes.TakeProfit);

                    if (!(result.IsSuccessful ?? false))
                        mBot.Print("Error when placing a sell order");
                    else
                        mState = STATUS_TRADING;
                }

            // Native NinjaTrader: Use BarsArray[0].Count instead of mRobot.QcBars
            if (mBot.BarsArray[0].Count > mBot.LastBar)
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
                       mBot.Instrument.FullName,
                       volume,
                       "",
                       null, //StopLossPips,
                       null, //TakeProfitPips,
                       openComment,
                       CalculationMode.Price,
                       ProfitCloseModes.TakeProfit);

                    if (!(result.IsSuccessful ?? false))
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
        public void OnInstancePositionClosed(object sender, PositionClosedEventArgs args)
        {
            if (args.Position.SymbolName == mBot.Instrument.FullName)
                mState = STATUS_IDLE;
        }
        #endregion
    }
}
// end of file
