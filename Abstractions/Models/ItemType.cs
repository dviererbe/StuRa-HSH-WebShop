using System;

namespace StuRaHsHarz.WebShop.Models
{
    public class ItemType : IEquatable<ItemType>
    {
        public ItemColor Color { get; set; }

        public ItemSize Size { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) Color, (int) Size);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is ItemType other && Equals(other);
        }

        public bool Equals(ItemType? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Color == other.Color && Size == other.Size;
        }
    }
}
