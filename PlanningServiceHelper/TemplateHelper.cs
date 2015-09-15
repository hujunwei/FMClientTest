using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using DataModel;
using Newtonsoft.Json;

namespace PlanningServiceHelper
{
    public class TemplateHelper
    {
        /// <summary>
        ///     Creates the template.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="active">if set to <c>true</c> [active].</param>
        /// <param name="categoryId">The category identifier.</param>
        /// <param name="attributeIds">The attribute ids.</param>
        /// <param name="qualifierIds">The qualifier ids.</param>
        /// <returns></returns>
        public static ConfigTemplate CreateTemplate(string name, bool active, int categoryId,
            List<int> attributeIds, List<int> qualifierIds)
        {
            var template = new ConfigTemplate
            {
                AttributeIds = attributeIds,
                QualifierIds = qualifierIds,
                Name = name,
                CategoryId = categoryId,
                Active = active
            };
            return template;
        }

        /// <summary>
        ///     Adds the template to service.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="template">The template.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Invalid url</exception>
        /// <exception cref="System.Exception">Adding Configuration to service returns insuccess response</exception>
        public static HttpResponseMessage AddTemplateToService(string url, ConfigTemplate template)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("Invalid url");
            }
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(template),
                        Encoding.UTF8, "application/json");
                    var response = client.SendAsync(request, CancellationToken.None).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(
                            "Adding Configuration to service returns insuccess response");
                    }
                    return response;
                }
            }
        }
    }
}