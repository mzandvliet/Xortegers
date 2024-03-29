using System;

using word_u32 = System.UInt32;
using word_u16 = System.UInt16;
using word_u8 = System.Byte;

namespace LateralInteger {
    public class Tests {
        /*
        Explanation:

        A different, toy implementation of integer arithmetic.
        

        We still use integer words to store data for now, but
        think of them as generic Boolean vectors.

        Each word stores 1 bit of WordLength integer numbers.
        An array of words stores WordLength numbers, each of
        ArrayLength bits.

        Arithmetic on these numbers can be implemented  at the
        software level by using the algebra of Boole, e.g.
        AND, OR, XOR, etc.

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

        - Bit shifting now means shifting array indices, which
        can still be relatively quick. And again, you shift
        like 32 of them at the same time.

        - Flexible, arbitrary overflow handling

        Since we're implementing fundamental ops like addition
        at user-code level, we get to decide what we do
        with any non-zero bits remaining after operations
        complete. It's like you'd always have overflow bit
        information available.

        - Revercimals: n-adics, p-adics

        Revercimals might work well in this format. Multiplication
        on the lintegers will require classical integer
        multiplication techniques, which at best have something
        like n*log(n) complexity. Revercimals can do better.

        === Modular arithmetic ===

        Supposing you implement bit shifts as increnting and
        decrementing the pointer to the word slice that points
        to the most-significant bit. In a modular sense of
        of course, such that the index is: i % WordLength

        You can take arbitrary sub-ranges of bits, and pawn
        them off to parallel or concurrent processes, provided
        that the operations on these numbers are mathematically
        sound.

        For weird number systems, the order of bits could be
        defined by some kind of function. The order could be
        jumbled in principle, but linearized by reorganizing
        the pointers to physical memory.

        For cache coherency, you need to take special care here.

        If a single adder requires a pass over *all* the bits
        of a number, you cannot trivially parallelize multiple
        operations involving that number.

        With something like p-adic arithmetic, utilizing the
        Chinese Remainder Theorem, you might be able to split
        up general polynomial calculations on these numbers,
        parallelizing calculations happening at multiple scales
        of 2 numbers.

        === Performance Testing ===

        First, regular managed dotnet.

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

        Now a very fun step! Adding 32 Byte-words

        Byte adds ticks: 125263
        LInt adds ticks: 49863

        Kapow!

        --

        Hypothesis: This could vectorize quite well?

        Theoretically, given some SIMD set with 16
        and 8 bit word intrinsics, a vectorizer
        should be able to take your fixed point
        arithmetic and optimize it really well.

        However, given the DotNet type system specifics,
        and perhaps that Burst is in its infancy, it
        rarely ever happens.

        Meanwhile, this LInteger arithmetic is 32-bit
        words all the way. If we parameterize loop
        counts to something that is compile-time
        constant, Burst might be able to vectorize
        some inner loops.

        --

        Int <-> LInt Conversion

        This is a big bottleneck right now, but
        you would want to minimize the need for
        conversion of one to the other anyway.

        */

        /*
        Add 32 32-bit numbers to each other
         */
        public static void AddInt32() {
            var rand = new System.Random(1234);

            // Generate some random integer inputs

            var aInt = new word_u32[32];
            var bInt = new word_u32[32];
            for (uint i = 0; i < aInt.Length; i++) {
                aInt[i] = (uint)rand.Next(0, int.MaxValue >> 2);
                bInt[i] = (uint)rand.Next(0, int.MaxValue >> 2);
            }

            // Convert to LInt format

            var aLInt = new word_u32[32];
            var bLInt = new word_u32[32];
            LUInt32.ToLInt(aInt, aLInt);
            LUInt32.ToLInt(bInt, bLInt);

            // Perform regular integer adds, measure time

            var rInt = new word_u32[32];
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 99999; i++) {
                UInt.Add(aInt, bInt, rInt);
            }
            watch.Stop();
            Console.WriteLine("Integer add ticks: " + watch.ElapsedTicks);

            // Perform linteger adds, measure time

            var rLInt = new word_u32[32];
            watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 99999; i++) {
                LUInt32.Add(aLInt, bLInt, rLInt);
            }
            watch.Stop();
            Console.WriteLine("LInteger<32> add ticks: " + watch.ElapsedTicks);

            // Print linteger addition results as integers

            var rAsInt = new word_u32[32];
            LUInt32.ToInt(rLInt, rAsInt);

