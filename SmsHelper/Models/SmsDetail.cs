namespace SmsHelper.Models
{
    class SmsDetail
    {
        public int Id { get; set; }
        public int SmsId { get; set; }
        public string Result { get; set; }
        public string ReceiveTime { get; set; }
        public string Content { get; set; }
        public string Date { get; set; }
        public string Sender { get; set; }
        public bool IsSended { get; set; }
    }
}