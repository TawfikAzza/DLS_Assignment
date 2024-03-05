namespace Domain {
    public class Operation {
        public required Guid Id { get; set; }
        public required double OperandA { get; set; }
        public required double OperandB { get; set; }
        public required double Result { get; set; }
        public required OperationType OperationType { get; set; }
    }
}
