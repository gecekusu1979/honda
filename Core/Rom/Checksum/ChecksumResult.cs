namespace HondaTuner.Core.Rom.Checksum
{
    public class ChecksumResult
    {
        public bool IsValid { get; }
        public int CalculatedValue { get; }
        public int ExpectedValue { get; }
        public int Address { get; }
        public string Message { get; }

        public ChecksumResult(bool isValid, int calculatedValue, int expectedValue, int address, string message)
        {
            IsValid = isValid;
            CalculatedValue = calculatedValue;
            ExpectedValue = expectedValue;
            Address = address;
            Message = message;
        }
    }
}
