using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FMtest
{
    public class FaciliityMasterClient
    {
        /// <summary>
        ///     The default address
        /// </summary>
        private const string FacilityMasterBaseUrl = "https://facilitymastertest.cloudapp.net/";

        /// <summary>
        ///     The certficate thumbprint
        /// </summary>
        private const string CertficateThumbprint = "90128E7726EF898920B9CBBE724FCA3BE97C2ED2";

        /// <summary>
        ///     Gets the facility data that will be used for Qualifier.
        /// </summary>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <returns>
        ///     A List of string used for Qualifier value
        /// </returns>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.Exception">
        ///     QueryFacilityMaster returns insuccess response
        ///     or
        ///     QueryFacilityMaster returns insuccess response
        ///     or
        ///     Invalid attributeType
        /// </exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception">QueryFacilityMaster returns insuccess response</exception>
        public static List<string> GetFacilityData(string attributeType)
        {
            if (string.IsNullOrEmpty(attributeType))
            {
                throw new ArgumentException();
            }
            var result = new HashSet<string>();
            switch (attributeType)
            {
                case "AzureGeo":
                {
                    var response = QueryFacilityMaster("api/Attribute?Type=" + attributeType);
                    //Parse response to JArray 
                    var responseContent = response.Content.ReadAsStreamAsync().Result;
                    JsonReader jsonReader = new JsonTextReader(new StreamReader(responseContent));
                    var jArrayResult = (JArray) JToken.ReadFrom(jsonReader);
                    foreach (var jToken in jArrayResult)
                    {
                        if (jToken == null)
                        {
                            continue;
                        }
                        var attributeItemName = jToken["Name"];
                        if (attributeItemName == null)
                        {
                            continue;
                        }
                        if (string.IsNullOrEmpty(attributeItemName.ToString()))
                        {
                            continue;
                        }
                        result.Add(attributeItemName.ToString());
                    }
                }
                    break;
                case "AzureRegion":
                {
                    var response = QueryFacilityMaster("api/Attribute?Type=AzureGeo");
                    //Parse response to JArray 
                    var responseContent = response.Content.ReadAsStreamAsync().Result;
                    JsonReader jsonReader = new JsonTextReader(new StreamReader(responseContent));
                    var jArrayResult = (JArray) JToken.ReadFrom(jsonReader);
                    foreach (var jToken in jArrayResult)
                    {
                        var azureRegions = jToken?["AzureRegions"];
                        if (azureRegions == null)
                        {
                            continue;
                        }
                        foreach (var jToken2 in azureRegions)
                        {
                            var azureRegionName = jToken2["Name"];
                            if (string.IsNullOrEmpty(azureRegionName.ToString()))
                            {
                                continue;
                            }
                            result.Add(azureRegionName.ToString());
                        }
                    }
                }
                    break;
                default:
                    throw new Exception("Invalid attributeType");
            }
            return result.ToList();
        }

        /// <summary>
        ///     Queries the result.
        /// </summary>
        /// <param name="facilityMasterApi">The facility master API.</param>
        /// <returns>
        ///     The httpResponseMessage instance
        /// </returns>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.Exception">
        ///     Certificate not found
        ///     or
        ///     Get facility master response fail
        /// </exception>
        /// <exception cref="Exception">Certificate not found</exception>
        private static HttpResponseMessage QueryFacilityMaster(string facilityMasterApi)
        {
            if (string.IsNullOrEmpty(facilityMasterApi))
            {
                throw new ArgumentException();
            }
            var certificate = GetCertificateFromStore(CertficateThumbprint);
            if (certificate == null)
            {
                throw new Exception("Certificate not found");
            }
            //Setting the certificate and httpClient
            using (var handler = new WebRequestHandler())
            {
                handler.ClientCertificates.Add(certificate);
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.BaseAddress = new Uri(FacilityMasterBaseUrl);
                    //Request based on Facility Master api
                    var response = httpClient.GetAsync(facilityMasterApi).Result;
                    if (response == null)
                    {
                        throw new Exception("Get facility master response fail");
                    }
                    return response;
                }
            }
        }

        /// <summary>
        ///     Gets the certificate from store.
        /// </summary>
        /// <param name="thumbprint">The thumbprint for the certificate.</param>
        /// <returns>
        ///     The X509Certificate2 instance
        /// </returns>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.Exception">Certificate with thumbprint:  + thumbprint + not found</exception>
        private static X509Certificate2 GetCertificateFromStore(string thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentException();
            }
            var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);
            var certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint,
                thumbprint, true);
            certStore.Close();
            if (certCollection.Count < 1)
            {
                throw new Exception("Certificate with thumbprint: " + thumbprint + "not found");
            }
            return certCollection[0];
        }
    }
}