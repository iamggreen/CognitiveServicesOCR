using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace HackathonPOC
{
    static class Program
    {

        // Use your own subscription key
        static string subscriptionKey = File.ReadAllText("subscriptionKey.txt");

        // Update this to your region specific url
        const string uriBase = "https://eastus.api.cognitive.microsoft.com/vision/v1.0/ocr";

        // This value is relative to the Images directory
        const string relativeImagePath = @"DriversLicense\2.jpg";

        static string imageFilePath = GetImageFilePath(relativeImagePath);
        static string outputDirectory = GetOutputDirectory(imageFilePath);

        [STAThread]
        static void Main()
        {
            string jsonResult = MakeOCRRequest(imageFilePath);

            CopyImage(imageFilePath);
            WriteJsonToFile(jsonResult);
            WriteFormattedResultsToFile(jsonResult);

            Console.WriteLine("\nDone! Press Enter to exit...\n");
            Console.ReadLine();
        }

        private static string GetImageFilePath(string relativeImagePath)
        {
            string executableDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string imageDirectoryPath = Path.Combine(executableDirectory, @"..\..\Images");

            return Path.GetFullPath(Path.Combine(imageDirectoryPath, relativeImagePath));
        }

        private static string GetOutputDirectory(string imageFilePath)
        {
            string executableDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string resultsDirectoryPath = Path.Combine(executableDirectory, @"..\..\..\Results");

            string outputDirectory = Path.GetFullPath(Path.Combine(resultsDirectoryPath, new DirectoryInfo(imageFilePath).Parent.Name));

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            return outputDirectory;
        }

        static string MakeOCRRequest(string imageFilePath)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters.
            string requestParameters = "language=unk&detectOrientation=true";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = client.PostAsync(uri, content).Result;

                // Get the JSON response.
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        static void CopyImage(string imageFilePath)
        {
            File.Copy(imageFilePath, Path.Combine(outputDirectory, $"Image{Path.GetExtension(imageFilePath)}"), true);
        }

        static void WriteJsonToFile(string json)
        {
            string prettyJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);

            File.WriteAllText(Path.Combine(outputDirectory, "Result.json"), prettyJson);
        }

        static void WriteFormattedResultsToFile(string json)
        {
            Model model = JsonConvert.DeserializeObject<Model>(json);

            StringBuilder output = new StringBuilder();

            foreach (var region in model.regions)
            {
                foreach (var line in region.lines)
                {
                    output.AppendLine(string.Join(" ", line.words.Select(word => word.text)));
                }

                output.AppendLine()
                    .AppendLine();
            }

            string result = output.ToString().Trim();

            File.WriteAllText(Path.Combine(outputDirectory, "FormattedResult.txt"), result);
        }
    }
}