﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using GasStationPOS.Core.Data.Database.Json.JsonFileSchemas;
using GasStationPOS.Core.Data.Models.TransactionModels;
using GasStationPOS.Core.Data.Models.UserModels;
using GasStationPOS.Core.Database.Json;
using GasStationPOS.Core.Services.Transaction_Payment;

namespace GasStationPOS.Core.Services.Receipt
{
    /// <summary>
    /// A class responsible for printing receipts for transactions to the console and saving them to a file.
    /// The receipt includes transaction details such as the transaction number, retail product items, fuel product items,
    /// total amount, payment method, and change.
    /// 
    /// Author: Vincent Fung
    /// Date: 3 April 2025
    /// 
    /// </summary>
    public class ReceiptService: IReceiptService
    {
        private string Name = "Shake-Stack Petrol";
        private Dictionary<string, string> Location = new Dictionary<string, string>
        {
            { "address", "2808 W.Broadway" },
            { "city", "Vancouver" },
            { "province", "British Columbia" },
            { "postal code", "V6K2G7" },
        };
        private string PhoneNumber = "(604)-XXX-XXXX";
        private int ReceiptNumber = 0;
        private string TransactionsPath = JsonFileConstants.TRANSACTIONS_JSON_FILE_PATH;

        ITransactionService transactionService;

        public ReceiptService(ITransactionService transactionService)
        {
            this.transactionService = transactionService;
        }

