using System;
using System.Collections.Generic;

namespace AnimLib {
    public static class Clipboard {
        private static object data = null;

        public static object Object {
            get {
                return data;
            }
            set {
                data = value;
            }
        }
    }
}