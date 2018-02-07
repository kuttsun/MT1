using System.ComponentModel.DataAnnotations.Schema;

namespace MT1.Kindle.Database
{
    public class ItemDetail
    {
        public int Id { get; set; }
        public string Title { get; set; } = null;
        public string Content { get; set; } = null;
        public string PublicationDate { get; set; } = null;
        public string Asin { get; set; } = null;
        public string DetailPageUrl { get; set; } = null;
        public string MediumImageUrl { get; set; } = null;
        public string LargeImageUrl { get; set; } = null;

        
        public string NodeId { get; set; } = null;
        [ForeignKey(nameof(NodeId))]
        public SaleInformation SaleInformation { get; set; }
    }
}