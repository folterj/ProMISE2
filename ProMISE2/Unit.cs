namespace ProMISE2
{
    public class Unit
    {
        public float pos = 0;
        public float m = 0;
        public PhaseType phase = PhaseType.None;
        public bool incol = true;
        public int zone = 0;

        public Unit()
        {
        }

        public Unit(float m, float pos, PhaseType phase)
        {
            this.pos = pos;
            this.m = m;
            this.phase = phase;
        }

        public Unit(Unit unit)
        {
            pos = unit.pos;
            m = unit.m;
            phase = unit.phase;
            incol = unit.incol;
            zone = unit.zone;
        }

        public static bool operator <(Unit unit1, Unit unit2)
        {
            return (unit1.pos < unit2.pos);
        }

        public static bool operator >(Unit unit1, Unit unit2)
        {
            return (unit1.pos > unit2.pos);
        }

    }
}
