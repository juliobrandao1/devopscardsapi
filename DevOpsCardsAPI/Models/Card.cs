namespace AzureBoardsAPI.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string AssignedTo { get; set; }
        public int? StoryPoints { get; set; }
        public string IterationPath { get; set; }
        public string State { get; set; }
        public string AreaPath { get; set; }
        public bool IsSelected { get; set; }
    }
}
