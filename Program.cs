using System;

using word = System.UInt32;

namespace Xortegers
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

        - Multiplication will be slower, I think?

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

        PRIME-valued word lengths! If there are any algorithms
        that you know would be fast if you could work with
        prime ratios intrinsically, this scheme might be worth
        a short. One area that just came up: spatial indexing
        scheme in which you want to avoid overlapping micro-
        word boundaries for particles moving in a binary field.

        - Good performance for non-32-bit number types

        With modern CPUs, to get the most efficient number crunching
        you often go to n-way SIMD optimization. You manage your
        data types such that they fit a given SIMD instruction
        set.

        However, it appears SIMD primarily deals with 32-bit
        types. C#, too, in its core language, assumes many
        operations happen at the 32-bit word level, and
        smaller word length types are emulated on this 32-bit
        pathway. (Note: 64-bit on modern systems, but same idea.)

        This means that while my fixed point types may make
        fantastic use of bits on a conceptual level, they
        still go through this 32-bit pipeline, which is
        now only pulling 8 bits worth of information through
        the ALUs per word, per instruction. What seems like
        a win in terms of information per bits in memory,
        turns out to be a waste in  terms of information
        per unit of bandwidth.

        Are there 8-bit SIMD intrinsics? Maybe yes, maybe no...

        https://stackoverflow.com/questions/8193601/sse-multiplication-16-x-uint8-t

        Anyway, the xorteger scheme would, in theory, make
        far better use of available bandwidth.

        - Bit shifting now means shifting array indexes, which
        can still be relatively quick. And again, you shift
        like 32 of them at the same time.

        - Flexible, arbitrary overflow handling

        Since we're implementing fundamental ops like addition
        at user-code level, we get to decide what we do
        with any non-zero bits remaining after operations
        complete.

        - Revercimals

        Revercimals might work well in this format. Addition
        on the xortegers will require classical integer
        multiplication techniques, which at best have something
        like n*log(n) complexity. Revercimals can do better.

        - Naming

        Xorteger is funny, but since it is only one of three
        boolean operations used to achieve the result, it is
        kind of disingenuous.

        How about Lateger? Lateral Integer?
        */
        static void Main(string[] args)
        {
            var intsA = new word[] {
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
            };

            var intsB = new word[] {
                3,
                1,
                6,
                7,
                11,
                9,
                123,
                2,
            };

            var xortsA = Int2Xort(intsA);
            var xortsB = Int2Xort(intsB);

            var intsR = new word[32];
            Int_Add(intsA, intsB, intsR);

            var xortsR = new word[32];
            Xort_Add(xortsA, xortsB, xortsR);

            var xortsRInt = new word[32];
            Xort2Int(xortsR, xortsRInt);

            PrintIntegers(intsA);
            Console.WriteLine("++++++++++++++++++++++++++++++++");
            PrintIntegers(intsB);
            Console.WriteLine("================================");
            PrintIntegers(xortsRInt);
            Console.WriteLine("=?=?=?=?=?======================");
            PrintIntegers(intsR);
        }

        public static void Int_Add(in word[] a, in word[] b, word[] r) {
            for (int i = 0; i < a.Length; i++) {
                r[i] = a[i] + b[i];
            }
        }

        public static word Xort_Add(in word[] a, in word[] b, word[] r) {
            word carry = 0;
            for (int i = 0; i < a.Length; i++) {
                word a_plus_b = a[i] ^ b[i];
                r[i] = a_plus_b ^ carry;
                carry = (a[i] & b[i]) ^ (carry & a_plus_b);
            }
            return carry;
        }

        public static word[] Int2Xort(in word[] ints) {
            var xorts = new word[32];

            for (int b = 0; b < 32; b++) {
                xorts[b] = 0;
                for (int i = 0; i < ints.Length; i++) {
                    xorts[b] |= ((ints[i] >> b) & 0x0000_0001) << i;
                }
            }

            return xorts;
        }

        public static void Xort2Int(in word[] xorts, word[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                ints[i] = 0;
                for (int b = 0; b < 32; b++) {
                    ints[i] |= ((xorts[b] >> i) & 0x0000_0001) << b;
                }
            }
        }

        public static void PrintIntegers(in word[] ints) {
            for (int i = 0; i < ints.Length; i++) {
                Console.WriteLine($@"[{i}]: {ints[i]}");
            }
        }

        public static void PrintXortegers(in word[] xorts) {
            for (int i = 0; i < xorts.Length; i++) {
                Console.WriteLine(ToBitString(xorts[i]));
            }
        }

        public static string ToBitString(in uint value) {
            string b = System.Convert.ToString(value, 2);
            b = b.PadLeft(8, '0');
            return b;
        }
    }
}
