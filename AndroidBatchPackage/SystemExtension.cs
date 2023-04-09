using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AndroidBatchPackage {
    public static class SystemExtension {
        public static V GetOrAddConstruct<K, V>(this IDictionary<K, V> dict, K key) where V : new() {
            V v;
            if (dict.TryGetValue(key, out v)) {
                return v;
            } else {
                v = new V();
                dict[key] = v;
                return v;
            }
        }
    }
}
