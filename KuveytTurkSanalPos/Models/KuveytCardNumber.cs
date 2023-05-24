namespace KuveytTurkSanalPos.Models
{
    public class KuveytCardNumber
    {
        public string CardNumber { get; set; }
        public int TotalAmount { get; set; }

        public string CardName { get; set; }
        public string CardExpireDateMonth { get; set; }
        public string CardExpireDateYear { get; set; }
        public string CardCVV { get; set; }


        public string Message { get; set; }

    }
}
