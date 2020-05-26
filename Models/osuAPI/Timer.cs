using System;

namespace Chino_chan.Models.osuAPI
{
    internal class Timer
    {
        #region Event
        public delegate void OnElapsed();
        public event OnElapsed Elapsed;
        #endregion
        #region Public variables
        public DateTime LastStartTime { get; private set; }
        public bool Repeat { get; set; }
        public bool Enabled { get; private set; }
        #endregion
        #region Private variables
        long Interval = 0;
        System.Threading.Timer SelfTimer;
        #endregion
        #region Constructor
        public Timer(long Interval)
        {
            this.Interval = Interval;
            SelfTimer = new System.Threading.Timer((state) =>
            {
                lock (state)
                {
                    LastStartTime = DateTime.Now;
                    Elapsed?.Invoke();
                    if (!Repeat)
                    {
                        Stop();
                    }
                }
            }, new object(), -1, Interval);
        }
        #endregion
        #region Manage timer
        public void Start(bool InstantStart = false)
        {
            Start(InstantStart, false);
        }
        public void Restart(bool InstantStart = false)
        {
            Start(InstantStart, true);
        }

        private void Start(bool InstantStart, bool Restart)
        {
            if (!Restart && Enabled)
                return;

            if (InstantStart)
            {
                SelfTimer.Change(0, Interval);
            }
            else
            {
                SelfTimer.Change(Interval, Interval);
            }
            Enabled = true;
        }

        public void Stop()
        {
            SelfTimer.Change(-1, Interval);
            Enabled = false;
        }
        #endregion
    }
}
