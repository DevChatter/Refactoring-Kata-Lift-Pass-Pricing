using System;
using Xunit;
using Nancy;
using Nancy.Testing;
using LiftPassPricing;
using System.Collections.Generic;

namespace LiftPassPricingTests
{
    /// <seealso>"http://www.marcusoft.net/2013/01/NancyTesting1.html"</seealso>
    public class PriceApiShould : IDisposable
    {
        private readonly Prices prices;
        private readonly Browser browser;

        public PriceApiShould()
        {
            this.prices = new Prices();
            this.browser = new Browser(with => with.Module(prices));
        }

        public void Dispose()
        {
            prices.Connection.Close();
        }

        [Theory]
        [InlineData("night", 19)]
        [InlineData("1jour", 35)]
        public void ReturnBasePrice_ForAdult(string type, int expected)
        {
            var result = browser.Get("/prices", with =>
            {
                with.Query("type", type);
                with.Query("age", "23");
                with.Query("date", "2022-09-23");
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.Result.StatusCode);
            Assert.Equal("application/json", result.Result.ContentType);

            Response json = result.Result.Body.DeserializeJson<Response>();
            Assert.Equal(expected, json.Cost);
        }

        [Fact]
        public void ReturnFullPrice_ForAdultOnMondayHoliday()
        {
            var result = browser.Get("/prices", with =>
            {
                with.Query("type", "1jour");
                with.Query("age", "23");
                with.Query("date", "2019-02-18");
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.Result.StatusCode);
            Assert.Equal("application/json", result.Result.ContentType);

            Response json = result.Result.Body.DeserializeJson<Response>();
            Assert.Equal(35, json.Cost);
        }

        [Fact]
        public void ReturnDiscountPrice_ForAdultOnMonday()
        {
            var result = browser.Get("/prices", with =>
            {
                with.Query("type", "1jour");
                with.Query("age", "23");
                with.Query("date", "2022-09-19");
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.Result.StatusCode);
            Assert.Equal("application/json", result.Result.ContentType);

            Response json = result.Result.Body.DeserializeJson<Response>();
            Assert.Equal(23, json.Cost);
        }

        [Fact]
        public void ReturnZeroCost_ForUnder6()
        {
            var result = browser.Get("/prices", with =>
            {
                with.Query("type", "1jour");
                with.Query("age", "5");
                with.Query("date", "2022-09-19");
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.Result.StatusCode);
            Assert.Equal("application/json", result.Result.ContentType);

            Response json = result.Result.Body.DeserializeJson<Response>();
            Assert.Equal(0, json.Cost);
        }

        [Fact]
        public void ReturnZeroCost_ForNightWithNoAge()
        {
            var result = browser.Get("/prices", with =>
            {
                with.Query("type", "night");
                with.Query("date", "2022-09-19");
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.Result.StatusCode);
            Assert.Equal("application/json", result.Result.ContentType);

            Response json = result.Result.Body.DeserializeJson<Response>();
            Assert.Equal(0, json.Cost);
        }

    }

    class Response
    {
        public int Cost { get; set; }
    }
}
