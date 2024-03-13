namespace Domain {
    public class Operation {
        public int Id { get; set; }
        public double OperandA { get; set; }
        public double OperandB { get; set; }
        public string OperationType { get; set; }
        public double Result { get; set; }
        
        public Dictionary<string,string> Headers { get; set; }
    }
}
