namespace SimpleRabbit
{
    public class RabbitInfo
    {
        public string exchange { get; set; } = "";
        public string queue { get; set; } = "";
        public string exchange_type { get; set; } = "fanout";
        public string routing_key { get; set; } = "";
    }
}
