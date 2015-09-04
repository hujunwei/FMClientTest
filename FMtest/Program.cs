using System;
using System.IO;
using Newtonsoft.Json;
using PlanningServiceHelper;


namespace FMtest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please Input qualifier name: ");
            var qualifierName = Console.ReadLine();
            var resultAzureGeo = FaciliityMasterClient.GetFacilityData(qualifierName);
            QualifierHelper.AddQualifier(qualifierName, resultAzureGeo);
            Console.WriteLine("Finish!");
            Console.ReadLine();
        }


    }
}
