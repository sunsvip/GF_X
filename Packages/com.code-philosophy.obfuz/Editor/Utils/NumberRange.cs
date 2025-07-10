namespace Obfuz.Utils
{
    public class NumberRange<T> where T : struct
    {
        public readonly T? min;
        public readonly T? max;

        public NumberRange(T? min, T? max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
