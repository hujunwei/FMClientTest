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
            //Console.WriteLine("Please Input qualifier name: ");
            //var qualifierName = Console.ReadLine();
            //var resultAzureGeo = FaciliityMasterClient.GetFacilityData(qualifierName);
            //QualifierHelper.AddQualifier(qualifierName, resultAzureGeo);
            //Console.WriteLine("Finish!");
            //Console.ReadLine();





            //var responseObject = 
            //var templateJToken = responseObject["Template"];
            //var templateRetrived = (ConfigTemplate)JsonConvert.DeserializeObject(templateJToken.ToString(), typeof(ConfigTemplate));



            //var client = new HttpClient();
            //using (
            //    var request = new HttpRequestMessage(HttpMethod.Get,
            //        "https://planningconfigtest.cloudapp.net/api/template/9"))
            //{
            //    using (var response = client.SendAsync(request, CancellationToken.None).Result)
            //    {


            //        var responseObject = response.Content.ReadAsAsync<JObject>().Result;


            //        using (System.IO.StreamWriter file =
            //            new System.IO.StreamWriter(@"C:\Users\junweihu\Desktop\TemplateResponseInfo.txt"))
            //        {
            //            file.Write(responseObject.ToString());
            //        }

            //    }
               

            //}
            //var response = ConfigurationHelper.QueryService("https://planningconfigtest.cloudapp.net/api/template/9");
            //var responseObject = response.Content.ReadAsAsync<JObject>().Result;
            //Console.WriteLine(responseObject["Name"]);
            

            ConfigurationHelper.CreateConfigurationFromCsv(@"C:\Users\junweihu\Desktop\Configurations.csv", 9);
          

        }
    }
}
