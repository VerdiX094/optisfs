using System.Globalization;
using UnityEngine;

namespace OptiSFS
{
    public static class Utility
    {
        public static int CompareToCultureInvariant(this string a, string b)
        {
            return b == null ? 1 : CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.None);
        }
    }
}