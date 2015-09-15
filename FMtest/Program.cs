using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using DataModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlanningServiceHelper;


namespace FMtest
{
    class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Please Input qualifier name: ");
            var qualifierName = Console.ReadLine();
            var resultAzureGeo = FaciliityMasterClient.GetFacilityData(qualifierName);
            QualifierHelper.IngestQualifierToService("AzureGeo", "null", resultAzureGeo);
            Console.WriteLine("Finish!");
            Console.ReadLine();



            //var configListCreatedFromCsv = ConfigurationHelper.CreateConfigurationsFromCsv(@"C:\Users\junweihu\Desktop\Milestone Lead Time.csv", 43);
            //Console.WriteLine("Sending " + configListCreatedFromCsv.Count + " configs to service..");
            ////ConfigurationHelper.IngestConfigurationToService(configListCreatedFromCsv);

            //Console.WriteLine("Done!");




            //var qualifierIds = new List<int> { 40,34,4,32,37 };
            //var attributeIds = new List<int> { 1114,8,9,1073 };
            //var template = TemplateHelper.CreateTemplate("Milestone Lead Time Global", true, 7, attributeIds, qualifierIds);
            //var response = TemplateHelper.AddTemplateToService("https://planningconfigtest.cloudapp.net/api/template/add", template);


            //AttributeHelper.IngestAttributeToService("LeadTimeCategory", false, "null", AttributeType.String);





        }
    }
}
