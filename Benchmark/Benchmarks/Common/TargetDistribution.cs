using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Common
{
    public class TargetDistribution
    {

        public enum Kind
        {
            Uniform, // pick uniform random
            Fixed,   // pick grain = robot
            FixedPlusOne,   // pick grain = robot+1
            FixedRandomRange, // pick grain = robot plus full random offset
            CycleTwo,   // pick round robin {robot, robot+1, ... robot+4}
            CycleFive,   // pick round robin {robot, robot+1, ... robot+4}
            CycleTen,   // pick round robin {robot, robot+1, ... robot+9}
            CycleThirty,   // pick round robin {robot, robot+1, ... robot+29}
            Biased, // some grains get more traffic
            FullRandom, // pick random grain from full nonegative integer range
            FullRandomSame, // same as full random, with robot(3:0)=target(3:0)
            FullRandomOther, // same as full random, with robot(3:0)!=target(3:0)
        }

        public TargetDistribution(Kind kind, int robot, int numrobots, int range, int seed)
        {
            this.kind = kind;
            this.range = range;
            this.robot = robot;

            switch (kind)
            {
                case Kind.FixedRandomRange:
                    pick = Math.Abs(seed / 4) * 2;
                    break;

                case Kind.Uniform:
                case Kind.Biased:
                case Kind.FullRandom:
                case Kind.FullRandomSame:
                case Kind.FullRandomOther:
                    this.random = new Random(seed ^ robot);
                    break;

                default:
                    break;
            }
        }

        

        private Random random;
        private Kind kind;
        private int robot;
        private int pick;
        private int range;

        private long NextLong()
        {
            return (((long)random.Next()) << 32) ^ random.Next();
        }

        public long Next()
        {
            switch (kind)
            {
                case Kind.Uniform:
                    return random.Next(range);

                case Kind.Biased:
                    var x = random.Next(range);
                    while (x > 0 && random.Next(2) == 1)
                        x = x / 2;
                    return x;

                case Kind.Fixed:
                    return robot % range;
                case Kind.FixedPlusOne:
                    return (robot + 1) % range;
                case Kind.FixedRandomRange:
                    return pick + (robot % range);

                case Kind.CycleTwo:
                    return (robot + (pick++ % 2)) % range;
                case Kind.CycleFive:
                    return (robot + (pick++ % 5)) % range;
                case Kind.CycleTen:
                    return (robot + (pick++ % 10)) % range;
                case Kind.CycleThirty:
                    return (robot + (pick++ % 30)) % range;

                case Kind.FullRandom:
                    return NextLong();
                case Kind.FullRandomSame:
                    return (NextLong() & ((-1L) << 3)) | ((uint)robot & 7);
                case Kind.FullRandomOther:
                    return (NextLong() & ((-1L) << 3)) | ((((uint)robot & 7) ^ 1));


                default: throw new Exception("unhandled case");
            }
        }
    }
}
