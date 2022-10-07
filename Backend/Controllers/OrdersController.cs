using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;
using Backend.Services;
using Microsoft.Extensions.Logging;
using StuRaHsHarz.WebShop.Exceptions;
using StuRaHsHarz.WebShop.Models;
using StuRaHsHarz.WebShop.Services;

namespace Backend.Controllers
{
    [Route("orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest orderRequest)
        {
            if (!ModelState.IsValid) return BadRequest();

            try
            {
                Order order = await _orderService.OrderAsync(orderRequest);

                return CreatedAtAction(
                    actionName: nameof(GetOrderDetails), 
                    routeValues: new { orderId = order.Id }, 
                    value: order);
            }
            catch (FormatException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (NotItemsEnoughInStock)
            {
                return NoContent();
            }
            catch (Exception unexpectedException)
            {
                _logger.LogError(unexpectedException, "Unexpected Exception occurred while placing order.");
                return InternalServerError();
            }
        }

        [HttpGet("{orderId:Guid}")]
        public async Task<IActionResult> GetOrderDetails([FromRoute] Guid orderId)
        {
            try
            {
                return Ok(await _orderService.LoadOrderAsync(orderId));
            }
            catch (OrderNotFound)
            {
                return NotFound();
            }
            catch (Exception unexpectedException)
            {
                _logger.LogError(unexpectedException, "Unexpected Exception occurred while loading order details.");
                return InternalServerError();
            }
        }

        private IActionResult InternalServerError()
        {
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
