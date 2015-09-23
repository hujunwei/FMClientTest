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
            var resultAzureRegion = FaciliityMasterClient.GetFacilityData("AzureRegion");
            QualifierHelper.IngestQualifierToService(qualifierName, "null", resultAzureRegion);
            Console.WriteLine("Finish!");
            Console.ReadLine();


            var configListCreatedFromCsv = ConfigurationHelper.CreateConfigurationsFromCsv(@"C:\Users\junweihu\Desktop\Milestone Lead Time.csv", 7);
            Console.WriteLine("Sending " + configListCreatedFromCsv.Count + " configs to service..");
            ConfigurationHelper.IngestConfigurationToService(configListCreatedFromCsv);
            using (var fileWriter = new StreamWriter(@"C:\Users\junweihu\Desktop\Sellable capacity data.txt"))
            {
                fileWriter.Write(JsonConvert.SerializeObject(configListCreatedFromCsv));
            }
            Console.WriteLine("Done!");



            //AttributeHelper.IngestAttributeToService("LeadTimeCategory", false, "null", AttributeType.String);

        }
    }
}
