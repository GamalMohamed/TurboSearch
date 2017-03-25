using System;
using System.Collections;

namespace TurboSearch
{
    public class BloomFilter<T>
    {
        private readonly HashFunction _getHashSecondary;

        private readonly BitArray _hashBits;

        private readonly int _hashFunctionCount;

        public BloomFilter(int capacity)
            : this(capacity, null)
        {
        }

        public BloomFilter(int capacity, int errorRate)
            : this(capacity, errorRate, null)
        {
        }

        public BloomFilter(int capacity, HashFunction hashFunction)
            : this(capacity, BestErrorRate(capacity), hashFunction)
        {
        }

        public BloomFilter(int capacity, float errorRate, HashFunction hashFunction)
            : this(capacity, errorRate, hashFunction, BestM(capacity, errorRate), BestK(capacity, errorRate))
        {
        }

        public BloomFilter(int capacity, float errorRate, HashFunction hashFunction, int m, int k)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException("capacity", capacity, "capacity must be > 0");
            }

            if (errorRate >= 1 || errorRate <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "errorRate",
                    errorRate,
                    string.Format("errorRate must be between 0 and 1, exclusive. Was {0}", errorRate));
            }

            if (m < 1)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        "The provided capacity and errorRate values would result in an array of length > int.MaxValue. Please reduce either of these values. Capacity: {0}, Error rate: {1}",
                        capacity,
                        errorRate));
            }

            if (hashFunction == null)
            {
                if (typeof(T) == typeof(string))
                {
                    this._getHashSecondary = HashString;
                }
                else if (typeof(T) == typeof(int))
                {
                    this._getHashSecondary = HashInt32;
                }
                else
                {
                    throw new ArgumentNullException(
                        "hashFunction",
                        "Please provide a hash function for your type T, when T is not a string or int.");
                }
            }
            else
            {
                this._getHashSecondary = hashFunction;
            }

            this._hashFunctionCount = k;
            this._hashBits = new BitArray(m);
        }

        public delegate int HashFunction(T input);

        public double Truthiness
        {
            get
            {
                return (double)this.TrueBits() / this._hashBits.Count;
            }
        }

        public void Add(T item)
        {
            int primaryHash = item.GetHashCode();
            int secondaryHash = this._getHashSecondary(item);

            for (int i = 0; i < this._hashFunctionCount; i++)
            {
                int hash = this.ComputeHash(primaryHash, secondaryHash, i);
                this._hashBits[hash] = true;
            }
        }

        public bool Contains(T item)
        {
            int primaryHash = item.GetHashCode();
            int secondaryHash = this._getHashSecondary(item);

            for (int i = 0; i < this._hashFunctionCount; i++)
            {
                int hash = this.ComputeHash(primaryHash, secondaryHash, i);
                if (this._hashBits[hash] == false)
                {
                    return false;
                }
            }

            return true;
        }

        private static float BestErrorRate(int capacity)
        {
            var c = (float)(1.0 / capacity);
            if (Math.Abs(c) > 0)
            {
                return c;
            }

            double y = int.MaxValue / (double)capacity;
            return (float)Math.Pow(0.6185, y);
        }

        private static int BestK(int capacity, float errorRate)
        {
            return (int)Math.Round(Math.Log(2.0) * BestM(capacity, errorRate) / capacity);
        }

        private static int BestM(int capacity, float errorRate)
        {
            return (int)Math.Ceiling(capacity * Math.Log(errorRate, 1.0 / Math.Pow(2, Math.Log(2.0))));
        }

        private static int HashInt32(T input)
        {
            var x = input as uint?;
            unchecked
            {
                x = ~x + (x << 15);
                x = x ^ (x >> 12);
                x = x + (x << 2);
                x = x ^ (x >> 4);
                x = x * 2057;
                x = x ^ (x >> 16);

                return (int)x;
            }
        }

        private static int HashString(T input)
        {
            var str = input as string;
            int hash = 0;

            if (str != null)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    hash += str[i];
                    hash += hash << 10;
                    hash ^= hash >> 6;
                }

                hash += hash << 3;
                hash ^= hash >> 11;
                hash += hash << 15;
            }

            return hash;
        }

        private int ComputeHash(int primaryHash, int secondaryHash, int i)
        {
            int resultingHash = (primaryHash + (i * secondaryHash)) % this._hashBits.Count;
            return Math.Abs(resultingHash);
        }

        private int TrueBits()
        {
            int output = 0;

            foreach (bool bit in this._hashBits)
            {
                if (bit)
                {
                    output++;
                }
            }

            return output;
        }

    }   
}
