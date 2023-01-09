using Microsoft.Data.Sqlite;
using Nancy;
using System;
using System.Data;
using System.Globalization;

namespace LiftPassPricing
{
    public class Prices : NancyModule
    {
        public SqliteConnection Connection { get; } = new SqliteConnection(@"Data Source=..\..\..\..\lift_pass.db");

        public Prices()
        {
            Connection.Open();

            Put("/prices", _ =>
            {
                int liftPassCost = int.Parse(this.Request.Query["cost"]);
                string liftPassType = (string)this.Request.Query["type"] ?? "";

                using (var command = Connection.CreateCommand())
                {
                    command.CommandText =  //
                       "INSERT INTO base_price (type, cost) VALUES (@type, @cost) " + //
                       "ON DUPLICATE KEY UPDATE cost = @cost;";
                    command.Parameters.Add(new("@type", SqliteType.Text) { Value = liftPassType });
                    command.Parameters.Add(new("@cost", SqliteType.Integer) { Value = liftPassCost });
                    command.Prepare();
                    command.ExecuteNonQuery();
                }

                return "";
            });

            base.Get("/prices", _ =>
            {
                int? age = this.Request.Query["age"] != null ? int.Parse(this.Request.Query["age"]) : null;

                if (age != null && age < 6)
                {
                    return GetResultString(0);
                }

                string typeParam = (string)this.Request.Query["type"] ?? "";
                double baseTypeCost = GetBaseCostForType(typeParam);

                int reduction;

                DateTime d = DateTime.ParseExact(this.Request.Query["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);

                reduction = 0;

                if (typeParam is not "night")
                {
                    bool isHoliday = CheckIfHoliday(d);

                    if (!isHoliday && d.DayOfWeek == DayOfWeek.Monday)
                    {
                        reduction = 35;
                    }

                    // TODO apply reduction for others
                    if (age != null && age < 15)
                    {
                        return GetResultString((int)Math.Ceiling(baseTypeCost * .7));
                    }

                    if (age is null || age <= 64)
                    {
                        double cost1 = baseTypeCost * (1 - reduction / 100.0); // todo: combine the costs
                        return GetResultString((int)Math.Ceiling(cost1));
                    }
                    double cost = baseTypeCost * .75 * (1 - reduction / 100.0);
                    return GetResultString((int)Math.Ceiling(cost));

                }
                if (age != null && age >= 6)
                {
                    if (age > 64)
                    {
                        return GetResultString((int)Math.Ceiling(baseTypeCost * .4));
                    }
                    return GetResultString((int)baseTypeCost);
                }
                return GetResultString(0);
            });

            After += ctx =>
            {
                ctx.Response.ContentType = "application/json";
            };

        }

        private bool CheckIfHoliday(DateTime date)
        {
            SqliteCommand holidayCmd = Connection.CreateCommand();
            holidayCmd.CommandText = "SELECT * FROM holidays";
            holidayCmd.Prepare();
            SqliteDataReader holidays = holidayCmd.ExecuteReader();
            while (holidays.Read())
            {
                var holiday = holidays.GetDateTime("holiday");
                if (this.Request.Query["date"] != null)
                {
                    if (date.Year == holiday.Year &&
                        date.Month == holiday.Month &&
                        date.Date == holiday.Date)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string GetResultString(int cost)
        {
            return "{ \"cost\": " + cost + "}";
        }

        private double GetBaseCostForType(string typeParam)
        {
            const string priceQuery = "SELECT cost FROM base_price WHERE type = @type";

            using (var costCmd = Connection.CreateCommand())
            {
                costCmd.CommandText = priceQuery;
                costCmd.Parameters.Add(new("@type", SqliteType.Text) { Value = typeParam });
                costCmd.Prepare();
                return (long)costCmd.ExecuteScalar();
            }
        }
    }
}
