using System;

namespace HondaTuner.Core
{
    public static class EcuConstants
    {
        public const int DefaultRomSize = 32768;       // 32KB Honda P28/P30
        public const int ExtendedRomSize = 65536;      // 64KB Honda P72/P06
        public const int Obd1BaudRate = 9600;           // Honda OBD1 K-Line
    }
}
