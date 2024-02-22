using CSWavefront.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArxLibertatisFTLConverter
{
    public class EqualsAutoDictionary<T, U> : Dictionary<T, U>
    {
        private Func<T, U> generator;

        public new U this[T key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                {
                    value = (base[key] = generator(key));
                }

                return value;
            }
            set
            {
                base[key] = value;
            }
        }

        public EqualsAutoDictionary(Func<T, U> generator) :base(new EqualsComparer<T>())
        {
            this.generator = generator;
        }
    }
}
