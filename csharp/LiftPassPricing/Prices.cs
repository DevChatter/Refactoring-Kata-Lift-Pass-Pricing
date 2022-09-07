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

            Get("/prices", _ =>
            {
                int? age = this.Request.Query["age"] != null ? Int32.Parse(this.Request.Query["age"]) : null;

                using (var costCmd = Connection.CreateCommand())
                {
                    string typeParam = (string)this.Request.Query["type"] ?? "" ;
                    costCmd.CommandText = //
                        "SELECT cost FROM base_price " + //
                        "WHERE type = @type";
                    costCmd.Parameters.Add(new("@type", SqliteType.Text) { Value = typeParam });
                    costCmd.Prepare();
                    double result = (long)costCmd.ExecuteScalar();

                    int reduction;
                    var isHoliday = false;

                    if (age != null && age < 6)
                    {
                        return "{ \"cost\": 0}";
                    }
                    else
                    {
                        reduction = 0;

                        if (!"night".Equals(this.Request.Query["type"]))
                        {
                            using (var holidayCmd = Connection.CreateCommand())
                            {
                                holidayCmd.CommandText = "SELECT * FROM holidays";
                                holidayCmd.Prepare();
                                using (var holidays = holidayCmd.ExecuteReader())
                                {

                                    while (holidays.Read())
                                    {
                                        var holiday = holidays.GetDateTime("holiday");
                                        if (this.Request.Query["date"] != null)
                                        {
                                            DateTime d = DateTime.ParseExact(this.Request.Query["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                            if (d.Year == holiday.Year &&
                                                d.Month == holiday.Month &&
                                                d.Date == holiday.Date)
                                            {
                                                isHoliday = true;
                                            }
                                        }
                                    }

                                }
                            }

                            if (this.Request.Query["date"] != null)
                            {
                                DateTime d = DateTime.ParseExact(this.Request.Query["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                if (!isHoliday && (int)d.DayOfWeek == 1)
                                {
                                    reduction = 35;
                                }
                            }

                            // TODO apply reduction for others
                            if (age != null && age < 15)
                            {
                                return "{ \"cost\": " + (int)Math.Ceiling(result * .7) + "}";
                            }
                            else
                            {
                                if (age == null)
                                {
                                    double cost = result * (1 - reduction / 100.0);
                                    return "{ \"cost\": " + (int)Math.Ceiling(cost) + "}";
                                }
                                else
                                {
                                    if (age > 64)
                                    {
                                        double cost = result * .75 * (1 - reduction / 100.0);
                                        return "{ \"cost\": " + (int)Math.Ceiling(cost) + "}";
                                    }
                                    else
                                    {
                                        double cost = result * (1 - reduction / 100.0);
                                        return "{ \"cost\": " + (int)Math.Ceiling(cost) + "}";
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (age != null && age >= 6)
                            {
                                if (age > 64)
                                {
                                    return "{ \"cost\": " + (int)Math.Ceiling(result * .4) + "}";
                                }
                                else
                                {
                                    return "{ \"cost\": " + result + "}";
                                }
                            }
                            else
                            {
                                return "{ \"cost\": 0}";
                            }
                        }
                    }
                }
            });

            After += ctx =>
            {
                ctx.Response.ContentType = "application/json";
            };

        }

    }
}
