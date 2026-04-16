using System;
using System.Net;
using System.Text.Json;
using Foody.Tests.DTOs;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
using RestSharp;
using RestSharp.Authenticators;


namespace Foody
{
    public class FoodyTests
    {
        private RestClient client;
        private static string foodId;

        [OneTimeSetUp] //веднъж го конфигурираме преди всички тестове
        public void Setup()
        {
            string jwtToken = GetJwtToken("userNameMartina", "123456");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:81")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:81");
            RestRequest restRequest = new RestRequest("/api/User/Authentication", Method.Post);
            restRequest.AddJsonBody(new { username, password });

            RestResponse respone = client.Execute(restRequest);

            if (respone.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(respone.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrEmpty(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }

                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authentication. Status code: {respone.StatusCode}, Resposne: {respone.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateFood_WithRequiredFields_ShouldSuccess()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "Soup",
                Description = "Soup with pepar and tomato",
                Url = ""
            };

            RestRequest request = new RestRequest("api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            RestResponse response = client.Execute(request);

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            //readyResponse съдържа Msg and FoodId      

            if (readyResponse.FoodId != null)
            {
                foodId = readyResponse.FoodId;
            }

            //проверка дали статус кода е 200 ОК
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        }

        [Order(2)]
        [Test]
        public void EditCreatedFoodTitle_ShouldSuccess()
        {
            RestRequest request = new RestRequest($"/api/Food/Edit/{foodId}", Method.Patch);
            request.AddJsonBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Chicken Soup"
                }
            });

            RestResponse response = client.Execute(request);
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(readyResponse.Msg, Is.EqualTo("Successfully edited"));
        }

        [Order(3)]
        [Test]
        public void GetAllFoods_ShouldSuccess()
        {
            RestRequest request = new RestRequest("/api/Food/All", Method.Get);
            RestResponse response = client.Execute(request);

            List<FoodDTO> foods = JsonSerializer.Deserialize<List<FoodDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.Not.Null.And.Not.Empty);
            Assert.That(foods, Is.Not.Null.And.Not.Empty);
            Assert.That(foods.Count, Is.GreaterThanOrEqualTo(1));

        }

        [Order(4)]
        [Test]
        public void DeleteFood_ShouldSuccess()
        {
            RestRequest request = new RestRequest($"/api/Food/Delete/{foodId}", Method.Delete);
            RestResponse response = client.Execute(request);

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(readyResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateFoodWithoutRequiredFields_ShouldReturnBadRequest()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "",
                Description = "Soup with pepar and tomato",
                Url = ""
            };

            RestRequest request = new RestRequest("api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            RestResponse response = client.Execute(request);

            //ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            //проверка дали статус кода е 200 ОК
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {

            string nonExistingId = "999999999";
            RestRequest request = new RestRequest($"/api/Food/Edit/{nonExistingId}", Method.Patch);
            request.AddJsonBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Chicken Soup"
                }
            });

            RestResponse response = client.Execute(request);
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(readyResponse.Msg, Is.EqualTo("No food revues..."));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingFood_ShouldReturnBadRequest()
        {
            string nonExistingId = "999999999";
            RestRequest request = new RestRequest($"/api/Food/Delete/{nonExistingId}", Method.Delete);
            RestResponse response = client.Execute(request);

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to delete this food revue!"));
        }

        [OneTimeTearDown] //веднъж разчистваме след изпълнението на всички тестове 
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}