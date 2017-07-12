using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADTools
{
    public static class Utilities<T>
    {
        public static List<T> ToList(T[] objects)
        {
            List<T> list = new List<T>();
            foreach (T obj in objects)
            {
                list.Add(obj);
            }
            return list;
        }

        public static List<string> TrimAll(List<string> objects)
        {
            List<string> list = new List<string>();
            foreach (string obj in objects)
            {
                list.Add(obj.Trim());
            }
            return list;
        }

        public static string[] TrimAll(string[] objects)
        {
            List<string> list = new List<string>();
            foreach (string obj in objects)
            {
                list.Add(obj.Trim());
            }
            return list.ToArray();
        }

        public static List<T> Skip(int numberToSkip, List<T> objects)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < objects.Count; i++)
            {
                if (i >= numberToSkip)
                    list.Add(objects[i]);
            }
            return list;
        }

        public static List<T> Skip(int numberToSkip, T[] objects)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < objects.Length; i++)
            {
                if (i >= numberToSkip)
                    list.Add(objects[i]);
            }
            return list;
        }
    }
}
