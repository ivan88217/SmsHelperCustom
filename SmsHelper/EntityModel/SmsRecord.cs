namespace SmsHelper.EntityModel
{
    class SmsRecord
    {
        public int Id { get; set; }
        public int SmsId { get; set; }
        public string Date { get; set; }
        public string Content { get; set; }
        public string Result { get; set; }
        public string Sender { get; set; }
        public string ReceiveTime { get; set; }
    }
}