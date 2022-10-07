using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StuRaHsHarz.WebShop.Models
{
    public class OrderRequest
    {
        [Required]
        [DataType(DataType.Text)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public Address? ShippingAddress { get; set; } = null;

        [Required] 
        public bool PayCash { get; set; } 

        [Required]
        public IEnumerable<OrderItem> Items { get; set; }
    }
}
