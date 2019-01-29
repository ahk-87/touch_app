using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace touch_app
{
    public class jsonClasses
    {
        public class Balances
        {
            public string status { get; set; }
            public bool show { get; set; }
            public List<Balance> balances { get; set; }
            public int invoicehistory { get; set; }
            public string payment { get; set; }
        }
        public class Balance
        {
            public Valid valid { get; set; }
            public List<Sub> subs { get; set; }
            public Title2 title { get; set; }
        }
        public class Title2
        {
            public string bold { get; set; }
            public string color { get; set; }
            public string size { get; set; }
            public string text { get; set; }
        }
        public class Sub
        {
            public string type { get; set; }
            public Title title { get; set; }
            public Value value { get; set; }
        }
        public class Value
        {
            public string bold { get; set; }
            public string color { get; set; }
            public string size { get; set; }
            public string text { get; set; }
        }
        public class Title
        {
            public string bold { get; set; }
            public string color { get; set; }
            public string size { get; set; }
            public string text { get; set; }
        }
        public class Valid
        {
            public string color { get; set; }
            public string size { get; set; }
            public string text { get; set; }
            public string boldify { get; set; }
        }






    }

    public class Json
    {
        public static T Desirialize<T>(string jsonResponse) where T : class
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(jsonResponse));
            T obj = serializer.ReadObject(stream) as T;

            return obj;
        }
    }
}
