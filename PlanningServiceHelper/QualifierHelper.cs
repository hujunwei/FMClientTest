using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using DataModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlanningServiceHelper
{
    public class QualifierHelper
    {
        /// <summary>
        ///     The planning configuration service root URL
        /// </summary>
        private const string PlanningConfigServiceRootUrl =
            "https://planningconfigtest.cloudapp.net/";

        /// <summary>
        ///     Ingests the qualifier to service.
        /// </summary>
        /// <param name="qualifierName">Name of the qualifier.</param>
        /// <param name="qualifierUrl">The qualifier URL.</param>
        /// <param name="qualifierValues">The qualifier values.</param>
        /// <exception cref="System.Exception">Ingesting qualifier to service returns insuccess response</exception>
        public static void IngestQualifierToService(string qualifierName, string qualifierUrl,
            List<string> qualifierValues)
        {
            var qualifierCreated = CreateQualifier(qualifierName, qualifierUrl, qualifierValues);
            var qualifierIdFromSevice = GetQualifierIdInService(qualifierName,
                PlanningConfigServiceRootUrl + "api/qualifier/");
            var response = qualifierIdFromSevice == -1
                ? PostQualifierToService(PlanningConfigServiceRootUrl + "api/qualifier/add/",
                    qualifierCreated)
                : PostQualifierToService(
                    PlanningConfigServiceRootUrl + "api/qualifier/update/" + qualifierIdFromSevice,
                    qualifierCreated);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Ingesting qualifier to service returns insuccess response");
            }
        }

        /// <summary>
        ///     Posts the qualifier to service.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="qualifier">The qualifier.</param>
        /// <returns>HttpResponseMessage after post</returns>
        /// <exception cref="System.ArgumentException">Invalid url</exception>
        /// <exception cref="System.Exception">Ingesting Attribute to service returns insuccess response</exception>
        private static HttpResponseMessage PostQualifierToService(string url, Qualifier qualifier)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("Invalid url");
            }
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(qualifier),
                        Encoding.UTF8, "application/json");
                    var response = client.SendAsync(request, CancellationToken.None).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(
                            "Ingesting Attribute to service returns insuccess response");
                    }
                    return response;
                }
            }
        }

        /// <summary>
        ///     Gets the qualifier identifier in service.
        /// </summary>
        /// <param name="qualifierName">Name of the qualifier.</param>
        /// <param name="url">The URL.</param>
        /// <returns>Id of qualifier</returns>
        /// <exception cref="System.ArgumentException">Invalid url</exception>
        /// <exception cref="System.Exception">Adding Configuration to service returns insuccess response</exception>
        private static int GetQualifierIdInService(string qualifierName, string url)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("Invalid url");
            }
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (var response = client.SendAsync(request, CancellationToken.None).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception(
                                "Adding Configuration to service returns insuccess response");
                        }
                        var qualifiersInSeviceList =
                            response.Content.ReadAsAsync<List<JObject>>().Result;
                        var qualifierNamesInSeviceList =
                            qualifiersInSeviceList.Select(qualifier => qualifier["Name"]).ToList();
                        var qualifierId = -1;
                        if (qualifierNamesInSeviceList.Contains(qualifierName))
                        {
                            var qualListFiltered =
                                qualifiersInSeviceList.Where(
                                    qualifier => qualifier["Name"].ToString() == qualifierName)
                                    .ToList();
                            qualifierId = int.Parse(qualListFiltered[0]["Id"].ToString());
                        }
                        return qualifierId;
                    }
                }
            }
        }

        /// <summary>
        ///     Create a Qualifier object
        /// </summary>
        /// <param name="name">Invariant Name to be indentified in Database for debug use</param>
        /// <param name="url">Url field for a Qualifier</param>
        /// <param name="values">The values.</param>
        /// <returns>
        ///     Qualifier object
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