            UInt.Print(aInt);
            Console.WriteLine("++++++++++++++++++++++++++++++++");
            UInt.Print(bInt);
            Console.WriteLine("================================");
            UInt.Print(rAsInt);
            Console.WriteLine("===== should be equal to: ======");
            UInt.Print(rInt);
        }

        /*
        Add 32 16-bit numbers to each other
        */
        public static void AddInt16() {
            var rand = new System.Random(1234);

            // Generate some random inputs

            var aInt = new word_u16[32];
            var bInt = new word_u16[32];
            for (uint i = 0; i < aInt.Length; i++) {
                aInt[i] = (ushort)rand.Next(0, ushort.MaxValue >> 2);
                bInt[i] = (ushort)rand.Next(0, ushort.MaxValue >> 2);
            }

            // Convert to LInt format

            var aLInt = new word_u32[16];
            var bLInt = new word_u32[16];
            LUInt32.ToLInt(aInt, aLInt);
            LUInt32.ToLInt(bInt, bLInt);

            // Perform regular integer adds, measure time

            var rInt = new word_u16[32];
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 99999; i++) {
                UInt.Add(aInt, bInt, rInt);
            }
            watch.Stop();
            Console.WriteLine("ushort add ticks: " + watch.ElapsedTicks);

            // Perform linteger adds, measure time

            var rLInt = new word_u32[16];
            watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 99999; i++) {
                LUInt32.Add(aLInt, bLInt, rLInt);
            }
            watch.Stop();
            Console.WriteLine("LInteger<u16> adds ticks: " + watch.ElapsedTicks);

            // Print linteger addition results as integers

            var rAsInt = new word_u16[32];
            LUInt32.ToInt(rLInt, rAsInt);

            UInt.Print(aInt);
            Console.WriteLine("++++++++++++++++++++++++++++++++");
            UInt.Print(bInt);
            Console.WriteLine("================================");
            UInt.Print(rAsInt);
            Console.WriteLine("===== should be equal to: ======");
            UInt.Print(rInt);
        }

        /*
        Add 32 8-bit numbers to each other
        */
        public static void AddInt8() {
            var rand = new System.Random(1234);

            // Generate some random inputs

            var aInt = new word_u8[32];
            var bInt = new word_u8[32];
            for (uint i = 0; i < aInt.Length; i++) {
                aInt[i] = (byte)rand.Next(0, byte.MaxValue >> 2);
                bInt[i] = (byte)rand.Next(0, byte.MaxValue >> 2);
            }

            // Convert to LInt format

            var aLInt = new word_u32[8];
            var bLInt = new word_u32[8];
            LUInt32.ToLInt(aInt, aLInt);
            LUInt32.ToLInt(bInt, bLInt);

            // Perform regular integer adds, measure time

            var rInt = new word_u8[32];
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 99999; i++) {
                UInt.Add(aInt, bInt, rInt);
            }
            watch.Stop();
            Console.WriteLine("Byte add ticks: " + watch.ElapsedTicks);

            // Perform linteger adds, measure time

            var rLInt = new word_u32[32];
            watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 99999; i++) {
                LUInt32.Add(aLInt, bLInt, rLInt);
            }
            watch.Stop();
            Console.WriteLine("LInteger<u8> adds ticks: " + watch.ElapsedTicks);

            // Print linteger addition results as integers

            var rAsInt = new word_u8[32];
            LUInt32.ToInt(rLInt, rAsInt);

            UInt.Print(aInt);
            Console.WriteLine("++++++++++++++++++++++++++++++++");
            UInt.Print(bInt);
            Console.WriteLine("================================");
            UInt.Print(rAsInt);
            Console.WriteLine("===== should be equal to: ======");
            UInt.Print(rInt);
        }
    }

    public static class UInt {
        public static void Add(in word_u32[] a, in word_u32[] b, word_u32[] r) {
            for (int i = 0; i < a.Length; i++) {
                r[i] = a[i] + b[i];
            }
        }

        public static void Add(in word_u16[] a, in word_u16[] b, word_u16[] r) {
            for (int i = 0; i < a.Length; i++) {
                r[i] = (word_u16)(a[i] + b[i]);
            }
        }

        public static void Add(in word_u8[] a, in word_u8[] b, word_u8[] r) {
            for (int i = 0; i < a.Length; i++) {
                r[i] = (word_u8)(a[i] + b[i]);
            }
        }

        public static void Print(in word_u32[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                Console.WriteLine($@"[{i}]: {ints[i]}");
            }
        }

        public static void Print(in word_u16[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                Console.WriteLine($@"[{i}]: {ints[i]}");
            }
        }

        public static void Print(in word_u8[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                Console.WriteLine($@"[{i}]: {ints[i]}");
            }
        }
    }

    public static class LUInt32 {
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
            for (int b = 0; b < lints.Length; b++) {
                lints[b] = 0;
                for (int i = 0; i < ints.Length; i++) {
                    lints[b] |= ((ints[i] >> b) & 0x0000_0001) << i;
                }
            }
        }

        public static void ToLInt(in word_u16[] ints, word_u32[] lints) {
            for (int b = 0; b < lints.Length; b++) {
                lints[b] = 0;
                for (int i = 0; i < ints.Length; i++) {
                    lints[b] |= (((uint)ints[i] >> b) & 0x0000_0001) << i;
                }
            }
        }

        public static void ToLInt(in word_u8[] ints, word_u32[] lints) {
            for (int b = 0; b < lints.Length; b++) {
                lints[b] = 0;
                for (int i = 0; i < ints.Length; i++) {
                    lints[b] |= (((uint)ints[i] >> b) & 0x0000_0001) << i;
                }
            }
        }

        public static void ToInt(in word_u32[] lints, word_u32[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                ints[i] = 0;
                for (int b = 0; b < lints.Length; b++) {
                    ints[i] |= ((lints[b] >> i) & 0x0000_0001) << b;
                }
            }
        }

        public static void ToInt(in word_u32[] lints, word_u16[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                ints[i] = 0;
                for (int b = 0; b < lints.Length; b++) {
                    ints[i] |= (ushort)(((lints[b] >> i) & 0x0000_0001) << b);
                }
            }
        }

        public static void ToInt(in word_u32[] lints, word_u8[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                ints[i] = 0;
                for (int b = 0; b < lints.Length; b++) {
                    ints[i] |= (byte)(((lints[b] >> i) & 0x0000_0001) << b);
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