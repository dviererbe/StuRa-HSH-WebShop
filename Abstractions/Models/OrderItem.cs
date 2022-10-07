using System.ComponentModel.DataAnnotations;
using System.Text;

namespace StuRaHsHarz.WebShop.Models
{
    public class OrderItem
    {
        [Required]
        public ItemType Type { get; set; }

        [Required]
        [Range(minimum: 1, maximum: 20)]
        public uint Amount { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder
                .Append("- ")
                .Append(Amount)
                .Append(" x ")
                .Append(Type.Color)
                .Append(", ")
                .Append(Type.Size);

            return stringBuilder.ToString();
        }
    }
}