﻿using System.Net;
using System.Security.Cryptography.X509Certificates;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Data.SqlClient;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace MiniProjektDatabaseToCSV;

internal class Program
{
    static void Main(string[] args)
    {
        DateTime userDate;
        DateTime currencyExchangeDate;
        var loop = true;
        while (loop)
        {
            //input a prevod zadaneho stringu na DateTime
            Console.WriteLine("Zadejte datum pro specificky kurzovni listek. Prosim zadejte datum ve formatu DD.MM.YYYY");
            string answerDate = Console.ReadLine();

            TryParseUserDate(answerDate, out userDate, out currencyExchangeDate);

            if (TryParseUserDate(answerDate, out userDate, out currencyExchangeDate) == true)
            {
                var currentUSDtoCZK = getExchangeRate(userDate);

                //vsechny prihlasovaci udaje
                using (SqlConnection connection =
                   new SqlConnection(
                       "Server=##############;Database=##############;User Id=##############;Password=##############;"))
                {
                    //navazani spojeni
                    connection.Open();

                    //zadani QUERY
                    using (SqlCommand command =
                           new SqlCommand("SELECT EnglishProductName, DealerPrice FROM DimProduct", connection))

                           //precteni a zapsani vystupu z daneho QUERY do textoveho souboru
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        try
                        {
                            using (StreamWriter writer = File.CreateText($"{currencyExchangeDate:dd.MM.yyyy}_adventureworks.csv"))
                            {
                                writer.WriteLine("Date;EnglishProductName;DealerPriceUSD;DealerPriceCZK");

                                while (reader.Read())
                                {
                                    string productName = reader.GetString(0);
                                    decimal priceUSD = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                                    decimal priceCZK = priceUSD * (decimal)currentUSDtoCZK;
                                    writer.WriteLine($"{currencyExchangeDate:dd.MM.yyyy};{productName};{priceUSD};{priceCZK}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Při psaní do souboru nastala chyba: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
   static bool TryParseUserDate(string date, out DateTime userDate, out DateTime currencyExchangeDate)
    {
        var check = true;
        userDate = DateTime.Parse(date);

            //zjisteni jestli je datum validni
            if (userDate > DateTime.Today)
            {
                userDate = DateTime.Today;
            }

            //zjisteni jestli to je pracovni den nebo vikend
            var dateChecker = Convert.ToInt32(userDate.DayOfWeek);
            if (dateChecker > 5)
            {
                if (dateChecker == 6)
                {
                    userDate = userDate.AddDays(-1);
                }
                else
                {
                    userDate = userDate.AddDays(-2);
                }
            }

            currencyExchangeDate = userDate;
            return check;
    }
    static double getExchangeRate(DateTime exchangeDate)
    {
        //stahnuti kurzovniho listku
        WebClient client = new WebClient();
        var data = client.DownloadString("https://www.cnb.cz/cs/financni-trhy/devizovy-trh/kurzy-devizoveho-trhu/kurzy-devizoveho-trhu/rok.txt?rok=2024");
        var lines = data.Split('\n');

        //zjisteni indexu konverze USD na CZK
        var firstLine = lines[0].Split('|');

        //Array.IndexOf firstLine, "1 USD"
        double currentUSDtoCZK = 0;

        //cyklus na nalezeni spravneho radku s aktualnima kurzama
        foreach (var line in lines)
        {
            var currentLine = line.Split('|');
            if (exchangeDate.ToString("dd.MM.yyyy") == currentLine[0])
            {
                //ziskani a zapsani kurzu USD na CZK
                currentUSDtoCZK = Convert.ToDouble(currentLine[Array.IndexOf(firstLine, "1 USD")]);
            }
        }
        return currentUSDtoCZK;
    }
    }
