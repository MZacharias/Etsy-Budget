using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using EstyBudget.Entities;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Configuration;
using System.Globalization;
using EstyBudget.Common;

namespace EstyBudget
{
    public partial class FrmEtsyBudget : Form
    {
        private const int LimitCount = 100;
        private string ShopName = ConfigurationManager.AppSettings.Get(Constants.ShopName);

        public FrmEtsyBudget()
        {
            InitializeComponent();
            dtMonth.Value = DateTime.Now;
        }

        //TODO: add error handling
        private void btnGo_Click(object sender, EventArgs e)
        {
            lblProgress.Visible = true;
            var month = dtMonth.Value;

            //get startDate
            var startDateTime = new DateTime(month.Year, month.Month, 1);
            var startDateEpoch = GetEpochFromDateTime(startDateTime);

            //get EndDate
            var lastDayOfMonth = startDateTime.AddMonths(1).AddDays(-1).Day;
            var endDateTime = new DateTime(startDateTime.Year, startDateTime.Month, lastDayOfMonth, 23, 59, 59);
            var endDateEpoch = GetEpochFromDateTime(endDateTime);

            var receiptResults = GetShopReceipts(startDateEpoch, endDateEpoch);
            var paymentInfo = GetPaymentsInfoForReceipts(receiptResults);
            var billingCharges = GetBillingChargesForTransactions(startDateEpoch, endDateEpoch);

            //create entities for all data retrieved
            var salesEntities = GetSalesEntities(receiptResults, paymentInfo, billingCharges);

            //get total shipping fees (etsy referencial integrity problem!)
            var totalShippingFees = GetTotalShippingFees(billingCharges);

            //get any other fees for the month
            var totalOtherFees = GetTotalOtherFees(billingCharges);

            //output excel file with budget info
            var excelFileContents = GetExcelFileForCollection(salesEntities, totalShippingFees, totalOtherFees, month);
            var saveFileDialog = new SaveFileDialog {Filter = @"Excel files|*.xlsx"};
            var result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                File.WriteAllBytes(saveFileDialog.FileName, excelFileContents);
                MessageBox.Show(@"Etsy Data has been output successfully!");
            }
            else
            {
                var defaultFilePath = Environment.CurrentDirectory + @"\Etsy Budget.xlsx";
                File.WriteAllBytes(defaultFilePath, excelFileContents);
                MessageBox.Show($@"Etsy Data has been output by default to: {defaultFilePath}");
            }
            lblProgress.Visible = false;
        }

        private List<ReceiptEntity> GetShopReceipts(double startDateEpoch, double endDateEpoch)
        {
            var url =
                $"https://openapi.etsy.com/v2/shops/{ShopName}/receipts?limit={LimitCount}&min_created={startDateEpoch}&max_created={endDateEpoch}&was_paid=true&was_shipped=true&includes=Transactions";

            var client = GetEtsyHttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = client.SendAsync(request);
            var result = response.Result.Content.ReadAsAsync<ReceiptResultsEntity>().Result;
            if (result.count <= LimitCount) return result.results;

            //more than _limitCount, then paginate
            var receiptResultsEntityList = PaginateEtsyData(url, result, result.count);
            var receiptList = new List<ReceiptEntity>();
            foreach (var receiptResultEntity in receiptResultsEntityList)
            {
                receiptList.AddRange(receiptResultEntity.results);
            }
            return receiptList;
        }

