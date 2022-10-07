namespace StuRaHsHarz.WebShop.Models
{
    public enum OrderState
    {
        created = 0,

        paymentReceived = 1,

        shipping = 2,

        shipped = 3,

        delivered = 4,

        cancelled = 5,

        pending = 6,
    }
}