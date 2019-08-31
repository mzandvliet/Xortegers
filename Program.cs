using System;

using word_u32 = System.UInt32;
using word_u8 = System.Byte;

namespace LateralIntegers
{
    class Program
    {
        /*
        Explanation:

        A different, toy implementation of integer arithmetic.
        

        We still use integer words to store data for now, but
        think of them as generic Boolean vectors.

        Each word stores 1 bit of WordLength integer numbers.
        An array of words stores WordLength numbers, each of
        ArrayLength bits.

        Arithmetic on these numbers can be implemented by using
        the algebra of Boole, using AND, OR, XOR, etc.

        In terms of performance this won't get us any improvements
        over hardware integers or anything, that's not what this
        is about. We're just having fun with numbers, really.

        Possible iffy things:

        - Operations will be slower, I think?

        In general, operations now require at least as many
        clock cycles as there are bits in a number. But conversely,
        you do add WordLength of them at the same time. Any
        operation on a single number is slower than the same
        single number in classic mode. But it is parallelized
        at wordLength level now, so you might still be fast.

        Todo: investigate that recent result proving nlogn limits
        on multiplication algorithm complexity.

        Possible cool things:

        - Arbitrary bit depth for numbers.

        You want a 4-bit integer? A 17-bit version? How about a
        877 bit number? You merely make longer arrays, and
        performance and memory scale ~linearly with that.

        Prime-valued word lengths? Go right ahead.

        - Good performance for non-32-bit number types

        With modern CPUs, to get the most efficient number crunching
        you often go to n-way SIMD optimization. You manage your
        data types such that they fit a given SIMD instruction
        set.

        C#, in its core language, assumes many operations
        happen at the 32-bit word level, and smaller word length
        types are emulated on this 32-bit pathway. (Note: 64-bit
        on modern systems, but same idea.)

        Unity's Burst compiler has limited auto-vectorization
        capabilities when it tries to optimize my fixed-point
        number implementations, too.

        This means that while my fixed point types may make
        fantastic use of bits on a conceptual level, they
        still go through this 32-bit pipeline, which is
        now only pulling 8 bits worth of information through
        the ALUs per word, per instruction. What seems like
        a win in terms of information per bits in memory,
        turns out to be a waste in  terms of information
        per unit of bandwidth.

        - Bit shifting now means shifting array indexes, which
        can still be relatively quick. And again, you shift
        like 32 of them at the same time.

        - Flexible, arbitrary overflow handling

        Since we're implementing fundamental ops like addition
        at user-code level, we get to decide what we do
        with any non-zero bits remaining after operations
        complete. It's like you'd always have overflow bit
        information available.

        - Revercimals

        Revercimals might work well in this format. Multiplication
        on the lintegers will require classical integer
        multiplication techniques, which at best have something
        like n*log(n) complexity. Revercimals can do better.

        - Naming

        Renamed everything from Xorteger to LInteger, meaning:

        Lateral Integer


        === Performance Testing ===

        With only the first 8 integers of A and B set to
        non-zero values, we get a vast difference in perf:

        99999x Integer additions:   33872   ticks
        99999x LInteger additions:  181261  ticks

        That's a ~5.3x decrease in speed.

        But when we fill the inputs up with significant
        non-zero numbers, we get:

        99999x Integer additions:   118922  ticks
        99999x LInteger additions:  181261  ticks

        Which is only a ~1.5x difference in speed.

        1.5x for this naive implementation is better
        than I expected.

        --

        It makes sense that integer arithmetic with lots
        of zeroes, running on cleverly designed hardware,
        would be able to skip lots of work.
        */

        static void Main(string[] args)
        {
            var rand = new System.Random(1234);

            // Generate some random inputs

            var aInt = new word_u32[32];
            for (uint i = 0; i < aInt.Length; i++) {
                aInt[i] = (uint)rand.Next(0, int.MaxValue >> 4);
            }

            var bInt = new word_u32[32];
            for (uint i = 0; i < aInt.Length; i++) {
                bInt[i] = (uint)rand.Next(0, int.MaxValue >> 4);
            }

            // Convert to LInt format

            var aLInt = new word_u32[32];
            var bLInt = new word_u32[32];
            LInt32.ToLInt(aInt, aLInt);
            LInt32.ToLInt(bInt, bLInt);

            // Perform regular integer adds, measure time

            var rInt = new word_u32[32];
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 99999; i++) {
                LInt32.Add(aInt, bInt, rInt);
            }
            watch.Stop();
            Console.WriteLine("Int adds ticks: " + watch.ElapsedTicks);

            // Perform linteger adds, measure time

            var rLInt = new word_u32[32];
            watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 99999; i++) {
                LInt32.Add(aLInt, bLInt, rLInt);
            }
            watch.Stop();
            Console.WriteLine("LInt adds ticks: " + watch.ElapsedTicks);

            // Print linteger addition results as integers

            var rAsInt = new word_u32[32];
            LInt32.ToInt(rLInt, rAsInt);

            PrintIntegers(aInt);
            Console.WriteLine("++++++++++++++++++++++++++++++++");
            PrintIntegers(bInt);
            Console.WriteLine("================================");
            PrintIntegers(rAsInt);
            Console.WriteLine("===== should be equal to: ======");
            PrintIntegers(rInt);
        }


        public static void PrintIntegers(in word_u32[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                Console.WriteLine($@"[{i}]: {ints[i]}");
            }
        }
    }

    public static class Int {
        public static void Add(in word_u32[] a, in word_u32[] b, word_u32[] r) {
            for (int i = 0; i < a.Length; i++) {
                r[i] = a[i] + b[i];
            }
        }
    }

    public static class LInt32 {
        public static word_u32 Add(in word_u32[] a, in word_u32[] b, word_u32[] r) {
            word_u32 carry = 0;
            for (int i = 0; i < a.Length; i++) {
                word_u32 a_plus_b = a[i] ^ b[i];
                r[i] = a_plus_b ^ carry;
                carry = (a[i] & b[i]) ^ (carry & a_plus_b);
            }
            return carry;
        }

        public static void ToLInt(in word_u32[] ints, word_u32[] lints) {
            for (int b = 0; b < 32; b++) {
                lints[b] = 0;
                for (int i = 0; i < ints.Length; i++) {
                    lints[b] |= ((ints[i] >> b) & 0x0000_0001) << i;
                }
            }
        }

        public static void ToInt(in word_u32[] lints, word_u32[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                ints[i] = 0;
                for (int b = 0; b < 32; b++) {
                    ints[i] |= ((lints[b] >> i) & 0x0000_0001) << b;
                }
            }
        }

        public static void Print(in word_u32[] lints) {
            for (int i = 0; i < lints.Length; i++) {
                Console.WriteLine(ToBitString(lints[i]));
            }
        }

        public static string ToBitString(in uint value) {
            string b = System.Convert.ToString(value, 2);
            b = b.PadLeft(8, '0');
            return b;
        }
    }
}
