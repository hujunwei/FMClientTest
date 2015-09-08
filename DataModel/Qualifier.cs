using System.Collections.Generic;
using Newtonsoft.Json;

namespace DataModel
{
    public class Qualifier
    {
        public Qualifier()
        {
            Custom = true;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public bool Custom { get; set; }

        private List<string> _values;

        [JsonIgnore]
        public string ValuesSerialized
        {
            get { return JsonConvert.SerializeObject(_values); }
            set { _values = JsonConvert.DeserializeObject<List<string>>(value); }
        }

        public virtual List<string> Values
        {
            get { return _values; }
            set { _values = value; }
        }
    }
}