        /// <summary>
        /// Prints the reciept to console.
        /// </summary>
        /// <returns></returns>
        public bool PrintReceipt(int transactionNumber)
        {
            int validTransactionNumber = transactionService.GetChosenTransactionNumberWithinBounds(transactionNumber);

            string contentToPrint = "";

            string data = File.ReadAllText(TransactionsPath);

            try
            {
                using (JsonDocument temp = JsonDocument.Parse(data))
                {
                    JsonElement transactions = temp.RootElement.GetProperty("Transactions");

                    JsonElement matchingTransactionElement = transactions.EnumerateArray().FirstOrDefault(transactionElement =>
                        Int32.Parse(transactionElement.GetProperty("TransactionNumber").ToString()) == validTransactionNumber);

                    // If transaction was found:

                    // JsonElement ValueKind property is set to Undefined
                    // when the JsonElement does not contain any valid data
                    // (an empty JsonElement is returned when no match is found in the FirstOrDefault query)
                    if (matchingTransactionElement.ValueKind != JsonValueKind.Undefined) // if the json element returned is NOT empty and contains data
                    {
                        contentToPrint += parseDataToTransaction(
                            matchingTransactionElement.GetProperty("TransactionNumber").GetInt32(),
                            matchingTransactionElement.GetProperty("TransactionRetailProductItems"),
                            matchingTransactionElement.GetProperty("TransactionFuelProductItems"),
                            matchingTransactionElement.GetProperty("TotalAmountDollars").GetDouble(),
                            matchingTransactionElement.GetProperty("PaymentMethod").ToString(),
                            matchingTransactionElement.GetProperty("ChangeDollars").GetDouble()
                        );
                    
                        // Print the Reciept to file.
                        outputReceiptToFile(contentToPrint);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a singular transation to a string.
        /// </summary>
        /// <returns></returns>
        private string parseDataToTransaction
        (
            int TransactionNumber,
            JsonElement TransactionRetailProductItems,
            JsonElement TransactionFuelProductItems,
            double TotalAmountDollars,
            string PaymentMethod,
            double ChangeDollars
        )
        {
            StringBuilder sb = new StringBuilder();
            // Title Part
            sb.AppendLine("Gas Prices\nSelf Serve");
            sb.Append("\n");

            // Address and Phone Number Part
            sb.AppendLine($"{Name}");
            sb.AppendLine($"{string.Join("\n", Location.Values)}");
            sb.AppendLine($"{PhoneNumber}");
            sb.Append("\n");

            // GST, Date, etc Part.
            sb.AppendLine($"GST:\t{generateNumberOfType("GST")}\t\t" +
                $"TRANSACTION #:\t{TransactionNumber}");
            sb.AppendLine($"DATE:\t{getFormattedDateAndTime()["date"]}\t\t" +
                $"TIME:\t\t\t{getFormattedDateAndTime()["time"]}");
            sb.AppendLine($"S/S:\t{generateNumberOfType("S/S")}\t\t\t" +
                $"TERMINAL:\t{generateNumberOfType("TERMINAL")}");
            sb.Append("\n");

            // Authorization Number
            sb.AppendLine($"AUTHORIZATION #:\t{generateAuthNumber()}\n");

            // Retail Product Part
            sb.AppendLine("RETAIL PRODUCT");
            sb.AppendLine(parseRetailProductItems(TransactionRetailProductItems));

            // Gasoline
            sb.AppendLine("GASOLINE");
            sb.AppendLine(parseGasolineProductItems(TransactionFuelProductItems));

            sb.AppendLine($"{TransactionRetailProductItems.GetArrayLength()} Items\t\t" +
                $"SUBTOTAL:\t\t\t\t\t" +
                $"${TotalAmountDollars}");
            sb.AppendLine($"\t\t\t\t\t\tTOTAL:\t\t\t${(TotalAmountDollars).ToString("#.##")}");

            if (PaymentMethod == "CARD")
            {
                sb.AppendLine($"VISA CARD XXXXXXXXXXXXXXXX{generateNumberOfType("CARD")}");
                sb.AppendLine($"ENTRY METHOD CONTACTLESS\n");
            }
            else
            {
                sb.AppendLine("CASH\n");
            }

            sb.AppendLine($"\t\t\t\t\t\tTENDERED:\t\t${ChangeDollars + TotalAmountDollars}");

            if (ChangeDollars == 0)
            {
                sb.AppendLine($"\t\t\t\t\t\tCASH CHANGE:\t$0.00\n");
            }
            else
            {
                sb.AppendLine($"\t\t\t\t\t\tCASH CHANGE:\t${ChangeDollars.ToString("#.##")}\n");
            }

            sb.AppendLine("THANK YOU FOR SHOPPING AT SHAKE STACK GAS STATION");

            // Final Return
            return sb.ToString();
        }

        /// <summary>
        /// Parses all the retail products.
        /// </summary>
        /// <param name="TransactionFuelProductItems"></param>
        /// <returns></returns>
        private string parseGasolineProductItems(JsonElement TransactionFuelProductItems)
        {
            StringBuilder sb = new StringBuilder();

            foreach (JsonElement gasProduct in TransactionFuelProductItems.EnumerateArray())
            {
                string QuantityClean = gasProduct.GetProperty("Quantity").GetDouble().ToString("#");
                sb.AppendLine($"{QuantityClean}\t\t" +
                    $"{gasProduct.GetProperty("FuelProduct").GetProperty("ProductName").ToString()}\t\t\t\t" +
                    $"${gasProduct.GetProperty("TotalItemPriceDollars").ToString()}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parses all the retail products.
        /// </summary>
        /// <param name="TransactionRetailProductItems"></param>
        /// <returns></returns>
        private string parseRetailProductItems(JsonElement TransactionRetailProductItems)
        {
            StringBuilder sb = new StringBuilder();

            foreach (JsonElement retailProduct in TransactionRetailProductItems.EnumerateArray())
            {
                sb.AppendLine($"{retailProduct.GetProperty("Quantity").ToString()}\t\t" +
                    $"{retailProduct.GetProperty("RetailProduct").GetProperty("ProductName").ToString()}\t\t\t\t" +
                    $"${retailProduct.GetProperty("TotalItemPriceDollars").ToString()}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Helper function that outputs a string to file.
        /// </summary>
        /// <param name="content"></param>
        private void outputReceiptToFile(string content)
        {
            String pathToWriteTo = $"output{ReceiptNumber}.txt";
            using (StreamWriter sw = new StreamWriter(pathToWriteTo))
            {
                sw.WriteLine(content);
            }
            String finalPath = Path.Combine(AppContext.BaseDirectory, pathToWriteTo);
            Console.WriteLine($"Printed to: {finalPath}");

            // Open the file with default text editor
            System.Diagnostics.Process.Start("explorer", finalPath);

            ReceiptNumber++;
        }

        /// <summary>
        /// Helper function that helps generate the gst number.
        /// For: GST, and Terminal Number
        /// </summary>
        /// <returns></returns>
        private string generateNumberOfType(string typeToGenerate)
        {
            var random = new Random();

            if (typeToGenerate == "GST")
            {
                return random.Next(100_000_000, 999_999_999).ToString();
            }
            if (typeToGenerate == "TERMINAL")
            {
                return "0" + random.Next(10_000_000, 99_999_999).ToString();
            }
            if (typeToGenerate == "S/S")
            {
                return random.Next(1_000_000, 9_999_999).ToString();
            }
            if (typeToGenerate == "CARD")
            {
                return random.Next(1_000, 9_999).ToString();
            }

            // Returns Nothing, if error.
            Console.WriteLine("Error: Please input a proper type.");
            return "";
        }

        /// <summary>
        /// Helper function that gets the current date in
        /// YYYY-MM-DD format.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> getFormattedDateAndTime()
        {
            return new Dictionary<string, string>
            {
                { "date", DateTime.Now.ToString("yyyy-MM-dd")},
                { "time", DateTime.Now.ToString("HH:mm")}
            };
        }

        /// <summary>
        /// Helper function that helps return an auth number.
        /// </summary>
        /// <returns></returns>
        private string generateAuthNumber()
        {
            var random = new Random();
            int mainID = random.Next(1_000_000, 9_999_999);
            int checkDigit = random.Next(0, 9);

            return $"{mainID}-{checkDigit}";
        }
    }
}
