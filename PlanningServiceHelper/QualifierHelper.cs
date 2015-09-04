using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using Newtonsoft.Json;

namespace PlanningServiceHelper
{
    public class QualifierHelper
    {
        /// <summary>
        /// Adds the qualifier.
        /// </summary>
        /// <param name="name">The name for Qualifier.</param>
        /// <param name="qualifierValList">The Qualifier in json.</param>
        /// <param name="url">The URL for Qualifier.</param>
        /// <param name="apiUrl">The API URL of facility master.</param>
        /// <returns>HttpWebResponse</returns>
        public static HttpWebResponse AddQualifier(string name, List<string> qualifierValList, string url = "null", string apiUrl = "https://planningconfigtest.cloudapp.net/api/qualifier/add")
        {
            HttpWebResponse response = null;
            try
            {
                var createdQualifier = CreateQualifier(name, url, qualifierValList);
                var request = (HttpWebRequest)WebRequest.Create(apiUrl);
                var requestContentInJson = JsonConvert.SerializeObject(createdQualifier);
                var requestContent = Encoding.ASCII.GetBytes(requestContentInJson);

                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = requestContent.Length;


                using (var stream = request.GetRequestStream())
                {
                    stream.Write(requestContent, 0, requestContent.Length);
                }
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return response;
        }


        /// <summary>
        /// Create a Qualifier object
        /// </summary>
        /// <param name="name">Invariant Name to be indentified in Database for debug use</param>
        /// <param name="url">Url field for a Qualifier</param>
        /// <param name="values">The values.</param>
        /// <returns>
        /// Qualifier object
        /// </returns>
        private static Qualifier CreateQualifier(string name, string url, List<string> values)
        {

            var qualifier = new Qualifier
            {
                Custom = true,
                Name = name,
                Url = url,
                Values = values
            };
            return qualifier;
        }

    }
}
