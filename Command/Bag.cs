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
        public List<string> Tiles(string url)
        {
            List<string> tileNums = new List<string>();
            try
            {
                // Create an HttpClient and send the request
                WebClient client = new WebClient();
                string response = client.DownloadString(url);
                //https://data.3dbag.nl/api/BAG3D_v2/wfs?&request=GetFeature&typeName=AG3D_v2:bag_tiles_3k&outputFormat=json&bbox=261000,525000,262000,527000;
                // Read the response as a string
                //string responseString = await response.Content.ReadAsStringAsync();
                dynamic responseJson = JsonConvert.DeserializeObject(response);
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

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            List<string> tileNums = Tiles("https://data.3dbag.nl/api/BAG3D_v2/wfs?&request=GetFeature&typeName=AG3D_v2:bag_tiles_3k&outputFormat=json&bbox=261000,525000,262000,527000");
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
            return Result.Succeeded;

        }
    }
}
