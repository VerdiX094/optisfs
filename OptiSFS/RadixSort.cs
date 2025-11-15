using System;
using System.Collections.Generic;
using System.Linq;
using SFS.World.Drag;
using UnityEngine;

namespace OptiSFS
{
    public static class SurfaceEndXRadixSort
    {
        public static void Sort(ref List<Surface> arr)
        {
            int n = arr.Count;
            uint[] keys = new uint[n];
            for (int i = 0; i < n; i++)
            {
                uint bits = BitConverter.ToUInt32(BitConverter.GetBytes(arr[i].line.end.x), 0);
                keys[i] = (bits & 0x80000000) != 0 ? ~bits : bits ^ 0x80000000; // This makes positive zero less than negative zero, but should be fine
            }

            var a = arr.ToArray();
            RadixSort(keys, a);
            
            arr = new List<Surface>(a);
        }

        public static bool Test()
        {
            List<Surface> test = new List<Surface>()
            {
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 2f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 1f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 3f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 7f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * -5f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 0f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * -80f)
                },
            };

            int count = test.Count;
            
            Sort(ref test);
            
            float max = float.NegativeInfinity;

            for (int i = 0; i < count; i++)
            {
                if (test[i].line.end.x < max) return false; // Isn't the new highest?
                max = test[i].line.end.x;
            }
            
            return count == test.Count;
        }

        private static void RadixSort(uint[] keys, Surface[] values)
        {
            int n = keys.Length;
            uint[] auxKeys = new uint[n];
            Surface[] auxVals = new Surface[n];

            const int BITS = 32;
            const int RADIX = 8;
            const int BUCKETS = 1 << RADIX;
            const uint mask = BUCKETS - 1;

            for (int shift = 0; shift < BITS; shift += RADIX)
            {
                int[] count = new int[BUCKETS];
                for (int i = 0; i < n; i++)
                    count[(int)((keys[i] >> shift) & mask)]++;

                for (int i = 1; i < BUCKETS; i++)
                    count[i] += count[i - 1];

                for (int i = n - 1; i >= 0; i--)
                {
                    int bucket = (int)((keys[i] >> shift) & mask);
                    int pos = --count[bucket];
                    auxKeys[pos] = keys[i];
                    auxVals[pos] = values[i];
                }

                Array.Copy(auxKeys, keys, n);
                Array.Copy(auxVals, values, n);
            }
        }
    }

    public static class GenericRadixSort
    {
        public static void Sort<T>(ref List<T> list, Func<T, uint> getScore)
        {
            int n = list.Count;
            uint[] keys = new uint[n];
            for (int i = 0; i < n; i++)
            {
                keys[i] = getScore(list[i]);
            }

            var vals = list.ToArray();
            uint[] auxKeys = new uint[n];
            T[] auxVals = new T[n];

            const int BITS = 32;
            const int RADIX = 8;
            const int BUCKETS = 1 << RADIX;
            const uint mask = BUCKETS - 1;

            for (int shift = 0; shift < BITS; shift += RADIX)
            {
                int[] count = new int[BUCKETS];
                for (int i = 0; i < n; i++)
                    count[(int)((keys[i] >> shift) & mask)]++;

                for (int i = 1; i < BUCKETS; i++)
                    count[i] += count[i - 1];

                for (int i = n - 1; i >= 0; i--)
                {
                    int bucket = (int)((keys[i] >> shift) & mask);
                    int pos = --count[bucket];
                    auxKeys[pos] = keys[i];
                    auxVals[pos] = vals[i];
                }

                Array.Copy(auxKeys, keys, n);
                Array.Copy(auxVals, vals, n);
            }
            
            list = new List<T>(vals);
        }
        
        private static void RadixSort<T>(uint[] keys, T[] values)
        {
            int n = keys.Length;
            uint[] auxKeys = new uint[n];
            T[] auxVals = new T[n];

            const int BITS = 32;
            const int RADIX = 8;
            const int BUCKETS = 1 << RADIX;
            const uint mask = BUCKETS - 1;

            for (int shift = 0; shift < BITS; shift += RADIX)
            {
                int[] count = new int[BUCKETS];
                for (int i = 0; i < n; i++)
                    count[(int)((keys[i] >> shift) & mask)]++;

                for (int i = 1; i < BUCKETS; i++)
                    count[i] += count[i - 1];

                for (int i = n - 1; i >= 0; i--)
                {
                    int bucket = (int)((keys[i] >> shift) & mask);
                    int pos = --count[bucket];
                    auxKeys[pos] = keys[i];
                    auxVals[pos] = values[i];
                }

                Array.Copy(auxKeys, keys, n);
                Array.Copy(auxVals, values, n);
            }
        }
    }
}