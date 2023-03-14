using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsService1
{
    internal class OrderHeader
    {
        private Dictionary<string, string> data = new Dictionary<string, string>();

        private string inputText;

        public void textSet(string text)
        {
            inputText = text;
        }

        public string textGet()
        {
            return inputText;
        }

        public void dataAdd(string key, string value)
        {
            data.Add(key, value);
        }

        public Dictionary<string, string> dataGetAll()
        {
            return data;
        }

        public string getValue(string key)
        {
            return data[key];
        }

        public bool hasValue(string key)
        {
            if (data.ContainsKey(key))
            {
                return true;
            }

            return false;
        }
    }
}