        private List<PaymentEntity> GetPaymentsInfoForReceipts(List<ReceiptEntity> receiptEntities)
        {
            var client = GetEtsyHttpClient();
            var paymentEntities = new List<PaymentEntity>();
            foreach (var receiptEntity in receiptEntities)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://openapi.etsy.com/v2/shops/{ShopName}/receipts/" + receiptEntity.receipt_id + "/payments");
                var response = client.SendAsync(request);
                var result = response.Result.Content.ReadAsAsync<PaymentResultsEntity>().Result;
                paymentEntities.AddRange(result.results);
            }
            return paymentEntities;
        }

        private List<BillChargeEntity> GetBillingChargesForTransactions(double startDateEpoch, double endDateEpoch)
        {
            var url = $"https://openapi.etsy.com/v2/users/__SELF__/charges?min_created={startDateEpoch}&max_created={endDateEpoch}&limit={LimitCount}";

            var client = GetEtsyHttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);        
            var response = client.SendAsync(request);
            var result = response.Result.Content.ReadAsAsync<BillChargeResultsEntity>().Result;
            if (result.count <= LimitCount) return result.results;

            //more than _limitCount, then paginate
            var billChargePaginatedList = PaginateEtsyData(url, result, result.count);
            var billChargeList = new List<BillChargeEntity>();
            foreach (var billCharge in billChargePaginatedList)
            {
                billChargeList.AddRange(billCharge.results);
            }
            return billChargeList;
        }

        private List<T> PaginateEtsyData<T>(string url, T firstResult, int totalRecordCount)
        {
            var totalPageCount = Math.Ceiling((double)totalRecordCount / LimitCount);
            var collection = new List<T> { firstResult };
            for (var i = 2; i <= totalPageCount; i++)
            {
                var client = GetEtsyHttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, url + $"&page={i}");
                var response = client.SendAsync(request);
                var result = response.Result.Content.ReadAsAsync<T>().Result;
                collection.Add(result);
            }
            return collection;
        }

        private List<SaleEntity> GetSalesEntities(List<ReceiptEntity> receipts, List<PaymentEntity> payments, List<BillChargeEntity> billCharges)
        {
            var saleEntities = new List<SaleEntity>();
            //var payPalBearerToken = GetPayPalBearerToken();

            foreach (var receipt in receipts)
            {
                //set date
                var saleEntity = new SaleEntity
                {
                    Date = GetDateTimeFromEpoch(receipt.creation_tsz)
                };

                //get each transaction within each receipt
                var itemCount = 1;
                float? totalEtsyFees = 0;
                float? totalPayPalFees = 0;
                foreach (var transaction in receipt.Transactions)
                {
                    //add all transactions descriptions
                    saleEntity.Description += $"Item {itemCount}: {transaction.title}" + Environment.NewLine;
                    itemCount++;

                    //set esty fees - get all billingCharges of type "transaction" by transactionId
                    var etsyFees = billCharges.Where(x =>
                        x.type == "transaction" && x.type_id == transaction.transaction_id);
                    foreach (var etsyFee in etsyFees)
                    {
                        totalEtsyFees += etsyFee.amount;
                    }

                    //TODO: get any PalPal fee's if necessary
                    //if (receipt.payment_method == "pp")
                    //{
                    //    try
                    //    {
                    //        GetPayPalFee(payPalBearerToken);
                    //    }
                    //    catch
                    //    {
                    //        //try to get new token if it expires
                    //        payPalBearerToken = GetPayPalBearerToken();
                    //    }
                    //    //pass listing_id on transaction to match to PayPal Invoice Number aka. Number
                    //    GetPayPalFee(payPalBearerToken);
                    //}
                }
                saleEntity.EstyFees = totalEtsyFees;
                saleEntity.PayPalFees = totalPayPalFees;

                var paymentsForRecipts = payments.Where(x => x.receipt_id == receipt.receipt_id);
                float? totalPayments = 0;
                float? totalFees = 0;
                float? totalAdjustedPayments = 0;
                float? totalAdjustedFees = 0;
                foreach (var paymentsForRecipt in paymentsForRecipts)
                {
                    totalPayments += paymentsForRecipt.amount_gross;
                    totalFees += paymentsForRecipt.amount_fees;
                    totalAdjustedPayments += paymentsForRecipt.adjusted_gross;
                    totalAdjustedFees += paymentsForRecipt.adjusted_fees;
                }
                //if adjustments contain values then use them
                if (totalAdjustedPayments.HasValue)
                {
                    saleEntity.SellingPriceAndShipping = totalAdjustedPayments / 100;
                    saleEntity.CreditCardFees = totalAdjustedFees / 100;
                    saleEntity.IsAdjustedSellingPriceAndShipping = true;
                }
                else
                {
                    saleEntity.SellingPriceAndShipping = totalPayments / 100;
                    saleEntity.CreditCardFees = totalFees / 100;
                }

                //use adjusted total from receipt if PayPal
                if (receipt.payment_method == "pp")
                {
                    saleEntity.SellingPriceAndShipping = receipt.adjusted_grandtotal;
                }

                //add to collection
                saleEntities.Add(saleEntity);
            }
            saleEntities = saleEntities.OrderBy(x => x.Date).ToList();
            return saleEntities;
        }

        private float? GetTotalShippingFees(List<BillChargeEntity> billCharges)
        {
            //shipping fee for entire month, etsy does not supply a referenced type ID for shipping label costs
            float? totalShippingFee = 0;
            var shippingFees = billCharges.Where(x => x.type == "shipping_labels");
            foreach (var shippingFee in shippingFees)
            {
                totalShippingFee += shippingFee.amount;
            }

            return totalShippingFee;
        }

        private float? GetTotalOtherFees(List<BillChargeEntity> billCharges)
        {
            //get any other fees for the month
            float? totalOtherFees = 0;
            var otherFees = billCharges.Where(x => x.type != "shipping_labels" && x.type != "transaction");
            foreach (var otherFee in otherFees)
            {
                totalOtherFees += otherFee.amount;
            }

            return totalOtherFees;
        }

        private double GetEpochFromDateTime(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        private DateTime GetDateTimeFromEpoch(double epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epoch).ToLocalTime();
        }

        private HttpClient GetEtsyHttpClient()
        {
            //make api call
            var handler = new OAuth.OAuthMessageHandler(
                ConfigurationManager.AppSettings.Get(Constants.ConsumerKeyName),
                ConfigurationManager.AppSettings.Get(Constants.ConsumerSecret),
                ConfigurationManager.AppSettings.Get(Constants.Token),
                ConfigurationManager.AppSettings.Get(Constants.TokenSecret));
            return new HttpClient(handler);
        }

        //private string GetPayPalBearerToken()
        //{
        //    //get authClient
        //    var authClient = new RestClient("https://api.paypal.com/v1/oauth2/token");
        //    var request = new RestRequest() { Method = Method.POST };

        //    request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        //    request.AddHeader("Accept", "application/json");
        //    request.AddParameter("grant_type", "client_credentials");
        //    authClient.Authenticator = new HttpBasicAuthenticator("");
        //    var response = authClient.Execute<PayPalAuthEntity>(request);
            
        //    if(response.StatusCode != HttpStatusCode.OK) throw new Exception("Could not get PayPal authentication token.");
        //    return response.Data.access_token;
        //}

        //private string GetPayPalFee(string bearerToken)
        //{
        //    //get authClient
        //    var client = new RestClient("https://api.paypal.com/v1/checkout/orders");
        //    var request = new RestRequest() { Method = Method.GET };

        //    request.AddHeader("Content-Type", "application/application/json");
        //    request.AddHeader("Accept", "application/json");
        //    request.AddHeader("Authorization", "Bearer " + bearerToken);
        //    var response = client.Execute(request);

        //    if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Could not get PayPal fees.");
        //    return null;
        //}

        private byte[] GetExcelFileForCollection(List<SaleEntity> collection, float? totalShippingFees, float? totalOtherFees, DateTime month)
        {
            byte[] result;
            using (ExcelPackage pck = new ExcelPackage())
            {
                //Create the worksheet
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add(month.ToString("MMMM", CultureInfo.InvariantCulture));

                //load totals on first row
                //Format the headers
                using (ExcelRange rng = ws.Cells["A1:L1"])
                {
                    rng.Style.Font.Size = 16;
                    rng.Style.Font.Bold = true;
                    rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                }
                var maxRow = "10000";
                ws.Cells["D1"].Value = "Totals";
                ws.Cells["D1"].Style.Font.UnderLine = true;
                ws.Cells["E1"].Formula = $"=SUBTOTAL(9, E3:E{maxRow})";
                ws.Cells["F1"].Formula = $"=SUBTOTAL(9, F3:F{maxRow})";
                ws.Cells["G1"].Value = totalShippingFees;
                ws.Cells["H1"].Formula = $"=SUBTOTAL(9, H3:H{maxRow})";
                ws.Cells["I1"].Formula = $"=SUBTOTAL(9, I3:I{maxRow})";
                ws.Cells["J1"].Formula = $"=SUBTOTAL(9, J3:J{maxRow})";
                ws.Cells["K1"].Value = totalOtherFees;
                ws.Cells["L1"].Formula = "=E1-F1-G1-H1-I1-J1-K1";

                //Load the datatable into the sheet. Print the column names
                ws.Cells["A2"].LoadFromCollection(collection, true);

                using (ExcelRange rng = ws.Cells["A2:L2"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Font.UnderLine = true;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                //default all cell styles
                using (ExcelRange rng = ws.Cells["A1:L10000"])
                {
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rng.Style.Fill.BackgroundColor.SetColor(Color.Pink);
                    rng.Style.Font.Color.SetColor(Color.MediumVioletRed);
                    rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    rng.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    rng.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                //column settings
                ws.Column(1).Width = 40;
                ws.Column(3).Style.Numberformat.Format = "MM-dd-yyyy";
                ws.Column(5).Style.Numberformat.Format = "$#,##0.00;$-#,##0.00";
                ws.Column(6).Style.Numberformat.Format = "$#,##0.00;$-#,##0.00";
                ws.Column(7).Style.Numberformat.Format = "$#,##0.00;$-#,##0.00";
                ws.Column(8).Style.Numberformat.Format = "$#,##0.00;$-#,##0.00";
                ws.Column(9).Style.Numberformat.Format = "$#,##0.00;$-#,##0.00";
                ws.Column(10).Style.Numberformat.Format = "$#,##0.00;$-#,##0.00";
                ws.Column(11).Style.Numberformat.Format = "$#,##0.00;$-#,##0.00";
                ws.Column(12).Style.Numberformat.Format = "$#,##0.00;$-#,##0.00";
                ws.Column(12).AutoFit();

                //do clean-up of unwanted columns
                ws.Cells["M2"].Value = String.Empty;
                using (ExcelRange rng = ws.Cells["M3:M10000"])
                {
                    foreach (var cell in rng)
                    {
                        if (cell.Value?.ToString().ToLower() == "true")
                        {
                            var rowNumber = cell.Start.Row;
                            using (ExcelRange row = ws.Cells[$"A{rowNumber}:L{rowNumber}"])
                            {
                                row.Style.Fill.BackgroundColor.SetColor(Color.Plum);
                            }
                        }
                        cell.Value = string.Empty;
                    }
                }

                result = pck.GetAsByteArray();
            }
            return result;
        }

    }
}
