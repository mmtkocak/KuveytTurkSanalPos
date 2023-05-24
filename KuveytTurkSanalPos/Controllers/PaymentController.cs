using KuveytTurkSanalPos.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace KuveytTurkSanalPos.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MakePayments(KuveytCardNumber p)
        {
            Random rnd = new Random();
            int sayi1 = rnd.Next(0, 99999);

            decimal amount = p.TotalAmount * 100m;          

            string cardHolderName = p.CardName;
            string cardNumber = p.CardNumber;
            string cardExpireDateMonth = p.CardExpireDateMonth;
            string cardExpireDateYear = p.CardExpireDateYear;
            string year = cardExpireDateYear.Substring(2);
            string cardCVV2 = p.CardCVV;

            string merchantOrderId = $"{sayi1}";
            string customerId = ""; //Müsteri Numarasi
            string merchantId = ""; //Magaza Kodu
            string userName = ""; //  api rollü kullanici adı
            string password = "";//  api rollü kullanici sifresi
            string okUrl = "https://websitesi.com/Payment/Approval";
            string failUrl = "https://websitesi.com/Payment/Fail";
            string description = p.Message;

            using (SHA1Managed sha1 = new SHA1Managed())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashedPasswordBytes = sha1.ComputeHash(passwordBytes);
                string hashedPassword = Convert.ToBase64String(hashedPasswordBytes);

                string hashData = $"{merchantId}{merchantOrderId}{amount}{okUrl}{failUrl}{userName}{hashedPassword}";
                //  byte[] hashBytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashData);
                byte[] hashBytes = Encoding.Default.GetBytes(hashData);
                byte[] hash = sha1.ComputeHash(hashBytes);
                string hashedData = Convert.ToBase64String(hash);

                string postData = $@"
                <KuveytTurkVPosMessage xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                    <APIVersion>1.0.0</APIVersion>
                    <OkUrl>{okUrl}</OkUrl>
                    <FailUrl>{failUrl}</FailUrl>
                    <HashData>{hashedData}</HashData>
                    <MerchantId>{merchantId}</MerchantId>
                    <CustomerId>{customerId}</CustomerId>
                    <UserName>{userName}</UserName>
                    <CardNumber>{cardNumber}</CardNumber>
                    <CardExpireDateYear>{year}</CardExpireDateYear>
                    <CardExpireDateMonth>{cardExpireDateMonth}</CardExpireDateMonth>
                    <CardCVV2>{cardCVV2}</CardCVV2>
                    <CardHolderName>{cardHolderName}</CardHolderName>
                    <CardType>Troy</CardType>
                    <BatchID>0</BatchID>
                    <TransactionType>Sale</TransactionType>
                    <InstallmentCount>0</InstallmentCount>
                    <Amount>{amount}</Amount>
                    <DisplayAmount>{amount}</DisplayAmount>
                    <CurrencyCode>0949</CurrencyCode>
                    <MerchantOrderId>{merchantOrderId}</MerchantOrderId>
                    <Description>{description}</Description>
                    <TransactionSecurity>3</TransactionSecurity>
                </KuveytTurkVPosMessage>";

                byte[] postDataBytes = Encoding.UTF8.GetBytes(postData);

                string serverUrl = "https://boa.kuveytturk.com.tr/sanalposservice/Home/ThreeDModelPayGate";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serverUrl);
                request.Timeout = 5 * 60 * 1000;
                request.Method = "POST";
                request.ContentType = "application/xml";
                request.ContentLength = postDataBytes.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(postDataBytes, 0, postDataBytes.Length);
                }

                using (WebResponse response = await request.GetResponseAsync())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader responseReader = new StreamReader(responseStream))
                {
                    string responseString = await responseReader.ReadToEndAsync();                  
                    return Content(responseString, "text/html");
                }
            }
        }


      
        public IActionResult Fail()
        {
            // Banka tarafından verilen hata içeriğini alın
            string authenticationResponse = Request.Form["AuthenticationResponse"];
            string decodedResponse = System.Net.WebUtility.UrlDecode(authenticationResponse);

            // XML'i deserialize ederek model nesnesine dönüştürün
            XmlSerializer serializer = new XmlSerializer(typeof(VPosTransactionResponseContract));
            VPosTransactionResponseContract model;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(decodedResponse)))
            {
                model = (VPosTransactionResponseContract)serializer.Deserialize(ms);
            }

            return RedirectToAction("Unsuccessful", "Payment");
        }

        public IActionResult Approval(string authenticationResponse)
        {
            string decodedResponse = System.Web.HttpUtility.UrlDecode(authenticationResponse);
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(VPosTransactionResponseContract));
            var model = new VPosTransactionResponseContract();

            using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(decodedResponse)))
            {
                model = serializer.Deserialize(ms) as VPosTransactionResponseContract;
            }

            TempData["MerchantOrderId"] = model.MerchantOrderId;
            TempData["MD"] = model.MD;
            TempData["Amount"] = model.VPosMessage.Amount.ToString();


            return RedirectToAction("Index", "SendApprove");

        }


        public IActionResult Success()
        {
            return View();
        }

        public IActionResult Unsuccessful()
        {
            return View();
        }

        public class VPosTransactionResponseContract
        {
            public string ACSURL { get; set; }
            public string AuthenticationPacket { get; set; }
            public string HashData { get; set; }
            public bool IsEnrolled { get; set; }
            public bool IsSuccess { get; }
            public bool IsVirtual { get; set; }
            public string MD { get; set; }
            public string MerchantOrderId { get; set; }
            public int OrderId { get; set; }
            public string PareqHtmlFormString { get; set; }
            public string Password { get; set; }
            public string ProvisionNumber { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public string RRN { get; set; }
            public string SafeKey { get; set; }
            public string Stan { get; set; }
            public DateTime TransactionTime { get; set; }
            public string TransactionType { get; set; }
            public KuveytTurkVPosMessage VPosMessage { get; set; }
        }

        public class KuveytTurkVPosMessage
        {
            public decimal Amount { get; set; }
        }


    }
}
