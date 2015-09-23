using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using DataModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlanningServiceHelper
{
    /// <summary>
    ///     Configuration Helper class
    /// </summary>
    public class ConfigurationHelper
    {
        /// <summary>
        ///     The planning configuration service root URL
        /// </summary>
        private const string PlanningConfigServiceRootUrl =
            "https://planningconfigtest.cloudapp.net/";

        /// <summary>
        /// Ingests the configuration to service.
        /// </summary>
        /// <param name="configsList">The configs list.</param>
        public static void IngestConfigurationToService(List<Configuration> configsList)
        {
            var count = 0;
            using (
                var logWriter =
                    new StreamWriter(Directory.GetCurrentDirectory() +
                                     @"\ConfigurationsHelperServiceErrorLog.txt"))
            {
                for (var i = 0; i < configsList.Count; i++)
                {
                    var responseForAdd =
                        AddConfigurationToService(
                            "https://planningconfigtest.cloudapp.net/api/Configuration/add/",
                            configsList[i]);
                    if (!responseForAdd.IsSuccessStatusCode)
                    {
                        //throw new Exception("Adding Configuration to service returns insuccess response");
                        logWriter.WriteLine("[Line: " + i +
                                            "] fails to be added, the Configuration Name is: " +
                                            configsList[i].Name);
                        logWriter.WriteLine("Service returns insuccessful response status code: " +
                                            responseForAdd.StatusCode);
                        Console.WriteLine("[Line: " + i +
                                          "] fails to be added, the Configuration Name is: " +
                                          configsList[i].Name);
                        Console.WriteLine("Service returns insuccessful response status code: " +
                                          responseForAdd.StatusCode);
                        continue;
                    }
                    count++;
                    Console.WriteLine("Added the " + count + "th config, name is: " +
                                      configsList[i].Name);
                }
            }
            Console.WriteLine("Added " + count + " configurations to service");
        }

        /// <summary>
        /// Creates the configuration from CSV.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="templateId">The template identifier.</param>
        /// <returns>A list of configuration instances.</returns>
        public static List<Configuration> CreateConfigurationsFromCsv(string filePath,
            int templateId)
        {
            var configListFromCsv = new List<Configuration>();
            //Get template from service to help validate the csv file is in correct format to create qualifier
            var response = QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId);
            //Parse response as template in JObject
            var templateJObjectFromService = response.Content.ReadAsAsync<JObject>().Result;
            //Get attributes of the template from service
            var attributesOfTemplateFromService =
                QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId +
                             "/attributes")
                    .Content.ReadAsAsync<List<JObject>>()
                    .Result;
            //Get qualifiers of the template from service
            var qualifiersOfTemplateFromService =
                QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId +
                             "/qualifiers")
                    .Content.ReadAsAsync<List<JObject>>()
                    .Result;
            var numAttributesOfTemplateFromService = attributesOfTemplateFromService.Count();
            var numQualifiersOfTemplateFromService = qualifiersOfTemplateFromService.Count();
            //Create the map for attributes and qualifiers keyed by their names
            var attributesMapKeyedByName =
                attributesOfTemplateFromService.ToDictionary(
                    attributeJObject => attributeJObject["Name"].ToString().ToLower());
            var qualifiersMapKeyedByName =
                qualifiersOfTemplateFromService.ToDictionary(
                    qualifierJObject => qualifierJObject["Name"].ToString().ToLower());
            using (var csvReader = new StreamReader(File.OpenRead(filePath)))
            {
                using (
                    var logWriter =
                        new StreamWriter(Directory.GetCurrentDirectory() +
                                         @"\ConfigurationsHelperValidationErrorLog.txt"))
                {
                    //MembersOfConfigLine is the first line of the Csv file, which contains Name, NameOfQuaifiers[], NameOfAttributes[], StartDate, EndDate
                    var membersOfConfigLine = csvReader.ReadLine();
                    if (membersOfConfigLine == null)
                    {
                        throw new Exception("Invalid Csv file for Configuration");
                    }
                    //Split first line to a list
                    var membersOfConfigList =
                        SplitCsv(membersOfConfigLine)
                            .Select(attribute => attribute.ToLower())
                            .ToList();
                    //Validate the first line matches the Template of Configuration from service
                    if (
                        ValidateCsvFormat(membersOfConfigList, qualifiersOfTemplateFromService,
                            attributesOfTemplateFromService) == false)
                    {
                        throw new Exception(
                            "Configuration in csv does not match corresponding template from service");
                    }
                    var lineCount = 1;
                    //Read begin from second line of Csv file, each line contains information for a configuration
                    while (!csvReader.EndOfStream)
                    {
                        var line = csvReader.ReadLine();
                        lineCount++;
                        if (line == null)
                        {
                            continue;
                        }
                        //Split readed line to a list that contains members of a Configuration class
                        var valueListForOneLine = SplitCsv(line).ToList();
                        if (!valueListForOneLine.Any() ||
                            valueListForOneLine.Count != membersOfConfigList.Count)
                        {
                            continue;
                        }
                        //First element is the name for a configuration
                        var configName = valueListForOneLine[0];
                        //CategoryId of a Configuration equals to CategoryId of the Template for this configuration
                        var configCategoryId = (int) templateJObjectFromService["CategoryId"];
                        //Template Id is passed outside of the function
                        var configTemplateId = templateId;
                        //Element in valueListForOneLine before the last one is the StartDate
                        DateTime dateTimeResult;
                        var configStartDate =
                            DateTime.TryParse(valueListForOneLine[valueListForOneLine.Count - 2],
                                out dateTimeResult)
                                ? dateTimeResult
                                : DateTime.Now;
                        //Last element in valueListForOneLine is the EndDate
                        var configEndDate =
                            DateTime.TryParse(valueListForOneLine[valueListForOneLine.Count - 1],
                                out dateTimeResult)
                                ? dateTimeResult
                                : configStartDate.AddDays(36);
                        //Create configQualifier list and configAttribute list for the configuration
                        var configQualifierList = new List<ConfigQualifier>();
                        var configAttributeList = new List<ConfigAttribute>();
                        var passValidation = true;
                        var errorMsg = "";
                        for (var i = 1;
                            i <=
                            numQualifiersOfTemplateFromService + numAttributesOfTemplateFromService;
                            i++)
                        {
                            if (i <= numQualifiersOfTemplateFromService)
                            {
                                //Fetch the qualifier using the map keyed by name
                                var targetQualifierJObject =
                                    qualifiersMapKeyedByName[membersOfConfigList[i]];
                                if (targetQualifierJObject == null)
                                {
                                    throw new Exception(
                                        "Configuration in csv does not match corresponding template from service");
                                }
                                var qualifierId = (int) targetQualifierJObject["Id"];
                                var qualifierValue = valueListForOneLine[i];
                                //Test the value for the Qualifier, whether it is there in the VALUES list
                                //In case of validation failure, add a error message to the console/log file
                                if (!ValidateQualifierValue(qualifierValue, targetQualifierJObject))
                                {
                                    errorMsg += "[Qualifier:" + membersOfConfigList[i] +
                                                "]Invalid qualifierValue: " + qualifierValue;
                                    passValidation = false;
                                    break;
                                }
                                var configQualifierVal = new ConfigQualifier
                                {
                                    QualifierId = qualifierId,
                                    QualifierValue = valueListForOneLine[i]
                                };
                                configQualifierList.Add(configQualifierVal);
                            }
                            else
                            {
                                var targetAttributeJObject =
                                    attributesMapKeyedByName[membersOfConfigList[i]];
                                if (targetAttributeJObject == null)
                                {
                                    throw new Exception(
                                        "Configuration in csv does not match corresponding template from service");
                                }
                                //Fetch the attributes using the map
                                //Test the value for the attribute, whether it passes the rule
                                //In case of validation failure, add a error message to the console/log file
                                var attributeId = (int) targetAttributeJObject["Id"];
                                var attributeValue = valueListForOneLine[i];
                                if (!ValidateAttributeValue(attributeValue, targetAttributeJObject))
                                {
                                    errorMsg += "[Attribute:" + membersOfConfigList[i] +
                                                "]Invalid attributeValue: " + attributeValue;
                                    passValidation = false;
                                    break;
                                }
                                var configAttributeVal = new ConfigAttribute
                                {
                                    AttributeId = attributeId,
                                    AttributeValue = attributeValue,
                                    Version = 1
                                };
                                configAttributeList.Add(configAttributeVal);
                            }
                        }
                        if (passValidation == false)
                        {
                            logWriter.WriteLine("[Line: " + lineCount +
                                                "] could not pass validation, the Configuration Name is: " +
                                                configName);
                            logWriter.WriteLine(errorMsg);
                            continue;
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
                        Console.WriteLine("[Line: " + lineCount + "]" + "Configuration Name: " +
                                          configName + " passes the validation");
                        configListFromCsv.Add(config);
                    }
                }
            }
            return configListFromCsv;
        }

        /// <summary>
        /// Creates the sellable capacity configs from CSV.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="templateId">The template identifier.</param>
        /// <returns>A list of configuration instances.</returns>
        public static List<Configuration> CreateSellableCapacityConfigsFromCsv(string filePath,
            int templateId)
        {
            var scConfigList = new List<Configuration>();
            //Get template from service to help validate the csv file is in correct format to create qualifier
            var response = QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId);
            //Parse response as template in JObject
            var templateJObjectFromService = response.Content.ReadAsAsync<JObject>().Result;
            //Get attributes of the template from service
            var attributesOfTemplateFromService =
                QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId +
                             "/attributes")
                    .Content.ReadAsAsync<List<JObject>>()
                    .Result;
            //Get qualifiers of the template from service
            var qualifiersOfTemplateFromService =
                QueryService(PlanningConfigServiceRootUrl + "api/template/" + templateId +
                             "/qualifiers")
                    .Content.ReadAsAsync<List<JObject>>()
                    .Result;
            var numAttributesOfTemplateFromService = attributesOfTemplateFromService.Count();
            var numQualifiersOfTemplateFromService = qualifiersOfTemplateFromService.Count();
            //Create the map for attributes and qualifiers keyed by their names
            var attributesMapKeyedByName =
                attributesOfTemplateFromService.ToDictionary(
                    attributeJObject => attributeJObject["Name"].ToString().ToLower());
            var qualifiersMapKeyedByName =
                qualifiersOfTemplateFromService.ToDictionary(
                    qualifierJObject => qualifierJObject["Name"].ToString().ToLower());
            using (var csvReader = new StreamReader(filePath))
            {
                using (
                    var logWriter =
                        new StreamWriter(Directory.GetCurrentDirectory() +
                                         @"\ConfigurationsHelperValidationErrorLog.txt"))
                {
                    //Read the schema of Csv
                    var firstLine = csvReader.ReadLine();
                    if (firstLine == null)
                    {
                        throw new Exception("File format Invalid");
                    }
                    var membersOfConfigList = SplitCsv(firstLine).ToList()
                        .Select(attribute => attribute.ToLower())
                        .ToList();
                    var sellableCapacityPerRackIndex =
                        membersOfConfigList.FindIndex(s => s.Equals("sellablecapacityperrack"));
                    var sellableCapacityPerRackStartDateIndex =
                        membersOfConfigList.FindIndex(s => s.Equals("start date"));
                    var sellableCapacityPerRackAttributeId =
                        (int)attributesMapKeyedByName["sellablecapacityperrack"]["Id"];
                    //Read values and form a Map
                    var scMap = new Dictionary<string, Configuration>();
                    var lineCount = 1;
                    while (!csvReader.EndOfStream)
                    {
                        var line = csvReader.ReadLine();
                        lineCount++;
                        if (line == null)
                        {
                            continue;
                        }
                        var valueListForOneLine = SplitCsv(line).ToList();
                        if (!valueListForOneLine.Any() ||
                            valueListForOneLine.Count != membersOfConfigList.Count)
                        {
                            continue;
                        }
                        //Concatenate qualifier value to form a key of map
                        var sb = new StringBuilder();
                        for (var i = 0; i < numQualifiersOfTemplateFromService; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append("," + valueListForOneLine[i]);
                            }
                            else
                            {
                                sb.Append(valueListForOneLine[i]);
                            }
                        }
                        var key = sb.ToString();
                        //If key in the map then update config's CapacityPerRack, StartDate and EndDate
                        if (scMap.ContainsKey(key))
                        {
                            var newTimeSeriesNumber =
                                valueListForOneLine[sellableCapacityPerRackStartDateIndex] + ":" +
                                valueListForOneLine[sellableCapacityPerRackIndex];
                            var config = scMap[key];
                            var errorMsg = "";
                            var sellableCapacityPerRackAttribute =
                                config.AttributeList.Single(
                                    a => a.AttributeId == sellableCapacityPerRackAttributeId);
                            if (
                                !ValidateAttributeValue(newTimeSeriesNumber,
                                    attributesMapKeyedByName["sellablecapacityperrack"]))
                            {
                                errorMsg += "[Attribute: CapacityPerRack" +
                                            "]Invalid attributeValue: " + newTimeSeriesNumber;
                                logWriter.WriteLine("[Line: " + lineCount +
                                                    "] could not pass validation, the Configuration Name is: " +
                                                    config.Name);
                                logWriter.WriteLine(errorMsg);
                                continue;
                            }
                            //Append a new TimeSeriesNumber
                            sellableCapacityPerRackAttribute.AttributeValue += "," +
                                                                               newTimeSeriesNumber;
                            DateTime dateTimeResult;
                            var configStartDate =
                                DateTime.TryParse(
                                    valueListForOneLine[valueListForOneLine.Count - 2],
                                    out dateTimeResult)
                                    ? dateTimeResult
                                    : DateTime.Now;
                            //Update start date and end date
                            var configEndDate =
                                DateTime.TryParse(
                                    valueListForOneLine[valueListForOneLine.Count - 2],
                                    out dateTimeResult)
                                    ? dateTimeResult
                                    : configStartDate.AddDays(36);
                            config.StartDate = configStartDate.CompareTo(config.StartDate) < 0
                                ? configStartDate
                                : config.StartDate;
                            config.EndDate = configStartDate.CompareTo(config.EndDate) > 0
                                ? configEndDate
                                : config.EndDate;
                            scMap[key] = config;
                        }
                        //Else create a new configuration and add to map
                        else
                        {
                            var timeSeriesNumber =
                                valueListForOneLine[sellableCapacityPerRackStartDateIndex] + ":" +
                                valueListForOneLine[sellableCapacityPerRackIndex];
                            var configName = key;
                            var configCategoryId = (int)templateJObjectFromService["CategoryId"];
                            var configTemplateId = templateId;
                            DateTime dateTimeResult;
                            var configStartDate =
                                DateTime.TryParse(
                                    valueListForOneLine[valueListForOneLine.Count - 2],
                                    out dateTimeResult)
                                    ? dateTimeResult
                                    : DateTime.Now;
                            //Update start date and end date
                            var configEndDate =
                                DateTime.TryParse(
                                    valueListForOneLine[valueListForOneLine.Count - 2],
                                    out dateTimeResult)
                                    ? dateTimeResult
                                    : configStartDate.AddDays(36);
                            var configQualifierList = new List<ConfigQualifier>();
                            var configAttributeList = new List<ConfigAttribute>();
                            var passValidation = true;
                            var errorMsg = "";
                            for (var i = 0;
                                i <
                                numQualifiersOfTemplateFromService +
                                numAttributesOfTemplateFromService;
                                i++)
                            {
                                if (i < numQualifiersOfTemplateFromService)
                                {
                                    var qualifierName = membersOfConfigList[i].ToLower();
                                    var targetQualifierJObject =
                                        qualifiersMapKeyedByName[qualifierName];
                                    if (targetQualifierJObject == null)
                                    {
                                        throw new Exception(
                                            "Configuration in csv does not match corresponding template from service");
                                    }
                                    var qualifierId = (int)targetQualifierJObject["Id"];
                                    var qualifierValue = valueListForOneLine[i];
                                    if (
                                        !ValidateQualifierValue(qualifierValue,
                                            targetQualifierJObject))
                                    {
                                        errorMsg += "[Qualifier:" + membersOfConfigList[i] +
                                                    "]Invalid qualifierValue: " + qualifierValue;
                                        passValidation = false;
                                        break;
                                    }
                                    var configQualifierVal = new ConfigQualifier
                                    {
                                        QualifierId = qualifierId,
                                        QualifierValue = qualifierValue
                                    };
                                    configQualifierList.Add(configQualifierVal);
                                }
                                else
                                {
                                    var targetAttributeJObject =
                                        attributesMapKeyedByName[membersOfConfigList[i].ToLower()];
                                    if (targetAttributeJObject == null)
                                    {
                                        throw new Exception(
                                            "Configuration in csv does not match corresponding template from service");
                                    }
                                    var attributeId = (int)targetAttributeJObject["Id"];
                                    var attributeValue = valueListForOneLine[i];
                                    if (targetAttributeJObject["Name"].ToString().ToLower() ==
                                        "sellablecapacityperrack")
                                    {
                                        attributeValue = timeSeriesNumber;
                                    }
                                    if (
                                        !ValidateAttributeValue(attributeValue,
                                            targetAttributeJObject))
                                    {
                                        errorMsg += "[Attribute:" + membersOfConfigList[i] +
                                                    "]Invalid attributeValue: " + attributeValue;
                                        passValidation = false;
                                        break;
                                    }
                                    var configAttributeVal = new ConfigAttribute
                                    {
                                        AttributeId = attributeId,
                                        AttributeValue = attributeValue,
                                        Version = 1
                                    };
                                    configAttributeList.Add(configAttributeVal);
                                }
                            }
                            if (passValidation == false)
                            {
                                logWriter.WriteLine("[Line: " + lineCount +
                                                    "] could not pass validation, the Configuration Name is: " +
                                                    configName);
                                logWriter.WriteLine(errorMsg);
                                continue;
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
                            scMap.Add(key, config);
                        }
                    }
                    //Process Map and create sellable capacity configuration list
                    scConfigList.AddRange(
                        scMap.Keys.Select(
                            key =>
                                FormatCapacityPerRackValue(scMap[key],
                                    sellableCapacityPerRackAttributeId)));
                }
            }
            return scConfigList;
        }

        /// <summary>
        /// Validates the qualifier value.
        /// </summary>
        /// <param name="qualifierValue">The qualifier value.</param>
        /// <param name="qualifierJObject">The qualifier j object.</param>
        /// <returns>Whether qualifier value is in the qualifier valueList.</returns>
        private static bool ValidateQualifierValue(string qualifierValue,
            JObject qualifierJObject)
        {
            //Check qualifier value is empty?
            if (string.IsNullOrEmpty(qualifierValue))
            {
                return false;
            }
            var qualifierValueList = qualifierJObject["Values"].ToList();
            return
                qualifierValueList.Any(
                    t => t.ToString().ToLower().Contains(qualifierValue.ToLower()));
        }

        /// <summary>
        /// Validates the attribute value.
        /// </summary>
        /// <param name="attributeValue">The attribute value.</param>
        /// <param name="attributeJObject">The attribute j object.</param>
        /// <returns>
        /// Boolean: whether the attribute value comply with attribute rule
        /// </returns>
        private static bool ValidateAttributeValue(string attributeValue,
            JObject attributeJObject)
        {
            var isSuccess = false;
            var attributeTypeString = attributeJObject["AttributeType"].ToString();
            if (string.IsNullOrEmpty(attributeTypeString))
            {
                return false;
            }
            var attributeType =
                (AttributeType) Enum.Parse(typeof (AttributeType), attributeTypeString);
            var attributeRule = attributeJObject["Rule"].ToString();
            switch (attributeType)
            {
                case AttributeType.Boolean:
                    bool boolVal;
                    isSuccess = bool.TryParse(attributeValue, out boolVal);
                    break;
                case AttributeType.Date:
                    isSuccess = ValidateDate(attributeValue);
                    break;
                case AttributeType.KeyValue:
                    var keyVal = attributeValue.Split(':');
                    if (keyVal.Count() == 2)
                    {
                        isSuccess = true;
                    }
                    break;
                case AttributeType.Number:
                    isSuccess = ValidNumberRange(attributeRule, attributeValue);
                    break;
                case AttributeType.TimeSeriesNumber:
                    var timeNumberList = attributeValue.Split(',');
                    isSuccess = true;
                    foreach (var timeData in timeNumberList)
                    {
                        var timeVal = timeData.Split(':');
                        if (timeVal.Count() != 2)
                        {
                            isSuccess = false;
                            break;
                        }
                        isSuccess = ValidateDate(timeVal[0]);
                        if (!isSuccess)
                        {
                            break;
                        }
                        isSuccess = ValidNumberRange(attributeRule, timeVal[1]);
                    }
                    break;
                case AttributeType.TimeSeriesString:
                    var timeStringList = attributeValue.Split(',');
                    foreach (var timeVal in timeStringList.Select(timeData => timeData.Split(':')))
                    {
                        if (timeVal.Count() != 2)
                        {
                            isSuccess = false;
                            break;
                        }
                        isSuccess = ValidateDate(timeVal[0]);
                    }
                    break;
                case AttributeType.String:
                    isSuccess = true;
                    break;
                default:
                    return false;
            }
            return isSuccess;
        }

        /// <summary>
        ///     Validates the date.
        /// </summary>
        /// <param name="dateString">The date string.</param>
        /// <returns>Boolean: whether the date is a correct DateTime.</returns>
        private static bool ValidateDate(string dateString)
        {
            DateTime dateVal;
            return DateTime.TryParse(dateString, out dateVal);
        }

        /// <summary>
        ///     Valids the number range.
        /// </summary>
        /// <param name="attributeRule">The attribute rule.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns>Boolean: whether the number in the range that specified by rule.</returns>
        private static bool ValidNumberRange(string attributeRule, string attributeValue)
        {
            double doubleValue;
            // First check if the type of the attribute value is valid
            var success = double.TryParse(attributeValue, out doubleValue);
            if (!success)
            {
                return false;
            }
            // Check if the value we got within the range as specified in attribute definition
            var limits = attributeRule.Split(':');
            if (limits.Count() != 2)
            {
                return false;
            }
            var lowerLimit = double.Parse(limits[0]);
            var upperLimit = double.Parse(limits[1]);
            if (doubleValue < lowerLimit || doubleValue > upperLimit)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Validates the CSV format.
        /// </summary>
        /// <param name="attributeOfConfigList">The attribute of configuration list.</param>
        /// <param name="configQualifiersForTemplateFromService">The configuration qualifiers for template from service.</param>
        /// <param name="configAttributesForTemplateFromService">The configuration attributes for template from service.</param>
        /// <returns>Boolean: whether the first line of csv file matches template of the configuration.</returns>
        private static bool ValidateCsvFormat(IReadOnlyList<string> attributeOfConfigList,
            List<JObject> configQualifiersForTemplateFromService,
            List<JObject> configAttributesForTemplateFromService)
        {
            var qualifiersNameListFromService =
                configQualifiersForTemplateFromService.Select(
                    qualifier => qualifier["Name"].ToString().ToLower())
                    .ToList();
            var attributesNameListFromService =
                configAttributesForTemplateFromService.Select(
                    attribute => attribute["Name"].ToString().ToLower())
                    .ToList();
            var numAttributesForTemplateFromService = configAttributesForTemplateFromService.Count();
            var numQualifierForTemplateFromService = configQualifiersForTemplateFromService.Count();
            var numAttributesOfConfig = attributeOfConfigList.Count();
            //numAttributes - 3 represents thar all attributes of Configuration in csv except Name, StartDate, EndDate 
            if ((numAttributesOfConfig - 3) !=
                configQualifiersForTemplateFromService.ToList().Count +
                configAttributesForTemplateFromService.ToList().Count)
            {
                return false;
            }
            //Check whether corresponding template of configuration contains name of qualifiers and attributes from Csv  
            for (var i = 1;
                i <= numQualifierForTemplateFromService + numAttributesForTemplateFromService;
                i++)
            {
                if (i <= numQualifierForTemplateFromService)
                {
                    if (!qualifiersNameListFromService.Contains(attributeOfConfigList[i]))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!attributesNameListFromService.Contains(attributeOfConfigList[i]))
                    {
                        return false;
                    }
                }
            }
            //Last two string should be "Start Date" and "End Date"
            if (!attributeOfConfigList[attributeOfConfigList.Count - 2].Contains("start date"))
            {
                return false;
            }
            if (!attributeOfConfigList[attributeOfConfigList.Count - 1].Contains("end date"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Adds the configuration to planning configuration service.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Invalid url</exception>
        /// <exception cref="System.Exception">Calling servise insuccess</exception>
        private static HttpResponseMessage AddConfigurationToService(string url,
            Configuration config)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("Invalid url");
            }
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(config),
                        Encoding.UTF8,
                        "application/json");
                    var response = client.SendAsync(request, CancellationToken.None).Result;
                    return response;
                }
            }
        }

        /// <summary>
        ///     Queries the planning configuration service.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Response from the service</returns>
        /// <exception cref="System.ArgumentException">Invalid url</exception>
        /// <exception cref="System.Exception">Calling servise insuccess</exception>
        private static HttpResponseMessage QueryService(string url)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("Invalid url");
            }
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    var response = client.SendAsync(request, CancellationToken.None).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Query servise returns insuccess insuccess");
                    }
                    return response;
                }
            }
        }

        /// <summary>
        ///     Splits the CSV.
        /// </summary>
        /// <param name="csvString">The CSV string.</param>
        /// <returns>List of string values</returns>
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
                    {
                        quoted = false;
                    }
                    else
                    {
                        sb.Append(c);
                    }
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
            {
                throw new ArgumentException("csvString has unterminated quotation mark.");
            }
            yield return sb.ToString();
        }

        /// <summary>
        /// Formats the SellableCapacityPerRack value.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="sellableCapacityPerRackAttributeId">The sellable capacity per rack attribute identifier.</param>
        /// <returns>Configuration instance that SellableCapacityPerRack was formatted.</returns>
        private static Configuration FormatCapacityPerRackValue(Configuration config,
            int sellableCapacityPerRackAttributeId)
        {
            var sellableCapacityPerRackAttribute =
                config.AttributeList.Single(a => a.AttributeId == sellableCapacityPerRackAttributeId);
            var sellableCapacityPerRackValue = sellableCapacityPerRackAttribute.AttributeValue;
            var valList = sellableCapacityPerRackValue.Split(',');
            var valSet = new HashSet<string>();
            for (var i = 0; i < valList.Count(); i++)
            {
                valSet.Add(valList[i]);
            }
            var valSortedList =
                valSet.ToList()
                    .OrderBy(v => Convert.ToDateTime(v.Split(':').ToList()[0].ToString()))
                    .ToList();
            var sb = new StringBuilder();
            for (var i = 0; i < valSortedList.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append("," + valSortedList[i]);
                }
                else
                {
                    sb.Append(valSortedList[i]);
                }
            }
            sellableCapacityPerRackAttribute.AttributeValue = sb.ToString();
            return config;
        }
    }
}