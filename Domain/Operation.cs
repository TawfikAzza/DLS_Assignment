namespace Domain {
    public class Operation {
        public int Id { get; set; }
        public double OperandA { get; set; }
        public double OperandB { get; set; }
        public OperationType OperationType { get; set; }
        public Result? Result { get; set; }
    }
}
