using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;
using Stripe.Checkout;

public class StripeOptions
{
    public string option { get; set; }
}

namespace server.Controllers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
              //.UseUrls("http://0.0.0.0:4242")
              //.UseWebRoot("public")
              .UseStartup<Startup>()
              .Build()
              .Run();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddNewtonsoftJson();
        }
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var keyVaultUrl = new Uri("https://YWDKeyVault.vault.azure.net/");
            var options = new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true
            };
            var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential(options));

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var keyVaultSecretName = environment == "Development" ? "LocalApiKey" : "ProductionApiKey";

            // Example: Retrieve a secret
            KeyVaultSecret secret = await client.GetSecretAsync(keyVaultSecretName);
            string secretValue = secret.Value;

            // This is a public sample test API key.
            // Don’t submit any personally identifiable information in requests made with this key.
            // Sign in to see your own test API key embedded in code samples.
            StripeConfiguration.ApiKey = secretValue;
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            app.UseRouting();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }

    [Route("ping")]
    [ApiController]
    public class PingController : Controller
    {
        [HttpGet]
        public string Ping()
        {
            return "Pong";
        }
    }


    [Route("create-checkout-session")]
    [ApiController]
    public class CheckoutApiController : Controller
    {
        [HttpPost]
        public ActionResult Create()
        {
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    // Provide the exact Price ID (for example, pr_1234) of the product you want to sell
                    Price = "price_1OilRIJxMgmHNB6PhTITdz2P",
                    Quantity = 1,
                  },
                },
                Mode = "payment",
                SuccessUrl = Helpers.GetFullUrlToStaticFile(HttpContext.Request, "success.html"),
                CancelUrl = Helpers.GetFullUrlToStaticFile(HttpContext.Request, "checkout.html"),
            };
            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
    }

    public static class Helpers
    {
        public static string GetFullUrlToStaticFile(HttpRequest request, string relativeFilePath)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var fullUrl = $"{baseUrl}/{relativeFilePath.TrimStart('/')}";
            return fullUrl;
        }
    }


    [ApiController]
    public class FeedbackController : Controller
    {
        [HttpPost("/send-feedback")]
        public IActionResult SendFeedback(string feedback)
        {
            try
            {
                var message = new MailMessage();
                message.To.Add(new MailAddress("yogadanny17@gmail.com"));  // Replace with your email address
                message.From = new MailAddress("yogadanny17@gmail.com");  // Replace with your server email address
                message.Subject = "New Feedback from Customer";
                message.Body = "test";

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = "smtp.gmail.com"; // For example, smtp.gmail.com for Gmail
                    smtpClient.Port = 587;
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential("yogadanny17@gmail.com", "zbuk jacm jfmo duyf");
                    smtpClient.Send(message);
                }

                //return RedirectToAction("FeedbackSent"); // Redirect to a confirmation page
                return new StatusCodeResult(200);
            }
            catch (Exception ex)
            {
                // Handle the exception according to your error handling policy
                // Log the exception and show a user-friendly error message
                return View("Error");
            }
        }
    }
}