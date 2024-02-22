using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ArxLibertatisFTLConverter
{
    public class EqualsComparer<T> : IEqualityComparer<T>
    {
        public bool Equals([AllowNull] T x, [AllowNull] T y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            return obj.GetHashCode();
        }
    }
}
