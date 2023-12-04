using LiveSplit.Model;
using System;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.AxHost;

namespace LiveSplit.UI.Components
{
    public class TimerPauseComponent : LogicComponent, IDeactivatableComponent
    {

        public override string ComponentName => "Timer Pause";

        public bool Activated { get; set; }

        private bool IsSplitting { get; set; }
        private bool IsAutoSplitting { get; set; }
        private int AutoSplitCounter { get; set; }
        private int SplitCounter { get; set; }

        private LiveSplitState State { get; set; }
        private TimerPauseSettings Settings { get; set; }
        private TimerModel Timer { get; set; }
        public TimerPauseComponent(LiveSplitState state)
        {
            Activated = true;

            State = state;
            Settings = new TimerPauseSettings();
            Timer = new TimerModel() { CurrentState = State };

            State.OnStart += State_OnStart;
            State.OnSkipSplit += State_OnSkipSplit;
            State.OnPause += State_OnPause;
            State.OnSplit += State_OnSplit;
            State.OnUndoSplit += State_OnUndoSplit;
        }

        public override void Dispose()
        {
            State.OnStart -= State_OnStart;
            State.OnSkipSplit -= State_OnSkipSplit;
            State.OnPause -= State_OnPause;
            State.OnSplit -= State_OnSplit;
            State.OnUndoSplit -= State_OnUndoSplit;
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return Settings;
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public override void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        private void State_OnStart(object sender, EventArgs e)
        {

            int splitTimes = State.Run.Count;

            SplitCounter = getLastAssignedPB_Split(splitTimes);
            IsSplitting = SplitCounter > 0;
            IsAutoSplitting = State.Run.AutoSplitter.IsActivated;

            if (IsSplitting && !IsAutoSplitting)
            {
                Timer.SkipSplit(); //Skip all existing splits (Trigger OnSkip Event)
            }
        }

        private Time getPersonalBestSplitTime(LiveSplitState state, int split)
        {
            ISegment segment = state.Run[split];
            return segment.PersonalBestSplitTime;
        }

        private int getLastAssignedPB_Split(int splitTimes)
        {
            for (int split = splitTimes - 1; split >= 0; --split)
            {
                //Check which split is the last split with an assigned personal best split time
                //-Set the SplitCounter to the index of this split + 1
                //-Exit the loop
                Time splitTime = getPersonalBestSplitTime(State, split);
                if (splitTime[State.CurrentTimingMethod] != null)
                {
                    //IsSplitting = true;
                    //SplitCounter = split + 1;
                    //break;
                    return split + 1;
                }
            }
            return 0;
        }

        private ISegment getPreviousSplit(LiveSplitState state)
        {
            return state.Run[state.CurrentSplitIndex - 1];
        }

        private ISegment getNextSplit(LiveSplitState state)
        {
            return state.Run[state.CurrentSplitIndex + 1];

        }

        private ISegment getCurrentSplit(LiveSplitState state)
        {
            return state.Run[state.CurrentSplitIndex];

        }

        protected void State_OnPause(object sender, EventArgs e)
        {
            State.Run.Offset = State.TimePausedAt;
        }

        protected void State_OnSkipSplit(object sender, EventArgs e)
        {
            //Skip all existing splits and set their existing Split Times
            if (IsSplitting && !IsAutoSplitting)
            {
                //Set the previous Split time to the previous PB Split Time
                ISegment previousSplit = getPreviousSplit(State);
                previousSplit.SplitTime = previousSplit.PersonalBestSplitTime;

                --SplitCounter;
                if (SplitCounter > 0)
                    Timer.SkipSplit();
                else
                    IsSplitting = false;
            }
        }

        protected void State_OnSplit(object sender, EventArgs e)
        {
            ISegment previousSplit = getPreviousSplit(State);
            if (IsSplitting && IsAutoSplitting)
            {
                //Set the previous Split time to the previous PB Split Time
                previousSplit.SplitTime = previousSplit.PersonalBestSplitTime;
                --SplitCounter;
                ++AutoSplitCounter;

                IsAutoSplitting = (SplitCounter - AutoSplitCounter) > 0;
                if (!IsAutoSplitting)
                {
                    Timer.SkipSplit();
                }
            }
            else
            {
                //Set the previous PB Split Time to the previous Split Time
                previousSplit.PersonalBestSplitTime = previousSplit.SplitTime;
            }
        }

        protected void State_OnUndoSplit(object sender, EventArgs e)
        {
            //Clear the current Split Time
            getCurrentSplit(State).PersonalBestSplitTime = new Time();
        }
    }
}
