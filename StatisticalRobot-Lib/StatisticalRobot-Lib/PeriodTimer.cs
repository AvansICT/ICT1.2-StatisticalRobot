namespace Avans.StatisticalRobot
{
    public class PeriodTimer(int msPeriod)
    {
        private DateTime _lastTickTime = DateTime.Now;

        private TimeSpan _period = TimeSpan.FromMilliseconds(msPeriod);

        public bool Check()
        {
            var result = false;

            if (DateTime.Now - _lastTickTime > _period)
            {
                result = true;
                _lastTickTime = DateTime.Now;
            }
            return result;
        }

    }
}
