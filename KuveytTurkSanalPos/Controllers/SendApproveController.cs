using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace KuveytTurkSanalPos.Controllers
{
    public class SendApproveController : Controller
    {
        public IActionResult Index()
        {

            string merchantOrderId = TempData["MerchantOrderId"] as string;
            string amount = TempData["Amount"] as string;
            string md = TempData["MD"] as string;
            string customerId = ""; //Müsteri Numarasi
            string merchantId = ""; //Magaza Kodu
            string userName = ""; //  api rollü kullanici adı
            string password = "";//  api rollü kullanici sifresi

            using (SHA1 sha = new SHA1CryptoServiceProvider())
            {
                string hashedPassword = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
                string hashstr = merchantId + merchantOrderId + amount.ToString() + userName + hashedPassword;
                // byte[] hashbytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(hashstr);
                byte[] hashbytes = Encoding.Default.GetBytes(hashstr);
                byte[] inputbytes = sha.ComputeHash(hashbytes);
                string hashData = Convert.ToBase64String(inputbytes);

                string server = "https://boa.kuveytturk.com.tr/sanalposservice/Home/ThreeDModelProvisionGate";

                string postData = $@"
                <KuveytTurkVPosMessage xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
                    <APIVersion>1.0.0</APIVersion>                  
                    <HashData>{hashData}</HashData>
                    <MerchantId>{merchantId}</MerchantId>
                    <CustomerId>{customerId}</CustomerId>
                    <UserName>{userName}</UserName>
                    <CurrencyCode>0949</CurrencyCode>
                    <TransactionType>Sale</TransactionType>
                    <InstallmentCount>0</InstallmentCount>                   
                    <Amount>{amount}</Amount>
                    <MerchantOrderId>{merchantOrderId}</MerchantOrderId>                   
                    <TransactionSecurity>3</TransactionSecurity>
                    <KuveytTurkVPosAdditionalData>
                    <AdditionalData>
                    <Key>MD</Key>
                    <Data>{md}</Data>
                    </AdditionalData>
                    </KuveytTurkVPosAdditionalData>
                </KuveytTurkVPosMessage>";

                byte[] buffer = Encoding.UTF8.GetBytes(postData);

                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(server);
                webReq.Timeout = 5 * 60 * 1000;
                webReq.Method = "POST";
                webReq.ContentType = "application/xml";
                webReq.ContentLength = buffer.Length;

                webReq.CookieContainer = new CookieContainer();

                using (Stream reqStream = webReq.GetRequestStream())
                {
                    reqStream.Write(buffer, 0, buffer.Length);
                }

                using (WebResponse webRes = webReq.GetResponse())
                using (Stream resStream = webRes.GetResponseStream())
                using (StreamReader resReader = new StreamReader(resStream))
                {
                    string responseString = resReader.ReadToEnd();

                    VPosTransactionResponseContract result;
                    using (TextReader reader = new StringReader(responseString))
                    {
                        result = (VPosTransactionResponseContract)new XmlSerializer(typeof(VPosTransactionResponseContract)).Deserialize(reader);
                    }


                    if (result.ResponseCode == "00")
                    {
                        return RedirectToAction("Success", "Payment");
                    }
                    else
                    {
                        return RedirectToAction("Unsuccessful", "Payment");
                    }
                }
            }
        }





        [Serializable]
        public class VPosMessageContract
        {
            public string OkUrl { get; set; }
            public string FailUrl { get; set; }
            public string HashData { get; set; }
            public string TerminalId { get; set; }
            public int MerchantId { get; set; }
            public int SubMerchantId { get; set; }
            public int CustomerId { get; set; }
            public string UserName { get; set; }
            public string HashPassword { get; set; }
            public string CustomerIPAddress { get; set; }
            public string MerchantOrderId { get; set; }
            public int InstallmentCount { get; set; }
            public int Amount { get; set; }
            public string DisplayAmount { get; set; }
            public string FECCurrencyCode { get; set; }
            public string CurrencyCode { get; set; }
            public VPosAdditionalDataSet AdditionalData { get; set; }
            public List<VPosAddressContract> Addresses { get; set; }
            public string APIVersion { get; set; }
            public string CardNumber { get; set; }
            public string CardExpireDateYear { get; set; }
            public string CardExpireDateMonth { get; set; }
            public string CardCVV2 { get; set; }
            public string CardHolderName { get; set; }
            //public int PaymentType { get; set; }
            public int QueryId { get; set; }
            public int DebtId { get; set; }
            public decimal SurchargeAmount { get; set; }
            public decimal SGKDebtAmount { get; set; }
            public Byte InstallmentMaturityCommisionFlag { get; set; }
            public int TransactionSecurity { get; set; }
        }
        /// Bu sınıf siparişler için ekstra bilgilerin listesini tutar.
        [Serializable]
        public class VPosAdditionalDataSet
        {
            public List<VPosAdditionalData> AdditionalDataList { get; set; }
        }
        /// Bu sınıf siparişler için ekstra bilgileri tutar.
        [Serializable]
        public class VPosAdditionalData
        {
            public string Key { get; set; }
            public string Data { get; set; }
            public string Description { get; set; }
        }


        /// Bu sınıf siparişler için adres bilgilerini tutar.
        [Serializable]
        public class VPosAddressContract
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string LastName { get; set; }
            public string Company { get; set; }
            public string Text { get; set; }
            public string District { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
            public string PostalCode { get; set; }
            public string PhoneNumber { get; set; }
            public string GSMNumber { get; set; }
            public string FaxNumber { get; set; }
            public string Email { get; set; }
        }

        /// Bu sınıf sanal pos işlem bilgilerinin ve sonuçlarının tutulduğu değişkenleri içeren sınıftır. 
        [Serializable]
        public class VPosTransactionResponseContract
        {
            public VPosMessageContract VPosMessage { get; set; } // eğer 3d secure ise geriye bu response dönülür.
            public bool IsEnrolled { get; set; }
            public bool IsVirtual { get; set; }
            public string PareqHtmlFormString { get; set; }
            // eğer işlem 3dsecure değilse.
            public string ProvisionNumber { get; set; }
            public string RRN { get; set; }
            public string Stan { get; set; }
            public string ResponseCode { get; set; }
            public bool IsSuccess { get; set; }
            public string ResponseMessage { get; set; }
            public int OrderId { get; set; }
            public DateTime TransactionTime { get; set; }
            public string MerchantOrderId { get; set; }
            public string HashData { get; set; }
            public string MD { get; set; }
            public string ReferenceId { get; set; }
        }
    }
}
