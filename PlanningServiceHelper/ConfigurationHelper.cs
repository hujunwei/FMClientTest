using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataModel;
using Newtonsoft.Json;
using System.Net.Http.Formatting;
using Newtonsoft.Json.Linq;


namespace PlanningServiceHelper
{
    public class ConfigurationHelper
    {


        private const string PlanningConfigServiceRootUrl = "https://planningconfigtest.cloudapp.net/";






        public static void CreateConfigurationFromCsv(string filePath, int templateId)
        {


            List<Configuration> configListFromCsv = new List<Configuration>();

            try
            {
                if (ValidateCsvFormat(filePath, templateId) == false)
                    throw new Exception("Configuration in csv does not match corresponding template from service");
                //Get template from service to help validate the csv file is in correct format to create qualifier
                var response = QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId);
                //Parse response as template in JObject
                var templateJObjectFromService = response.Content.ReadAsAsync<JObject>().Result;
                var attributesForTemplateFromService = templateJObjectFromService["Attributes"];
                var qualifiersForTemplateFromService = templateJObjectFromService["Qualifiers"];
                var numAttributesForTemplateFromService = attributesForTemplateFromService.Count();
                var numQualifierForTemplateFromService = qualifiersForTemplateFromService.Count();


                using (var csvReader = new StreamReader(File.OpenRead(filePath)))
                {
                    var attributeOfConfigLine = csvReader.ReadLine();
                    if (attributeOfConfigLine == null)
                        throw new Exception("Invalid Csv file for Configuration");
                    var attributeOfConfigList = SplitCsv(attributeOfConfigLine).ToList();


                    while (!csvReader.EndOfStream)
                    {
                        var line = csvReader.ReadLine();
                        if (line == null) continue;
                        var valueListForOneLine = SplitCsv(line).ToList();
                        if (!valueListForOneLine.Any()) continue;
                        var configName = valueListForOneLine[0];
                        var configCategoryId = (int)templateJObjectFromService["CategoryId"];
                        var configTemplateId = templateId;
                        var configStartDate = Convert.ToDateTime(valueListForOneLine[valueListForOneLine.Count - 2]);
                        var configEndDate = Convert.ToDateTime(valueListForOneLine[valueListForOneLine.Count - 1]);
                        var configQualifierList = new List<ConfigQualifier>();
                        var configAttributeList = new List<ConfigAttribute>();
                        for (var i = 1; i <= numQualifierForTemplateFromService + numAttributesForTemplateFromService; i++)
                        {
                            if (i <= numQualifierForTemplateFromService)
                            {
                                var qualifierByName =
                                    qualifiersForTemplateFromService.SingleOrDefault(
                                        qualifier => qualifier["Name"].ToString() == attributeOfConfigList[i]);

                                if (qualifierByName == null)
                                    throw new Exception("Configuration in csv does not match corresponding template from service");

                                var configQualifierVal = new ConfigQualifier
                                {
                                    QualifierId = (int)qualifierByName["Id"],
                                    QualifierValue = valueListForOneLine[i]
                                };

                                configQualifierList.Add(configQualifierVal);

                            }
                            else
                            {
                                var attributeByName =
                                    attributesForTemplateFromService.SingleOrDefault(
                                        attribute => attribute["Name"].ToString() == attributeOfConfigList[i]);

                                if (attributeByName == null)
                                    throw new Exception("Configuration in csv does not match corresponding template from service");

                                var configAttributeVal = new ConfigAttribute
                                {
                                    AttributeId = (int)attributeByName["Id"],
                                    AttributeValue = valueListForOneLine[i],
                                    Version = 1,
                                };

                                configAttributeList.Add(configAttributeVal);
                            }
                        }
                        var config = new Configuration
                        {
                            Name = configName,
                            Active = true,
                            CategoryId = configCategoryId,
                            QualifierList = configQualifierList,
                            AttributeList = configAttributeList,
                            TemplateId = configTemplateId,
                            StartDate = configStartDate,
                            EndDate = configEndDate
                        };

                        //AddConfigurationToService(PlanningConfigServiceRootUrl + "add", config);
                        configListFromCsv.Add(config);
                    }
                }
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }


            using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(@"C:\Users\junweihu\Desktop\TemplateResponseInfo.txt"))
            {
                file.Write(JsonConvert.SerializeObject(configListFromCsv));
                Console.WriteLine("Finish!");
            }

        }



        private static bool ValidateCsvFormat(string filePath, int templateId)
        {
            //Get template from service to help validate the csv file is in correct format to create qualifier
            var response = QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId);
            //Parse response as template in JObject
            var templateJObjectFromService = response.Content.ReadAsAsync<JObject>().Result;
            var attributesForTemplateFromService = templateJObjectFromService["Attributes"];
            var qualifiersForTemplateFromService = templateJObjectFromService["Qualifiers"];
            var qualifiersNameListFromService =
                qualifiersForTemplateFromService.ToList().Select(qualifier => qualifier["Name"]).ToList();
            var attributesNameListFromService =
                attributesForTemplateFromService.ToList().Select(attribute => attribute["Name"]).ToList();
            var numAttributesForTemplateFromService = attributesForTemplateFromService.Count();
            var numQualifierForTemplateFromService = qualifiersForTemplateFromService.Count();
            using (var csvReader = new StreamReader(File.OpenRead(filePath)))
            {
                //Parse first line as attributes
                var attributeOfConfigLine = csvReader.ReadLine();
                if (attributeOfConfigLine == null)
                    return false;

                var attributeOfConfigList = SplitCsv(attributeOfConfigLine).ToList();
                if (attributeOfConfigList[0] != "Name")
                    return false;

                var numAttributesOfConfig = attributeOfConfigList.Count();
                //numAttributes - 3 represents: all attributes of Configuration in csv except Name, StartDate, EndDate 
                if ((numAttributesOfConfig - 3) != qualifiersForTemplateFromService.ToList().Count + attributesForTemplateFromService.ToList().Count)
                    return false;

                //Check whether corresponding template of configuration contains name of qualifiers and attributes from Csv  
                for (var i = 1; i <= numQualifierForTemplateFromService + numAttributesForTemplateFromService; i++)
                {
                    if (i <= numQualifierForTemplateFromService)
                    {
                        if (!qualifiersNameListFromService.Contains(attributeOfConfigList[i]))
                            return false;
                    }
                    else
                    {
                        if (!attributesNameListFromService.Contains(attributeOfConfigList[i]))
                            return false;
                    }
                }

                //Last two string should be "Start Date" and "End Date"
                if (attributeOfConfigList[attributeOfConfigList.Count - 2] != "Start Date")
                    return false;
                if (attributeOfConfigList[attributeOfConfigList.Count - 1] != "End Date")
                    return false;
            }
            return true;
        }




        private static HttpResponseMessage AddConfigurationToService(string url, Configuration config)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                throw new ArgumentException("Invalid url");
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(config), Encoding.UTF8, "application/json")
            };
            var response = client.SendAsync(request, CancellationToken.None).Result;
            if (!response.IsSuccessStatusCode)
                throw new Exception("Calling servise insuccess");

            return response;
        }







        private static HttpResponseMessage QueryService(string url)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                throw new ArgumentException("Invalid url");

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = client.SendAsync(request, CancellationToken.None).Result;
            if (!response.IsSuccessStatusCode)
                throw new Exception("Calling servise insuccess");

            return response;
        }





        /// <summary>
        /// Splits the CSV.
        /// </summary>
        /// <param name="csvString">The CSV string.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">csvString;Unterminated quotation mark.</exception>
        private static IEnumerable<string> SplitCsv(string csvString)
        {
            var sb = new StringBuilder();
            var quoted = false;
            foreach (var c in csvString)
            {
                if (quoted)
                {
                    if (c == '"')
                        quoted = false;
                    else
                        sb.Append(c);
                }
                else
                {
                    switch (c)
                    {
                        case '"':
                            quoted = true;
                            break;
                        case ',':
                            yield return sb.ToString();
                            sb.Length = 0;
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }
            }

            if (quoted)
                throw new ArgumentException("csvString", "Unterminated quotation mark.");

            yield return sb.ToString();
        }



    }
}
