using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManiaRTRender
{
    public enum Judgement
    {
        MAX = 0,
        J_300 = 1,
        J_200 = 2,
        J_100 = 3,
        J_50 = 4,
        MISS = 5
    }

    public class BaseNote : IComparable<BaseNote>
    {
        public long TimeStamp = 0;
        public int Column = 0;
        public long Duration = 0;
        public long EndTime => TimeStamp + Duration;

        public int CompareTo(BaseNote other)
        {
            return TimeStamp.CompareTo(other.TimeStamp);
        }

        public void Scale(double rate)
        {
            Duration = (long)(Duration / rate);
            TimeStamp = (long)(TimeStamp / rate);
        }


    }

    public class Note : BaseNote
    {
        public Judgement Judgement = Judgement.MISS;
    }

    public class Action : BaseNote
    {
        public Judgement JudgementStart = Judgement.MISS;
        public Judgement JudgementEnd = Judgement.MISS;
        public Note Target;
        public bool IsHolding = false;
    }

    public class ManiaBeatmap
    {
        public int Key = -1;
        public bool IsMania = false;
        public double[] JudgementWindow;
        public IList<Note> Notes;
    }
}
