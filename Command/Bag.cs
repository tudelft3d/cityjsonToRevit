using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace cityjsonToRevit
{
    [Transaction(TransactionMode.Manual)]

    class Bag : IExternalCommand
    {
        static async Task<List<string>> Main(string url)
        {
            List<string> tileNums = new List<string>();
            try
            {
                // Create an HttpClient and send the request
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                //https://data.3dbag.nl/api/BAG3D_v2/wfs?&request=GetFeature&typeName=AG3D_v2:bag_tiles_3k&outputFormat=json&bbox=261000,525000,262000,527000;
                // Read the response as a string
                string responseString = await response.Content.ReadAsStringAsync();
                dynamic responseJson = JsonConvert.DeserializeObject(responseString);
                foreach (var feature in responseJson.features)
                {
                    tileNums.Add(feature.properties.tile_id.ToString());
                }
            }
           
            catch
            {
                TaskDialog.Show("Error", "An error occurred while trying to download the files. Please check your internet connection and try again. ");
            }
            return tileNums;
        }

        public async Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            List<string> tileNums = await Main("https://data.3dbag.nl/api/BAG3D_v2/wfs?&request=GetFeature&typeName=AG3D_v2:bag_tiles_3k&outputFormat=json&bbox=261000,525000,262000,527000");
            string cjUrl = "https://data.3dbag.nl/cityjson/v210908_fd2cee53/3dbag_v210908_fd2cee53_";
            foreach (string tileNum in tileNums)
            {

                string cjUrlAll = cjUrl + tileNum + ".json" + ".gz";
                string gzFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TEMP\\" + tileNum + ".gz";
                if (!File.Exists(gzFile))
                {
                    using (var client2 = new WebClient())
                    {
                        client2.DownloadFile(cjUrlAll, gzFile);
                    }
                }
                using (FileStream fileToDecompressAsStream = new FileStream(gzFile, FileMode.Open))
                using (GZipStream decompressionStream = new GZipStream(fileToDecompressAsStream, CompressionMode.Decompress))
                using (StreamReader sr = new StreamReader(decompressionStream))
                {
                    string json = sr.ReadToEnd();
                    var json_obj = JsonConvert.DeserializeObject(json);
                }
            }

        }
    }
}
