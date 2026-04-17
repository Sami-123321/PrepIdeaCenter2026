using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using PrepIdeCenter.Models;
using PrepIdeCenter.Models;

namespace ExamPrepIdeaCenter
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatatedIdeaId;//за Orer2, правим променлива, в която ще пазим последната създадена идеа

        private const string BaseUrl = "http://144.91.123.158:82";
        private const string LoginEmail = "Exam123@example.com";
        private const string LoginPassword = "Exam123";

        [OneTimeSetUp]
        public void Setup()
        {
            // ❗ винаги взимаме нов валиден token
            string jwtToken = GetJwtToken(LoginEmail, LoginPassword);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);

            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Auth failed: {response.StatusCode}, {response.Content}");
            }

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // ✔️ ВАЖНО: правилното поле е accessToken
            var token = json.GetProperty("accessToken").GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("Token is empty!");
            }

            return token;
        }
        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaData = new IdeaDTO
            {
                Title = "Test Idea " + Guid.NewGuid(),
                Description = "This is a test idea description.",
                Url = null
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);

            var response = client.Execute(request);

            // 🔍 DEBUG (важно за изпит)
            Console.WriteLine("Status: " + response.StatusCode);
            Console.WriteLine("Content: " + (response.Content ?? "NULL"));

            // ✔️ проверка за статус
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)
                .Or.EqualTo(HttpStatusCode.NoContent));

            // ✔️ ако има body → парсваме
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var result = JsonSerializer.Deserialize<JsonElement>(response.Content);

                if (result.TryGetProperty("msg", out var msg))
                {
                    Assert.That(msg.GetString(), Does.Contain("Success"));
                }
            }
        }
        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItems, Is.Not.Empty);
            Assert.That(responseItems, Is.Not.Null);


            lastCreatatedIdeaId = responseItems.LastOrDefault()?.Id;


        }

        [Order(3)]
        [Test]
        public void EditExistIdea_ShouldReturnSuccess()
        {
            var editedRequestData = new IdeaDTO
            {
                Title = "Edited Idea ",
                Description = "This is a edited idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatatedIdeaId);
            request.AddJsonBody(editedRequestData);

            var response = this.client.Execute(request);
            Console.WriteLine("===== RESPONSE =====");
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.Content);

            var editedResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(editedResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatatedIdeaId);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 ok.");
            //Assert.That(response.Content, Is.EqualTo("\"The idea is deleted!\""));
            //Assert.That(response.Content, Does.Contain("The idea is deleted!"));
            Assert.That(response.Content.Trim('"'), Is.EqualTo("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void CreateIdea_WithMissingRequeredFields_ShouldReturnBedRequest()
        {
            var ideaData = new IdeaDTO
            {
                Title = "",
                Description = "This is a test idea description",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);

            request.AddJsonBody(ideaData);

            var response = this.client.Execute(request);

            Console.WriteLine("===== RESPONSE =====");
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Reaquest");
        }

        [Order(6)]
        [Test]
        public void EditNotExistingIdea_ShouldReturnBadRequest()
        {
            string notExistingIdeaId = "99999";
            var editRequestData = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is a edited idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", notExistingIdeaId);
            request.AddJsonBody(editRequestData); // ✅ ВАЖНО


            var response = this.client.Execute(request);

            Console.WriteLine("===== RESPONSE =====");
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");

        }

        [Order(7)]
        [Test]
        public void DeleteNotExistingIdea_ShouldReturnNotFound()
        {
            string notExistingIdeaId = "99999";

            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", notExistingIdeaId);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 oBad Request.");
            Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client?.Dispose();
        }
    }
}