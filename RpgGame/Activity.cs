using System.Numerics;

namespace Gra
{
    public enum ActivityType
    {
        WAIT,
        WALK
    }

    public class Activity
    {
        public Vector3 v3;
        public string s;
        public long i;
        public long i2;
        public bool b;

        public ActivityType Type;
    }
}
