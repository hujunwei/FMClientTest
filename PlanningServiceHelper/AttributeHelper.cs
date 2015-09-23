using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using DataModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Attribute = DataModel.Attribute;

namespace PlanningServiceHelper
{
    public class AttributeHelper
    {
        /// <summary>
        ///     The planning configuration service root URL
        /// </summary>
        private const string PlanningConfigServiceRootUrl =
            "https://planningconfigtest.cloudapp.net/";

        /// <summary>
        ///     Ingests the attribute to service.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="required">if set to <c>true</c> [required].</param>
        /// <param name="rule">The rule.</param>
        /// <param name="type">The type.</param>
        public static void IngestAttributeToService(string attributeName, bool required, string rule, AttributeType type = AttributeType.String)
        {
            var attributeCreated = CreateAttribute(attributeName, required, rule, type);
            var attributeIdFromSevice = GetAttributeIdInService(attributeName,
                PlanningConfigServiceRootUrl + "api/attribute/");
            var response = attributeIdFromSevice == -1
                ? PostAttributeToService(PlanningConfigServiceRootUrl + "api/attribute/add/",
                    attributeCreated)
                : PostAttributeToService(
                    PlanningConfigServiceRootUrl + "api/attribute/update/" + attributeIdFromSevice,
                    attributeCreated);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Ingesting Attribute to service returns insuccess response");
            }
            Console.WriteLine("Done!");
        }

        /// <summary>
        ///     Creates the attribute.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="required">if set to <c>true</c> [required].</param>
        /// <param name="rule">The rule.</param>
        /// <param name="type">The type.</param>
        /// <returns>Attribute instance.</returns>
        private static Attribute CreateAttribute(string name, bool required, string rule, AttributeType type = AttributeType.String)
        {
            var attribute = new Attribute
            {
                Name = name,
                AttributeType = type,
                Required = required,
                Rule = rule
            };
            return attribute;
        }

        /// <summary>
        ///     Posts the attribute to service.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="attribute">The attribute.</param>
        private static HttpResponseMessage PostAttributeToService(string url, Attribute attribute)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("Invalid url");
            }
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(attribute), Encoding.UTF8, "application/json");
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
        ///     Gets the attribute identifier in service.
        /// </summary>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="url">The URL.</param>
        /// <returns>AttributeId from service.</returns>
        private static int GetAttributeIdInService(string attributeName, string url)
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
                        var attributesInSeviceList =
                            response.Content.ReadAsAsync<List<JObject>>().Result;
                        var attributeNamesInSeviceList =
                            attributesInSeviceList.Select(attribute => attribute["Name"]).ToList();
                        var attributeId = -1;
                        if (attributeNamesInSeviceList.Contains(attributeName))
                        {
                            var attrListFiltered =
                                attributesInSeviceList.Where(
                                    attribute => attribute["Name"].ToString() == attributeName)
                                    .ToList();
                            attributeId = int.Parse(attrListFiltered[0]["Id"].ToString());
                        }
                        return attributeId;
                    }
                }
            }
        }
    }
}