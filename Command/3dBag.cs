using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.IO.Compression;

namespace cityjsonToRevit
{
    class _3dBag
    {
        static async Task Main(string[] args)
        {
            // Create an HttpClient and send the request
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://data.3dbag.nl/api/BAG3D_v2/wfs?&request=GetFeature&typeName=AG3D_v2:bag_tiles_3k&outputFormat=json&bbox=261000,525000,262000,527000");
            // Read the response as a string
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            dynamic responseJson = JsonConvert.DeserializeObject(responseString);
            List<string> tileNums = new List<string>();
            foreach (var feature in responseJson.features)
            {
                tileNums.Add(feature.properties.tile_id.ToString());
            }
            string cjUrl = "https://data.3dbag.nl/cityjson/v210908_fd2cee53/3dbag_v210908_fd2cee53_";
            foreach (string tileNum in tileNums)
            {
                string cjUrlAll = cjUrl + tileNum + ".json" + ".gz";
                string gzFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TEMP\\" + tileNum + ".gz";
                using (var client2 = new WebClient())
                {
                    client2.DownloadFile(cjUrlAll, gzFile);
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
