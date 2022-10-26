
namespace taskium.server 
{
    class Task
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? TaskLabel { get; set; }
        public string? Description { get; set; }
        public string? Repo { get; set; }
        public string? Branch { get; set; }
        public string? StdErr { get; set; }
        public string? StdOut { get; set; }
        public bool IsStarted { get; set; }
        public bool IsComplete { get; set; }
        public int ReturnCode { get; set; }
    }
}
