using System;
using System.CodeDom;
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
                //Get template from service to help validate the csv file is in correct format to create qualifier
                var response = QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId);
                //Parse response as template in JObject
                var templateJObjectFromService = response.Content.ReadAsAsync<JObject>().Result;
                //Get configAttributes for the template from service
                var configAttributesForTemplateFromService = QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId + "/attributes").Content.ReadAsAsync<List<JObject>>().Result;
                //Get configQualifiers for the template from service
                var configQualifiersForTemplateFromService = QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId + "/qualifiers").Content.ReadAsAsync<List<JObject>>().Result;
                var numAttributesForTemplateFromService = configAttributesForTemplateFromService.Count();
                var numQualifierForTemplateFromService = configQualifiersForTemplateFromService.Count();

                //Get Qualifiers for Template
                var qualifierIdsForTemplateFromService =
                    configQualifiersForTemplateFromService.Select(qualifier => (int)qualifier["Id"]).ToList();
                //Create a Map for qualifiers of the Template to help validate qualifier value
                var qualifiersMapForTemplateFromService = qualifierIdsForTemplateFromService
                    .Select(qualifierId => QueryService(PlanningConfigServiceRootUrl + "api/qualifier/" + qualifierId).Content.ReadAsAsync<JObject>().Result)
                    .ToDictionary(qualifier => (int)qualifier["Id"]);


                //Get Attributes for Template
                var attributeIdsForTemplateFromService =
                    configAttributesForTemplateFromService.Select(attribute => (int)attribute["Id"]).ToList();
                //Create a Map for qualifiers of the Template to help validate qualifier value
                var attributesMapForTemplateFromService = attributeIdsForTemplateFromService
                    .Select(attributeId => QueryService(PlanningConfigServiceRootUrl + "api/attribute/" + attributeId).Content.ReadAsAsync<JObject>().Result)
                    .ToDictionary(attribute => (int)attribute["Id"]);

                //TODO: Add the map for attributes and qualifiers keyed by their names
                var configAttributesMap = configAttributesForTemplateFromService.ToDictionary(attributeJObject => attributeJObject["Name"].ToString());
                var configQualifiersMap = configQualifiersForTemplateFromService.ToDictionary(qualifierJObject => qualifierJObject["Name"].ToString());


                using (var csvReader = new StreamReader(File.OpenRead(filePath)))
                {
                    //AttributeOfConfigLine is the first line of the Csv file, which contains Name, NameOfQuaifiers[], NameOfAttributes[], StartDate, EndDate
                    var attributeOfConfigLine = csvReader.ReadLine();
                    if (attributeOfConfigLine == null)
                        throw new Exception("Invalid Csv file for Configuration");
                    //Split first line to a list
                    var attributeOfConfigList = SplitCsv(attributeOfConfigLine).ToList();

                    //Validate the first line matches the Template for Configuration from service
                    if (ValidateCsvFormat(attributeOfConfigList, configQualifiersForTemplateFromService, configAttributesForTemplateFromService) == false)
                        throw new Exception("Configuration in csv does not match corresponding template from service");

                    //Read begin from second line of Csv file, each line contains infomation for a configuration
                    while (!csvReader.EndOfStream)
                    {

                        var line = csvReader.ReadLine();
                        if (line == null) continue;
                        //Split readed line to a list that contains information needed for a Csv file
                        var valueListForOneLine = SplitCsv(line).ToList();
                        if (!valueListForOneLine.Any()) continue;
                        //First element is the name for a configuration
                        var configName = valueListForOneLine[0];
                        //CategoryId of a Configuration equals to CategoryId of the Template for this configuration
                        var configCategoryId = (int)templateJObjectFromService["CategoryId"];
                        var configTemplateId = templateId;
                        //Element in valueListForOneLine before the last one is the StartDate
                        var configStartDate = Convert.ToDateTime(valueListForOneLine[valueListForOneLine.Count - 2]);
                        //Last element in valueListForOneLine is the EndDate
                        var configEndDate = Convert.ToDateTime(valueListForOneLine[valueListForOneLine.Count - 1]);
                        //Create configQualifier list and configAttribute list for the configuration
                        var configQualifierList = new List<ConfigQualifier>();
                        var configAttributeList = new List<ConfigAttribute>();
                        for (var i = 1; i <= numQualifierForTemplateFromService + numAttributesForTemplateFromService; i++)
                        {
                            if (i <= numQualifierForTemplateFromService)
                            {
                                //var qualifierByName =   //TODO: Fetch the qualifier using the map
                                //    qualifiersForTemplateFromService.SingleOrDefault(
                                //        qualifier => qualifier["Name"].ToString() == attributeOfConfigList[i]);


                                var qualifierByName = configQualifiersMap[attributeOfConfigList[i]];
                                if (qualifierByName == null)
                                    throw new Exception("Configuration in csv does not match corresponding template from service");
                                var qualifierId = (int)qualifierByName["Id"];
                                var qualifierValue = valueListForOneLine[i];
                                //TODO: Test the value for the Qualifier, whether it is there in the VALUES list
                                // In case of validation failure, add a error message to the console/log file

                                if (!ValidateQualifierValue(qualifierId, qualifierValue, qualifiersMapForTemplateFromService))
                                    throw new Exception("Invalid qualifierValue: " + qualifierValue);


                                var configQualifierVal = new ConfigQualifier
                                {
                                    QualifierId = qualifierId,
                                    QualifierValue = valueListForOneLine[i]
                                };

                                configQualifierList.Add(configQualifierVal);

                            }
                            else
                            {
                                //var attributeByName = //TODO: Fetch the attributes using the map
                                //    attributesForTemplateFromService.SingleOrDefault(
                                //        attribute => attribute["Name"].ToString() == attributeOfConfigList[i]);

                                var attributeByName = configAttributesMap[attributeOfConfigList[i]];

                                if (attributeByName == null)
                                    throw new Exception("Configuration in csv does not match corresponding template from service");

                                //TODO: Test the value for the attribute, whether it passes the rule
                                // In case of validation failure, add a error message to the console/log file
                                var attributeId = (int) attributeByName["Id"];
                                var attributeValue = valueListForOneLine[i];
                                if (!ValidateAttributeValue(attributeId, attributeValue, attributesMapForTemplateFromService))
                                    throw new Exception("Invalid attributeValue: " + attributeValue);


                                var configAttributeVal = new ConfigAttribute
                                {
                                    AttributeId = attributeId,
                                    AttributeValue = attributeValue,
                                    Version = 1
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
                        new System.IO.StreamWriter(@"C:\Users\junweihu\Desktop\ConfigurationCreated.txt"))
            {
                file.Write(JsonConvert.SerializeObject(configListFromCsv));
                Console.WriteLine("Finish!");
            }

        }



        private static bool ValidateQualifierValue(int qualifierId, string qualifierValue, Dictionary<int, JObject> qualifiersMapForTemplateFromService)
        {
            var targetQualifier = qualifiersMapForTemplateFromService[qualifierId];
            var qualifierValueList = targetQualifier["Values"].ToList();
            var valid = qualifierValueList.Contains(qualifierValue);
            if (valid == false)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\junweihu\Desktop\TemplateResponseInfo.txt"))
                {
                    file.Write("Invalid qualifier value: " + qualifierValue);
                }

            }
            return true;

        }



        private static bool ValidateAttributeValue(int attributeId, string attributeValue, Dictionary<int, JObject> attributesMapForTemplateFromService)
        {
            var targetAttribute = attributesMapForTemplateFromService[attributeId];
           
            bool success = false;
            using (StreamWriter logWriter = File.AppendText(@"C:\Users\junweihu\Desktop\ConfigurationHelperErrorLog.txt"))
            {
                var attributeTypeString = targetAttribute["AttributeType"].ToString();
                if (string.IsNullOrEmpty(attributeTypeString))
                {
                    logWriter.Write("AttributeType is null or empty");
                    return false;
                }
                var attributeType = (AttributeType)Enum.Parse(typeof(AttributeType), attributeTypeString);
            
                var attributeRule = targetAttribute["Rule"].ToString();
               
                switch (attributeType)
                {
                    case AttributeType.Boolean:
                        bool boolVal;
                        success = Boolean.TryParse(attributeValue, out boolVal);
                        if (success == false)
                        {
                            logWriter.Write(attributeValue + " does not match AttributeRule for AttributeType: Boolean" );
                        }
                        break;

                    case AttributeType.Date:
                        success = ValidateDate(attributeValue);
                        if (success == false)
                        {
                            logWriter.Write(attributeValue + " does not match AttributeRule for AttributeType: Date");
                        }
                        break;

                    case AttributeType.KeyValue:
                        var keyVal = attributeValue.Split(':');
                        if (keyVal.Count() == 2)
                            success = true;
                        if (success == false)
                        {
                            logWriter.Write(attributeValue + " does not match AttributeRule for AttributeType: KeyValue");
                        }
                        break;

                    case AttributeType.Number:
                        success = ValidNumberRange(attributeRule, attributeValue);
                        if (success == false)
                        {
                            logWriter.Write(attributeValue + " does not match AttributeRule for AttributeType: Number");
                        }
                        break;

                    case AttributeType.TimeSeriesNumber:
                        var timeNumberList = attributeValue.Split(',');
                        success = true;
                        foreach (var timeData in timeNumberList)
                        {
                            var timeVal = timeData.Split(':');
                            if (timeVal.Count() != 2)
                            {
                                success = false;
                                break;
                            }

                            success = ValidateDate(timeVal[0]);
                            if (!success)
                            {
                                logWriter.Write(attributeValue + " does not match AttributeRule for AttributeType: TimeSeriesNumber");
                                break;
                            }
                            success = ValidNumberRange(attributeRule, timeVal[1]);
                        }
                        break;
                    case AttributeType.TimeSeriesString:
                        var timeStringList = attributeValue.Split(',');
                        foreach (var timeData in timeStringList)
                        {
                            var timeVal = timeData.Split(':');
                            if (timeVal.Count() != 2)
                            {
                                success = false;
                                logWriter.Write(attributeValue + " does not match AttributeRule for AttributeType: TimeSeriesString");
                                break;
                            }
                            success = ValidateDate(timeVal[0]);
                        }
                        break;
                    case AttributeType.String:
                        success = true;
                        break;
                }
            }

            return success;

        }

        private static bool ValidateDate(string dateString)
        {
            DateTime dateVal;
            return DateTime.TryParse(dateString, out dateVal);
        }

        private static bool ValidNumberRange(string attributeRule, string attributeValue)
        {
            double doubleValue;
            // First check if the type of the attribute value is valid
            bool success = Double.TryParse(attributeValue, out doubleValue);
            if (!success)
                return false;
            // Check if the value we got within the range as specified in attribute definition
            var limits = attributeRule.Split(':');
            if (limits.Count() != 2)
                return false;
            var lowerLimit = Double.Parse(limits[0]);
            var upperLimit = Double.Parse(limits[1]);
            if (doubleValue < lowerLimit || doubleValue > upperLimit)
                return false;

            return true;
        }


        private static bool ValidateCsvFormat(IReadOnlyList<string> attributeOfConfigList, List<JObject> configQualifiersForTemplateFromService, List<JObject> configAttributesForTemplateFromService)
        {
            var qualifiersNameListFromService =
                configQualifiersForTemplateFromService.Select(qualifier => qualifier["Name"]).ToList();
            var attributesNameListFromService =
                configAttributesForTemplateFromService.Select(attribute => attribute["Name"]).ToList();
            var numAttributesForTemplateFromService = configAttributesForTemplateFromService.Count();
            var numQualifierForTemplateFromService = configQualifiersForTemplateFromService.Count();
            var numAttributesOfConfig = attributeOfConfigList.Count();
            //numAttributes - 3 represents thar all attributes of Configuration in csv except Name, StartDate, EndDate 
            if ((numAttributesOfConfig - 3) != configQualifiersForTemplateFromService.ToList().Count + configAttributesForTemplateFromService.ToList().Count)
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
