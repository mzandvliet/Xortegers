using System;
using System.Collections.Generic;
using System.Linq;

public static class PrimeFactorTests {
    public static void Run() {
        /*
        For a range of numbers, print the
        maximum number of each prime required
        to factor those numbers.
         */

        const int numMax = 128; // ushort.MaxValue

        var primes = SievePrimes(numMax);
       
        var maxFactors = new Dictionary<uint, uint>();

        for (uint num = 0; num < numMax; num++) {
            var factors = FindPrimeFactors(num, primes);

            // Print factorization for this number
            string factorsString = factors
                .Select(pair => $@"({pair.Key} * {pair.Value})")
                .DefaultIfEmpty(num.ToString())
                .Aggregate((a, b) => a + " * " + b);

            Console.WriteLine($@"{num} -> {factorsString}");

            // Record any maximum uses of a single factor in the construction of a number
            foreach (var factor in factors) {
                if (!maxFactors.ContainsKey(factor.Key)) {
                    maxFactors.Add(factor.Key, factor.Value);
                } else {
                    maxFactors[factor.Key] = Math.Max(maxFactors[factor.Key], factor.Value);
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($@"Total primes: {primes.Count}");

        Console.WriteLine();
        Console.WriteLine($@"Prime: Max Factor Count");
        foreach (var factor in maxFactors) {
            Console.WriteLine($@"{factor.Key}:     {factor.Value}");
        }
    }

    public static Dictionary<uint, uint> FindPrimeFactors(uint num, IList<uint> primes) {
        if (num / 2 > primes[primes.Count-1]) {
            throw new ArgumentException("Cannot find prime factorizations for numbers higher than the largest given prime");
        }

        var factors = new Dictionary<uint, uint>();

        while (num > 1) {
            for (int p = 0; p < primes.Count; p++) {
                var prime = primes[p];
                if (num % prime == 0) {
                    if (!factors.ContainsKey(prime)) {
                        factors.Add(prime, 1);
                    } else {
                        factors[prime]++;
                    }
                    
                    num /= prime;
                }

                if (prime > num) {
                    break;
                }
            }
        }
        
        return factors;
    }

    /*
    Naive Sieve of Eratosthenes implementation
     */
    public static IList<uint> SievePrimes(int n) {
        var primes = new List<uint>();

        for (uint i = 2; i < n; i++) {
            primes.Add(i);
        }

        int pIndex = 0;
        while (pIndex < primes.Count) {
            uint p = primes[pIndex];

            for (int i = primes.Count - 1; i > pIndex; i--) {
                if (primes[i] % p == 0) {
                    primes.RemoveAt(i);
                }
                // Todo: could early out once current p > primes.last/2
            }

            pIndex++;
        }
        
        return primes;
    }
}