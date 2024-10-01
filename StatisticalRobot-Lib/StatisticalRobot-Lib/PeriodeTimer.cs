
namespace Avans.StatisticalRobot;

public class PeriodeTimer
{

    private DateTime lastTickTime;
    private TimeSpan periode;

    public PeriodeTimer(int msPeriode) 
    {
        this.periode = TimeSpan.FromMilliseconds(msPeriode);
        this.lastTickTime = DateTime.Now;
    }

    public bool Check()
    {
        var result = false;

        if(DateTime.Now - lastTickTime > periode) 
        {
            result = true;
            lastTickTime = DateTime.Now;
        }

        return result;
    }
}