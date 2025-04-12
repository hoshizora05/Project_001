namespace PlayerProgression.Data
{
    public struct StatValue
    {
        public float BaseValue;
        public float CurrentValue;
        public float MinValue;
        public float MaxValue;
        
        public StatValue(float baseVal, float current, float min, float max)
        {
            BaseValue = baseVal;
            CurrentValue = current;
            MinValue = min;
            MaxValue = max;
        }
    }
}