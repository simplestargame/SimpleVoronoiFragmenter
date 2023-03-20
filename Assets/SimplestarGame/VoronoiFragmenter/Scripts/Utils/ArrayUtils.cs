using System;

namespace SimplestarGame
{

    public class ArrayUtils
    {
        public static T[] DeepClone<T>(T[] array)
        {
            T[] result = new T[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] is ICloneable)
                {
                    result[i] = (T)(array[i] as ICloneable).Clone();
                }
                else
                {
                    result[i] = array[i];
                }
            }
            return result;
        }

    }
}
