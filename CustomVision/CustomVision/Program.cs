using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CustomVision
{
    class Program
    {
        private const string projectId = "459ede4d-5049-49b2-b58d-e7563fb9e80a";
        private const string trainingEndpoint =
            "https://southcentralus.api.cognitive.microsoft.com/customvision/v2.2/Training/";
        private const string trainingKey = "5f9e9b0b8ff441e691df62404b940be2";
        private const string predictionEndpoint =
            "https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction/";
        private const string predictionKey = "331e420747514e08bf8b9bd7543dc510";
        private const string textEndpoint = "https://northeurope.api.cognitive.microsoft.com/text/analytics/v2.0";
        private const string textKey = "d633dc19113f470c96d1acd34afa7d19";

        private const string imageUrlsFile = "https://azurebootcampmxa.blob.core.windows.net/downloads/imageUrls.txt";

        private static readonly HttpClient httpClient = new HttpClient();

        private static async Task Main(string[] args)
        {
            // Set the headers required for REST API calls

            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/*");
            httpClient.DefaultRequestHeaders.Add("Training-Key", trainingKey);
            httpClient.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", textKey);

            // Extract image URLs from the file
            
            var imageUrls = (await httpClient.GetStringAsync(imageUrlsFile)).Split('\r', '\n')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            // Fetch all the tags from Custom Vision
            
            var tags = JArray.Parse(await httpClient.GetStringAsync($"{trainingEndpoint}projects/{projectId}/tags"))
                .Select(tag => new {id = tag.Value<string>("id"), name = tag.Value<string>("name")})
                .ToList();

            // Get the IDs of the tags we're interested in

            var axesId = tags.First(t => t.name == "Axes").id;
            var bootsId = tags.First(t => t.name == "Boots").id;
            var goodId = tags.First(t => t.name == "Good").id;
            var badId = tags.First(t => t.name == "Bad").id;

            // Upload 50 each of the axes and boots

            await UploadTaggedImages(imageUrls, axesId, "/gear_images/axes/", 50);
            await UploadTaggedImages(imageUrls, bootsId, "/gear_images/boots/", 50);

            // Train the model

            await Train();

            Console.WriteLine("Please set the new iteration as default in Custom Vision and press any key.");
            Console.ReadKey();

            // Check that an axe and a boot are correctly predicted

            var testAxe = imageUrls.Last(url => url.Contains("/gear_images/axes"));
            var testBoot = imageUrls.Last(url => url.Contains("/gear_images/boots"));

            Console.WriteLine(await PredictImageTag(testAxe));
            Console.WriteLine(await PredictImageTag(testBoot));

            Console.WriteLine("Please reset Custom Vision project and press any key.");
            Console.ReadKey();

            // Tag some images with like / dislike

            await UploadSentiment(imageUrls, goodId, badId, "/gear_images/boots/", 15);

            Console.WriteLine("Please set the new iteration as default in Custom Vision and press any key");
            Console.ReadKey();

            // Predict sentiment on a boot

            Process.Start(new ProcessStartInfo {FileName = testBoot, UseShellExecute = true});
            Console.WriteLine(await PredictImageTag(testBoot));
        }

        private static async Task UploadTaggedImages(
            IEnumerable<string> imageUrls,
            string tagId,
            string tagPath,
            int numberToUpload)
        {
            foreach (var imageUrl in imageUrls.Where(url => url.Contains(tagPath)).Take(numberToUpload))
            {
                await UploadTaggedImage(tagId, imageUrl);
            }
        }

        private static async Task UploadTaggedImage(string tagId, string imageUrl)
        {
            var imageInfo = new {images = new[] {imageUrl}.Select(img => new {url = img, tagIds = new[] {tagId}})};

            var response = await httpClient.PostAsync(
                $"{trainingEndpoint}projects/{projectId}/images/urls",
                new StringContent(JsonConvert.SerializeObject(imageInfo), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException(await response.Content.ReadAsStringAsync());
            }
        }

        private static async Task Train()
        {
            var response = await httpClient.PostAsync(
                $"{trainingEndpoint}projects/{projectId}/train",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        private static async Task<string> PredictImageTag(string imageUrl)
        {
            var imageInfo = new {Url = imageUrl};

            var response = await httpClient.PostAsync(
                $"{predictionEndpoint}{projectId}/url",
                new StringContent(JsonConvert.SerializeObject(imageInfo), Encoding.UTF8, "application/json"));

            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException(responseText);
            }

            return imageUrl + ": " + string.Join(
                ", ",
                JObject.Parse(responseText)["predictions"]
                    .Select(
                        tag => new
                        {
                            name = tag.Value<string>("tagName"), probability = tag.Value<decimal>("probability")
                        })
                    .Select(tag => $"{tag.name}={tag.probability}"));
        }

        private static async Task<double> GetSentiment(string text)
        {
            var textInfo = new {documents = new[] {new {language = "en", id = "1", text}}};

            var response = await httpClient.PostAsync(
                $"{textEndpoint}/sentiment",
                new StringContent(JsonConvert.SerializeObject(textInfo), Encoding.UTF8, "application/json"));

            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException(responseText);
            }

            return JObject.Parse(responseText)["documents"].First().Value<double>("score");
        }

        private static async Task UploadSentiment(List<string> imageUrls, string goodId, string badId, string tagPath, int numberToUpload)
        {
            foreach (var imageUrl in imageUrls.Where(url => url.Contains(tagPath)).Take(numberToUpload))
            {
                Process.Start(new ProcessStartInfo {FileName = imageUrls[0], UseShellExecute = true});
                Console.Write($"What do you think of {imageUrl}? ");
                var answer = Console.ReadLine();
                var sentiment = await GetSentiment(answer);
                var tagId = sentiment > 0.5 ? goodId : badId;
                await UploadTaggedImage(tagId, imageUrl);
            }
        }
    }
}